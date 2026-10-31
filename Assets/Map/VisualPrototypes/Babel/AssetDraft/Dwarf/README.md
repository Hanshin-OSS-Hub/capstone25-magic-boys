# 바벨탑 드워프 대장장이 비교 시안

선택 모델: **Dwarf Blacksmith — JanPohl**. 사용자 다운로드 원본으로 2026-10-31 작업.

## 출처와 변경

- 공식 페이지: https://sketchfab.com/3d-models/dwarf-blacksmith-e48bfff956ef483cb987890dc856c8b5
- 라이선스: CC BY 4.0 https://creativecommons.org/licenses/by/4.0/ . 제작자와 출처를 표기하고 변경 사실을 표시한다.
- 발표 크레딧: `Dwarf Blacksmith by JanPohl (Sketchfab), CC BY 4.0. Rigging, Unity materials and gameplay adaptation for Magic Boys.`
- 사용자가 제공한 `dwarf-blacksmith.zip` SHA256: `C61D1106DFE796BCF660495C0DAB5A75C3B20E8E969C3735CC5DB1CCF62439B6`.
- `Source/Dwarf.fbx`는 내부 원본 ZIP의 FBX를 보존한다. 원본은 뼈대와 애니메이션이 없는 정적 메시였다. 원래부터 게임플레이용으로 리깅됐다고 주장하지 않는다.
- `Rigged/Dwarf_Humanoid.fbx`는 별도 파생 파일이다. Blender 4.5.3에서 드워프 비율에 맞춘 52개 관절(손가락 포함), 몸·바지·장갑의 자동 스킨과 장비별 웨이트를 추가하고 T 자세로 바인딩했다. 전신 비율을 사람에 맞춰 늘이거나 줄이지 않았다.
- `Textures/`는 원본 BaseColor/Normal/Metallic/Roughness를 보존한다. `*_MetallicSmoothness.png`만 URP용 R=Metallic, A=1-Roughness 변환물이다. 원본 Height 맵은 사용하지 않는다.
- 재생성 스크립트: `../../Editor/rig_dwarf.py`. Blender 포터블·ZIP·임시 분석·로그는 `WhisperServer/.runtime/`에 있으며 Git에서 제외된다. 전역 Blender/Python 설치는 변경하지 않았다.

## 비교 씬과 구조

- 씬: `Assets/Map/VisualPrototypes/Babel/AssetDraft/Dwarf/Babel_Dwarf_Compare.unity`
- 플레이어 프리팹: `Player_Babel_Dwarf.prefab`
- 승인된 외형은 실제 `0326_MapMerge` 씬에도 연결했다. 미채택 PlainHuman/Runesmith 시안은 미참조 파일을 정리했다. 음성·포털 코드는 보존한다.
- 게임플레이 Animator/Avatar/이벤트는 기존 플레이어가 담당한다. 드워프 Animator는 Controller 없이 비활성화하고 `BabelDwarfMotion`이 Humanoid 자세를 전달한다.
- 망치는 선택 에셋의 메시/텍스처로 교체하고 1.2배 균일 확대한다. 기존 망치 객체와 공격 참조를 유지하며, 시안에서 오른손 체형 차이만큼 위치를 보정한다. 공격 판정의 위치에 영향이 있으므로 실제 타격은 수동 확인 대상이다.
- 1인칭에서는 팔·손·망치를 표시하고, 얼굴·수염·몸통·장비·넓은 장갑 소매는 그림자만 남긴다. 3인칭에서는 전체 외형을 표시한다. 원본 방 충돌과 문 위치는 변경하지 않는다.

렌더 중 발견한 수정 사항: 보호구/털 장비를 해당 쪽 어깨에 연결했고, 허리 경계의 골반 웨이트가 팔까지 적용되지 않도록 제한했다. 원본 손가락 끝 위치를 분석해 손가락 관절을 추가했다. 수동 제작 리그이므로 모든 극단적인 자세의 교차/겹침이 해결됐다는 보장은 하지 않는다.

## 확인 순서

