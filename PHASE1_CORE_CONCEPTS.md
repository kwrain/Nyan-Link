# Phase 1 핵심 개념 상세 설명

## 🎯 이 문서의 목적
Phase 1에서 **반드시 이해해야 할 핵심 개념**을 코드 예시와 함께 자세히 설명합니다.

---

## 1. 헥사곤 좌표계: 왜 이렇게 복잡한가? 🤔

### 문제 상황
Unity Tilemap은 **Offset 좌표계**를 사용합니다. 하지만 헥사곤 타일의 게임 로직을 구현하려면 **Cube 좌표계**가 훨씬 효율적입니다.

### 왜 두 가지 좌표계가 필요한가?

#### Offset 좌표계의 문제점
```csharp
// Offset 좌표로 인접 타일 찾기 (복잡함!)
Vector3Int[] GetNeighbors(Vector3Int offset)
{
    if (offset.y % 2 == 0)  // 짝수 열
    {
        return new Vector3Int[] {
            new Vector3Int(offset.x - 1, offset.y - 1, 0),
            new Vector3Int(offset.x, offset.y - 1, 0),
            // ... 복잡한 조건문
        };
    }
    else  // 홀수 열 (다른 패턴!)
    {
        return new Vector3Int[] {
            new Vector3Int(offset.x, offset.y - 1, 0),
            new Vector3Int(offset.x + 1, offset.y - 1, 0),
            // ... 또 다른 복잡한 조건문
        };
    }
}
```

**문제점:**
- 홀수/짝수 열마다 다른 로직 필요
- 버그 발생 가능성 높음
- 코드 가독성 낮음

#### Cube 좌표계의 장점
```csharp
// Cube 좌표로 인접 타일 찾기 (간단함!)
HexCoordinates[] GetNeighbors(HexCoordinates cube)
{
    return new HexCoordinates[] {
        new HexCoordinates(cube.q + 1, cube.r, cube.s - 1),     // 항상 동일한 패턴!
        new HexCoordinates(cube.q + 1, cube.r - 1, cube.s),
        new HexCoordinates(cube.q, cube.r - 1, cube.s + 1),
        new HexCoordinates(cube.q - 1, cube.r, cube.s + 1),
        new HexCoordinates(cube.q - 1, cube.r + 1, cube.s),
        new HexCoordinates(cube.q, cube.r + 1, cube.s - 1)
    };
}
```

**장점:**
- 항상 동일한 패턴
- 홀수/짝수 구분 불필요
- 코드가 간결하고 명확

### 실제 사용 패턴 (암기하세요!)

```csharp
// ✅ 올바른 사용법
void ProcessTileTouch(Vector3Int touchOffset)
{
    // 1. Offset → Cube 변환
    HexCoordinates cube = HexCoordinates.OffsetToCube(touchOffset);
    
    // 2. 게임 로직 계산 (Cube 좌표 사용)
    HexCoordinates[] neighbors = cube.GetNeighbors();
    int distance = cube.DistanceTo(targetCube);
    
    // 3. Cube → Offset 변환
    Vector3Int resultOffset = cube.ToOffset();
    
    // 4. Unity Tilemap에 적용
    tilemap.SetTile(resultOffset, newTile);
}
```

```csharp
// ❌ 잘못된 사용법 (하지 마세요!)
void ProcessTileTouch(Vector3Int touchOffset)
{
    // Offset 좌표로 직접 게임 로직 계산 (복잡하고 버그 발생 가능)
    Vector3Int[] neighbors = GetNeighborsOffset(touchOffset); // 복잡한 조건문 필요
}
```

---

## 2. ScriptableObject: 데이터와 코드의 분리 📊

### 왜 ScriptableObject를 사용하는가?

#### 문제 상황
```csharp
// ❌ 하드코딩된 값 (나쁜 예)
public class TileMatcher
{
    private const int MIDDLE_CHAIN_MIN = 5;
    private const int MIDDLE_CHAIN_MAX = 8;
    private const int LONG_CHAIN_MIN = 9;
    
    // 밸런스를 조정하려면 코드를 수정해야 함!
}
```

**문제점:**
- 밸런스 조정 시 코드 수정 필요
- 프로그래머 없이는 밸런싱 불가능
- 빌드 없이는 테스트 불가능

#### ScriptableObject 사용 (좋은 예)
```csharp
// ✅ ScriptableObject 사용
public class TileMatcher
{
    private BalanceData balanceData;
    
    public void Initialize()
    {
        balanceData = DataManager.Instance.BalanceData;
    }
    
    public ChainTier GetChainTier(int chainLength)
    {
        return balanceData.GetChainTier(chainLength);
        // 밸런스 조정 시 코드 수정 불필요!
    }
}
```

