# 사용되지 않는 스크립트 분석

## 📋 분석 기준
- **Phase 2 완료 시점** 기준으로 분석
- 실제 코드에서 참조되는지 확인
- Phase 3 이후에서 사용 예정인 스크립트는 별도 표시

---

## ✅ Phase 2에서 사용되는 스크립트

### Puzzle 시스템 핵심
- ✅ `PuzzleBoardManager.cs` - 그리드 생성 및 관리
- ✅ `TileInstance.cs` - 타일 데이터 구조
- ✅ `HexOffsetUtils.cs` - Offset 좌표계 유틸리티
- ✅ `TileInputHandler.cs` - 입력 처리
- ✅ `TileMatcher.cs` - 타일 매칭 로직
- ✅ `TileSelectionVisualizer.cs` - 시각적 피드백 (연결선)
- ✅ `PuzzleBoardInitializer.cs` - 자동 초기화

### 데이터 구조
- ✅ `GridShapeData.cs` - 그리드 쉐이프 정의
- ✅ `TileColor.cs` (Enum) - 타일 색상 정의

### 유틸리티
- ✅ `HexGridCalculator.cs` - 에디터에서만 사용 (Cell Size 계산)

### 에디터 스크립트
- ✅ `NyanLinkTestAssetsCreator.cs` - 테스트 에셋 생성
- ✅ `NyanLinkTestSceneSetup.cs` - 테스트 씬 구성

---

## ❌ Phase 2에서 사용되지 않는 스크립트

### 레거시 (제거 권장)
1. **`HexCoordinates.cs`** ❌
   - 위치: `Assets/_NyanLink/Scripts/Core/HexCoordinates/HexCoordinates.cs`
   - 이유: Phase 2에서 Unity Tilemap Offset 좌표계로 전환하여 사용 안 함
   - 상태: 레거시 코드, 삭제 가능

2. **`HexUtils.cs`** ❌
   - 위치: `Assets/_NyanLink/Scripts/Core/HexCoordinates/HexUtils.cs`
   - 이유: HexCoordinates를 사용하므로 함께 사용 안 함
   - 상태: 레거시 코드, 삭제 가능

### Phase 2에서 제거된 기능
3. **`TileAnimation.cs`** ❌
   - 위치: `Assets/_NyanLink/Scripts/Puzzle/TileAnimation.cs`
   - 이유: Phase 2에서 타일 애니메이션 기능 제거됨
   - 상태: 현재 사용 안 함, Phase 3에서 재검토 예정

### Phase 3 이후에서 사용 예정
4. **`BalanceData.cs`** ⏳
   - 위치: `Assets/_NyanLink/Scripts/Data/Definitions/BalanceData.cs`
   - 이유: Phase 3 (특수 아이템 시스템)에서 사용 예정
   - 상태: 현재 비어있음, Phase 3에서 구현 예정

5. **`DataManager.cs`** ⏳
   - 위치: `Assets/_NyanLink/Scripts/Core/Managers/DataManager.cs`
   - 이유: Phase 2에서 사용 안 함, Phase 3 이후에서 사용 예정
   - 상태: BalanceData 로드 코드 주석 처리됨

### Phase 4 이후에서 사용 예정
6. **`CharacterData.cs`** ⏳
   - 위치: `Assets/_NyanLink/Scripts/Data/Definitions/CharacterData.cs`
   - 이유: Phase 6 (성장 시스템)에서 사용 예정

7. **`EquipmentData.cs`** ⏳
   - 위치: `Assets/_NyanLink/Scripts/Data/Definitions/EquipmentData.cs`
   - 이유: Phase 6 (성장 시스템)에서 사용 예정

8. **`StageData.cs`** ⏳
   - 위치: `Assets/_NyanLink/Scripts/Data/Definitions/StageData.cs`
   - 이유: Phase 4 이후에서 사용 예정

9. **`BossBattleConfig.cs`** ⏳
   - 위치: `Assets/_NyanLink/Scripts/Data/Definitions/BossBattleConfig.cs`
   - 이유: Phase 5 (보스 전투 시스템)에서 사용 예정

