# 매직 보이즈 음성 서버 실행 안내

## 현재 확인된 상태

- 기준 브랜치: `20260504_boss_tile` (사용자 확인상 최종 제출본)
- 기준 커밋: `089b63f236c469ecbdfe1b2900dc5d058e4b48e6`
- 작업 브랜치: `work/whisper-local-setup`
- 확인일: 2026-10-29, 한국 시간
- 현재 PC에서 환경 준비, CPU 모델 로딩, 서버 시작·종료·재시작, HTTP 요청 및 파일 음성 인식 성공.
- 이후 사용자 확인으로 재부팅 후 `start.cmd` 실행, 숫자 1 불 마법, Q 실제 마이크 `Fireball` 인식과 마법 발동까지 성공했습니다. 자동 검증과 사용자 확인은 검증 기록에서 구분합니다.
- 사용자 스크린샷의 녹음 종료→응답은 약 2.07초(2.0665초), 단일 관측값입니다. 이후 포털 수정 후 스킬·음성 주문·동일 맵 유지도 사용자 확인을 받았습니다. 발표 PC 동작은 아직 미검증입니다.
- 상세 명령과 측정 결과: [검증 기록](VERIFICATION.md)

## 평소 실행: 이미 준비된 현재 PC

1. `WhisperServer/start.cmd`를 더블클릭합니다. 또는 저장소 루트 PowerShell에서 실행합니다.

   ```powershell
   .\WhisperServer\start.cmd
   ```

2. 아래 메시지가 나올 때까지 기다리고 창을 열어 둡니다.

   ```text
   Whisper 모델 준비 완료 (CPU / INT8).
   Running on http://127.0.0.1:5000
   ```

3. Unity를 실행합니다. 서버 창을 닫으면 음성인식이 중단됩니다.
4. 종료할 때 서버 창에서 `Ctrl+C`를 누릅니다. 배치 작업 종료 확인이 나오면 `Y`로 종료합니다. 오류 때문에 `계속하려면...`이 나오면 로그를 확인한 뒤 창을 닫습니다.

평소 실행은 환경을 활성화하거나 패키지를 설치하지 않습니다. 프로젝트 안의 Python과 모델을 사용하며 Hugging Face 오프라인 모드로 실행합니다. 서버를 중복 실행하지 마세요. 자동 실행 서비스는 설치하지 않았으므로 PC 재부팅 후에는 다시 실행해야 합니다.

## 최초 준비: 새 PC 또는 환경을 다시 만들 때

Windows x64, 64비트 PowerShell, 인터넷 연결이 필요합니다. 전역 Python·Conda 설치는 필요하지 않습니다. 저장소를 최종 사용할 경로에 먼저 둔 다음 아래 명령을 실행하세요.

```powershell
cd C:\Unity\capstone25-magic-boys
powershell -NoProfile -ExecutionPolicy Bypass -File .\WhisperServer\setup.ps1
```

첫 줄 경로는 해당 PC의 저장소 위치로 바꿉니다. `ExecutionPolicy Bypass`는 이번에 실행하는 PowerShell 프로세스에만 적용합니다. 시스템 실행 정책을 변경하지 않습니다.

준비 순서:

1. 공식 uv 0.13.6 Windows x64 배포 파일 다운로드 및 SHA-256 검사.
2. uv가 제공하는 독립 CPython 3.10.19를 `.python`에 설치. Windows 레지스트리 등록과 PATH 추가는 하지 않음.
3. `.venv` 생성 후 `requirements.txt`의 고정 버전 45개 설치 및 의존성 검사.
4. 기존 `small` 모델을 `models/small`에 다운로드하고 CPU·INT8 로딩 검사.
5. `준비 완료` 메시지 확인 후 평소에는 `start.cmd` 사용.

준비 스크립트 재실행은 기존 다운로드와 환경을 재사용합니다. 이번 PC에서도 재실행을 확인했습니다. 기존 `.venv`가 불완전하거나 다른 경로에서 복사된 경우 자동으로 삭제하지 않고 오류를 냅니다. 해당 폴더를 다른 이름으로 보존한 뒤 다시 준비하세요.

