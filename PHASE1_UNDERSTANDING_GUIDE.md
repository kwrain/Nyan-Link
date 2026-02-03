# Phase 1 이해 가이드

## 📚 학습 순서 (반드시 이 순서대로 읽으세요!)

### 1단계: Enum 이해하기 (가장 쉬움)
### 2단계: 헥사곤 좌표계 시스템 (가장 중요하고 복잡함)
### 3단계: ScriptableObject 데이터 구조
### 4단계: 매니저 패턴 이해
### 5단계: 전체 데이터 흐름 이해

---

## 1단계: Enum 이해하기 ⭐ 쉬움

### 📁 위치
`Assets/_NyanLink/Scripts/Data/Enums/`

### 🎯 목적
게임에서 사용하는 **고정된 값들의 집합**을 정의합니다. 예를 들어 타일 색상은 Red, Blue, Yellow 등 6가지만 존재합니다.

### 📖 읽어야 할 파일
1. `TileColor.cs` - 타일 색상 (6가지)
2. `ItemEffectType.cs` - 아이템 효과 타입 (6가지)
3. `EquipmentGrade.cs` - 장비 등급 (5가지)
4. `EquipmentType.cs` - 장비 타입 (3가지)
5. `BossBattleType.cs` - 보스 전투 타입 (4가지)
6. `RunnerState.cs` - 러너 상태 (7가지)

### 💡 핵심 개념
- **Enum은 선택지의 목록**입니다
- `TileColor.Red`처럼 사용합니다
- `switch` 문에서 자주 사용됩니다

### ✅ 이해 체크리스트
- [ ] 각 Enum이 무엇을 나타내는지 이해했는가?
- [ ] 왜 이 값들만 존재하는지 이해했는가?
- [ ] 코드에서 `TileColor.Red`처럼 사용할 수 있는가?

---

## 2단계: 헥사곤 좌표계 시스템 ⭐⭐⭐ 가장 중요!

### 📁 위치
`Assets/_NyanLink/Scripts/Core/HexCoordinates/`

### 🎯 목적
**헥사곤(6각형) 타일의 위치를 표현하고 계산**하는 시스템입니다. 이 부분이 가장 복잡하지만 **반드시 이해해야 합니다!**

### 📖 읽어야 할 파일 (순서대로!)
1. `HEX_COORDINATES_EXPLANATION.md` (프로젝트 루트) - **먼저 이것부터 읽으세요!**
2. `HexCoordinates.cs` - 좌표 변환 및 기본 연산
3. `HexUtils.cs` - 유틸리티 함수들

### 💡 핵심 개념

#### 1. 왜 두 가지 좌표계가 필요한가?

**Offset 좌표계 (Unity Tilemap)**
- Unity가 타일을 화면에 그릴 때 사용
- `Vector3Int(0, 0, 0)` 같은 형태
- 홀수/짝수 열마다 인접 타일 위치가 다름 (복잡함!)

**Cube 좌표계 (게임 로직)**
- 게임 로직 계산에 사용
- `HexCoordinates(q, r, s)` 형태
- 항상 `q + r + s = 0` (수학적으로 깔끔함!)
- 인접 타일 찾기가 간단함!

#### 2. 변환 과정

```
Unity Tilemap (Offset) → HexCoordinates (Cube) → 게임 로직 계산 → Offset → Unity Tilemap
```

**실제 사용 예시:**
```csharp
// 1. Unity에서 Offset 좌표 얻기
Vector3Int offset = tilemap.WorldToCell(touchPosition);

// 2. Cube 좌표로 변환
HexCoordinates cube = HexCoordinates.OffsetToCube(offset);

// 3. 인접 타일 찾기 (게임 로직)
HexCoordinates[] neighbors = cube.GetNeighbors();

// 4. 다시 Offset으로 변환해서 Unity에 적용
Vector3Int neighborOffset = neighbors[0].ToOffset();
tilemap.SetTile(neighborOffset, newTile);
```

