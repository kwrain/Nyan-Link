# 싱글톤 패턴 사용 가이드

## 📋 개요

이 프로젝트에서는 `Singleton<T>` 베이스 클래스를 상속받아 싱글톤 패턴을 구현합니다.

## 🎯 기본 구조

### 베이스 클래스
```csharp
public abstract class Singleton<T> : MonoBehaviour where T : Component
```

### 사용 방법
```csharp
public class MyManager : Singleton<MyManager>
{
    protected override void Awake()
    {
        base.Awake();
        // 초기화 로직
    }

    protected override async void Start()
    {
        base.Start();
        await Initialize();
    }

    public override async Task Initialize()
    {
        // 비동기 초기화 로직
        await Task.CompletedTask;
    }
}
```

## ✅ 싱글톤 클래스 작성 규칙

### 1. 반드시 상속받기
```csharp
// ✅ 올바른 사용
public class GameManager : Singleton<GameManager>

// ❌ 잘못된 사용
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    // ...
}
```

### 2. Awake와 Start 오버라이드
```csharp
protected override void Awake()
{
    base.Awake(); // 반드시 호출!
    // 추가 초기화 로직
}

protected override async void Start()
{
    base.Start(); // 반드시 호출!
    await Initialize();
}
```

### 3. Initialize는 async Task로 구현
```csharp
public override async Task Initialize()
{
    // 비동기 초기화 로직
    // 예: 데이터 로드, 네트워크 연결 등
    await SomeAsyncOperation();
}
```

### 4. Instance 접근
```csharp
// ✅ 올바른 사용
GameManager.Instance.ChangeState(GameState.InGame);
DataManager.Instance.BalanceData;

// ❌ 잘못된 사용
GameManager gameManager = new GameManager(); // 불가능!
```

## 🔑 주요 기능

### 1. 자동 생성 및 관리
- `Instance` 접근 시 자동으로 생성됨
- 씬 전환 시에도 유지됨 (`DontDestroyOnLoad`)
- 중복 인스턴스 자동 제거

### 2. Thread-Safe
- `lock`을 사용하여 멀티스레드 환경에서도 안전

### 3. Application 종료 처리
- `OnApplicationQuit`에서 안전하게 정리
- 종료 시점에 `Instance`가 `null` 반환

### 4. 씬 전환 이벤트
```csharp
protected override void ScenePreloadEvent(Scene currScene)
{
    // 씬 전환 전 처리
}

protected override void SceneLoadedEvent(Scene scene, LoadSceneMode SceneMode)
{
    // 씬 전환 후 처리
}
```

### 5. Application 종료 콜백
```csharp
// 다른 클래스에서 등록
SomeManager.Instance.AddOnApplicationQuitCallback(
    this, 
    "Cleanup", 
    () => { /* 정리 로직 */ }
);
```

## 📝 실제 사용 예시

### GameManager 예시
```csharp
public class GameManager : Singleton<GameManager>
{
    public GameState currentState = GameState.Menu;

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
        // DataManager 초기화
        await DataManager.Instance.Initialize();
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
    }
}
```

### DataManager 예시
```csharp
public class DataManager : Singleton<DataManager>
{
    private BalanceData _balanceData;

    protected override void Awake()
    {
        base.Awake();
    }

    public override async Task Initialize()
    {
        // 데이터 로드
        _balanceData = Resources.Load<BalanceData>("Data/BalanceData");
        await Task.CompletedTask;
    }

    public BalanceData BalanceData => _balanceData;
}
```

## ⚠️ 주의사항

### 1. 초기화 순서
```csharp
// ✅ 올바른 순서
protected override async void Start()
{
    base.Start();
    await Initialize(); // Start에서 Initialize 호출
}

// ❌ 잘못된 순서
protected override void Awake()
{
    base.Awake();
    Initialize(); // Awake에서 동기 호출하면 안됨!
}
```

### 2. Null 체크
```csharp
// ✅ 안전한 사용
if (GameManager.Instance != null)
{
    GameManager.Instance.ChangeState(GameState.InGame);
}

// Application 종료 시점에는 Instance가 null일 수 있음
```

### 3. 중복 생성 방지
- `Singleton<T>`가 자동으로 처리하므로 수동으로 `FindObjectOfType` 사용 불필요

### 4. DontDestroyOnLoad
- 베이스 클래스에서 자동 처리되므로 수동 호출 불필요

## 🔄 초기화 흐름

```
1. Instance 접근 또는 씬에 GameObject 존재
   ↓
2. Awake() 호출
   ↓
3. Start() 호출
   ↓
4. Initialize() 호출 (비동기)
   ↓
5. 사용 가능
```

## 📚 참고

- 베이스 클래스: `Assets/_NyanLink/Scripts/Core/Singleton/Singleton.cs`
- 예시 구현: 
  - `Assets/_NyanLink/Scripts/Core/Managers/GameManager.cs`
  - `Assets/_NyanLink/Scripts/Core/Managers/DataManager.cs`

## ✅ 체크리스트

새로운 싱글톤 매니저를 만들 때:

- [ ] `Singleton<T>`를 상속받았는가?
- [ ] `Awake()`에서 `base.Awake()`를 호출했는가?
- [ ] `Start()`에서 `base.Start()`를 호출하고 `Initialize()`를 호출했는가?
- [ ] `Initialize()`가 `async Task`로 구현되었는가?
- [ ] `Instance`로 접근하는가? (직접 생성하지 않음)
- [ ] 필요한 경우 씬 전환 이벤트를 오버라이드했는가?
