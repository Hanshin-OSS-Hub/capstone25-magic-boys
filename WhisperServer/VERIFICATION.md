# 현재 PC 실행 검증 기록

2026-10-29 한국 시간 기준. 사용자 확인상 최종 제출본인 `20260504_boss_tile`의 커밋 `089b63f236c469ecbdfe1b2900dc5d058e4b48e6`에서 `work/whisper-local-setup`을 생성했다. 시작 시 작업 트리는 깨끗했으며 원격 push는 하지 않았다.

## 환경 확인

- 기존 `AGENTS.md`를 조사 범위와 상위 경로에서 찾지 못함. 루트 README에는 저장소 이름만 존재.
- 기존 전역 Python·Conda를 사용하지 않음. 초기 `py -0p`는 `No installed Pythons found!`를 반환.
- CPU: Intel Core i3-12100F, 물리 4코어/논리 8개.
- 프로젝트 전용 CPython: 3.10.19, Windows x64.
- 의존성: 원본 명세에서 가져온 버전 45개 설치, `uv pip check` 통과.
- `ctranslate2.get_supported_compute_types('cpu')`: `float32`, `int16`, `int8`, `int8_float32`.
- 모델: `Systran/faster-whisper-small`, 리비전 `536b0662742c02347bc0e980a01041f333bce120`.
- 모델 네 파일 486,212,372바이트 다운로드 완료 및 CPU/INT8 로딩 성공.

## 실제 실행한 주요 명령

아래는 저장소 루트 기준이다. 환경 변수는 각 준비 프로세스에서 프로젝트 내부 경로로 설정했다.

```powershell
git status --short --branch
git branch --show-current
git rev-parse HEAD
git switch -c work/whisper-local-setup
py -0p
```

공식 uv 0.13.6 ZIP과 SHA-256 파일을 다운로드한 뒤 `Get-FileHash`로 검증하고 `.tools`에 압축을 풀었다. 확인한 SHA-256:

```text
A2A8820D5EEA32F08EF467EF3B7F66883A9B076D5737A2FC2FBB5F458D1BB50F
```

최초 Python·가상환경·라이브러리 준비에 실행한 핵심 명령:

```powershell
$env:UV_PYTHON_INSTALL_DIR=Join-Path (Get-Location) 'WhisperServer/.python'
$env:UV_CACHE_DIR=Join-Path (Get-Location) 'WhisperServer/.cache/uv'
& 'WhisperServer/.tools/uv.exe' python install 3.10.19 --no-bin --no-registry --no-config
& 'WhisperServer/.tools/uv.exe' venv 'WhisperServer/.venv' --python 3.10.19 --managed-python --no-config
& 'WhisperServer/.tools/uv.exe' pip install --python 'WhisperServer/.venv/Scripts/python.exe' -r 'WhisperServer/requirements.txt' --no-config
& 'WhisperServer/.tools/uv.exe' pip check --python 'WhisperServer/.venv/Scripts/python.exe' --no-config
```

이후 준비 및 검증:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File WhisperServer/setup.ps1
.\WhisperServer\start.cmd
$env:PYTHONUTF8='1'
$env:PYTHONIOENCODING='utf-8'
& 'WhisperServer/.venv/Scripts/python.exe' 'WhisperServer/verify_http.py'
```

`setup.ps1`은 기존 환경을 재사용하고 의존성 재확인·모델 최초 다운로드·CPU 로딩까지 완료했다. 서버 재시작도 같은 `start.cmd`로 실행했다.

## 직접 자동 검증한 단계별 결과

아래 표는 최초 자동 검증 당시의 결과를 보존한 것이다. 이후 사용자가 직접 확인한 결과는 다음 별도 절에 기록한다.

| 단계 | 판정 | 근거 |
|---|---|---|
| Python·의존성 준비 | 성공 | Python 3.10.19, 45개 패키지, 의존성 검사 통과 |
| 모델 준비 | 성공 | 로컬 모델 CPU/INT8 로딩 완료 |
| 서버 시작 | 성공 | Flask가 5000 포트에서 대기 |
| HTTP 연결 | 성공 | 실제 Unity 주소 `http://localhost:5000/transcribe` 사용 |
| 요청 형식 검사 | 성공 | audio 없는 multipart 요청에 400과 예상 error 응답 |
| 파일 음성 인식 | 성공 | 기존 음성 파일을 16kHz 모노 PCM16 WAV로 전송, 200과 ` Fireball` 반환 |
| 종료·재시작 | 성공 | Ctrl+C 후 5000 연결 실패 확인, start.cmd로 재시작 후 동일 인식 성공 |
| 다운로드 없는 재시작 | 성공 | 시작 스크립트가 HF_HUB_OFFLINE=1 설정, 모델 local_files_only=True로 로딩 |
| 물리적 인터넷 차단 상태 | 미검증 | OS 네트워크 연결 자체를 끊지는 않음 |
| 사용자의 실제 마이크 녹음 | 미검증 | 파일 오디오만 사용 |
| Unity에서 마법 발동 | 미검증 | Unity 실행·사용자 주문 발화는 수행하지 않음 |
| 발표 PC 성능·동작 | 미검증 | 다른 PC일 수 있음 |