#### 3. 주요 메서드 이해하기

**`HexCoordinates.OffsetToCube(Vector3Int offset)`**
- Unity의 Offset 좌표 → Cube 좌표 변환
- **언제 사용?** Unity Tilemap에서 좌표를 가져왔을 때

**`hex.ToOffset()`**
- Cube 좌표 → Unity의 Offset 좌표 변환
- **언제 사용?** 계산 후 Unity Tilemap에 적용할 때

**`hex.GetNeighbors()`**
- 6방향 인접 타일 좌표 배열 반환
- **언제 사용?** 타일 매칭 판정, 드래그 연결 확인

**`HexCoordinates.Distance(a, b)`**
- 두 타일 간의 거리 계산
- **언제 사용?** Blast 아이템 범위 계산, Line Clear 범위 확인

**`HexUtils.GetLine(from, to)`**
- 두 타일 사이의 직선 경로 반환
- **언제 사용?** Line Clear 아이템 효과

**`HexUtils.GetRange(center, radius)`**
- 중심에서 반경 내의 모든 타일 반환
- **언제 사용?** Blast 아이템 효과

### ⚠️ 주의사항
- **절대 Unity Tilemap의 Offset 좌표로 게임 로직을 계산하지 마세요!**
- 항상 Cube 좌표로 변환한 후 계산하고, 다시 Offset으로 변환하세요!
- 홀수/짝수 열 처리는 `HexCoordinates`가 자동으로 처리합니다.

### ✅ 이해 체크리스트
- [ ] Offset 좌표와 Cube 좌표의 차이를 설명할 수 있는가?
- [ ] 왜 Cube 좌표가 게임 로직에 더 적합한지 이해했는가?
- [ ] `OffsetToCube`와 `ToOffset`의 사용 시점을 알 수 있는가?
- [ ] `GetNeighbors()`가 왜 중요한지 이해했는가?
- [ ] 실제 코드에서 좌표 변환을 할 수 있는가?

---

## 3단계: ScriptableObject 데이터 구조 ⭐⭐ 중요

### 📁 위치
`Assets/_NyanLink/Scripts/Data/Definitions/`

### 🎯 목적
**게임 데이터를 외부에서 관리**할 수 있게 합니다. 코드를 수정하지 않고도 게임 밸런스를 조정할 수 있습니다!

### 📖 읽어야 할 파일 (중요도 순)
1. `BalanceData.cs` - **가장 중요!** 모든 수치의 중심
2. `GridShapeData.cs` - 그리드 모양 정의
3. `StageData.cs` - 스테이지 정보
4. `CharacterData.cs` - 캐릭터 스탯
5. `EquipmentData.cs` - 장비 정보
6. `BossBattleConfig.cs` - 보스 전투 설정
7. `LootTableData.cs` - 전리품 테이블
8. `TileEffectData.cs` - 타일 효과 수치

### 💡 핵심 개념

#### 1. ScriptableObject란?
- Unity에서 **데이터를 저장하는 에셋**입니다
- 코드를 수정하지 않고도 **Inspector에서 값을 변경**할 수 있습니다
- 게임 실행 중에도 변경 가능합니다 (에디터에서)

#### 2. 데이터 흐름

```
ScriptableObject 에셋 생성 → Resources 폴더에 저장 → DataManager가 로드 → 게임에서 사용
```

#### 3. BalanceData의 역할
**모든 수치의 중심!** 다른 ScriptableObject들도 BalanceData를 참조할 수 있습니다.

**예시:**
```csharp
// BalanceData에서 체인 티어 확인
BalanceData balance = DataManager.Instance.BalanceData;
BalanceData.ChainTier tier = balance.GetChainTier(chainLength);

if (tier == BalanceData.ChainTier.Long)
{
    // Long Chain 처리
}
```

#### 4. 에디터에서 생성하기
1. Unity 에디터에서 `Assets` → `Create` → `NyanLink` → `Data` → 원하는 데이터 선택
2. 생성된 에셋을 `Assets/Resources/Data/` 하위 폴더에 저장
3. `DataManager`가 자동으로 로드합니다

