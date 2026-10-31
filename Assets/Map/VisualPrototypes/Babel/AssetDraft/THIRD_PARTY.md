# 시안 외부 자산 출처

2026-10-31 정리: 아래 FreeMale1/PlainHuman은 미채택 시안의 역사적 출처 기록이다. 해당 모델과 시안은 최종 게임에서 사용하지 않으며 미참조 파일을 로컬 백업으로 옮겼다. 현재 플레이어는 마지막 항목의 JanPohl Dwarf Blacksmith다.

## Poly Haven

공식 자산의 필요한 JPEG 맵 7개만 내려받았습니다. 합계 18.03 MiB입니다. `Textures/SOURCES.json`에 파일별 URL·크기·공식 MD5를 기록했고, 다운로드 후 각 파일의 MD5를 공식 값과 대조했습니다. 라이선스는 CC0 1.0입니다. 감사 표시로 출처를 남깁니다.

- Medieval Blocks 03: https://polyhaven.com/a/medieval_blocks_03 — 2K Diffuse / Normal GL. Rob Tuytel.
- Stone Tiles: https://polyhaven.com/a/stone_tiles — 2K Diffuse / Normal GL. Christopher Melani.
- Fabric Pattern 07: https://polyhaven.com/a/fabric_pattern_07 — 1K Normal GL. 로브·후드·깃발에 사용. 체크무늬 색상 맵은 사용하지 않았습니다.
- Leather Red 02: https://polyhaven.com/a/leather_red_02 — 1K Coll1 / Normal GL. 어둡게 착색해 장갑·부츠 등에 사용.
- 라이선스 안내: https://polyhaven.com/license
- CC0 1.0 전문: https://creativecommons.org/publicdomain/zero/1.0/

Unity URP에서 Normal Map으로 임포트했고 석재는 최대 2048, 천·가죽은 최대 1024로 제한했습니다. 색상 맵은 sRGB, 노멀은 선형으로 읽습니다. 변위로 원본 바닥/벽 충돌을 바꾸지 않습니다.

## 프로젝트 기존 자산

- Decrepit Dungeon LITE: `Assets/Decrepit Dungeon LITE/`의 기존 메시·텍스처를 참조합니다. https://assetstore.unity.com/packages/3d/environments/dungeons/decrepit-dungeon-lite-33936 — Standard Unity Asset Store EULA. 해당 자산의 소유권이나 재배포 조건을 CC0로 바꾸는 것이 아닙니다.
- Starter Assets Armature / 기존 Kevin Iglesias 망치·애니메이션은 저장소의 파일을 참조합니다. 원본 파일·라이선스를 변경하지 않았습니다.
- 새 의상, 문 아치, 탑 문양과 시안 도구는 이번 작업에서 직접 제작했습니다. 신규 유료 자산 구매, Mixamo 로그인·다운로드, N-hance 패키지 다운로드는 하지 않았습니다.

## FreeMale1 — 룬 대장장이의 인물 몸체

- 제작자/배포자: SirLancelotGames (crimsondemon01).
- 공식 배포 페이지: https://crimsondemon01.itch.io/freemale1
- 2026-10-30 무료 다운로드. 배포자가 프로젝트 사용을 허용하고 크레딧은 선택 사항으로 명시합니다. CC0라고 재표기하지 않습니다.
- 사용 허용 문구(원문): “you can use it in anything you want no need to give me credit if you don't want to”.
- 원본: `Runesmith/Source/Male1.fbx`, 34,030,348 bytes. 공식 `FreeMale1.zip` SHA-256: `ADCB8F337077FA0E138CB355DB34645EBA60D2E743D6912EC9CB9B1560D353C6`.
- 인물 모델과 내장 피부·머리·눈·의상 텍스처를 사용합니다. 모델 내부에는 MakeHuman 계열 이름이 남아 있습니다. 제작자의 모델링·텍스처 작업을 이번 작업에서 제작했다고 주장하지 않습니다.
- 변형: 기존 게임 Humanoid 뼈대에 맞춘 별도 메시, URP Lit 재질, 의상 색상 조정, 1인칭용 머리 렌더러 분리. 원본 FBX 보존.
- 추가 금속 보호구·룬은 이번 시안에서 제작했고, 가죽 작업 장비와 큰 망치는 이전 시안을 재사용합니다.
- 발표 크레딧 권장: `Character base: FreeMale1 by SirLancelotGames — itch.io. Adapted for Magic Boys.`

Elf Blacksmith, Unity Challenger, Mixamo 및 검색 중 살펴본 다른 모델은 이 시안에 포함하지 않았습니다.

## 2026-10-30 PlainHuman 후속 시안

`PlainHuman/`은 위 FreeMale1의 기존 다운로드를 재사용합니다. 원본 FBX와 텍스처는 `Runesmith/Source`, `Runesmith/Textures`에 있으므로 PlainHuman 폴더만 다른 PC로 복사하면 부족합니다. 원본 뼈대·정점·노멀·스킨은 보존하고 URP 재질과 1인칭용 머리 인덱스 분리만 별도로 적용합니다. Runesmith의 임의 제작 장비는 포함하지 않습니다. 다른 사이트의 새 캐릭터를 다운로드했다고 주장하지 않습니다. 조사 결과와 확인 방법은 `PlainHuman/README.md` 참조.

## 2026-10-31 Dwarf Blacksmith — JanPohl

사용자가 선택하고 직접 다운로드한 원본을 `Dwarf/` 시안에 사용합니다. 공식 출처는 https://sketchfab.com/3d-models/dwarf-blacksmith-e48bfff956ef483cb987890dc856c8b5 이며 CC BY 4.0(https://creativecommons.org/licenses/by/4.0/)입니다. 제작자·출처·라이선스와 변경 사항을 표시해야 합니다.

원본 FBX는 보존하고 별도 파생 FBX에 Humanoid/손가락 리깅을 추가했습니다. Unity URP용 재질 변환, 망치 연결 및 시점별 표시 처리를 적용했습니다. 모델링·텍스처를 이번에 제작했다고 주장하지 않습니다. 크레딧 문구·파일 해시·재생성·검증은 `Dwarf/README.md`를 참조하세요. Blender 포터블과 원본 ZIP은 Git 제외 임시 환경이며 에셋에 포함하지 않습니다.
