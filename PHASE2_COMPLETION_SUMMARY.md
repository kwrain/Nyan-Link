# Phase 2 완료 요약 및 작업 내역

## 📋 Phase 2 작업 개요

**작업 기간**: 2026-01-23  
**상태**: ✅ **완료**

**목표**: 헥사곤 타일 퍼즐의 핵심 조작 및 매칭 시스템 구현

---

## ✅ 구현 완료된 핵심 기능

### 1. 벌집형 그리드 생성 시스템 (`PuzzleBoardManager`)

**구현 내용:**
- Unity Tilemap 기반 Pointy-top 헥사곤 그리드 생성
- `GridShapeData`를 이용한 스테이지별 그리드 쉐이프 지원
- 홀수/짝수 열 교차 배치 로직 (짝수 열에 타일 1개 추가로 균형 맞춤)
- 중앙 타일 (0, 0, 0) 기준 그리드 배치
- 카메라 자동 중앙 배치 기능
- **AnimatedTile 자동 로드 및 상태별 타일 관리**

**주요 메서드:**
- `Initialize()`: 그리드 초기화 및 생성
- `CreateGrid()`: 그리드 생성 (GridShapeData 기반)
- `SpawnTile()`: 새 타일 스폰 (상태 지정 가능)
- `RemoveTile()`: 타일 제거
- `SetTileState()`: 타일 상태 변경 및 타일맵 업데이트
- `GetTileAtOffset()`: Offset 좌표로 타일 조회
- `GetCellCenterWorld()`: 셀 중심 월드 위치 계산
- `GetTileByColorAndState()`: 색상과 상태에 따른 AnimatedTile 반환
- `LoadAnimatedTiles()`: 에디터에서 AnimatedTile 자동 로드

**기술적 결정:**
- ✅ **Unity Tilemap Offset 좌표계 직접 사용** (HexCoordinates 제거)
- ✅ Pointy-top 헥사곤 배치 방식 채택
- ✅ 타일 크기와 간격을 Inspector에서 조정 가능
- ✅ `centerOffsetX = -(width / 2) - 1` 공식으로 중앙 정렬
- ✅ **AnimatedTile을 딕셔너리로 관리** (`Dictionary<TileColor, Dictionary<TileState, TileBase>>`)
- ✅ **타일맵 타일 앵커 설정** (셀 중심: 0.5, 0.5, 0)
- ✅ **상세 디버그 로그** (타일 위치, 스프라이트 정보, 타일 앵커 등)

---

### 2. 헥사곤 좌표계 유틸리티 (`HexOffsetUtils`)

**구현 내용:**
- Pointy-top 헥사곤을 위한 Offset 좌표계 기반 유틸리티
- 인접 타일 탐색 (6방향)
- 거리 계산 (BFS 기반)
- 인접성 검증

**주요 메서드:**
- `GetNeighbors(Vector3Int offset)`: 인접 타일 6개 반환
- `GetDistance(Vector3Int from, Vector3Int to)`: 두 타일 간 거리 계산
- `IsAdjacent(Vector3Int offset1, Vector3Int offset2)`: 인접성 검증

**기술적 결정:**
- ✅ **Unity Tilemap Offset 좌표계 직접 사용** (Cube 좌표계 제거)
- ✅ `IsAdjacent`에서 양방향 검증으로 대칭성 보장
- ✅ `GetDistance`는 `GetNeighbors`를 기반으로 BFS 구현

**수정 이력:**
- 인접 타일 계산의 대칭성 문제 해결 (A가 B의 인접이면 B도 A의 인접)
- 짝수/홀수 열에 따른 인접 타일 계산 정확도 개선

---

### 3. 타일 인스턴스 시스템 (`TileInstance`)

**구현 내용:**
- 타일 데이터 구조 정의
- Offset 좌표 기반 위치 관리
- 타일 색상, 선택 상태, 활성화 상태 관리
- **타일 상태 관리 (TileState enum 추가)**