다운로드 대상:

| 대상 | 버전·크기 |
|---|---|
| uv | 0.13.6, 압축 파일 15,239,500바이트 |
| CPython | 3.10.19, 압축 파일 22,569,949바이트 |
| Python 라이브러리 | 45개, 최초 다운로드 약 100~200MB 예상. 플랫폼과 배포 파일에 따라 달라짐 |
| Whisper 모델 | 4개 파일 합계 486,212,372바이트, 약 486MB |

압축 해제된 환경과 캐시를 위한 추가 디스크 공간도 필요합니다. 모델 다운로드 진행률은 파일 개수 기준이어서 큰 `model.bin` 하나를 받는 동안 숫자가 오래 멈춘 것처럼 보일 수 있습니다.

## 코드와 설정의 근거

| 항목 | 현재 설정·근거 |
|---|---|
| 진입점 | `whisper_server.py`의 `__main__` |
| Python | 기존 `environment.yml`에 `python=3.10.19` 명시 |
| 라이브러리 | 원본 명세의 pip 버전을 `requirements.txt`로 옮김. Conda 항목의 pip 25.3, setuptools 80.9.0, wheel 0.45.1도 유지 |
| 핵심 버전 | faster-whisper 1.2.1 / Flask 3.1.2 / CTranslate2 4.6.1 / PyAV 16.0.1 / NumPy 2.2.6 |
| 모델 | `Systran/faster-whisper-small`, 원래 코드와 같은 다국어 `small` (`small.en` 아님) |
| 모델 리비전 | `536b0662742c02347bc0e980a01041f333bce120` |
| 모델 위치 | `WhisperServer/models/small`, 서버 파일 위치를 기준으로 계산 |
| 추론 | `device="cpu"`, `compute_type="int8"`, 영어 지정 `language="en"` |
| 서버 바인딩 | 기존 설정 유지: `0.0.0.0:5000` |
| Unity 주소 | `http://localhost:5000/transcribe`로 고정 |
| 인증 | 이 로컬 음성 경로에는 API 키나 토큰 불필요 |

원본 `environment.yml`은 수정하지 않았습니다. 이번 환경은 Conda 전체 복제품이 아니라 동일 Python 버전과 라이브러리 버전을 사용하는 프로젝트 전용 환경입니다.

현재 CPU에서 CTranslate2가 `int8`을 지원하고 실제 모델 로딩·추론이 성공했습니다. GPU가 있어도 현재 코드는 GPU를 사용하지 않습니다. CUDA·cuDNN·GPU 추론은 이번에 설치하거나 검증하지 않았습니다. PyAV가 오디오를 디코딩하므로 별도 FFmpeg 실행 파일 설치도 필요하지 않았습니다.

서버는 기존처럼 모든 네트워크 인터페이스에 바인딩하며 인증을 제공하지 않습니다. 이번 실행은 Unity와 서버를 같은 PC에서 사용하는 구성입니다. 외부 공개용 배포 설정을 추가하지 않았습니다.

## Unity의 요청·응답과 직접 검증

실제 요청 코드:

- `Assets/Whisper/Samples/OpenAI Unity/0.2.2/Whisper/WhisperPure.cs`
- WAV 생성: 같은 폴더의 `SaveWav.cs`
- 명령 해석: `Assets/Player/Scripts/VoiceCommandSystem.cs`
- 마법 발동: `Assets/Skill/Skill/MagicAttack.cs`

요청은 `POST /transcribe`, `multipart/form-data`, 필드명 `audio`, 파일명 `audio.wav`, 파일 MIME `audio/wav`입니다. Unity 녹음 주파수는 16kHz입니다.

정상 응답 예시:

```json
{"text":" Fireball","language":"en","duration":1.8}
```