**장점:**
- 코드 수정 없이 밸런스 조정 가능
- 기획자가 직접 수치 변경 가능
- 런타임에도 변경 가능 (에디터에서)

### ScriptableObject 생명주기

```
1. 에디터에서 생성
   ↓
2. Resources 폴더에 저장
   ↓
3. 게임 시작 시 DataManager가 로드
   ↓
4. 게임 전역에서 사용
```

### 실제 사용 예시

```csharp
// BalanceData 사용
public class GameplayManager : MonoBehaviour
{
    private BalanceData balanceData;
    
    void Start()
    {
        balanceData = DataManager.Instance.BalanceData;
    }
    
    void OnTileRemoved(int chainLength)
    {
        // BalanceData에서 체인 티어 확인
        BalanceData.ChainTier tier = balanceData.GetChainTier(chainLength);
        
        // 티어에 따라 다른 처리
        switch (tier)
        {
            case BalanceData.ChainTier.Middle:
                CreateItem(ItemLevel.Level1);
                break;
            case BalanceData.ChainTier.Long:
                CreateItem(ItemLevel.Level2);
                break;
        }
        
        // 점수 계산도 BalanceData 사용
        int score = balanceData.baseTileScore * chainLength;
        if (tier == BalanceData.ChainTier.Middle)
            score += balanceData.middleChainBonus;
        else if (tier == BalanceData.ChainTier.Long)
            score += balanceData.longChainBonus;
    }
}
```

---

## 3. 싱글톤 패턴: 전역 접근의 핵심 🔑

### 싱글톤이란?
**하나의 인스턴스만 존재**하도록 보장하는 디자인 패턴입니다.

### 왜 싱글톤을 사용하는가?

#### 문제 상황
```csharp
// ❌ 매번 찾아야 함 (비효율적)
public class SomeClass
{
    void DoSomething()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        DataManager dm = FindObjectOfType<DataManager>();
        // 매번 FindObjectOfType 호출 (느림!)
    }
}
```

#### 싱글톤 사용 (좋은 예)
```csharp
// ✅ Instance로 바로 접근
public class SomeClass
{
    void DoSomething()
    {
        GameManager.Instance.ChangeState(GameState.InGame);
        BalanceData balance = DataManager.Instance.BalanceData;
        // 빠르고 간단!
    }
}
```

### 싱글톤 구현 원리

이 프로젝트에서는 `Singleton<T>` 베이스 클래스를 상속받아 싱글톤을 구현합니다.

```csharp
// 베이스 클래스
public abstract class Singleton<T> : MonoBehaviour where T : Component
{
    static protected T instance;
    
    static public T Instance
    {
        get
        {
            // 자동 생성 및 관리
            if (instance == null)
            {
                instance = FindAnyObjectByType<T>();
                if (instance == null)
                {
                    GameObject go = new GameObject("# " + typeof(T).Name);
                    instance = go.AddComponent<T>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }
}

// 사용 예시
public class GameManager : Singleton<GameManager>
{
    protected override void Awake()
    {
        base.Awake(); // 반드시 호출!
    }
    
    protected override async void Start()
    {
        base.Start(); // 반드시 호출!
        await Initialize();
    }
    
    public override async Task Initialize()
    {
        // 초기화 로직
        await Task.CompletedTask;
    }
}
```

**장점:**
- 코드 중복 제거
- Thread-safe (lock 사용)
- Application 종료 시 안전한 정리
- 씬 전환 이벤트 지원

### 싱글톤 사용 시 주의사항

1. **반드시 베이스 클래스 상속**
   ```csharp
   // ✅ 올바른 사용
   public class GameManager : Singleton<GameManager>
   
   // ❌ 잘못된 사용
   public class GameManager : MonoBehaviour
   ```

2. **Awake와 Start 오버라이드**
   ```csharp
   protected override void Awake()
   {
       base.Awake(); // 반드시 호출!
   }
   
   protected override async void Start()
   {
       base.Start(); // 반드시 호출!
       await Initialize();
   }
   ```

3. **Initialize는 async Task로 구현**
   ```csharp
   public override async Task Initialize()
   {
       // 비동기 초기화 로직
       await Task.CompletedTask;
   }
   ```

4. **Null 체크**
   ```csharp
   // 안전한 사용법
   if (GameManager.Instance != null)
   {
       GameManager.Instance.ChangeState(GameState.InGame);
   }
   ```

5. **초기화 순서 주의**
   - `GameManager.Instance`가 먼저 초기화되어야 함
   - `DataManager`는 `GameManager`의 `Initialize()`에서 초기화됨

---

## 4. 데이터 흐름: 전체 그림 그리기 🗺️

### 전체 아키텍처

