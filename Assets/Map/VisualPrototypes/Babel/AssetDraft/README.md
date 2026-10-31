# 승인된 바벨탑 방·드워프 시안

실제 게임 적용 설명은 `Assets/Map/BabelGameplay/README.md`를 참고한다.

보존한 비교 씬: `Dwarf/Babel_Dwarf_Compare.unity`.
- F6 원래 방/승인된 방, F7 시작 위치 복귀.
- F8 원래 마네킹/드워프, F9 정면 관찰/플레이 복귀, F10 밝기 비교.
- V 1·3인칭. 실제 게임 씬에는 비교 도구가 없다.

`RoomI_Babel_Assets.prefab`은 승인된 기둥·문틀·석재 벽·소품의 기준이다. 실제 게임용 복제본은 `Assets/Map/BabelGameplay/Rooms`에 있다. 다른 모양의 방은 기존 형상에 석재 재질·UV와 중립 조명을 적용했다.

미채택 마법사/사람 시안과 예전 캡처·생성 도구의 미참조 파일 190개(약 157MB)는 로컬 `WhisperServer/.runtime/retired-visuals`에 보관하고 Assets에서 제거했다. Git에는 이 백업을 넣지 않는다. 승인된 비교 씬의 의존 파일과 드워프 원본·리깅 자료는 남겼다.

비교 씬 검사 진입점: `BabelDwarfVerification.Run`.
실제 던전 검사 진입점: `BabelGameplayVerification.Run`.
둘 다 Unity 배치 Play 검사가 끝나면 Editor를 종료하므로 별도 실행 중인 Editor와 동시에 사용하지 않는다.

평소에는 재생성이 필요 없다. Unity에서 씬을 열고 Play한다. 출처는 `THIRD_PARTY.md`, 드워프 구조와 제약은 `Dwarf/README.md`에 있다.