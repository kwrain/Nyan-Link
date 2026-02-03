# 냥링크 (Nyan-Link) Unity 프로젝트 구성 프롬프트

## 프로젝트 개요
- **프로젝트명**: 냥링크 (Nyan Link)
- **장르**: 하이브리드 퍼즐 RPG (러너 & 시퀀스 퍼즐)
- **Unity 버전**: Unity 6.0+
- **플랫폼**: 모바일 (Android / iOS) - Portrait (세로 모드)
- **렌더링 파이프라인**: URP (Universal Render Pipeline) 2D

## 기술 스택 및 필수 패키지
- **Puzzle Rendering**: Unity Tilemap (Hexagonal Pointy-top)
- **UI System**: Unity UI Toolkit (UXML/USS)
- **Input System**: New Input System Package
- **비동기 처리**: Unity Awaitable 또는 UniTask
- **데이터 저장**: Newtonsoft.Json (암호화 래퍼 적용)
- **최적화**: UnityEngine.Pool.ObjectPool<T>

## 프로젝트 폴더 구조

```
Assets/
├── Scripts/
│   ├── Core/
│   │   ├── Enums/
│   │   │   ├── TileColor.cs
│   │   │   ├── TileType.cs
│   │   │   ├── ItemEffectType.cs
│   │   │   ├── BossBattleType.cs
│   │   │   ├── AttackType.cs
│   │   │   └── RunnerState.cs
│   │   ├── HexCoordinates/
│   │   │   └── HexCoordinates.cs (Offset <-> Cube 변환)
│   │   ├── Managers/
│   │   │   ├── GameManager.cs
│   │   │   ├── SceneManager.cs
│   │   │   └── AudioManager.cs
│   │   └── Utils/
│   │       ├── HexUtils.cs
│   │       └── CoordinateConverter.cs
│   ├── Puzzle/
│   │   ├── TileInstance.cs
│   │   ├── PuzzleBoardState.cs
│   │   ├── PuzzleBoardController.cs
│   │   ├── TileMatcher.cs
│   │   └── ItemSpawner.cs
│   ├── Runner/
│   │   ├── RunnerController.cs
│   │   ├── RunnerStateMachine.cs
│   │   ├── ParallaxController.cs
│   │   └── ObstacleSpawner.cs
│   ├── Boss/
│   │   ├── BossController.cs
│   │   ├── BossBattleConfigSO.cs (추상 클래스)
│   │   ├── SequenceBattleConfig.cs
│   │   ├── RhythmBattleConfig.cs
│   │   ├── ColorQuotaBattleConfig.cs
│   │   └── MemoryBattleConfig.cs
│   ├── Character/
│   │   ├── CharacterData.cs (ScriptableObject)
│   │   ├── CharacterStats.cs
│   │   └── CharacterController.cs
│   ├── Equipment/
│   │   ├── EquipmentItemData.cs (ScriptableObject)
│   │   ├── EquipmentSlot.cs
│   │   └── EquipmentManager.cs
│   ├── UI/
│   │   ├── BaseUIView.cs (추상 클래스)
│   │   ├── Screens/
│   │   │   ├── SplashScreen.cs
│   │   │   ├── LobbyScreen.cs
│   │   │   ├── GameScreen.cs
│   │   │   └── ResultScreen.cs
│   │   ├── Popups/
│   │   │   ├── PausePopup.cs
│   │   │   ├── ConfirmPopup.cs
│   │   │   └── RewardPopup.cs
│   │   └── HUD/
│   │       ├── StaminaBar.cs
│   │       ├── ScoreDisplay.cs
│   │       └── ItemSlotUI.cs
│   └── Data/
│       ├── ScriptableObjects/
│       │   ├── RunnerConfigSO.cs
│       │   ├── TileEffectData.cs
│       │   ├── GridShapeData.cs
│       │   ├── StageData.cs
│       │   └── UIThemeData.cs
│       └── SaveSystem/
│           ├── SaveData.cs
│           └── SaveManager.cs
├── Art/
│   ├── Sprites/
│   │   ├── Characters/
│   │   ├── Tiles/
│   │   ├── Items/
│   │   ├── Obstacles/
│   │   └── UI/
│   ├── Atlases/
│   │   ├── SpriteAtlas_UI.spriteatlas
│   │   └── SpriteAtlas_Game.spriteatlas
│   └── VFX/
├── UI/
│   ├── UXML/
│   │   ├── SplashScreen.uxml
│   │   ├── LobbyScreen.uxml
│   │   ├── GameScreen.uxml
│   │   └── ResultScreen.uxml
│   └── USS/
│       ├── Common.uss
│       ├── TopViewStyle.uss (레트로/픽셀 아트)
│       └── BottomViewStyle.uss (모던/플랫 디자인)
├── Scenes/
│   ├── 00_Splash.unity
│   ├── 01_Lobby.unity
│   ├── 02_Game.unity
│   └── 03_Result.unity
├── Prefabs/
│   ├── Characters/
│   ├── Tiles/
│   ├── Obstacles/
│   ├── VFX/
│   └── UI/
├── Settings/
│   ├── ProjectSettings/
│   ├── URP_2DRendererData.asset
│   └── InputSystem/
│       └── InputActions.inputactions
└── Resources/
    └── Data/
        ├── Characters/
        ├── Equipment/
        ├── Stages/
        └── Themes/
```

## 초기 설정 작업 순서

