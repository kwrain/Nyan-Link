# Phase 2 테스트 씬 구성 요약

## 📋 테스트 씬 구성 전략

### 접근 방식: **Phase별 독립 테스트 씬**
각 Phase마다 해당 Phase의 기능만 테스트할 수 있는 최소한의 씬을 구성합니다.

**장점:**
- 각 Phase의 기능을 독립적으로 테스트 가능
- 버그 발생 시 해당 Phase에서만 집중 가능
- 새로운 팀원이 특정 Phase만 테스트하기 쉬움

**구성 방식:**
- Phase 2: 퍼즐 시스템만 테스트하는 씬
- Phase 3: Phase 2 + 특수 아이템 시스템
- Phase 4: Phase 3 + 러너 시스템
- ... (단계적으로 기능 추가)

---

## 🎯 현재 Phase 2 테스트 씬 구성

### 씬 구조
```
씬 구조:
├── Main Camera
│   └── Orthographic, Size: 10, Position: (0, 0, -10)
│
├── Grid
│   ├── Grid Component (Hexagon Layout)
│   │
│   └── Tilemap (자식)
│       ├── Tilemap Component
│       ├── TilemapRenderer Component
│       ├── PuzzleBoardManager ⭐ (그리드 생성/관리)
│       ├── TileAnimation ⭐ (애니메이션 처리)
│       ├── TileInputHandler ⭐ (입력 처리)
│       └── TileMatcher ⭐ (매칭 로직)
│
├── GameManager
│   ├── GameManager Component
│   └── PuzzleBoardInitializer ⭐ (자동 초기화)
│
└── EventSystem
    ├── EventSystem Component
    └── StandaloneInputModule Component
```

### 주요 컴포넌트

1. **PuzzleBoardManager**
   - 그리드 생성 및 관리
   - 타일 인스턴스 관리
   - 타일 스폰/제거

2. **TileAnimation**
   - 스폰/제거/선택 애니메이션 처리

3. **TileInputHandler**
   - 터치/드래그 입력 처리
   - 타일 선택 관리

4. **TileMatcher**
   - 타일 매칭 로직
   - 체인 검증 및 처리

5. **PuzzleBoardInitializer** (신규 추가)
   - 게임 시작 시 자동으로 그리드 초기화
   - GridShapeData가 없으면 기본 그리드 자동 생성

---

## ✅ Phase 2 요구사항 체크

### 1. 벌집형 그리드 생성 ✅
- [x] `PuzzleBoardManager`: 그리드 생성 및 관리
- [x] 홀수/짝수 열 교차 배치 로직
- [x] `GridShapeData`를 이용한 쉐이프 마스크 적용
- [x] **자동 초기화** (PuzzleBoardInitializer)

**테스트 방법:**
1. Play 버튼 클릭
2. 그리드가 자동으로 생성되는지 확인
3. GridShapeData 없으면 기본 7x7 그리드 생성

### 2. 타일 인스턴스 시스템 ✅
- [x] `TileInstance`: 타일 데이터 구조
- [ ] 타일 풀링 시스템 (ObjectPool 사용) ⚠️
  - **참고**: Phase 2 완료 기준에는 포함되지 않음
  - Phase 8에서 구현 예정

**테스트 방법:**
- 타일이 정상적으로 생성되고 관리되는지 확인

### 3. 입력 처리 ✅
- [x] `TileInputHandler`: 터치/드래그 입력 처리
- [x] 터치 보정 (Collider 1.3배, Y+ 오프셋)
- [x] 드래그 중 타일 선택/취소 로직
- [x] EventSystem 자동 추가

**테스트 방법:**
1. 마우스로 타일 클릭 및 드래그
2. 같은 색상 타일 연결 확인
3. 다른 색상 선택 시 이전 선택 취소 확인
4. Back-track (되돌아가기) 확인

