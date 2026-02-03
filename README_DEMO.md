# Nyan-Link 데모 설정 가이드

## 프로젝트 개요
Nyan-Link는 하이브리드 퍼즐 RPG 게임으로, 상단의 러너 액션과 하단의 헥사곤 퍼즐이 결합된 게임입니다.

## 데모 씬 설정 방법

### 1. 씬 준비
1. Unity 에디터에서 `Assets/Scenes/SampleScene.unity`를 엽니다.
2. 기존 오브젝트를 정리합니다.

### 2. 카메라 설정
1. Main Camera를 선택합니다.
2. Projection을 **Orthographic**으로 설정합니다.
3. Size를 **10**으로 설정합니다.
4. Position을 **(0, 0, -10)**으로 설정합니다.

### 3. Grid 및 Tilemap 설정
1. Hierarchy에서 우클릭 → **2D Object → Tilemap → Hexagonal** 선택
2. 생성된 Grid 오브젝트를 선택합니다.
3. Grid 컴포넌트에서:
   - Cell Layout: **Hexagon**
   - Cell Size: **X: 1, Y: 1**
   - Gap: **X: 0, Y: 0**
4. Tilemap 오브젝트에 `PuzzleBoardManager` 컴포넌트를 추가합니다.
5. Tilemap 오브젝트에 `TileInputHandler` 컴포넌트를 추가합니다.

### 4. 러너 시스템 설정
1. 상단 뷰용 빈 GameObject를 생성합니다 (이름: "RunnerView").
2. Position을 **(0, 3, 0)**으로 설정합니다.
3. 하위에 Sprite를 생성하여 고양이 캐릭터로 사용합니다 (임시로 Quad 사용 가능).
4. RunnerView에 `RunnerController` 컴포넌트를 추가합니다.
5. RunnerView에 Rigidbody2D와 Collider2D를 추가합니다.

### 5. GameManager 설정
1. 빈 GameObject를 생성합니다 (이름: "GameManager").
2. `GameManager` 컴포넌트를 추가합니다.
3. Inspector에서:
   - Puzzle Board: Tilemap 오브젝트 할당
   - Runner: RunnerView 오브젝트 할당
   - HUD: (나중에 UI 추가 시 할당)

### 6. Grid Shape 데이터 생성
1. Project 창에서 우클릭 → **Create → NyanLink → GridShape**
2. 이름을 "DefaultGridShape"로 설정합니다.
3. Width: **7**, Height: **8**로 설정합니다.
4. PuzzleBoardManager의 Default Shape에 할당합니다.

### 7. Runner Config 생성
1. Project 창에서 우클릭 → **Create → NyanLink → RunnerConfig**
2. 이름을 "DefaultRunnerConfig"로 설정합니다.
3. RunnerController의 Config에 할당합니다.

### 8. 입력 시스템 설정
1. `Assets/InputSystem_Actions.inputactions` 파일을 엽니다.
2. 새로운 Action Map을 생성합니다 (이름: "Gameplay").
3. Action을 추가합니다:
   - 이름: "Touch"
   - Action Type: **Value**
   - Control Type: **Vector2**
   - Binding: **Touch → Primary Touch Position**
4. Action을 추가합니다:
   - 이름: "TouchEnd"
   - Action Type: **Button**
   - Binding: **Touch → Primary Touch Press**

### 9. 테스트
1. Play 버튼을 눌러 게임을 실행합니다.
2. 마우스 클릭으로 타일을 선택하고 드래그하여 매칭합니다.
3. 2개 이상의 타일을 연결하면 제거되고 새 타일이 생성됩니다.

## 주요 기능

### 구현 완료
- ✅ 헥사곤 좌표계 시스템 (Offset ↔ Cube 변환)
- ✅ 퍼즐 보드 생성 및 타일 관리
- ✅ 타일 매칭 및 제거 시스템
- ✅ 러너 기본 상태 머신 (Run, Jump, Slide, Stumble)
- ✅ 패럴랙스 배경 시스템
- ✅ 기본 UI 구조 (UI Toolkit 기반)
- ✅ 게임 매니저 및 스태미나 시스템

### 향후 구현 필요
- ⏳ 특수 아이템 시스템
- ⏳ 보스 전투 시스템
- ⏳ UI Toolkit UXML/USS 파일
- ⏳ 실제 스프라이트 아트
- ⏳ 사운드 시스템
- ⏳ 데이터 저장 시스템

## 폴더 구조
```
Assets/
├── Scripts/
│   ├── Core/          # 핵심 시스템 (Enums, HexCoordinates, GameManager)
│   ├── Puzzle/        # 퍼즐 시스템
│   ├── Runner/        # 러너 시스템
│   ├── UI/            # UI 시스템
│   └── Data/          # 데이터 구조
├── Data/              # ScriptableObject 에셋
├── Prefabs/           # 프리팹
├── UI/                # UI Toolkit 파일 (UXML, USS)
└── Art/               # 아트 리소스
```

## 참고사항
- Unity 6.0.2 이상 필요
- URP (Universal Render Pipeline) 사용
- New Input System 사용
- UI Toolkit 사용 (기존 uGUI 대신)

