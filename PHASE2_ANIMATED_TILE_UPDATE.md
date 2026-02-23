# Phase 2 AnimatedTile 시스템 업데이트

**작성일**: 2026-01-23  
**업데이트 내용**: AnimatedTile 시스템 구현 및 타일 위치 문제 해결

---

## 📋 업데이트 개요

Phase 2에 AnimatedTile 시스템을 추가하여 타일의 상태별 시각적 표현을 지원하도록 구현했습니다.

---

## ✅ 구현 완료 사항

### 1. AnimatedTile 에셋 생성 시스템

**구현 내용:**
- 18개 AnimatedTile 에셋 자동 생성 (6색상 × 3상태)
- 색상별 스프라이트 자동 생성 및 저장
- 상태별 애니메이션 속도 설정

**생성되는 타일:**
- `HexagonTile_{Color}_{State}.asset` 형식
- 예: `HexagonTile_Red_Normal`, `HexagonTile_Red_ItemLv1`, `HexagonTile_Red_ItemLv2`
- 총 18개 (Red, Blue, Yellow, Purple, Orange, Cyan × Normal, ItemLv1, ItemLv2)

**생성되는 스프라이트:**
- `HexagonSprite_{Color}.asset` 형식
- 기본 헥사곤 스프라이트에 색상 틴트 적용
- 총 6개 (각 색상별)

**사용 방법:**
- Unity 메뉴: `NyanLink/Setup/Create Animated Tiles`

---

### 2. TileState Enum 추가

**위치**: `Assets/_NyanLink/Scripts/Data/Enums/TileState.cs`

**정의:**
```csharp
public enum TileState
{
    Normal,      // 일반 상태 (아이템 효과 없음)
    ItemLv1,     // 아이템 Lv1 효과 보유
    ItemLv2      // 아이템 Lv2 효과 보유
}
```

**용도:**
- 타일의 상태를 나타내는 enum
- Phase 3 아이템 시스템에서 사용 예정

---

### 3. PuzzleBoardManager 업데이트

**추가된 기능:**
- `_tilesByColorAndState`: 색상별 × 상태별 타일 딕셔너리
- `autoLoadTiles`: 게임 시작 시 AnimatedTile 자동 로드
- `LoadAnimatedTiles()`: 에디터에서 AnimatedTile 자동 로드
- `GetTileByColorAndState()`: 색상과 상태에 따른 타일 반환
- `SetTileState()`: 타일 상태 변경 및 타일맵 업데이트
- `SpawnTile()`: 상태 지정 가능하도록 확장

**변경 사항:**
- 기존 `redTile`, `blueTile` 등 개별 필드 제거
- 딕셔너리 기반 타일 관리로 변경
- 상태별 타일 전환 지원

---

### 4. 타일 위치 문제 해결

**문제:**
- 타일이 월드 좌표 (0, 0, 0) 기준으로 보이지 않고 좌측 하단에 표시됨
- 로그에 출력되는 타일 중앙의 월드 포지션과 실제 타일 배치 위치 불일치

**원인:**
1. 스프라이트 생성 시 `baseSprite.rect`를 그대로 사용하여 새 텍스처와 불일치
2. `baseSprite.pivot`이 픽셀 단위인데 `Sprite.Create()`는 정규화된 값 필요
3. 타일맵 타일 앵커가 기본값 (0, 0, 0)으로 설정되어 스프라이트 pivot과 불일치

**해결:**
1. **스프라이트 rect 설정**: `new Rect(0, 0, width, height)` 사용 (전체 텍스처)
2. **Pivot 정규화**: `baseSprite.pivot`을 정규화된 값으로 변환
   ```csharp
   Vector2 normalizedPivot = new Vector2(
       baseSprite.pivot.x / baseSprite.rect.width,
       baseSprite.pivot.y / baseSprite.rect.height
   );
   ```
3. **타일맵 타일 앵커 설정**: 셀 중심 (0.5, 0.5, 0)으로 설정
   ```csharp
   tilemap.orientationMatrix = Matrix4x4.TRS(
       new Vector3(0.5f, 0.5f, 0f),
       Quaternion.identity,
       Vector3.one
   );
   ```