10. **`LootTableData.cs`** ⏳
    - 위치: `Assets/_NyanLink/Scripts/Data/Definitions/LootTableData.cs`
    - 이유: Phase 6 (성장 시스템)에서 사용 예정

11. **`TileEffectData.cs`** ⏳
    - 위치: `Assets/_NyanLink/Scripts/Data/Definitions/TileEffectData.cs`
    - 이유: Phase 3 (특수 아이템 시스템)에서 사용 예정

### Enum (Phase 3 이후에서 사용 예정)
12. **`ItemEffectType.cs`** ⏳
    - 위치: `Assets/_NyanLink/Scripts/Data/Enums/ItemEffectType.cs`
    - 이유: Phase 3에서 사용 예정

13. **`RunnerState.cs`** ⏳
    - 위치: `Assets/_NyanLink/Scripts/Data/Enums/RunnerState.cs`
    - 이유: Phase 4 (러너 시스템)에서 사용 예정

14. **`BossBattleType.cs`** ⏳
    - 위치: `Assets/_NyanLink/Scripts/Data/Enums/BossBattleType.cs`
    - 이유: Phase 5 (보스 전투 시스템)에서 사용 예정

15. **`EquipmentType.cs`** ⏳
    - 위치: `Assets/_NyanLink/Scripts/Data/Enums/EquipmentType.cs`
    - 이유: Phase 6 (성장 시스템)에서 사용 예정

16. **`EquipmentGrade.cs`** ⏳
    - 위치: `Assets/_NyanLink/Scripts/Data/Enums/EquipmentGrade.cs`
    - 이유: Phase 6 (성장 시스템)에서 사용 예정

### Core 시스템
17. **`Singleton.cs`** ⏳
    - 위치: `Assets/_NyanLink/Scripts/Core/Singleton/Singleton.cs`
    - 이유: DataManager에서 사용하지만 Phase 2에서 DataManager 사용 안 함
    - 상태: Phase 3 이후에서 사용 예정

### 에디터 스크립트 (Phase 3 이후용)
18. **`NyanLinkBalanceDataInitializer.cs`** ⏳
    - 위치: `Assets/Editor/NyanLinkBalanceDataInitializer.cs`
    - 이유: BalanceData 생성용, Phase 3 이후에서 사용 예정
    - 상태: 현재 비활성화됨 (주석 처리)

---

## 🗑️ 삭제 권장 스크립트

다음 스크립트는 Phase 2에서 사용되지 않으며, 레거시 코드이므로 삭제를 권장합니다:

1. **`HexCoordinates.cs`** - Offset 좌표계로 전환하여 더 이상 사용 안 함
2. **`HexUtils.cs`** - HexCoordinates를 사용하므로 함께 사용 안 함

**주의**: 삭제하기 전에 Git에 커밋하여 나중에 필요시 복구할 수 있도록 하세요.

---

## 📝 정리 요약

### Phase 2에서 실제 사용되는 스크립트: **9개**
- Puzzle 시스템: 7개
- 데이터 구조: 2개 (GridShapeData, TileColor)
- 유틸리티: 1개 (HexGridCalculator - 에디터용)

### Phase 2에서 사용되지 않는 스크립트: **18개**
- 레거시 (삭제 권장): 2개
- Phase 2에서 제거된 기능: 1개
- Phase 3 이후에서 사용 예정: 15개

---

## 💡 권장 사항

1. **레거시 코드 삭제**
   - `HexCoordinates.cs`와 `HexUtils.cs`는 삭제 권장
   - Git에 커밋 후 삭제하여 코드베이스 정리

2. **Phase 3 이후 스크립트 유지**
   - Phase 3 이후에서 사용될 스크립트는 유지
   - 현재 사용 안 하더라도 나중에 필요하므로 삭제하지 않음

3. **TileAnimation 재검토**
   - Phase 2에서 제거되었지만, Phase 3에서 재검토 예정
   - 현재는 유지하되 사용하지 않음

---

**작성일**: 2026-01-23
