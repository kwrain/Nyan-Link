# Phase 2 테스트 씬 설정 가이드

## 🎯 목적
Phase 2 퍼즐 시스템을 테스트하기 위한 씬을 구성합니다.

---

## 📋 빠른 설정 (자동)

### 방법 1: 에디터 메뉴 사용 (권장)

1. Unity 에디터에서 메뉴바 클릭: **NyanLink > Setup > Phase 2 Test Scene**
2. 자동으로 씬이 구성됩니다.
3. 다음 단계로 진행하세요.

---

## 📋 수동 설정 (상세)

### 1단계: 씬 준비

1. Unity 에디터에서 **File > New Scene** 또는 기존 씬 열기
2. 기존 오브젝트 정리 (필요시)

### 2단계: 카메라 설정

1. **Main Camera** 선택
2. Inspector에서 다음 설정:
   - **Projection**: Orthographic
   - **Size**: 10
   - **Position**: (0, 0, -10)
   - **Background**: 어두운 색상 (선택사항)

### 3단계: Grid 및 Tilemap 설정

#### 방법 A: Unity 메뉴 사용
1. Hierarchy에서 우클릭 → **2D Object → Tilemap → Hexagonal** 선택
2. 생성된 **Grid** 오브젝트 선택
3. Inspector에서 Grid 컴포넌트 설정:
   - **Cell Layout**: Hexagon
   - **Cell Size**: X: 1, Y: 1
   - **Gap**: X: 0, Y: 0

#### 방법 B: 수동 생성
1. 빈 GameObject 생성 (이름: "Grid")
2. **Grid** 컴포넌트 추가
3. 위 설정 적용

### 4단계: PuzzleBoardManager 설정

1. **Tilemap** 오브젝트 선택 (Grid의 자식)
2. 다음 컴포넌트 추가:
   - `PuzzleBoardManager`
   - `TileAnimation`
   - `TileInputHandler`
   - `TileMatcher`

3. **PuzzleBoardManager** Inspector 설정:
   - **Tilemap**: Tilemap 컴포넌트 자동 할당됨
   - **Hexagon Tile**: (나중에 타일 에셋 생성 후 할당)
   - **Grid Shape Data**: (다음 단계에서 생성)
   - **Tile Size**: 1
   - **Tile Spacing**: 0.1

4. **TileInputHandler** Inspector 설정:
   - **Board Manager**: PuzzleBoardManager 자동 할당됨
   - **Tile Animation**: TileAnimation 자동 할당됨

5. **TileMatcher** Inspector 설정:
   - **Board Manager**: PuzzleBoardManager 자동 할당됨
   - **Input Handler**: TileInputHandler 자동 할당됨

### 5단계: GridShapeData 생성

1. Project 창에서 **Resources/Data/GridShapes** 폴더로 이동
   - 폴더가 없으면 생성

2. 폴더에서 우클릭 → **Create > NyanLink > Grid Shape**

3. 이름을 **"DefaultGridShape"**로 설정

4. Inspector에서 설정:
   - **Width**: 7
   - **Height**: 7
   - **Shape Mask**: 기본값 사용 (모든 타일 활성화)

5. **PuzzleBoardManager**의 **Grid Shape Data**에 할당

### 6단계: GameManager 설정

1. 빈 GameObject 생성 (이름: "GameManager")
2. `GameManager` 컴포넌트 추가
3. Play 모드에서 자동으로 초기화됩니다.

### 7단계: 테스트

1. **Play** 버튼 클릭
2. 마우스로 타일을 클릭하고 드래그하여 매칭 테스트
3. 2개 이상의 같은 색상 타일을 연결하면 제거되고 새 타일이 생성됩니다.

---

## 🔧 문제 해결

### 문제: 타일이 보이지 않음
- **해결**: Hexagon Tile 에셋이 할당되지 않았을 수 있습니다. 임시로 기본 타일을 사용하거나, 타일 에셋을 생성하세요.

### 문제: 터치 입력이 작동하지 않음
- **해결**: 
  1. `TileInputHandler`의 `Board Manager`가 올바르게 할당되었는지 확인
  2. 카메라가 올바른 위치에 있는지 확인
  3. EventSystem이 씬에 있는지 확인 (UI Toolkit 사용 시 필요)

### 문제: 타일이 생성되지 않음
- **해결**: 
  1. `GridShapeData`가 올바르게 할당되었는지 확인
  2. `Shape Mask`가 모든 타일을 활성화하도록 설정되었는지 확인
  3. `PuzzleBoardManager`의 `Initialize()`가 호출되었는지 확인

### 문제: 애니메이션이 작동하지 않음
- **해결**: 
  1. `TileAnimation` 컴포넌트가 추가되었는지 확인
  2. `PuzzleBoardManager`의 `Tile Animation` 필드에 할당되었는지 확인
  3. 타일 GameObject가 올바르게 설정되었는지 확인

---

## 📝 다음 단계

테스트 씬이 정상적으로 작동하면:

1. ✅ 타일 스프라이트 에셋 생성
2. ✅ 타일 색상별 스프라이트 할당
3. ✅ 애니메이션 커브 조정
4. ✅ 터치 보정 값 조정
5. ✅ Phase 3 준비 (특수 아이템 시스템)

---

## 💡 참고사항

- **타일 에셋**: 현재는 기본 Unity 타일을 사용하지만, 나중에 커스텀 스프라이트로 교체 가능
- **애니메이션**: `TileAnimation` 컴포넌트에서 애니메이션 커브와 지속시간 조정 가능
- **디버그**: 각 컴포넌트의 `Debug Log` 옵션을 활성화하면 상세한 로그 확인 가능
