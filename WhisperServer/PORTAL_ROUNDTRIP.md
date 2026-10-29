# 포털 왕복 수정 및 검증 기록

기준: `work/whisper-local-setup`, HEAD `089b63f236c469ecbdfe1b2900dc5d058e4b48e6`. 기존 음성 서버 준비 파일과 미커밋 변경을 보존했다. 작업 시작부터 있던 `capstone25-magic-boys.slnx` 삭제 상태도 복구하거나 덮어쓰지 않았다. 원격 push는 하지 않았다.

## 원인과 코드 근거

1. **기존 던전은 유지되지 않았다.** 이전 `PuzzleTravelManager`의 `LoadSceneAsync(sceneName)`은 Single 로드다. 던전 객체가 파괴되고, 복귀 시 `DungeonGenerator.Start()`와 `DungeonBuild()`가 다시 실행된다. `saveSeed`는 있으나 방·적·상자 객체의 상태를 저장·복원하는 구현은 없었다. 동일 시드도 같은 진행 상태를 뜻하지 않으며 생성 과정이 전역 Unity Random을 공유한다.
2. **좌표 적용 시점이 맵 준비보다 빨랐다.** 이전 매니저는 `sceneLoaded`에서 저장 좌표로 즉시 이동했다. 이 이벤트는 Start 전이고 던전 생성은 Start의 코루틴이므로, 복귀 좌표의 바닥이 생기기 전에 배치할 수 있었다. 퍼즐 스폰도 전역 `GameObject.Find`로 찾았고 바닥 확인 없이 기존 좌표를 유지하는 실패 경로가 있었다.
3. **이전 낙하 상태가 남았다.** `ThirdPersonController`는 `_verticalVelocity`와 밀림 상태를 별도로 보관한다. 기존 이동 코드는 Rigidbody 속도만 초기화했으므로 해당 상태를 초기화하지 않았다. 이것이 사용자가 본 두 번째 진입 추락의 단일 원인이라고 실제 게임에서 재현·확정한 것은 아니다.
4. **음성 객체와 플레이어의 수명이 달랐다.** 던전 Player는 PersistentPlayer로 유지되지만 Whisper와 VoiceCommand는 씬 루트였다. 퍼즐 씬에 별도 음성 연결·의도적인 음성 차단 플래그는 없다. 기존 복귀 로드에서 새 VoiceCommand가 가리키는 씬 Player는 PersistentPlayer의 중복 제거 대상이라, 유지된 Player와 새 음성 대상이 어긋날 수 있다. 사용자 로그의 빈 결과·Fiber는 인식/명령 불일치이고 Fireball 응답은 인식 단계가 살아 있었다는 근거다. 모든 실패를 녹음 중단으로 묶지 않았다.

관련 파일: `Assets/Skill/Scenes/PuzzleTravelManager.cs`, `PersistentPlayer.cs`, `Assets/Map/Scripts/DungeonGenerator.cs`, `PuzzlePortal.cs`, 메인 던전·퍼즐 씬의 Player/Whisper/VoiceCommand/PuzzleSpawnPoint 참조.

## 수정 범위

| 파일 | 수정 |
|---|---|
| `Assets/Skill/Scenes/PuzzleTravelManager.cs` | 현재 던전 씬을 로드된 상태로 보관하되 원래 활성 루트만 일시 비활성화. 퍼즐만 Additive 로드하고 복귀 시 퍼즐을 해제해 원래 던전을 재활성화. 진입할 때마다 포털·복귀 좌표를 갱신하고 목적 씬 내부에서 스폰 탐색. Start 이후 바닥·배치 공간을 검사하고 플레이어를 한 번 배치. 실패 시 진입 취소·복귀. 던전 Single 언로드 때 보관 상태 초기화 |
| `Assets/Map/Scripts/PuzzlePortal.cs` | 포털 소속 씬·객체를 전달하고 비활성화 때 트리거 캐시 초기화. 원본 CP949 인코딩 보존 |
| `Assets/Player/Scripts/ThirdPersonController.cs` | 텔레포트 때만 낙하·밀림 속도와 이동 입력 초기화 |
| `Assets/Player/Scripts/VoiceCommandSystem.cs` | 기존 음성 객체 둘을 현재 Player 자식으로 연결하고 마법 대상을 명시적으로 연결. 인식 결과와 명령 전달 여부 로그 |
| `Assets/Whisper/.../WhisperPure.cs` | 전환 중 녹음·대기 HTTP 요청·이전 결과 정리. 요청 대기 중 중복 녹음 방지. 녹음/응답 번호와 활성 씬 로그 |
| `Assets/Message/Scripts/MessageManager.cs` | Additive 상태에서 비활성 던전 안내 매니저를 계속 가리키지 않도록 활성화/비활성화 때 정적 참조 연결·해제 |
| `WhisperServer/.gitignore` | 임시 분리 테스트 프로젝트 제외 |

현재 세션의 던전 한 개만 보관한다. 모든 객체를 DontDestroyOnLoad로 전환하거나 매 프레임 위치를 보정하지 않았다. 맵 시드 재생성으로 진행 상태를 복원했다고 간주하지 않는다. 퍼즐 씬은 매 진입 새로 로드한다. 게임 종료 후 저장 시스템·맵 디자인·음성 엔진·오디오·그림자 설정은 추가하지 않았다.

## 자동 검증과 한계