**주요 속성:**
- `OffsetPosition`: Unity Tilemap Offset 좌표 (Vector3Int)
- `Color`: 타일 색상 (TileColor enum)
- `State`: 타일 상태 (TileState enum: Normal, ItemLv1, ItemLv2)
- `IsSelected`: 선택 상태
- `IsActive`: 활성화 상태

**기술적 결정:**
- ✅ Offset 좌표만 사용 (Cube 좌표 제거)
- ✅ GameObject 참조는 나중에 추가 가능하도록 설계
- ✅ 풀링 시스템을 고려한 구조 (Phase 8에서 구현 예정)
- ✅ 타일 상태별 AnimatedTile 지원 (Phase 3 아이템 시스템 준비)

---

### 4. 입력 처리 시스템 (`TileInputHandler`)

**구현 내용:**
- 터치/드래그 입력 처리
- 마우스 및 터치 입력 지원
- 인접 타일만 선택 가능하도록 검증
- 같은 색상 타일만 연결 가능
- Back-track (되돌아가기) 처리

**주요 기능:**
- `OnInputBegan()`: 터치 시작 처리
- `OnInputMoved()`: 드래그 중 처리 (마지막 선택 타일 기준 인접 검증)
- `OnInputReleased()`: 터치 종료 및 매칭 처리
- `GetTileAtScreenPosition()`: 스크린 좌표로 타일 조회

**기술적 결정:**
- ✅ 터치 보정: 마우스 이동 민감도 설정 (기본값: 0.1)
- ✅ `OnInputBegan`과 `OnInputMoved` 중복 호출 방지
- ✅ 마지막으로 체인에 추가된 타일 기준으로 인접 타일 검증
- ✅ UI 위 터치는 무시

**수정 이력:**
- 중복 호출 방지 로직 추가
- 마지막 선택 타일 기준 인접 검증으로 변경
- 불필요한 범위 검색 제거

---

### 5. 타일 매칭 로직 (`TileMatcher`)

**구현 내용:**
- 인접 타일 매칭 판정
- 동일 색상 2개 이상 연결 시 매칭
- 다른 색상 선택 시 이전 선택 취소
- Back-track 처리
- 체인 유효성 검증

**주요 기능:**
- `ProcessMatch()`: 매칭 처리 및 타일 제거
- `RemoveMatchedTiles()`: 매칭된 타일 제거
- `SpawnNewTiles()`: 새 타일 스폰 (랜덤 색상)
- `GetRandomTileColor()`: 랜덤 색상 생성 (6가지 색상 모두 사용)

**기술적 결정:**
- ✅ 최소 매칭 개수: 2개 (설정 가능)
- ✅ 모든 색상 랜덤 생성 (Red, Blue, Yellow, Purple, Orange, Cyan)
- ✅ 제자리 즉시 스폰 (중력 낙하 없음)
- ✅ Phase 3에서 특수 아이템 생성 로직 추가 예정

**특수 타일 생성 로직 (Phase 3 준비):**
- 체인 조건 만족 시 마지막 타일 위치에 동일 색상의 특수 타일 생성
- 나머지 공간에는 랜덤 일반 타일 생성
- 특수 타일은 시각적으로 다르게 표현 예정

---

### 6. 시각적 피드백 시스템 (`TileSelectionVisualizer`)

**구현 내용:**
- 선택된 타일 체인 연결선 표시
- LineRenderer를 사용한 직선 연결
- 흐르는 효과 애니메이션 (선택적)
- 타일 위에 표시되도록 렌더링 순서 조정

**주요 기능:**
- `UpdateConnectionLine()`: 연결선 업데이트
- `CreateConnectionLine()`: 연결선 생성
- `ClearConnectionLine()`: 연결선 제거
- `UpdateFlowAnimation()`: 흐르는 효과 애니메이션