```
┌─────────────────────────────────────────┐
│         Unity 에디터                    │
│  ScriptableObject 생성 및 편집          │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│      Resources 폴더                     │
│  Data/BalanceData/DefaultBalanceData    │
│  Data/Stages/stage_01.asset             │
│  Data/Characters/cat_01.asset           │
│  ...                                     │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│      게임 시작 (GameManager)            │
│  ┌──────────────────────────────────┐  │
│  │  DataManager.Initialize()        │  │
│  │  ├─ Resources.LoadAll()          │  │
│  │  ├─ Dictionary에 저장            │  │
│  │  └─ ID 기반 검색 가능            │  │
│  └──────────────────────────────────┘  │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│      게임 플레이                        │
│  ┌──────────────────────────────────┐  │
│  │  DataManager.Instance            │  │
│  │  .GetStageData("stage_01")       │  │
│  │  .GetCharacterData("cat_01")    │  │
│  │  .BalanceData                    │  │
│  └──────────────────────────────────┘  │
│                                          │
│  ┌──────────────────────────────────┐  │
│  │  HexCoordinates                  │  │
│  │  .OffsetToCube()                 │  │
│  │  .GetNeighbors()                 │  │
│  │  .DistanceTo()                   │  │
│  └──────────────────────────────────┘  │
└─────────────────────────────────────────┘
```

### 실제 코드 흐름 예시

```csharp
// 1. 게임 시작
public class GameplayManager : MonoBehaviour
{
    private StageData currentStage;
    private BalanceData balance;
    
    void Start()
    {
        // 2. DataManager에서 데이터 가져오기
        currentStage = DataManager.Instance.GetStageData("stage_01");
        balance = DataManager.Instance.BalanceData;
        
        // 3. 그리드 생성
        CreateGrid(currentStage.gridShape);
    }
    
    void OnTileTouched(Vector3Int offset)
    {
        // 4. 좌표 변환
        HexCoordinates cube = HexCoordinates.OffsetToCube(offset);
        
        // 5. 인접 타일 확인
        HexCoordinates[] neighbors = cube.GetNeighbors();
        
        // 6. 매칭 판정
        List<HexCoordinates> matched = FindMatchingTiles(cube, neighbors);
        
        // 7. 체인 티어 확인 (BalanceData 사용)
        BalanceData.ChainTier tier = balance.GetChainTier(matched.Count);
        
        // 8. 처리
        RemoveTiles(matched);
        if (tier != BalanceData.ChainTier.Short)
        {
            CreateItem(tier);
        }
    }
}
```

---

## 5. 핵심 암기 사항 📝

### 반드시 기억해야 할 것들

1. **헥사곤 좌표계**
   - Offset = Unity 렌더링용
   - Cube = 게임 로직용
   - 항상 변환해서 사용!

2. **ScriptableObject**
   - 데이터와 코드 분리
   - Resources 폴더에 저장
   - DataManager가 로드

3. **BalanceData**
   - 모든 수치의 중심
   - 다른 시스템들이 참조

4. **싱글톤**
   - `Instance`로 접근
   - 전역에서 사용 가능

### 코드 템플릿 (복사해서 사용하세요!)

```csharp
// 헥사곤 좌표 변환 템플릿
Vector3Int offset = tilemap.WorldToCell(position);
HexCoordinates cube = HexCoordinates.OffsetToCube(offset);
// ... 게임 로직 계산 ...
Vector3Int resultOffset = cube.ToOffset();
tilemap.SetTile(resultOffset, tile);

// 데이터 가져오기 템플릿
BalanceData balance = DataManager.Instance.BalanceData;
StageData stage = DataManager.Instance.GetStageData("stage_01");

// 체인 티어 확인 템플릿
BalanceData.ChainTier tier = balance.GetChainTier(chainLength);
if (tier == BalanceData.ChainTier.Long)
{
    // Long Chain 처리
}
```

---

## ✅ 최종 체크리스트

다음 질문에 모두 답할 수 있어야 합니다:

1. **헥사곤 좌표계**
   - [ ] Offset과 Cube 좌표의 차이를 설명할 수 있는가?
   - [ ] 왜 두 좌표계가 필요한가?
   - [ ] 변환 과정을 코드로 작성할 수 있는가?

2. **ScriptableObject**
   - [ ] ScriptableObject가 무엇인가?
   - [ ] 왜 사용하는가?
   - [ ] 어떻게 생성하고 사용하는가?

3. **매니저 패턴**
   - [ ] 싱글톤이 무엇인가?
   - [ ] GameManager와 DataManager의 역할은?
   - [ ] 어떻게 데이터를 가져오는가?

4. **전체 흐름**
   - [ ] 데이터가 어떻게 로드되는가?
   - [ ] 게임에서 어떻게 사용되는가?
   - [ ] 새로운 데이터를 추가하는 방법은?

**모든 질문에 답할 수 있다면 Phase 1 완벽 이해! 🎉**
