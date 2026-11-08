import glob
import io
import os
import site
import time
from pathlib import Path

# NVIDIA GPU용 cuBLAS/cuDNN (requirements-gpu.txt)이 설치돼 있으면 DLL 경로 등록
for _sp in site.getsitepackages():
    for _bin in glob.glob(os.path.join(_sp, "nvidia", "*", "bin")):
        try:
            os.add_dll_directory(_bin)
        except (FileNotFoundError, OSError):
            pass
        os.environ["PATH"] = _bin + os.pathsep + os.environ.get("PATH", "")

import numpy as np
from faster_whisper import WhisperModel
from flask import Flask, request, jsonify

app = Flask(__name__)

model_dir = Path(__file__).resolve().parent / "models" / "small"
if not (model_dir / "model.bin").is_file():
    raise FileNotFoundError("모델이 없습니다. WhisperServer/setup.ps1을 먼저 실행하세요.")

# 스킬 명령어를 핫워드로 주면 짧은 단어 인식률이 크게 올라감 (initial_prompt는 목록이 결과에 섞여 나와서 사용 안 함)
COMMAND_HOTWORDS = "fireball fire water ice earth wall rock thunder lightning storm meteor anvil ultimate"


def load_model():
    # WHISPER_DEVICE=cpu 로 강제 가능. 기본은 GPU 시도 후 실패하면 CPU.
    forced = os.environ.get("WHISPER_DEVICE", "").lower()
    if forced != "cpu":
        try:
            m = WhisperModel(str(model_dir), device="cuda", compute_type="float16", local_files_only=True)
            list(m.transcribe(np.zeros(16000, dtype=np.float32), language="en")[0])
            return m, "GPU / FP16"
        except Exception as e:
            print(f"GPU 사용 불가 → CPU로 실행합니다 ({str(e).splitlines()[0][:80]})", flush=True)
    m = WhisperModel(str(model_dir), device="cpu", compute_type="int8", local_files_only=True)
    return m, "CPU / INT8"


print("Whisper small 모델 로딩 중...", flush=True)
model, device_name = load_model()
# 첫 요청이 느리지 않도록 미리 한 번 실행
list(model.transcribe(np.zeros(16000, dtype=np.float32), language="en")[0])
print(f"Whisper 모델 준비 완료 ({device_name}).", flush=True)


@app.post("/transcribe")
def transcribe():
    if "audio" not in request.files:
        return jsonify({"error": "No audio file received"}), 400

    started = time.perf_counter()
    data = request.files["audio"].read()

    segments, info = model.transcribe(
        io.BytesIO(data),
        language="en",
        beam_size=1,                      # 짧은 명령어는 빔 서치 불필요
        best_of=1,
        temperature=0.0,                  # 재시도(온도 올려 재디코딩) 없이 한 번에
        vad_filter=True,                  # 앞뒤 무음 제거
        vad_parameters=dict(min_silence_duration_ms=300),
        without_timestamps=True,
        condition_on_previous_text=False,
        hotwords=COMMAND_HOTWORDS,
    )
    text = "".join(s.text for s in segments)
    elapsed = (time.perf_counter() - started) * 1000
    print(f"[STT] {elapsed:.0f}ms ({info.duration:.1f}s 음성) → '{text.strip()}'", flush=True)

    return jsonify({
        "text": text,
        "language": info.language,
        "duration": info.duration
    })


if __name__ == "__main__":
    print("🚀 Whisper STT 서버 실행 중...")
    print("📌 POST → http://localhost:5000/transcribe")
    app.run(host="0.0.0.0", port=5000)