현재 게임 프로젝트가 에디터에 열려 있어 원본 프로젝트를 자동 실행하지 않았다. Unity 6000.4.0f1을 별도 `WhisperServer/PortalTravelTests` 프로젝트에서 `-batchmode -nographics -executeMethod PortalVerificationEditor.Run`으로 실행했다. 이 임시 폴더는 Git 제외다.

- 실제 수정한 전환·포털·플레이어 이동·음성 클라이언트·명령 코드와 기존 PersistentPlayer를 복사해 Unity에서 컴파일·실행.
- 던전 생성기와 마법 대상, 진행 상태 객체는 최소 모의 구성. 실제 녹음 대신 `Fireball` 결과를 주입하여 대상 호출 횟수를 확인.
- 같은 PuzzleA 두 번 왕복, 스폰 위치가 다른 PuzzleB 한 번 왕복 통과.
- 동일 던전 씬 핸들·생성기 식별자·시드·방 배치 진단값 유지, 생성 횟수 1, 진행 객체 동일성·파괴된 상자 대체 객체의 부재 유지 확인.
- 두 번째 진입 전 낙하 속도 -50을 주입해 안전 배치 및 배치 후 바닥 아래 추락이 없는지 확인.
- 진입마다 복귀 포털 좌표를 변경해 이전 좌표가 재사용되지 않는지 확인.
- 퍼즐·던전 복귀 후 주입한 Fireball의 명령 전달 각 1회, 이전 결과 폐기, Fiber의 명령 미전달 확인.
- 바닥 없는 NoFloor 씬은 예상대로 오류를 기록하고 진입을 취소, 원래 던전으로 복귀.
- 최종 실행 종료 코드 0, `[PORTAL TEST] 전체 통과` 확인. 구문·공백 검사는 `git diff --check` 통과.

로그: `.runtime/portal-travel-tests.log`. 초기 테스트 도구 준비에서는 JSON 모듈 누락·에디터 도메인 재로드로 실행기가 사라지는 문제를 임시 프로젝트에서 해결했다. Unity의 검색 DB 초기화 예외도 로그에 있으나 이후 검증 시나리오는 수행되어 통과했다. 실제 제품 코드의 실행 성공과 혼동하지 않는다.

**수정 후 실제 게임의 두 번 왕복, 실제 몬스터 HP·상자 진행 상태, 숫자 1, 마이크 Q 주문과 불 이펙트, 퍼즐 손 인식은 아직 미검증이다.** 이전 사용자의 최초 던전 성공 결과는 기존 검증 기록에 별도로 유지한다.

후속 사용자 확인: 퍼즐 왕복 후 스킬·음성인식 및 마법 발동·동일 맵 유지 성공. 위 미검증 문장은 최초 자동 테스트 시점의 한계이며, 이후 사용자 확인과 구분해 보존한다. 반복 횟수·모든 진행 객체 상태·발표 PC 성능은 추가 확인 대상이다.

## 사용자 확인: 가장 먼저 1회 복귀의 던전·위치 확인

1. 변경 코드 컴파일이 끝나고 Console에 컴파일 오류가 없는지 확인한 뒤, 기존 Play를 종료하고 `0326_MapMerge`에서 새 Play 세션을 시작한다. 음성 서버는 계속 켜 둔다. 이번 검증 중 Play 중단·씬 직접 열기·Backspace 맵 재생성은 하지 않는다.
2. 맵 생성 후 숫자 1과 Q Fireball을 각각 확인한다. 가까운 적 한 마리를 처치하거나 상자 하나를 파괴하고, 포털 근처 방 구성·진행 상태를 기억한다.
3. 파란 진입 포털에서 E. `[PuzzleTravel #1] 진입`의 던전 핸들·generator·seed·rooms·layout·portal·return 값을 보존한다.
4. 퍼즐에서 플레이어가 바닥 위에 서 있고 이동·점프가 되는지 확인한다. Q Fireball을 시험하면 녹음/응답/명령전달 로그가 각각 있어야 한다. 빈 결과나 Fiber를 Fireball 성공으로 세지 않는다.
5. 퍼즐을 진행해 보물상자를 파괴하고 활성화된 파란 복귀 포털에서 E.
6. **첫 결정적 확인:** `[PuzzleTravel #2] 던전 복귀`에 `동일던전=True 배치=True`가 나오는지, 원래 포털 근처인지 확인한다. 방 구성뿐 아니라 처치한 적·파괴한 상자가 되살아나지 않았는지 확인한다.
7. 복귀 후 숫자 1, 쿨타임을 기다린 뒤 Q Fireball을 확인한다. `[Whisper] Result`와 `[VoiceFlow] 명령전달=True`, 실제 투사체 생성을 구분한다.
8. 같은 포털로 다시 진입하고 바닥·이동을 확인한 뒤 복귀한다. `#3` 진입/배치, `#4` 복귀에서도 같은 검사를 반복한다. 다른 포털도 시험했다면 같은 퍼즐/다른 퍼즐 여부를 함께 기록한다.

실패하면 첫 오류와 해당 전환 번호의 진입·배치·복귀 로그, 숫자 1 성공 여부, 마지막 VoiceFlow 단계만 전달하면 된다. `동일던전=True`는 맵 객체 유지 진단이지 모든 몬스터/상자 진행 검증을 대신하지 않는다. 반복 소리는 사용자 확인상 원거리 몬스터의 정상 공격이므로 이번 합격 판정에서 제외한다.
