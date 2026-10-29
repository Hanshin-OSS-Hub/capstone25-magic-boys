"""기존 small 모델을 프로젝트 내부에 고정 버전으로 준비합니다."""
import os
from pathlib import Path

ROOT = Path(__file__).resolve().parent
os.environ.setdefault("HF_HOME", str(ROOT / ".cache" / "huggingface"))

from faster_whisper import WhisperModel
from huggingface_hub import snapshot_download

REPOSITORY = "Systran/faster-whisper-small"
REVISION = "536b0662742c02347bc0e980a01041f333bce120"
MODEL_DIR = ROOT / "models" / "small"
FILES = ["config.json", "model.bin", "tokenizer.json", "vocabulary.txt"]

if __name__ == "__main__":
    print(f"모델 준비: {REPOSITORY} / {REVISION}", flush=True)
    print("최초 다운로드 약 486MB. 이미 받은 파일은 재사용합니다.", flush=True)
    snapshot_download(
        repo_id=REPOSITORY,
        revision=REVISION,
        local_dir=MODEL_DIR,
        allow_patterns=FILES,
    )
    print("CPU / INT8 모델 로딩 검사 중...", flush=True)
    model = WhisperModel(str(MODEL_DIR), device="cpu", compute_type="int8", local_files_only=True)
    print(f"모델 준비 완료: {MODEL_DIR}", flush=True)