### 1단계: 프로젝트 기본 설정
1. Unity 6.0+ 프로젝트 생성
2. URP 2D 템플릿 선택 또는 기존 프로젝트에 URP 2D 추가
3. Project Settings 설정:
   - **Resolution**: 1080 x 1920 (Portrait)
   - **Orientation**: Portrait
   - **Target Platform**: Android / iOS

### 2단계: 필수 패키지 설치
1. **Package Manager**에서 다음 패키지 설치:
   - Input System (New Input System)
   - UI Toolkit
   - 2D Tilemap Extras (헥사곤 타일맵 지원)
   - Newtonsoft.Json (JSON Utility)

### 3단계: 폴더 구조 생성
위의 폴더 구조에 따라 모든 폴더와 기본 파일 생성

### 4단계: 핵심 시스템 구현
1. **Enums 정의** (Scripts/Core/Enums/)
   - TileColor, TileType, ItemEffectType, BossBattleType, AttackType, RunnerState

2. **HexCoordinates 시스템** (Scripts/Core/HexCoordinates/)
   - Offset 좌표 ↔ Cube 좌표 변환 로직
   - 인접 타일 탐색 유틸리티

3. **ScriptableObject 데이터 구조** (Scripts/Data/ScriptableObjects/)
   - RunnerConfigSO
   - TileEffectData
   - GridShapeData
   - StageData
   - CharacterData
   - EquipmentItemData
   - UIThemeData

4. **기본 매니저 클래스**
   - GameManager (싱글톤)
   - SceneManager
   - AudioManager

### 5단계: UI Toolkit 설정
1. **Panel Settings** 생성:
   - Scale Mode: Scale With Screen Size
   - Reference Resolution: 1080 x 1920
   - Match: Height 1.0 (세로형 게임)

2. **기본 UXML/USS 파일** 생성:
   - Common.uss (공통 스타일)
   - TopViewStyle.uss (레트로 스타일)
   - BottomViewStyle.uss (모던 스타일)

3. **BaseUIView 추상 클래스** 구현

### 6단계: 씬 구성
1. **Splash Scene** 생성:
   - Camera 설정
   - UI Toolkit Panel
   - SplashScreen UXML 연결

2. **Lobby Scene** 생성:
   - Camera 설정
   - UI Toolkit Panel
   - LobbyScreen UXML 연결

3. **Game Scene** 생성:
   - **상단 뷰 (40%)**: Camera 설정 (Viewport Rect: 0, 0.6, 1, 0.4)
   - **하단 뷰 (60%)**: UI Toolkit Panel (Viewport Rect: 0, 0, 1, 0.6)
   - Tilemap Grid (Hexagonal Pointy-top)
   - GameScreen UXML 연결

### 7단계: 타일맵 설정
1. **Grid 생성**:
   - Grid Component 추가
   - Cell Layout: Hexagonal
   - Cell Size: 적절한 크기 설정
   - Cell Gap: 0

2. **Tilemap 생성**:
   - Tilemap Component 추가
   - Tile Palette 생성 (헥사곤 타일)

### 8단계: Input System 설정
1. **InputActions.inputactions** 생성:
   - Touch Input Action Map
   - UI Input Action Map

2. **Input Manager** 스크립트 생성

## 구현 우선순위

### Phase 1: 핵심 인프라
1. ✅ 프로젝트 구조 및 폴더 생성
2. ✅ Enums 및 기본 데이터 구조
3. ✅ HexCoordinates 시스템
4. ✅ 기본 매니저 클래스
5. ✅ UI Toolkit 기본 설정

### Phase 2: 퍼즐 시스템
1. TileInstance 및 PuzzleBoardState
2. 타일 매칭 로직
3. 타일 제거 및 스폰 시스템
4. 아이템 생성 로직

### Phase 3: 러너 시스템
1. RunnerController 및 State Machine
2. Parallax 배경 시스템
3. 장애물 스폰 시스템
4. 충돌 판정

### Phase 4: 보스 전투
1. BossController 기본 구조
2. 각 전투 타입별 Config 구현
3. 보스 패턴 시스템

### Phase 5: UI 구현
1. BaseUIView 및 기본 화면들
2. HUD 시스템
3. 팝업 시스템
4. 결과 화면

### Phase 6: 성장 시스템
1. 캐릭터 데이터 및 스탯
2. 장비 시스템
3. 저장 시스템

## 주의사항 및 베스트 프랙티스

1. **성능 최적화**:
   - ObjectPool 사용 (타일, VFX, 장애물)
   - Sprite Atlas 활용
   - Draw Call 최소화

2. **모바일 최적화**:
   - 터치 영역 1.2배 확대
   - 터치 인식점 Y+ 오프셋
   - 해상도 대응 (Flex Layout 활용)

3. **코드 구조**:
   - ScriptableObject 기반 데이터 주도 설계
   - 싱글톤 패턴 (매니저 클래스)
   - 이벤트 시스템 활용

4. **UI Toolkit**:
   - 인라인 스타일 지양
   - USS Class 활용
   - Flex Layout으로 반응형 구현

5. **비동기 처리**:
   - Coroutine 대신 Awaitable/UniTask 사용
   - 보스 패턴, 튜토리얼 등에 활용

## 다음 단계
이 프롬프트를 기반으로 Unity MCP를 통해 프로젝트 구성 작업을 시작합니다. 각 단계별로 구현을 진행하며, 필요시 기획서를 참조하여 세부 사항을 보완합니다.


