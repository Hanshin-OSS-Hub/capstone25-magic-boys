# 조원용 음성인식 실행 메모

## 처음 받는 PC에서 한 번

1. `20260504_boss_tile` 브랜치를 받고 Unity Hub에서 **Unity 6000.4.0f1**로 연다.
2. 저장소를 앞으로 사용할 경로에 둔다. 저장소 루트에서 PowerShell 실행:

   ```powershell
   powershell -NoProfile -ExecutionPolicy Bypass -File .\WhisperServer\setup.ps1
   ```

3. 인터넷 연결을 유지하고 `준비 완료`까지 기다린다. 전용 Python 3.10.19와 라이브러리, Whisper small 모델(약 486MB)을 내려받는다. 라이브러리 등 추가 다운로드와 압축 해제 공간이 필요하다. 전역 Python, CUDA, API 키는 필요 없다.

`.venv`, `.python`, `models`, `.tools`, `.runtime`은 Git에 포함하지 않는다. 다른 PC에 가상환경을 복사하지 말고 위 준비 과정을 실행한다. 경로를 옮긴 가상환경은 그대로 사용하지 않는다.

## 매번 플레이할 때

1. `WhisperServer/start.cmd` 더블클릭.
2. `Whisper 모델 준비 완료 (CPU / INT8)`와 포트 **5000** 대기 메시지를 확인하고 창을 열어 둔다.
3. Unity에서 `Assets/Map/Scenes/0326_MapMerge.unity`를 열고 Play. 또는 `Assets/Skill/Scenes/MainMenu.unity`에서 게임 시작.
4. Game 화면을 클릭하고 **숫자 1**로 불 마법을 먼저 확인한다. 마나와 재사용 대기시간을 확보한다.
5. **Q를 누른 채 `Fireball`이라고 말하고 Q를 놓는다.** 실제 게임 씬의 최대 녹음 시간은 **3초**다(코드 기본값 5초와 다름). Windows 기본 마이크와 데스크톱 앱 마이크 권한을 확인한다.
6. Console의 녹음 → Sending to server → Result/VoiceFlow 명령 전달과 실제 불 마법 발동을 확인한다. 인식 문자열만 나온 것은 발동 성공과 구분한다.
7. 종료할 때 Unity Play를 끄고 서버 창에서 **Ctrl+C**, 필요하면 Y. 재부팅 후에는 start.cmd를 다시 실행한다.

Unity와 서버를 **같은 PC**에서 실행한다. Unity 요청 주소는 `http://localhost:5000/transcribe`다. 발표 PC가 다르면 그 PC에서도 최초 준비와 실제 마이크 검증을 한다.

## 문제가 생기면

- 서버가 곧바로 닫힘/환경 없음: PowerShell에서 `start.cmd`를 실행해 오류를 읽고 `setup.ps1` 준비 여부 확인.
- 모델 없음: 인터넷 연결 상태에서 `setup.ps1` 재실행. 평소 start.cmd는 오프라인 모델을 사용한다.
- 포트 5000 사용 중: 이미 열린 서버 창이 있는지 확인. 서버를 두 개 실행하지 않는다.
- 녹음 로그 없음: Game 포커스, Q 입력, Windows 마이크 권한·기본 입력 장치 확인.
- 녹음은 되지만 요청 오류: 서버 창과 포트 확인. 브라우저 루트 주소의 404는 정상일 수 있다(`/health` 없음).
- Result가 비어 있거나 `Fiber`: 서버 중단으로 단정하지 말고 마이크 음량·발음을 확인해 다시 말한다.
- Result가 `Fireball`인데 발동 안 됨: 숫자 1, 마나·쿨다운, VoiceFlow의 명령 대상 로그를 함께 확인한다.

마이크 없이 HTTP/파일 인식만 따로 검사:

```powershell
.\WhisperServer\.venv\Scripts\python.exe -X utf8 .\WhisperServer\verify_http.py
```

사용자가 이전 버전에서 재부팅 후 서버 실행, 실제 마이크 Fireball 발동, 퍼즐 왕복 후 음성·스킬·동일 맵 유지를 확인했다. 약 **2.07초** 응답은 당시 **단일 측정값**이다. 이번 외형 통합 후 실제 마이크·발표 PC 성능은 다시 수동 확인한다.

상세 설정·다운로드·오류 처리는 [README](README.md), 기존 자동/사용자 검증 구분은 [VERIFICATION](VERIFICATION.md) 참고.