### 4. 타일 매칭 로직 ✅
- [x] `TileMatcher`: 인접 타일 매칭 판정
- [x] 동일 속성 2개 이상 연결 시 매칭
- [x] 다른 타입 선택 시 이전 선택 취소
- [x] Back-track (되돌아가기) 처리

**테스트 방법:**
1. 2개 이상의 같은 색상 타일 연결
2. 매칭 완료 시 타일 제거 확인
3. 체인 길이에 따른 티어 판정 확인 (로그)

### 5. 타일 제거 및 스폰 ✅
- [x] 매칭된 타일 제거 애니메이션
- [x] 제자리 즉시 스폰 (중력 낙하 없음)
- [x] 새 타일 색상 랜덤 생성

**테스트 방법:**
1. 타일 제거 시 애니메이션 확인
2. 제거된 위치에 새 타일 즉시 스폰 확인
3. 새 타일 색상이 랜덤인지 확인

---

## 🎮 테스트 시나리오

### 기본 테스트
1. **씬 시작**
   - Play 버튼 클릭
   - 그리드가 자동으로 생성되는지 확인
   - 타일들이 랜덤 색상으로 생성되는지 확인

2. **타일 선택**
   - 마우스로 타일 클릭
   - 선택 애니메이션 확인 (스케일 증가)
   - 드래그하여 인접 타일 선택

3. **타일 매칭**
   - 같은 색상 2개 이상 연결
   - 매칭 완료 시 제거 애니메이션 확인
   - 새 타일 스폰 애니메이션 확인

4. **Back-track 테스트**
   - 여러 타일 선택 후 이전 타일로 되돌아가기
   - 선택 해제 애니메이션 확인

5. **다른 색상 선택**
   - 다른 색상 타일 선택 시 이전 선택 취소 확인

### 고급 테스트
1. **긴 체인 생성**
   - 5개 이상의 타일 연결
   - Middle Chain 티어 확인 (로그)
   - 9개 이상 연결 시 Long Chain 티어 확인

2. **터치 보정 테스트**
   - 타일 경계 근처 클릭 시 정확한 선택 확인
   - Y축 오프셋 보정 확인

---

## 📊 Phase 2 완료 기준 체크

로드맵의 완료 기준:
- [x] 벌집형 그리드가 정확히 생성됨 ✅
- [x] 드래그로 타일을 연결할 수 있음 ✅
- [x] 매칭된 타일이 제거되고 새 타일이 생성됨 ✅
- [x] 터치 보정이 적용되어 조작이 부드러움 ✅

**결론**: ✅ **Phase 2의 모든 핵심 기능을 테스트할 수 있습니다!**

---

## 🔧 개선 사항 (수정 완료)

### ✅ 수정 완료된 항목
1. **PuzzleBoardManager 자동 초기화**
   - `PuzzleBoardInitializer` 컴포넌트 추가
   - 게임 시작 시 자동으로 `Initialize()` 호출

2. **기본 GridShapeData 자동 생성**
   - GridShapeData가 없으면 기본 7x7 그리드 자동 생성
   - `createDefaultGridIfMissing` 옵션으로 제어 가능

3. **EventSystem 자동 추가**
   - 씬 구성 시 EventSystem 자동 생성
   - UI 입력 처리 지원

### ⏳ 향후 구현 예정
1. **타일 풀링 시스템**
   - Phase 8에서 구현 예정
   - 성능 최적화를 위한 선택 사항

---

## 💡 사용 방법

### 빠른 시작
1. Unity 에디터에서 메뉴: **NyanLink > Setup > Phase 2 Test Scene**
2. Play 버튼 클릭
3. 마우스로 타일 드래그하여 테스트

### 수동 설정
- `PHASE2_TEST_SCENE_SETUP.md` 참고

---

## 📝 다음 단계

1. ✅ 테스트 씬 구성 완료
2. ⏳ 실제 테스트 및 버그 수정
3. ⏳ 터치 보정 값 조정 (필요시)
4. ⏳ 타일 스프라이트 에셋 생성
5. ⏳ Phase 3 준비 (특수 아이템 시스템)