**기술적 결정:**
- ✅ **연결선만 구현** (아웃라인 효과는 제거됨)
- ✅ 연결선은 2개 이상 타일 선택 시 표시
- ✅ 마우스 포인터 위치의 미리보기 타일까지 연결
- ✅ 터치 종료 시 연결선 제거
- ✅ Z 위치 조정 (-0.1) 및 View 모드로 타일 위에 표시
- ✅ TilemapRenderer와 동일한 Sorting Layer 사용

**렌더링 순서 문제 해결:**
- LineRenderer의 Z 위치를 타일맵보다 앞에 배치 (-0.1)
- `alignment = LineAlignment.View` 설정
- 2D 렌더링용 Material 사용 (`Sprites/Default`)
- Shadow casting 비활성화

---

## 🔄 주요 변경 사항 및 수정 이력

### 1. AnimatedTile 시스템 구현 (최신)

**구현 내용:**
- 18개 AnimatedTile 에셋 자동 생성 (6색상 × 3상태)
- 색상별 스프라이트 생성 로직 (`CreateColoredSprite`)
- PuzzleBoardManager에서 상태별 타일 관리
- 타일 상태 변경 시 타일맵 자동 업데이트

**기술적 도전:**
- 스프라이트 rect와 pivot 설정 문제로 타일 위치 불일치 발생
- 타일맵 타일 앵커와 스프라이트 pivot 불일치

**해결:**
- `CreateColoredSprite`에서 pivot을 정규화된 값으로 변환
- 타일맵 타일 앵커를 셀 중심 (0.5, 0.5, 0)으로 설정
- 스프라이트 생성 시 rect를 전체 텍스처 크기로 설정
- 상세 디버그 로그 추가로 문제 진단 용이

**결과:**
- ✅ 타일이 정확한 위치에 표시됨
- ✅ 상태별 타일 전환 정상 작동
- ✅ Phase 3 아이템 시스템 준비 완료

---

### 2. 좌표계 변경: HexCoordinates → Unity Offset 좌표계

**변경 이유:**
- Unity Tilemap의 네이티브 좌표계 사용으로 변환 오류 방지
- 코드 단순화 및 유지보수성 향상

**영향 범위:**
- `HexOffsetUtils`: Offset 좌표계 기반으로 재작성
- `TileInstance`: Cube 좌표 제거, Offset만 사용
- `PuzzleBoardManager`: Offset 좌표 직접 사용
- `TileInputHandler`: Offset 좌표 기반 검증

**결과:**
- ✅ 좌표 변환 오류 완전 제거
- ✅ 인접 타일 계산 정확도 향상
- ✅ 코드 복잡도 감소

---

### 2. 인접 타일 계산 대칭성 보장

**문제:**
- `IsAdjacent` 함수에서 A가 B의 인접이지만 B가 A의 인접이 아닌 경우 발생
- 짝수/홀수 열에 따라 인접 타일 계산 불일치

**해결:**
- `IsAdjacent`에서 양방향 검증 구현
- `GetNeighbors` 함수의 정확도 개선

**결과:**
- ✅ 모든 좌표에서 인접성 검증 정확도 100%
- ✅ 거리 계산 정확도 향상

---

### 3. 입력 처리 최적화

**문제:**
- `OnInputBegan`과 `OnInputMoved`에서 동일 타일이 중복 선택됨
- 불필요한 범위 검색으로 성능 저하

**해결:**
- 마우스 이동 민감도 설정으로 중복 호출 방지
- 마지막 선택 타일 기준으로 인접 타일만 검증
- `GetTileAtScreenPosition`은 정확한 위치의 타일만 반환

**결과:**
- ✅ 중복 선택 문제 해결
- ✅ 성능 개선
- ✅ 조작성 향상

---

### 4. 시각적 피드백 구현 및 정리

**구현:**
- ConnectionLine 구현 (타일 체인 연결선)
- 아웃라인 효과는 제거 (Phase 3에서 재검토 예정)

**기술적 도전:**
- LineRenderer가 Tilemap보다 뒤에 표시되는 문제
- Sorting Layer/Order 설정만으로는 해결되지 않음

