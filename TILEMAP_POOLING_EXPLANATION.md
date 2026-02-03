# Unity Tilemap과 ObjectPool에 대한 설명

## 🤔 문제 상황

**사용자 질문**: "Unity Tilemap으로 타일을 추가/삭제하는데, 여기에 ObjectPool을 사용할 수 있어? Tilemap의 타일은 오브젝트가 아니지 않아?"

**답변**: 맞습니다! Unity Tilemap의 타일은 GameObject가 아닙니다.

---

## 📊 Unity Tilemap의 구조

### Tilemap의 타일은 무엇인가?

```csharp
// Unity Tilemap의 타일 구조
Tilemap.SetTile(offset, tileBase);  // tileBase는 TileBase 에셋 참조
```

**특징:**
- `TileBase`는 **에셋** (ScriptableObject 상속)
- GameObject가 아님
- 메모리상에 하나만 존재 (에셋 참조)
- `SetTile()`은 단순히 **에셋 참조를 저장**하는 것

### 현재 코드 구조

```csharp
// PuzzleBoardManager.cs
TileInstance tileInstance = new TileInstance(cube, randomColor);  // 데이터 구조
_tiles[cube] = tileInstance;  // 딕셔너리에 저장

tilemap.SetTile(offset, hexagonTile);  // TileBase 에셋 참조만 저장
```

**문제점:**
- `TileInstance`는 매번 `new`로 생성됨
- `TileBase` 에셋은 이미 재사용됨 (에셋이므로)
- 하지만 `TileInstance` 객체는 풀링되지 않음

---

## ✅ 풀링 가능한 것들

### 1. TileBase 에셋
**이미 풀링됨** (에셋이므로 자동으로 재사용)
- `hexagonTile` 에셋은 하나만 존재
- 모든 타일이 같은 에셋을 참조

### 2. TileInstance 객체 (데이터 구조)
**풀링 가능** - 현재는 풀링되지 않음
- `TileInstance`는 클래스이므로 매번 `new`로 생성
- 풀링하면 GC 압력 감소 가능

---

## 🔧 TileInstance 풀링 구현 방법

### 방법 1: 간단한 풀링 (권장)

```csharp
using UnityEngine.Pool;

public class PuzzleBoardManager : MonoBehaviour
{
    // TileInstance 풀
    private ObjectPool<TileInstance> _tileInstancePool;
    
    private void Awake()
    {
        // 풀 생성
        _tileInstancePool = new ObjectPool<TileInstance>(
            createFunc: () => new TileInstance(default(HexCoordinates), TileColor.Red),
            actionOnGet: (tile) => { /* 재사용 시 초기화 */ },
            actionOnRelease: (tile) => tile.Reset(),
            actionOnDestroy: (tile) => { /* 파괴 시 처리 */ }
        );
    }
    
    private TileInstance CreateTileInstance(HexCoordinates cube, TileColor color)
    {
        TileInstance tile = _tileInstancePool.Get();
        // 위치와 색상 설정 (TileInstance에 SetPosition, SetColor 메서드 필요)
        tile.SetPosition(cube);
        tile.Color = color;
        return tile;
    }
    
    private void RemoveTileInstance(TileInstance tile)
    {
        _tileInstancePool.Release(tile);
    }
}
```

### 방법 2: TileInstance 구조 변경 필요

현재 `TileInstance`는 `CubePosition`이 `private set`이므로, 풀링을 위해 수정 필요:

```csharp
public class TileInstance
{
    public HexCoordinates CubePosition { get; private set; }  // private set
    
    // 풀링을 위한 메서드 추가 필요
    public void SetPosition(HexCoordinates cube)
    {
        // private set이므로 직접 설정 불가능
        // 구조 변경 필요
    }
}
```

---

## 💡 실제로 풀링이 필요한가?

### 성능 분석

**TileInstance 생성 비용:**
- 단순 클래스 (필드 몇 개)
- GC 압력: 매우 낮음
- 생성 비용: 거의 없음

**실제 성능 영향:**
- 타일 개수: 7x7 = 49개 (초기 생성)
- 매칭 시: 2~10개 정도 생성/제거
- **GC 압력: 거의 없음**

### 결론

**현재 구조에서는 TileInstance 풀링이 크게 필요하지 않습니다.**

**이유:**
1. 타일 생성/제거 빈도가 낮음
2. TileInstance는 가벼운 데이터 구조
3. TileBase 에셋은 이미 재사용됨
4. 실제 성능 병목은 렌더링/애니메이션

---

## 🎯 권장 사항

### Phase 2에서는 풀링 생략
- 기능 구현에 집중
- 성능 최적화는 Phase 8 (밸런싱 및 최적화)에서

### Phase 8에서 고려할 풀링 대상
1. **VFX (파티클 시스템)** - 실제로 필요함
2. **장애물 GameObject** - Phase 4에서 생성
3. **UI 요소** - Phase 7에서 생성

### TileInstance 풀링은 선택사항
- 성능 프로파일링 후 필요시 추가
- 현재는 우선순위 낮음

**참고**: 프로젝트는 총 8개의 Phase로 구성되어 있습니다:
- Phase 1: 데이터 구조 및 인프라 구축
- Phase 2: 퍼즐 시스템 핵심 구현 (현재 작업 중)
- Phase 3: 특수 아이템 시스템
- Phase 4: 러너 시스템 및 장애물
- Phase 5: 보스 전투 시스템
- Phase 6: 성장 시스템 (캐릭터/장비)
- Phase 7: UI/UX 구현
- Phase 8: 밸런싱 및 최적화

---

## 📝 요약

| 항목 | 풀링 가능 여부 | 현재 상태 | 권장 사항 |
|------|---------------|----------|----------|
| TileBase 에셋 | ✅ (이미 재사용됨) | 풀링됨 | 변경 불필요 |
| TileInstance 객체 | ✅ (가능하지만) | 풀링 안됨 | Phase 8에서 선택적 구현 |
| GameObject 타일 | ✅ (필요시) | 현재 미사용 | 필요시 구현 |

**결론**: Unity Tilemap의 타일은 GameObject가 아니므로 일반적인 ObjectPool을 사용할 수 없습니다. 하지만 TileInstance 데이터 구조는 풀링 가능하지만, 현재는 성능상 큰 이점이 없어 Phase 2에서는 생략하는 것이 좋습니다.
