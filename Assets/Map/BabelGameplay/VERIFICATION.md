# 실제 던전 외형 통합 검증 — 2026-10-31

기준: `3c77536f`(음성 서버 준비·포털 복구 체크포인트), 원래 제출 기준 `089b63f236c469ecbdfe1b2900dc5d058e4b48e6`.
작업 브랜치 `work/whisper-local-setup`, 사용자 지정 푸시 대상 `20260504_boss_tile`.

## 직접 자동 확인

Unity **6000.4.0f1**, 실제 `0326_MapMerge` 씬.

- `BabelGameplayIntegration.Build`: 생성 목록의 방/문/막힌 통로를 시각 복제본으로 연결. 각 원본과 Collider 개수, MeshCollider 참조, Connector 로컬 위치 일치 검사 통과. 플레이어 기존 Animator/Avatar/컨트롤러와 Collider 개수 유지 검사 통과.
- `BabelGameplayIntegration.CleanRetiredPrototypes`: 최종 씬과 다른 프로젝트 씬·프리팹·재질의 의존 파일을 보존. 미참조 190파일, 157,326,784바이트를 로컬 백업 후 Assets에서 제거.
- `BabelGameplayVerification.Run`: 실제 씬 Play 후 전환 API로 던전 → Puzzle → 던전 → Puzzle → 던전. 두 실행 모두 종료 코드 0.
- 최종 반복의 시드 `2056123686`, 생성 타일 7개. 각 복귀 후 같은 타일 인스턴스·시드·플레이어 인스턴스와 음성 시스템의 Whisper/MagicAttack 참조 확인.
- 시작/복귀 좌표 `(-3.19, -0.01, 2.62)`. 두 퍼즐 진입 좌표 `(0.00, -0.01, -21.29)`. 시작·퍼즐·복귀에서 CharacterController 접지 확인.
- 드워프 Avatar 유효, 중복 애니메이션 실행기 비활성, 기존 마네킹 숨김, 드워프 표시, 골반 로컬 위치 범위 확인.
- 기준 씬의 직렬화 객체 742개 ID가 모두 남아 있음을 대조했다. 기존 MonoBehaviour 중 저장 내용이 바뀐 것은 던전 생성기의 시각 프리팹 배열/디버그 조명 설정뿐이다. 기존 플레이어·스킬·음성·포털 스크립트의 저장 참조는 그대로다.
- 실제 카메라의 시작 1·3인칭 및 복귀 렌더를 직접 확인. HUD는 카메라 단독 캡처에 포함되지 않는다. 시작 방의 기존 회색 보상 큐브에는 마지막으로 목재 재질만 적용했다(판정·보상 코드 불변).

사용한 Unity 명령 인수:

```text
-batchmode -quit -noaudio -projectPath C:/Unity/capstone25-magic-boys -executeMethod BabelGameplayIntegration.Build -logFile .../WhisperServer/.runtime/babel-gameplay-build.log
-batchmode -quit -noaudio -projectPath C:/Unity/capstone25-magic-boys -executeMethod BabelGameplayIntegration.CleanRetiredPrototypes -logFile .../WhisperServer/.runtime/babel-cleanup.log
-batchmode -noaudio -force-d3d11 -projectPath C:/Unity/capstone25-magic-boys -executeMethod BabelGameplayVerification.Run -logFile .../WhisperServer/.runtime/babel-gameplay-final.log
```

이 검사들은 실제 입력 대신 같은 씬 전환 API를 사용했고, 복귀 앵커는 안전한 시작 위치였다. E 키 감지나 실제 포털 트리거 통과를 자동 성공으로 간주하지 않는다. 같은 타일 인스턴스 보존을 검사했으며 몬스터 체력·상자 개봉 등 모든 진행 상태를 개별 검증한 것은 아니다.

## 이전 사용자 확인

외형 통합 전 사용자가 재부팅 후 start.cmd, 숫자 1, Q 실제 마이크 Fireball 발동, 퍼즐 왕복 후 스킬·음성·동일 맵 유지를 확인했다. 약 2.07초는 단일 측정이다. 이번 통합 후 자동 결과와 혼동하지 않는다.

## 남은 수동 확인과 제한

- 실제 WASD/점프/구르기, 망치 타격 판정과 손·수염·앞치마 겹침.
- 실제 포털 E 진입/복귀 두 번과 각각 숫자 1/Q 마이크 발동.
- 실제 마이크/HTTP 서버는 이번 외형 통합 검사에서 다시 실행하지 않았다. 조원용 방법은 `WhisperServer/TEAM_QUICKSTART.md`.
- 배치 테스트에서 `Cannot initialize WebCamTexture because WebCamDevice is not selected`가 발생했다. 이 상태에서도 전환·배치 검사는 통과했으나 퍼즐 손동작 인식·클리어는 미검증이다. 웹캠이 있는 실제 플레이 환경에서 별도로 확인한다.
- Room I 이외 방은 기존 형상에 재질·UV·조명을 통일한 범위다. 모든 방을 같은 장식 구성으로 재설계하지 않았다. 보스 방의 개별 소품, 장비 관통, 전체 던전 성능/발표 PC FPS는 추가 확인 대상이다.

자동 로그/캡처/정리 백업은 `.runtime`에만 보관하며 Git에 포함하지 않는다. `.slnx` 기존 삭제는 이번 커밋에서 제외한다.