입력 파일: `Assets/Sound/SFX/fireball_voice.mp3`. 이 파일의 인식 결과는 실제 사용자의 발음·마이크·잡음 환경을 대표하지 않는다.

| 검사 시각(KST) | 음성 길이 | HTTP | 응답 text | 요청부터 응답까지 |
|---|---|---|---|---|
| 18:32:57 | 1.8초 | 200 | ` Fireball` | 4.607초 |
| 18:34:19, 재시작 후 | 1.8초 | 200 | ` Fireball` | 4.313초 |

두 값은 파일 요청의 단일 관측값이다. 녹음·Unity 프레임·마법 발동 시간은 포함하지 않으며 평균이나 최악 지연으로 해석하면 안 된다. 실시간 조작감과 동시 게임 실행 성능은 별도 검증이 필요하다.

## 사용자 직접 확인 결과 추가 (2026-10-29)

근거: 사용자의 확인 보고와 첨부 Unity Console 스크린샷. 아래 결과는 에이전트가 직접 자동 실행한 결과와 구분한다.

| 항목 | 사용자 확인 결과 |
|---|---|
| PC 재부팅 후 재실행 | `start.cmd`로 서버 실행 성공 |
| 직접 스킬 입력 | 숫자 1로 불 마법 발동 성공 |
| 실제 마이크 음성 인식 | Q로 녹음한 `Fireball` 인식 성공. 스크린샷에 `[Whisper] Result = Fireball` 표시 |
| Unity 마법 연동 | 사용자 확인상 실제 음성 입력 후 불 마법 발동 성공 |
| 녹음 종료부터 응답까지 | 스크린샷의 `Response - RecordEnd = 2.0665s`, 약 **2.07초** |
| 퍼즐 씬 왕복 후 음성 유지 | 아직 미검증. 다음 사용자 검증 대상 |

2.07초는 **단일 측정값**이며 평균·최악 지연·최적화 효과를 의미하지 않는다. 발화 시작이나 실제 마법 생성까지의 전체 시간도 아니다. 앞선 자동 파일 검사 4.607초·4.313초와 조건이 다르므로 직접적인 성능 개선 수치로 비교하지 않는다.

왕복 검증 준비를 위해 `PuzzlePortal.cs`, `PuzzleTravelManager.cs`, `PersistentPlayer.cs`와 관련 씬·프리팹, 복귀 포털 활성화에 연결된 상자 보상 코드를 필요한 만큼 읽었다. 진입은 던전의 파란 포털 범위에서 E, 복귀는 상자 파괴로 활성화된 퍼즐의 파란 포털 범위에서 E다. 씬을 에디터에서 직접 열거나 Play를 중단하면 실제 왕복 검증이 되지 않는다.

플레이어는 씬 이동 때 유지되지만 기존 분석상 음성 객체와 대상 참조의 수명은 별도다. 유지 여부는 확정하지 않았으며, 복귀 후 녹음 로그·인식 결과·불 마법 발동을 각각 확인해야 한다. 코드나 씬은 수정하지 않았다.

## 오류·경고와 처리