### ✅ 이해 체크리스트
- [ ] ScriptableObject가 무엇인지 이해했는가?
- [ ] 왜 코드 대신 ScriptableObject를 사용하는지 이해했는가?
- [ ] BalanceData가 모든 수치의 중심이라는 것을 이해했는가?
- [ ] 에디터에서 ScriptableObject를 생성할 수 있는가?
- [ ] Resources 폴더 구조를 이해했는가?

---

## 4단계: 매니저 패턴 이해 ⭐⭐ 중요

### 📁 위치
`Assets/_NyanLink/Scripts/Core/Managers/`

### 🎯 목적
게임의 **전역 상태와 데이터를 관리**합니다. 싱글톤 패턴을 사용하여 어디서든 접근 가능합니다.

### 📖 읽어야 할 파일
1. `GameManager.cs` - 게임 상태 관리
2. `DataManager.cs` - 데이터 로드 및 관리

### 💡 핵심 개념

#### 1. 싱글톤 패턴
**하나의 인스턴스만 존재**하도록 보장하는 패턴입니다.

이 프로젝트에서는 `Singleton<T>` 베이스 클래스를 상속받아 구현합니다.

```csharp
// 어디서든 접근 가능
GameManager.Instance.ChangeState(GameState.InGame);
BalanceData balance = DataManager.Instance.BalanceData;

// 베이스 클래스 상속
public class GameManager : Singleton<GameManager>
{
    protected override void Awake()
    {
        base.Awake();
    }
    
    protected override async void Start()
    {
        base.Start();
        await Initialize();
    }
    
    public override async Task Initialize()
    {
        // 초기화 로직
    }
}
```

#### 2. GameManager의 역할
- 게임 상태 관리 (Menu, Lobby, InGame, Paused 등)
- 다른 매니저들의 초기화
- 씬 전환 관리

#### 3. DataManager의 역할
- Resources 폴더에서 ScriptableObject 로드
- ID 기반으로 데이터 검색
- 게임 시작 시 모든 데이터 미리 로드

**사용 예시:**
```csharp
// 스테이지 데이터 가져오기
StageData stage = DataManager.Instance.GetStageData("stage_01");

// 캐릭터 데이터 가져오기
CharacterData character = DataManager.Instance.GetCharacterData("cat_01");

// 밸런싱 데이터 가져오기
BalanceData balance = DataManager.Instance.BalanceData;
```

#### 4. 초기화 순서
```
GameManager.Instance 접근
    ↓
GameManager.Awake() (base.Awake() 호출)
    ↓
GameManager.Start() (base.Start() 호출)
    ↓
GameManager.Initialize() (비동기)
    ↓
DataManager.Instance.Initialize() (비동기)
    ↓
모든 데이터 로드 완료
```

**중요:** `Initialize()`는 `async Task`로 구현되며, `Start()`에서 `await`로 호출됩니다.

### ✅ 이해 체크리스트
- [ ] 싱글톤 패턴이 무엇인지 이해했는가?
- [ ] `Instance`를 통해 접근하는 방식을 이해했는가?
- [ ] GameManager와 DataManager의 역할을 구분할 수 있는가?
- [ ] DataManager에서 데이터를 가져오는 방법을 알 수 있는가?

---

## 5단계: 전체 데이터 흐름 이해 ⭐⭐⭐ 통합 이해

### 🎯 전체 흐름도

```
[Unity 에디터]
    ↓
[ScriptableObject 생성] (Assets/Create/NyanLink/Data/)
    ↓
[Resources 폴더에 저장] (Assets/Resources/Data/)
    ↓
[게임 시작]
    ↓
[GameManager 초기화]
    ↓
[DataManager 초기화]
    ↓
[모든 ScriptableObject 로드]
    ↓
[게임 플레이]
    ↓
[데이터 사용]
    - BalanceData: 밸런싱 수치
    - StageData: 스테이지 정보
    - CharacterData: 캐릭터 스탯
    - EquipmentData: 장비 정보
    - HexCoordinates: 타일 위치 계산
```