**해결:**
- Z 위치 조정 (-0.1)으로 타일 위에 표시
- 2D 렌더링 모드 설정 (`LineAlignment.View`)
- 2D용 Material 사용

**결과:**
- ✅ 연결선이 타일 위에 정확히 표시됨
- ✅ 시각적 피드백 제공

---

## 📊 Phase 2 완료 기준 체크

로드맵의 완료 기준:
- [x] ✅ 벌집형 그리드가 정확히 생성됨
- [x] ✅ 드래그로 타일을 연결할 수 있음
- [x] ✅ 매칭된 타일이 제거되고 새 타일이 생성됨
- [x] ✅ 터치 보정이 적용되어 조작이 부드러움

**추가 완료 사항:**
- [x] ✅ 시각적 피드백 (연결선) 구현
- [x] ✅ 좌표계 통일 (Offset 좌표계)
- [x] ✅ 인접 타일 계산 정확도 개선
- [x] ✅ 입력 처리 최적화

---

## 🎨 테스트 에셋 생성

### 에디터 스크립트 (`NyanLinkTestAssetsCreator`)

**생성 기능:**
- `CreatePhase2TestAssets()`: Phase 2 테스트 에셋 자동 생성
  - 기본 GridShapeData 생성
  - 헥사곤 스프라이트 자동 생성 (`HexagonTexture.asset`에 포함)
- `CreateAnimatedTiles()`: AnimatedTile 에셋 자동 생성
  - 18개 AnimatedTile 생성 (6색상 × 3상태)
  - 색상별 스프라이트 자동 생성 및 저장

**생성되는 에셋:**
- `Resources/Data/GridShapes/DefaultGridShape.asset`
- `_NyanLink/Art/Tiles/HexagonTexture.asset` (HexagonSprite 포함)
- `_NyanLink/Art/Tiles/HexagonSprite_Red.asset` (색상별 스프라이트)
- `_NyanLink/Art/Tiles/HexagonSprite_Blue.asset`
- `_NyanLink/Art/Tiles/HexagonSprite_Yellow.asset`
- `_NyanLink/Art/Tiles/HexagonSprite_Purple.asset`
- `_NyanLink/Art/Tiles/HexagonSprite_Orange.asset`
- `_NyanLink/Art/Tiles/HexagonSprite_Cyan.asset`
- `_NyanLink/Art/Tiles/HexagonTile_Red_Normal.asset` (AnimatedTile)
- `_NyanLink/Art/Tiles/HexagonTile_Red_ItemLv1.asset`
- `_NyanLink/Art/Tiles/HexagonTile_Red_ItemLv2.asset`
- ... (총 18개 AnimatedTile: 6색상 × 3상태)

**사용 방법:**
- Unity 메뉴: `NyanLink/Setup/Create Phase 2 Test Assets` (기본 에셋 생성)
- Unity 메뉴: `NyanLink/Setup/Create Animated Tiles` (AnimatedTile 생성)

**AnimatedTile 생성 로직:**
- 기본 헥사곤 스프라이트에 색상 틴트 적용하여 색상별 스프라이트 생성
- 각 색상별 스프라이트를 AnimatedTile의 `m_AnimatedSprites` 배열에 할당
- 상태별 애니메이션 속도 설정 (Normal: 1.0, ItemLv1: 0.5, ItemLv2: 1.5)
- 스프라이트의 rect와 pivot을 올바르게 설정하여 타일 위치 정확도 보장

---

## 📁 주요 파일 구조

