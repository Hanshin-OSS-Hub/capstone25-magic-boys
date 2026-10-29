"""Unity와 같은 multipart audio/wav 요청으로 실행 중인 서버를 검사합니다.

마이크 녹음이나 Unity 마법 발동은 검사하지 않습니다.
"""
import argparse
import io
import json
import time
import uuid
import wave
from datetime import datetime, timedelta, timezone
from pathlib import Path
from urllib.error import HTTPError, URLError
from urllib.request import Request, urlopen

from faster_whisper.audio import decode_audio

ROOT = Path(__file__).resolve().parent


def post(url, audio=None):
    boundary = "MagicBoys" + uuid.uuid4().hex
    body = b""
    if audio is not None:
        body = (
            f'--{boundary}\r\nContent-Disposition: form-data; name="audio"; '
            'filename="audio.wav"\r\nContent-Type: audio/wav\r\n\r\n'
        ).encode("ascii") + audio + b"\r\n"
    body += f"--{boundary}--\r\n".encode("ascii")
    request = Request(url, data=body, headers={"Content-Type": f"multipart/form-data; boundary={boundary}"})
    start = time.perf_counter()
    try:
        response = urlopen(request, timeout=120)
    except HTTPError as error:
        response = error
    with response:
        return response.status, json.loads(response.read()), round(time.perf_counter() - start, 3)


def main():
    parser = argparse.ArgumentParser(description="실행 중인 음성 서버의 HTTP·파일 음성 인식 검사")
    parser.add_argument("--url", default="http://localhost:5000/transcribe")
    parser.add_argument("--audio", type=Path, default=ROOT.parent / "Assets/Sound/SFX/fireball_voice.mp3")
    parser.add_argument("--expected", default="fire", help="인식 결과에 포함되어야 하는 문자열")
    args = parser.parse_args()
    if not args.audio.is_file():
        raise FileNotFoundError(f"음성 파일을 찾을 수 없습니다: {args.audio}")

    status, error_body, _ = post(args.url)
    if status != 400 or error_body.get("error") != "No audio file received":
        raise RuntimeError(f"audio 누락 요청의 예상 응답과 다릅니다: HTTP {status}, {error_body}")
    print("HTTP 연결 및 audio 필드 누락 처리: 통과 (400)", flush=True)

    samples = decode_audio(str(args.audio), sampling_rate=16000)
    pcm = (samples.clip(-1, 1) * 32767).astype("<i2").tobytes()
    buffer = io.BytesIO()
    with wave.open(buffer, "wb") as wav:
        wav.setnchannels(1)
        wav.setsampwidth(2)
        wav.setframerate(16000)
        wav.writeframes(pcm)
    status, body, seconds = post(args.url, buffer.getvalue())
    text = body.get("text", "")
    passed = (
        status == 200
        and isinstance(text, str)
        and args.expected.lower() in text.lower()
        and body.get("language") == "en"
        and isinstance(body.get("duration"), (int, float))
        and body["duration"] > 0
    )
    report = {
        "확인시각_KST": datetime.now(timezone(timedelta(hours=9))).isoformat(),
        "요청주소": args.url,
        "음성파일": str(args.audio.resolve()),
        "전송형식": "multipart audio=audio.wav, PCM16, mono, 16000Hz",
        "HTTP_상태": status,
        "응답": body,
        "요청부터_응답까지_초": seconds,
        "예상_문자열": args.expected,
        "파일_인식_통과": passed,
        "실제_마이크": "미검증",
        "Unity_마법_발동": "미검증",
    }
    runtime = ROOT / ".runtime"
    runtime.mkdir(exist_ok=True)
    (runtime / "last-verification.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(report, ensure_ascii=False, indent=2), flush=True)
    if not passed:
        raise RuntimeError("파일 음성 인식 검사 실패. 응답과 예상 문자열을 확인하세요.")


if __name__ == "__main__":
    try:
        main()
    except (OSError, URLError, ValueError, RuntimeError) as error:
        print(f"검증 실패: {error}", flush=True)
        raise SystemExit(1)