- 브랜치 생성 시 `.git` 쓰기가 샌드박스에서 차단되어 승인된 권한으로 같은 명령을 재실행했고 성공.
- 외부 다운로드 연결이 샌드박스에서 차단되어 승인된 네트워크 접근으로 진행. TLS 검증을 끄지 않음.
- uv 캐시의 일시적 파일 이름 변경 접근 거부가 발생했으나 도구의 재시도로 설치 완료. 이 오류 때문에 권한이나 의존성 버전을 바꾸지 않음.
- CTranslate2가 `pkg_resources` 폐기 예정 경고를 출력. 원본 명세의 setuptools 80.9.0 유지 상태에서 실제 로딩·추론 성공.
- Flask 개발 서버 경고는 원래 실행 방식에서 발생. 서버 엔진은 교체하지 않음.
- 서버 요청 처리 오류는 위 두 파일 인식 검사에서 발생하지 않음.

## 코드 변경 범위

- `whisper_server.py`: 준비와 실행을 분리하기 위해 모델 경로를 서버 파일 기준 로컬 경로로 고정하고 자동 다운로드를 금지. 모델 누락 안내 및 CPU/INT8 로그 수정.
- `requirements.txt`: 기존 환경 명세의 라이브러리 버전을 pip 설치에 사용할 수 있도록 추출.
- `setup.ps1`, `prepare_model.py`: 프로젝트 내부 환경·고정 모델 준비와 검사.
- `start.cmd`: 전용 Python·로컬 모델로 반복 실행.
- `verify_http.py`: 실제 요청 형식에 맞춘 파일 음성·HTTP 검증 도구.
- `.gitignore`: 환경·모델·캐시·검증 결과가 Git에 들어가지 않도록 제외.
- `README.md`, `VERIFICATION.md`: 재실행·인수 및 검증 기록.

모델 종류, CPU/INT8, 영어 인식, /transcribe의 요청·응답, 포트, 기존 임시 파일 처리, Unity 코드·씬·맵·플레이어·스킬은 유지했다.

## 이후 포털 왕복 조사·수정 (2026-10-29)

위 변경 범위는 서버 준비 당시의 기록이다. 이후 사용자가 포털 왕복 문제의 수정을 요청하여 씬 전환·음성 객체 수명 관련 Unity 코드를 추가 수정했다. 상세 근거와 수동 절차는 [포털 왕복 기록](PORTAL_ROUNDTRIP.md)에 정리했다.

- 사용자 추가 확인: 최초 던전의 숫자 1·Q 음성 주문은 성공. 기존 구현에서 퍼즐 복귀 후에도 숫자 1은 성공. 음성 로그에는 빈 결과·Fireball·Fiber가 있어 인식 전체 중단으로 판단하지 않았다.
- 소리 반복은 사용자가 원거리 몬스터 공격으로 확인하여 이번 수정에서 제외. 오디오·몬스터 공격 코드는 수정하지 않았다.
- 별도 최소 Unity 프로젝트에서 수정 코드를 컴파일하고 같은 퍼즐 두 번 왕복, 다른 퍼즐 한 번 왕복, 바닥 없는 퍼즐 진입 취소를 자동 확인했다. 던전 식별자·생성 횟수·객체 유지·포털별 복귀 위치·배치 후 낙하 여부·음성 명령 대상 유지가 통과했다.
- 자동 테스트의 던전 생성기·마법 대상·진행 객체는 모의 구성이고 음성 결과는 주입했다. 실제 게임 맵·몬스터 HP·상자 진행·숫자키·마이크·실제 불 마법·퍼즐 손 인식은 수정 후 아직 미검증이다.
- 실제 프로젝트는 사용자 에디터에 열려 있어 자동으로 Play하거나 기존 에디터를 종료하지 않았다. 임시 테스트 프로젝트만 별도로 실행·종료했다.

## 사용자 후속 확인: 포털 수정 후

사용자는 퍼즐 왕복 후 스킬·실제 마이크 음성 주문·동일 맵 유지가 정상임을 확인했다. 이 결과는 사용자 직접 검증이며 위의 최소 씬 자동 테스트와 구분한다. 반복 왕복의 추락 방지·모든 진행 객체·발표 PC 성능까지 별도로 검증 완료했다고 확장하지 않는다.
