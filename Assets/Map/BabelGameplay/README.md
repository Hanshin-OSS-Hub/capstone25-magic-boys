# 바벨탑 던전 외형 통합

실제 플레이 씬: `Assets/Map/Scenes/0326_MapMerge.unity`. MainMenu의 시작 대상도 이 씬이다.

- 기존 Player 객체에 승인된 JanPohl 드워프와 큰 망치 외형을 연결했다. 원래 Animator/Avatar/컨트롤러, 무기 객체, 마법 발사점, 물리·음성·포털 로직은 유지한다.
- 던전 생성 배열은 같은 순서와 개수의 `Rooms` 시각 복제 프리팹으로 연결했다. 원본 방 크기, Collider, MeshCollider 메시, Connector 좌표를 검사했다. 원본 프리팹은 유지한다.
- Room I는 승인된 석재 구조물·문틀·기둥·벽 가장자리 소품을 적용했다. 다른 형태의 시작/일반/출구/퍼즐 연결 방은 원래 형태에 석재 재질과 UV를 적용했다. Room I 장식을 모양이 다른 방에 억지로 확대하지 않았다.
- 중립적인 어두운 환경광과 조명을 사용하며 에디터의 방별 디버그 색 조명은 껐다. 퍼즐 씬 자체는 수정하지 않았다.
- 비교용 F6~F10 UI와 안전용 비교 씬 바닥은 실제 던전에 넣지 않았다. V 시점 전환은 기존 조작이다.

## 확인 순서

1. 실제 씬을 Play하고 시작 방의 바닥/벽, V 1·3인칭 드워프와 큰 망치를 확인한다.
2. WASD, 점프, Ctrl 구르기, 마우스 공격, 숫자 1을 확인한다. 실제 타격 판정과 장비 겹침은 수동 확인한다.
3. [음성 메모](../../../WhisperServer/TEAM_QUICKSTART.md)에 따라 서버를 켜고 Q Fireball을 확인한다.
4. 실제 포털 근처에서 E로 퍼즐 진입 → 복귀를 두 번 반복한다. 같은 맵/포털 근처 복귀, 바닥 접지, 각 복귀 후 숫자 1/Q를 확인한다.

Room I가 무작위 생성에서 나오지 않으면 승인된 비교 씬 `Assets/Map/VisualPrototypes/Babel/AssetDraft/Dwarf/Babel_Dwarf_Compare.unity`에서 장식을 별도로 볼 수 있다.

## 파일 정리와 출처

미채택 시안 중 최종 씬/프리팹에서 참조하지 않는 파일은 `WhisperServer/.runtime/retired-visuals`에 로컬 보관 후 Assets에서 제외한다. 이 백업, 환경, 모델 캐시와 테스트 로그는 커밋하지 않는다. 승인된 비교 씬과 실제 의존 에셋, 드워프 원본/리깅 FBX·텍스처·출처는 보존한다.

드워프: **Dwarf Blacksmith by JanPohl (Sketchfab), CC BY 4.0. Rigging, Unity materials and gameplay adaptation for Magic Boys.**

- https://sketchfab.com/3d-models/dwarf-blacksmith-e48bfff956ef483cb987890dc856c8b5
- https://creativecommons.org/licenses/by/4.0/
- 다른 건축·텍스처 출처: `Assets/Map/VisualPrototypes/Babel/AssetDraft/THIRD_PARTY.md`

배포/발표 크레딧에도 제작자·출처·라이선스·변경 사항을 남긴다. 기존 Unity Asset Store 자산의 라이선스를 CC0로 바꾸는 것은 아니다.