`duration`은 오디오 길이이며 서버 처리 시간이 아닙니다. `audio` 필드가 없으면 HTTP 400과 `{"error":"No audio file received"}`가 반환됩니다. `/health`와 `/` 확인 페이지는 없습니다. 브라우저에서 루트 주소에 접근해 404가 나오는 것만으로 서버 장애라고 판단하지 마세요.

서버가 실행 중일 때 다른 PowerShell 창에서 파일 음성 인식을 검사할 수 있습니다.

```powershell
.\WhisperServer\.venv\Scripts\python.exe -X utf8 .\WhisperServer\verify_http.py
```

이 검사는 저장소의 `Assets/Sound/SFX/fireball_voice.mp3`를 16kHz·모노·PCM16 WAV로 변환하고 Unity와 같은 필드·MIME으로 요청합니다. 입력 필드 누락의 400 응답과 음성 파일의 200 응답, 영어 결과에 `fire` 포함 여부를 검사합니다. 실제 마이크와 Unity는 사용하지 않습니다.

마지막 결과는 `.runtime/last-verification.json`에 저장합니다. 특정 음성 파일도 검사할 수 있습니다.

```powershell
.\WhisperServer\.venv\Scripts\python.exe -X utf8 .\WhisperServer\verify_http.py --audio "C:\음성파일\주문.wav" --expected fire
```

## Unity에서 사용자가 확인할 순서

1. 서버가 정상 대기 중인지 확인합니다.
2. Unity **6000.4.0f1**에서 프로젝트를 엽니다.
3. 씬 **`Assets/Map/Scenes/0326_MapMerge.unity`**를 엽니다.
4. Console을 열고 Play를 누릅니다. 맵 생성이 끝날 때까지 기다립니다.
5. Game 창을 클릭하고, 일시정지·메뉴를 닫은 상태에서 **Q를 누른 채 영어로 “Fireball”**이라고 말한 뒤 Q를 놓습니다. 최대 녹음 시간은 **3초**입니다.
6. 인식 응답까지 기다립니다. 현재 파일 검사에서는 약 4.3~4.6초가 걸렸으므로 바로 다음 주문을 연속 입력하지 마세요.
7. 다음을 따로 확인합니다.

| 단계 | 성공 기준 |
|---|---|
| 녹음 시작 | Console에 `[Whisper] Recording...` |
| 요청 시작 | `[Whisper] Sending to server...` |
| 마이크 음성 인식 | `[Whisper] Result = ...`에 `fire` 또는 `fireball` 포함 |
| 실제 마법 발동 | 불 투사체 생성과 스킬 효과 확인. 별도 마우스 클릭은 필요 없음 |

씬 저장값은 코드 기본값과 다릅니다. 이 씬의 녹음은 Q·3초이고, 직접 스킬 입력은 숫자 1~5입니다. 먼저 숫자 1로 불 마법이 나오는지 비교하면 음성 문제와 스킬 상태 문제를 구분할 수 있습니다. 잠금·마나 부족·쿨타임·프리팹 문제는 `[MagicAttack] Cast Failed:` 로그를 확인하세요. 숫자 5/Y 슬롯은 현재 의도적으로 실패하는 테스트 기능입니다.

현재 코드는 첫 번째 마이크를 자동 선택하며 씬의 `voiceCommandText`도 미연결입니다. 인식 확인은 Console을 기준으로 하세요. 퍼즐 씬 왕복 후 참조 유지와 일시정지 중 늦은 응답 처리는 이번 검증 범위 밖입니다.

## 대표 오류 대처

