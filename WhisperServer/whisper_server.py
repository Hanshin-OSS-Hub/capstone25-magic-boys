from faster_whisper import WhisperModel
from flask import Flask, request, jsonify
import tempfile

app = Flask(__name__)

print("🔁 Whisper 모델 로딩 중 (GPU)...")
model = WhisperModel("small", device="cpu", compute_type="int8")
print("🔥 GPU Whisper 모델 준비 완료!")

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
