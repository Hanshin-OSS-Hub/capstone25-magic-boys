$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# 환경 변수는 이 프로세스에만 적용하며 PATH와 Windows Python 등록은 변경하지 않습니다.
$env:UV_PYTHON_INSTALL_DIR = Join-Path $PSScriptRoot '.python'
$env:UV_CACHE_DIR = Join-Path $PSScriptRoot '.cache/uv'
$env:HF_HOME = Join-Path $PSScriptRoot '.cache/huggingface'
$env:HF_HUB_OFFLINE = '0'
$env:PYTHONUTF8 = '1'
$env:PYTHONIOENCODING = 'utf-8'
$uv = Join-Path $PSScriptRoot '.tools/uv.exe'
$python = Join-Path $PSScriptRoot '.venv/Scripts/python.exe'

function Invoke-Uv {
    & $uv @args
    if ($LASTEXITCODE -ne 0) { throw "환경 준비 명령 실패: uv $args" }
}

try {
    if (-not [Environment]::Is64BitOperatingSystem -or $env:PROCESSOR_ARCHITECTURE -ne 'AMD64') {
        throw '이 준비 스크립트는 Windows x64용입니다. 64비트 PowerShell에서 실행하세요.'
    }
    if (-not (Test-Path -LiteralPath $uv)) {
        Write-Host '준비 도구 uv 0.13.6 다운로드 중 (약 15MB)...'
        $toolDir = Join-Path $PSScriptRoot '.tools'
        New-Item -ItemType Directory -Force -Path $toolDir | Out-Null
        $archive = Join-Path $toolDir 'uv.zip'
        Invoke-WebRequest -UseBasicParsing -Uri 'https://github.com/astral-sh/uv/releases/download/0.13.6/uv-x86_64-pc-windows-msvc.zip' -OutFile $archive
        $expected = 'A2A8820D5EEA32F08EF467EF3B7F66883A9B076D5737A2FC2FBB5F458D1BB50F'
        if ((Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash -ne $expected) {
            throw 'uv 다운로드 체크섬이 일치하지 않습니다. 압축을 풀지 않았습니다.'
        }
        Expand-Archive -LiteralPath $archive -DestinationPath $toolDir -Force
    }
    $version = & $uv --version
    if ($LASTEXITCODE -ne 0 -or $version -notlike 'uv 0.13.6 *') {
        throw '준비 도구 버전이 예상과 다릅니다. .tools 폴더를 확인하세요.'
    }
    Write-Host '프로젝트 전용 Python 3.10.19 준비 중 (최초 약 23MB)...'
    Invoke-Uv python install 3.10.19 --no-bin --no-registry --no-config
    if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot '.venv'))) {
        Invoke-Uv venv (Join-Path $PSScriptRoot '.venv') --python 3.10.19 --managed-python --no-config
    }
    if (-not (Test-Path -LiteralPath $python)) {
        throw '.venv가 불완전합니다. 기존 폴더를 다른 이름으로 보존한 뒤 준비를 다시 실행하세요.'
    }
    $pythonVersion = & $python --version
    if ($LASTEXITCODE -ne 0 -or $pythonVersion -ne 'Python 3.10.19') {
        throw '가상환경 버전 또는 경로가 다릅니다. .venv 폴더를 다른 이름으로 보존한 뒤 다시 실행하세요.'
    }
    Write-Host '기존 환경 명세의 라이브러리 설치 및 의존성 검사 중...'
    Invoke-Uv pip install --python $python -r (Join-Path $PSScriptRoot 'requirements.txt') --no-config
    if (Get-Command nvidia-smi -ErrorAction SilentlyContinue) {
        Write-Host 'NVIDIA GPU 감지: GPU 음성 인식 라이브러리 설치 중 (최초 약 1GB)...'
        Invoke-Uv pip install --python $python -r (Join-Path $PSScriptRoot 'requirements-gpu.txt') --no-config
    }
    Invoke-Uv pip check --python $python --no-config
    & $python (Join-Path $PSScriptRoot 'prepare_model.py')
    if ($LASTEXITCODE -ne 0) { throw '모델 준비에 실패했습니다. 위 오류를 확인하고 다시 실행하세요.' }
    Write-Host '준비 완료. 평소에는 start.cmd만 실행하세요. 이 창에서는 서버를 시작하지 않습니다.'
} catch {
    Write-Host "준비 실패: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}