**결과:**
- ✅ 타일이 정확한 위치에 표시됨
- ✅ 로그의 월드 포지션과 실제 타일 위치 일치
- ✅ 중앙 타일 (0, 0, 0)이 월드 좌표 (0, 0, 0) 근처에 표시됨

---

### 5. 상세 디버그 로그 추가

**추가된 정보:**
- `CellCornerWorldPos`: 셀 좌측 하단 모서리 위치
- `CellCenterWorldPos`: 셀 중심 위치
- `Grid Position`: Grid GameObject의 transform position
- `Grid Cell Size`: Grid의 셀 크기
- `Tile Anchor`: 타일맵의 타일 앵커 위치
- `Sprite Pivot`: 실제 사용된 타일의 스프라이트 pivot
- `Sprite Rect`: 스프라이트의 rect 정보

**용도:**
- 타일 위치 문제 진단
- 그리드 설정 확인
- 스프라이트 설정 확인

---

## 🔧 기술적 세부 사항

### 스프라이트 생성 로직

**`CreateColoredSprite` 메서드:**
1. 기본 스프라이트의 텍스처 복사
2. 색상 틴트 적용 (픽셀 단위)
3. 새 텍스처 생성
4. 스프라이트 생성 시:
   - `rect`: `new Rect(0, 0, width, height)` (전체 텍스처)
   - `pivot`: 정규화된 값으로 변환
   - `pixelsPerUnit`: 원본과 동일

### 타일맵 설정

**`NyanLinkTestSceneSetup`에서:**
- Grid 생성: Hexagon Layout, Cell Size (1, 1, 0)
- Tilemap 생성 후 타일 앵커 설정:
  ```csharp
  tilemap.orientationMatrix = Matrix4x4.TRS(
      new Vector3(0.5f, 0.5f, 0f),
      Quaternion.identity,
      Vector3.one
  );
  ```

---

## 📝 파일 변경 사항

### 새로 생성된 파일
- `Assets/_NyanLink/Scripts/Data/Enums/TileState.cs`

### 수정된 파일
- `Assets/Editor/NyanLinkTestAssetsCreator.cs`
  - `CreateAnimatedTiles()` 메서드 추가
  - `CreateColoredSprite()` 메서드 추가 및 수정
  - `SetAnimatedTileProperties()` 메서드 추가
  - 스프라이트 생성 로직 개선
- `Assets/_NyanLink/Scripts/Puzzle/PuzzleBoardManager.cs`
  - AnimatedTile 관리 로직 추가
  - 상태별 타일 전환 지원
  - 상세 디버그 로그 추가
- `Assets/_NyanLink/Scripts/Puzzle/TileInstance.cs`
  - `State` 속성 추가
- `Assets/Editor/NyanLinkTestSceneSetup.cs`
  - 타일맵 타일 앵커 설정 추가

---

## 🎯 Phase 3 준비 사항

### 완료된 준비 사항
- ✅ 타일 상태 시스템 구현 (TileState enum)
- ✅ 상태별 AnimatedTile 에셋 생성
- ✅ 상태별 타일 전환 로직 구현
- ✅ 타일 위치 정확도 보장

### Phase 3에서 사용 예정
- 특수 아이템 타일 생성 시 `SetTileState()` 사용
- 아이템 효과 발동 시 상태 변경
- 상태별 시각적 표현 (현재는 애니메이션 속도만 다름)

---

## 💡 알려진 제한사항

1. **애니메이션 프레임**: 현재는 각 상태별로 동일한 스프라이트 사용 (애니메이션 속도만 다름)
   - Phase 3에서 상태별 다른 스프라이트 사용 예정

2. **런타임 로드**: 현재는 에디터에서만 AnimatedTile 자동 로드 가능
   - 런타임에서는 Resources 폴더 사용 또는 Inspector에서 할당 필요

---

## 📚 참고 문서

- `PHASE2_COMPLETION_SUMMARY.md`: Phase 2 전체 완료 요약
- `PROJECT_ROADMAP.md`: 전체 프로젝트 로드맵
- `PHASE3_START_GUIDE.md`: Phase 3 시작 가이드

---

**작성일**: 2026-01-23  
**작성자**: AI Assistant  
**검토 상태**: 사용자 확인 완료
