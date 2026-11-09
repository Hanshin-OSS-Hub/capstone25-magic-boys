# 엔딩 연출 복구

Unity Play를 종료한 뒤 아래 두 파일을 원래 경로로 복사하면 이번 작업 직전 상태로 돌아갑니다.

- `EndingScene.unity` → `Assets/Tower/EndingScene.unity`
- `EndingSequenceController.cs` → `Assets/Tower/Scripts/EndingSequenceController.cs`

추가된 `Assets/Tower/Scripts/TowerEndingCameraPath.cs` 및 `.meta`는 복구한 씬에서 참조하지 않으므로, 필요하면 함께 제거할 수 있습니다.

`manifest.json`에는 작업 전 다른 씬과 새 탑 모델·머티리얼·텍스처의 해시가 기록되어 있습니다. 원본 에셋은 수정하지 않습니다.
