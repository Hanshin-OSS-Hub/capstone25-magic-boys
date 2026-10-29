@echo off
setlocal
chcp 65001 >nul
cd /d "%~dp0"
set "PYTHONUTF8=1"
set "PYTHONIOENCODING=utf-8"
set "HF_HOME=%~dp0.cache\huggingface"
set "HF_HUB_OFFLINE=1"
if not exist ".venv\Scripts\python.exe" goto missing
if not exist "models\small\model.bin" goto missing
echo 음성 서버를 시작합니다. 정상 대기 후 Unity를 실행하세요. 종료: Ctrl+C
".venv\Scripts\python.exe" -u whisper_server.py
set "server_exit=%ERRORLEVEL%"
if not "%server_exit%"=="0" pause
exit /b %server_exit%
:missing
echo 준비된 환경 또는 모델이 없습니다. README.md의 최초 준비 절차를 실행하세요.
pause
exit /b 1
