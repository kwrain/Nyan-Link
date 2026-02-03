# Phase 2 테스트 에셋 생성 가이드

## 🎯 목적
Phase 2 테스트에 필요한 기본 에셋을 자동으로 생성합니다.

---

## 📋 필요한 에셋

### 1. GridShapeData (그리드 쉐이프 데이터)
- **위치**: `Resources/Data/GridShapes/DefaultGridShape.asset`
- **용도**: 벌집형 그리드 모양 정의
- **기본값**: 7x7 그리드, 모든 타일 활성화

### 2. 헥사곤 타일 에셋 (TileBase)
- **위치**: `Assets/_NyanLink/Art/Tiles/DefaultHexagonTile.asset`
- **용도**: Unity Tilemap에서 렌더링할 타일
- **참고**: 스프라이트는 나중에 할당 필요

---

## 🚀 빠른 생성 방법

### 방법 1: 에디터 메뉴 사용 (권장)

1. Unity 에디터에서 메뉴바 클릭: **NyanLink > Setup > Create Phase 2 Test Assets**
2. 자동으로 다음이 생성됩니다:
   - 기본 GridShapeData
   - 기본 헥사곤 타일

### 방법 2: 테스트 씬 구성 시 자동 생성

1. **NyanLink > Setup > Phase 2 Test Scene** 실행
2. 씬 구성 완료 후 "에셋 생성" 버튼 클릭
3. 자동으로 테스트 에셋 생성

---

## 📝 수동 생성 방법

### GridShapeData 생성

1. Project 창에서 `Resources/Data/GridShapes` 폴더 선택
2. 우클릭 → **Create > NyanLink > Grid Shape**
3. 이름을 "DefaultGridShape"로 변경
4. Inspector에서 설정:
   - **Width**: 7
   - **Height**: 7
   - **Shape Mask**: 모든 타일 활성화 (기본값)

### 헥사곤 타일 생성

#### 방법 A: 기본 Tile 사용
1. Project 창에서 `Assets/_NyanLink/Art/Tiles` 폴더 선택
2. 우클릭 → **Create > Tile**
3. 이름을 "DefaultHexagonTile"로 변경
4. **주의**: 스프라이트를 할당해야 타일이 보입니다

#### 방법 B: ColoredHexagonTile 사용 (프로그래밍 방식)
1. Project 창에서 `Assets/_NyanLink/Art/Tiles` 폴더 선택
2. 우클릭 → **Create > NyanLink > Tiles > Colored Hexagon Tile**
3. 이름을 "DefaultHexagonTile"로 변경
4. Inspector에서 **Tile Color** 설정 가능

---

## 🔧 PuzzleBoardManager에 할당

### 1. 씬에서 Tilemap 오브젝트 선택

### 2. PuzzleBoardManager 컴포넌트 확인

### 3. Inspector에서 할당:
- **Hexagon Tile**: `Assets/_NyanLink/Art/Tiles/DefaultHexagonTile.asset`
- **Grid Shape Data**: `Resources/Data/GridShapes/DefaultGridShape.asset`

---

## 🎨 타일 스프라이트 할당 (선택사항)

현재는 빈 타일이 생성되므로, 타일이 보이지 않을 수 있습니다.

### 스프라이트 할당 방법:

1. **DefaultHexagonTile** 선택
2. Inspector에서 **Sprite** 필드에 헥사곤 스프라이트 할당
3. 또는 임시로 기본 스프라이트 사용:
   - Unity 기본 스프라이트 (예: Circle, Square 등)
   - 나중에 실제 헥사곤 스프라이트로 교체

### 스프라이트 없이 테스트하는 방법:

- 타일은 보이지 않지만 기능은 정상 작동합니다
- 터치/드래그 입력은 정상 작동
- 매칭 로직도 정상 작동
- 시각적 피드백만 없을 뿐입니다

---

## ✅ 생성 확인

### 생성된 파일 확인:

```
Assets/
├── Resources/
│   └── Data/
│       └── GridShapes/
│           └── DefaultGridShape.asset ✅
│
└── _NyanLink/
    └── Art/
        └── Tiles/
            └── DefaultHexagonTile.asset ✅
```

### 에셋이 제대로 생성되었는지 확인:

1. Project 창에서 위 경로 확인
2. 에셋을 선택하여 Inspector에서 내용 확인
3. PuzzleBoardManager에 할당 가능한지 확인

---

## 🐛 문제 해결

### 문제: 에셋이 생성되지 않음
- **해결**: 폴더 경로가 올바른지 확인
- **해결**: Unity 에디터를 재시작

### 문제: 타일이 보이지 않음
- **해결**: 타일 스프라이트 할당 필요
- **해결**: TilemapRenderer 컴포넌트 확인

### 문제: 그리드가 생성되지 않음
- **해결**: GridShapeData가 올바르게 할당되었는지 확인
- **해결**: PuzzleBoardInitializer가 작동하는지 확인

---

## 📝 다음 단계

1. ✅ 테스트 에셋 생성 완료
2. ⏳ PuzzleBoardManager에 에셋 할당
3. ⏳ Play 버튼으로 테스트
4. ⏳ 타일 스프라이트 추가 (선택사항)

---

## 💡 참고사항

- **GridShapeData**: 다양한 그리드 모양을 만들 수 있습니다 (Create > NyanLink > Grid Shape)
- **타일**: 여러 색상의 타일을 만들 수 있습니다 (Create > NyanLink > Tiles > Colored Hexagon Tile)
- **스프라이트**: 나중에 아트 작업 시 실제 헥사곤 스프라이트로 교체 가능