1. 위 씬을 열고 Play. F9 정면 관찰/플레이 복귀, V 1/3인칭, F8 원본 마네킹/드워프 비교.
2. F9 관찰 모드를 해제하고 Game 화면을 클릭한다. 3인칭에서 WASD 걷기 → 마우스 왼쪽 클릭 망치 공격을 확인한다. 손·어깨·앞치마·무릎의 심한 늘어남이나 분리를 우선 확인한다.
3. Ctrl 구르기와 점프, 1인칭 시야, 숫자 1 및 서버 실행 후 Q 실제 음성 주문을 확인한다.
4. F6 기존 방/꾸민 방, F10 밝은/어두운 조명, F7 시작 위치 복귀.

## 자료와 검사

원본 정적 FBX와 별도 리깅 FBX, 원본 텍스처를 보존했다. `../../Editor/rig_dwarf.py`는 Blender 4.5.3으로 리깅 FBX를 재생성한 스크립트다. 이미 생성된 FBX를 플레이하는 데 Blender는 필요 없다. 기존 여러 시안을 순서대로 생성하던 도구는 미채택 시안과 함께 정리했다.

현재 비교 씬 자동 검사 진입점은 `BabelDwarfVerification.Run`이다. Unity 배치 실행 인수 `-batchmode -noaudio -force-d3d11 -projectPath <저장소경로> -executeMethod BabelDwarfVerification.Run -logFile <로그경로>`로 실행하며 완료 후 Editor가 종료된다. 실제 던전 검사는 `BabelGameplayVerification.Run`이다.

왼손은 대기 상태에서 관절 회전으로만 망치 잡기를 보정한다. 공격/이동에서는 보정을 해제한다. 손가락 접촉, 수염/앞치마/어깨 장비 겹침, 실제 타격은 수동 확인 대상이다.

아래는 통합 전 시안 검증의 기록이다. 당시 캡처는 정리 시 로컬 `.runtime/retired-visuals`로 옮겼다. 실제 게임 통합의 최신 결과는 `Assets/Map/BabelGameplay/VERIFICATION.md`에 별도로 남긴다.
## 2026-10-31 최종 검증 기록

왼손 보정까지 포함해 Unity 6000.4.0f1에서 `BuildDwarfAndVerify` 실행 종료 코드 **0**을 확인했다. 최종 로그는 `dwarf-grip-final.log`다.

- 자동 확인: 유효한 Humanoid, 시작 CharacterController 접지, 기존 게임플레이 Avatar/바닥 충돌 메시 유지, 새 인물 Collider 0, 시안 메시와 리깅 FBX의 스킨 데이터 일치.
- 이동·90도 회전 전후 골반 로컬 위치 약 `(0, 0.72, 0.02)` 유지. 걷기 포즈 전달, 공격 트리거 후 렌더, 구르기 20%/50% 포즈 샘플, 1인칭 가림 및 F8 외형 전환 통과.
- 직접 시각 확인: `Previews/01_Front.png`, `02_Side.png`, `05_Walk.png`, `06_Attack.png`, `09_FirstPerson_Idle.png`, 구르기 캡처. 확인한 프레임에서 보호구 분리/팔의 심한 늘어남은 사라졌고, 대기 자세의 왼손 위치가 손잡이에 가까워졌다. 스크린샷은 카메라 렌더로 HUD를 포함하지 않는다.
- 미검증: 실제 WASD/마우스 입력, 구르기 이동과 충돌, 실제 타격 판정, 숫자 1/Q 마이크 주문, 실제 던전/포털 왕복, 발표 PC 성능. 애니메이션 포즈 렌더를 실제 게임플레이 검증으로 보고하지 않는다.
- 남은 육안 확인: 극단적인 자세의 수염/앞치마/어깨 장비 겹침, 손가락과 손잡이의 정밀 접촉, 1인칭에서 큰 망치가 가리는 화면 비율. 완성형 상용 리그 수준으로 검증한 것은 아니다.

이번 생성·검사에서 컴파일 오류나 테스트 예외는 없었다. 기존 프로젝트의 메타 파일/중복 DLL/Unity 서비스 경고는 별개로 남아 있다.