```
Assets/_NyanLink/Scripts/Puzzle/
├── PuzzleBoardManager.cs      # 그리드 생성 및 관리 (AnimatedTile 지원)
├── TileInstance.cs             # 타일 데이터 구조 (TileState 추가)
├── HexOffsetUtils.cs           # Offset 좌표계 유틸리티
├── TileInputHandler.cs         # 입력 처리
├── TileMatcher.cs               # 타일 매칭 로직
└── TileSelectionVisualizer.cs  # 시각적 피드백

Assets/_NyanLink/Scripts/Data/
├── Enums/
│   ├── TileColor.cs            # 타일 색상 enum
│   └── TileState.cs            # 타일 상태 enum (Normal, ItemLv1, ItemLv2)
└── Definitions/
    └── GridShapeData.cs        # 그리드 쉐이프 정의

Assets/_NyanLink/Scripts/Core/
└── HexCoordinates/             # (사용 안 함, 레거시)

Assets/Editor/
└── NyanLinkTestAssetsCreator.cs # 테스트 에셋 생성 스크립트 (AnimatedTile 생성 포함)
└── NyanLinkTestSceneSetup.cs    # 테스트 씬 구성 스크립트 (타일 앵커 설정 포함)
```

---

## 🔧 기술 스택 및 아키텍처

### 사용 기술
- **Unity Tilemap**: 헥사곤 그리드 렌더링
- **LineRenderer**: 연결선 시각화
- **ScriptableObject**: 데이터 구조 (GridShapeData)
- **AnimationCurve**: 타일 애니메이션 (선택적)

### 아키텍처 패턴
- **Manager 패턴**: PuzzleBoardManager, TileMatcher
- **Handler 패턴**: TileInputHandler
- **Visualizer 패턴**: TileSelectionVisualizer
- **Singleton 패턴**: GameManager (Phase 1에서 구현)

---

## 📝 로드맵 반영 사항

### Phase 2 업데이트 필요 사항

1. **시각적 피드백 추가**
   - ✅ 연결선 구현 완료
   - ⏳ 아웃라인 효과는 Phase 3에서 재검토

2. **좌표계 변경**
   - ✅ Unity Offset 좌표계 직접 사용
   - ✅ HexCoordinates 제거

3. **특수 타일 생성 로직 명확화**
   - ✅ Phase 3에서 마지막 타일 위치에 동일 색상 특수 타일 생성
   - ✅ 나머지 공간에는 랜덤 일반 타일 생성

4. **모든 색상 랜덤 생성**
   - ✅ 6가지 색상 모두 랜덤 생성 (Red, Blue, Yellow, Purple, Orange, Cyan)
   - ✅ 색상별 등장 확률 조정 구조 준비 완료

---

## 🚀 다음 단계 (Phase 3)

### Phase 3 준비 사항
1. ✅ 특수 타일 생성 로직 구조 준비 완료
2. ⏳ 특수 타일 시각적 표현 (아트 작업)
3. ⏳ 아이템 효과 구현
4. ⏳ 아이템 사용 UI 구현

### Phase 3 주요 작업
- 체인 티어 판정 (Middle: 5~8개, Long: 9개 이상)
- 특수 아이템 생성 로직
- 아이템 효과 구현 (6가지 타입)
- 아이템 사용 UI

---

## 💡 알려진 제한사항 및 향후 개선

### Phase 8에서 구현 예정
- [ ] 타일 풀링 시스템 (ObjectPool)
- [ ] 성능 최적화
- [ ] Draw Call 최적화

### 선택적 개선 사항
- [ ] 타일 제거 애니메이션 (현재 즉시 제거)
- [ ] 타일 스폰 애니메이션 (현재 즉시 스폰)
- [ ] 디버그 모드 개선
- [ ] 애니메이션 커브 세밀 조정

---

## 📚 참고 문서

- `PROJECT_ROADMAP.md`: 전체 프로젝트 로드맵
- `PHASE2_TEST_SCENE_SETUP.md`: 테스트 씬 설정 가이드
- `PHASE2_TEST_ASSETS_GUIDE.md`: 테스트 에셋 생성 가이드
- `HEX_COORDINATES_EXPLANATION.md`: 좌표계 설명 (레거시)

---

**작성일**: 2026-01-23  
**작성자**: AI Assistant  
**검토 상태**: 사용자 확인 필요
