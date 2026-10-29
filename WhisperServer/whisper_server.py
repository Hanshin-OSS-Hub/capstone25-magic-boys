from faster_whisper import WhisperModel
from flask import Flask, request, jsonify
import tempfile
from pathlib import Path

app = Flask(__name__)

model_dir = Path(__file__).resolve().parent / "models" / "small"
if not (model_dir / "model.bin").is_file():
    raise FileNotFoundError("모델이 없습니다. WhisperServer/setup.ps1을 먼저 실행하세요.")
print("Whisper small 모델 로딩 중 (CPU / INT8)...", flush=True)
model = WhisperModel(str(model_dir), device="cpu", compute_type="int8", local_files_only=True)
print("Whisper 모델 준비 완료 (CPU / INT8).", flush=True)

@app.post("/transcribe")
def transcribe():
    if "audio" not in request.files:
        return jsonify({"error": "No audio file received"}), 400

    audio_file = request.files["audio"]

    with tempfile.NamedTemporaryFile(delete=False, suffix=".wav") as temp:
        audio_file.save(temp.name)
        segments, info = model.transcribe(temp.name, language="en")
        text = "".join([s.text for s in segments])

    return jsonify({
        "text": text,
        "language": info.language,
        "duration": info.duration
    })

if __name__ == "__main__":
    print("🚀 Whisper STT 서버 실행 중...")
    print("📌 POST → http://localhost:5000/transcribe")
    app.run(host="0.0.0.0", port=5000)