| 증상 | 확인·대응 |
|---|---|
| 환경 또는 모델이 없다는 안내 | `setup.ps1` 실행. 준비 완료 전 서버를 실행하지 않기 |
| 다운로드 중 네트워크 오류 | 인터넷·방화벽·프록시 상태 확인 후 준비 명령 재실행. 캐시는 재사용됨 |
| `No module named ...` | 전역 Python 대신 `start.cmd` 사용. 필요하면 준비 스크립트 재실행 |
| `DLL load failed` | Windows x64 여부와 전체 오류 로그 확인. CTranslate2/ONNX Runtime 및 해당 PC의 Visual C++ 런타임 상태를 확인해야 하며, 무작정 CUDA를 설치하지 않기 |
| `pkg_resources is deprecated` | 현재 CTranslate2 4.6.1의 경고. 이 경고가 있어도 본 PC에서 로딩·인식 성공. 고정한 setuptools 80.9.0을 임의로 최신화하지 않기 |
| Flask 개발 서버 경고 | 기존 `app.run()`이 내는 안내. 본 로컬 검증에서는 정상 대기·응답 확인 |
| 5000 포트 사용 중 | 기존 서버 창을 확인하고 중복 실행 종료. 관련 없는 프로세스를 강제 종료하지 않기 |
| Unity 연결 실패 | 서버 창의 오류 확인, 위 파일 검사 실행. Unity와 서버가 같은 PC인지 확인 |
| 마이크 없음·인식 안 됨 | Windows 마이크 권한·입력 장치·실제 입력 레벨 확인. 첫 번째 장치를 자동 선택하는 현재 코드의 제약 고려 |
| 결과는 나오는데 마법이 안 나옴 | 해금·MP·쿨타임 및 `Cast Failed` 로그 확인. 숫자 1 입력과 비교 |

현재 서버는 요청마다 OS 임시 폴더에 WAV를 남기는 기존 동작을 유지합니다(`delete=False`). 개인정보가 포함된 음성 사용 및 장기 운영 전에는 파일 수명 관리가 별도 개선 대상입니다. 이번에는 엔진·명령 판정·Unity·마법 코드를 변경하지 않았습니다.

## 다른 PC로 옮길 때

- 가장 간단한 방법: 변경한 소스 파일이 포함된 저장소를 옮긴 뒤 `setup.ps1` 실행.
- 서버 준비에 필요한 파일: `whisper_server.py`, `setup.ps1`, `requirements.txt`, `prepare_model.py`, `start.cmd`.
- 안내·검증에 필요한 파일: `README.md`, `VERIFICATION.md`, `verify_http.py`, 검사할 음성 파일. 기본 검사는 저장소의 `fireball_voice.mp3`를 참조.
- 기존 인수 근거인 `environment.yml`도 함께 보존.
- `.python`, `.venv`, `.tools`, `.cache`, `models`, `.runtime`은 Git에서 제외됨. Git으로 소스만 복사하면 환경과 모델은 따라가지 않음.
- `.venv`는 절대 경로를 포함하므로 PC 간 복사해서 재사용하지 말고 새 PC에서 생성. 저장소 경로를 옮긴 경우에도 재생성 필요.
- 모델 다운로드를 줄이려면 `models/small`의 네 파일을 함께 옮길 수 있음. 다만 최초 준비는 Python·라이브러리를 받기 위해 인터넷이 필요하며, 모델 준비 스크립트도 원격 정보를 확인함.
- 완전 오프라인 신규 PC 설치용 패키지는 이번에 만들지 않음. 발표 PC를 사전에 온라인 상태에서 준비한 뒤 오프라인 실행을 확인할 것.
- 현재 서버와 Unity의 포트는 5000으로 고정. 포트를 변경하려면 양쪽 코드가 함께 맞아야 함. PC의 IP가 달라도 같은 PC에서 실행하면 Unity의 `localhost` 주소는 그대로 사용.
- 발표 PC에서 Unity 버전, 마이크 권한·장치, 포트 충돌, 실제 마이크 인식, 마법 발동, 게임과 동시 실행할 때 지연·프레임을 다시 검증.

## 공식 참고 자료

- [uv의 Python 설치 방식](https://docs.astral.sh/uv/guides/install-python/)
- [faster-whisper 1.2.1 요구사항·CPU 실행](https://github.com/SYSTRAN/faster-whisper/blob/v1.2.1/README.md)
- [사용한 모델 리비전](https://huggingface.co/Systran/faster-whisper-small/tree/536b0662742c02347bc0e980a01041f333bce120)