### 💡 실제 사용 시나리오

#### 시나리오 1: 타일 매칭 판정
```csharp
// 1. 터치 위치를 Offset 좌표로 변환
Vector3Int offset = tilemap.WorldToCell(touchPosition);

// 2. Cube 좌표로 변환
HexCoordinates cube = HexCoordinates.OffsetToCube(offset);

// 3. 인접 타일 확인
HexCoordinates[] neighbors = cube.GetNeighbors();
foreach (var neighbor in neighbors)
{
    // 인접 타일과 색상 비교
    if (IsSameColor(cube, neighbor))
    {
        // 매칭!
    }
}
```

#### 시나리오 2: 체인 티어 판정
```csharp
// 1. 체인 길이 확인
int chainLength = matchedTiles.Count;

// 2. BalanceData에서 티어 확인
BalanceData balance = DataManager.Instance.BalanceData;
BalanceData.ChainTier tier = balance.GetChainTier(chainLength);

// 3. 티어에 따라 아이템 생성
if (tier == BalanceData.ChainTier.Middle)
{
    // Lv.1 아이템 생성
}
else if (tier == BalanceData.ChainTier.Long)
{
    // Lv.2 아이템 생성
}
```

#### 시나리오 3: 스테이지 시작
```csharp
// 1. 스테이지 데이터 가져오기
StageData stage = DataManager.Instance.GetStageData("stage_01");

// 2. 그리드 쉐이프 가져오기
GridShapeData gridShape = stage.gridShape;

// 3. 그리드 생성
CreateGrid(gridShape);

// 4. 보스 설정 가져오기
BossBattleConfig bossConfig = stage.bossConfig;
```

### ✅ 최종 이해 체크리스트
- [ ] 전체 데이터 흐름을 설명할 수 있는가?
- [ ] ScriptableObject가 어떻게 로드되고 사용되는지 이해했는가?
- [ ] 헥사곤 좌표계가 실제로 어떻게 사용되는지 이해했는가?
- [ ] 매니저들이 어떻게 협력하는지 이해했는가?
- [ ] 새로운 데이터를 추가하는 방법을 알 수 있는가?

---

## 🚨 반드시 이해해야 할 핵심 포인트

### 1. 헥사곤 좌표계 (가장 중요!)
- **Offset 좌표 = Unity 렌더링용**
- **Cube 좌표 = 게임 로직용**
- **항상 변환해서 사용!**

### 2. ScriptableObject = 외부 데이터 관리
- 코드 수정 없이 밸런싱 가능
- Resources 폴더에 저장
- DataManager가 로드

### 3. BalanceData = 모든 수치의 중심
- 다른 시스템들이 BalanceData를 참조
- 체인 티어, 점수, 아이템 효과 등 모든 수치 포함

### 4. 싱글톤 패턴 = 전역 접근
- `GameManager.Instance`
- `DataManager.Instance`
- 어디서든 접근 가능

---

## 📝 다음 단계 (Phase 2 준비)

Phase 1을 완전히 이해했다면:
1. 헥사곤 좌표계를 직접 테스트해보세요
2. ScriptableObject를 생성하고 값을 변경해보세요
3. DataManager가 데이터를 제대로 로드하는지 확인하세요

**Phase 2에서는 이 데이터 구조를 사용하여 실제 퍼즐 시스템을 구현합니다!**

---

## ❓ 질문이 있다면?

다음 항목들을 확인해보세요:
1. `HEX_COORDINATES_EXPLANATION.md` - 헥사곤 좌표계 상세 설명
2. 각 ScriptableObject 파일의 주석
3. 코드 내부의 `/// <summary>` 주석

**이해가 안 되는 부분이 있으면 해당 파일을 다시 읽어보세요!**
