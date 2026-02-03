# HexCoordinates가 필요한 이유

## 🤔 질문: Unity Tilemap에서도 타일 좌표를 알 수 있는데 왜 HexCoordinates가 필요한가?

좋은 질문입니다! Unity Tilemap은 **Offset 좌표계**를 사용하지만, 헥사곤 타일의 **게임 로직**을 구현하기 위해서는 **Cube/Axial 좌표계**가 훨씬 효율적입니다.

---

## 📊 두 좌표계의 차이

### 1. Offset 좌표계 (Unity Tilemap 기본)

Unity Tilemap은 **Offset 좌표계**를 사용합니다. 이는 직사각형 그리드처럼 보이지만, 헥사곤의 **게임 로직**에는 부적합합니다.

```
Offset 좌표 예시 (Pointy-top 헥사곤):
     (0,0)  (1,0)  (2,0)
   (0,1)  (1,1)  (2,1)  (3,1)
     (0,2)  (1,2)  (2,2)
```

**문제점:**
- 인접 타일을 찾기 어려움 (홀수/짝수 열마다 다름)
- 거리 계산이 복잡함
- 매칭 판정 로직이 복잡해짐

### 2. Cube 좌표계 (게임 로직용)

**Cube 좌표계**는 헥사곤 타일의 게임 로직에 최적화되어 있습니다.

```
Cube 좌표 (q, r, s):
     (0,-1,1)  (1,-1,0)  (1,0,-1)
   (-1,0,1)  (0,0,0)  (1,0,-1)  (2,-1,-1)
     (-1,1,0)  (0,1,-1)  (1,1,-2)
```

**특징:**
- **q + r + s = 0** (항상 성립)
- 인접 타일이 항상 **6방향**으로 일정함
- 거리 계산이 간단함: `distance = (|q1-q2| + |r1-r2| + |s1-s2|) / 2`

---

## 🎯 실제 사용 예시

### 예시 1: 인접 타일 찾기

**Offset 좌표로 인접 타일 찾기 (복잡함):**
```csharp
// Offset 좌표로 인접 타일 찾기
Vector3Int[] GetNeighbors(Vector3Int offset) {
    if (offset.y % 2 == 0) {
        // 짝수 열
        return new Vector3Int[] {
            new Vector3Int(offset.x - 1, offset.y - 1, 0),
            new Vector3Int(offset.x, offset.y - 1, 0),
            new Vector3Int(offset.x - 1, offset.y, 0),
            new Vector3Int(offset.x + 1, offset.y, 0),
            new Vector3Int(offset.x - 1, offset.y + 1, 0),
            new Vector3Int(offset.x, offset.y + 1, 0)
        };
    } else {
        // 홀수 열 (다른 패턴!)
        return new Vector3Int[] {
            new Vector3Int(offset.x, offset.y - 1, 0),
            new Vector3Int(offset.x + 1, offset.y - 1, 0),
            new Vector3Int(offset.x - 1, offset.y, 0),
            new Vector3Int(offset.x + 1, offset.y, 0),
            new Vector3Int(offset.x, offset.y + 1, 0),
            new Vector3Int(offset.x + 1, offset.y + 1, 0)
        };
    }
}
```

**Cube 좌표로 인접 타일 찾기 (간단함):**
```csharp
// Cube 좌표로 인접 타일 찾기
HexCoordinates[] GetNeighbors(HexCoordinates cube) {
    return new HexCoordinates[] {
        new HexCoordinates(cube.q + 1, cube.r, cube.s - 1),     // 오른쪽
        new HexCoordinates(cube.q + 1, cube.r - 1, cube.s),     // 오른쪽 위
        new HexCoordinates(cube.q, cube.r - 1, cube.s + 1),     // 왼쪽 위
        new HexCoordinates(cube.q - 1, cube.r, cube.s + 1),     // 왼쪽
        new HexCoordinates(cube.q - 1, cube.r + 1, cube.s),     // 왼쪽 아래
        new HexCoordinates(cube.q, cube.r + 1, cube.s - 1)      // 오른쪽 아래
    };
}
// 항상 동일한 패턴! 홀수/짝수 구분 불필요!
```

### 예시 2: 거리 계산

**Offset 좌표로 거리 계산 (복잡함):**
```csharp
int GetDistance(Vector3Int a, Vector3Int b) {
    // Offset 좌표로 거리 계산은 복잡한 공식 필요
    // 홀수/짝수 열에 따라 다른 계산식...
}
```

**Cube 좌표로 거리 계산 (간단함):**
```csharp
int GetDistance(HexCoordinates a, HexCoordinates b) {
    return (Math.Abs(a.q - b.q) + Math.Abs(a.r - b.r) + Math.Abs(a.s - b.s)) / 2;
}
// 한 줄로 끝!
```

### 예시 3: 타일 매칭 판정 (드래그 연결)

**냥링크 게임에서 필요한 기능:**
- 드래그로 타일을 연결할 때, **인접한 타일만** 선택 가능해야 함
- Back-track (되돌아가기) 처리
- 직선 경로 판정 (Line Clear 아이템용)

**Offset 좌표로 구현:**
```csharp
bool IsAdjacent(Vector3Int from, Vector3Int to) {
    // 홀수/짝수 열 체크 필요
    // 복잡한 조건문...
}
```

**Cube 좌표로 구현:**
```csharp
bool IsAdjacent(HexCoordinates from, HexCoordinates to) {
    return GetDistance(from, to) == 1;  // 거리가 1이면 인접!
}
```

---

## 🔄 두 좌표계의 역할 분담

기획서에 따르면:

### Offset 좌표계 (Unity Tilemap)
- **용도**: Unity Tilemap API 호출용
- **사용처**: 
  - `Tilemap.SetTile(offset, tile)` - 타일 배치
  - `Tilemap.GetTile(offset)` - 타일 조회
  - 렌더링 관련 작업

### Cube 좌표계 (게임 로직)
- **용도**: 게임 로직 계산용
- **사용처**:
  - 타일 매칭 판정
  - 인접 타일 탐색
  - 거리 계산
  - Line Clear (직선 경로) 판정
  - Blast (범위 파괴) 판정

---

## 💡 실제 구현 구조

```csharp
// 1. Unity Tilemap에서 Offset 좌표 얻기
Vector3Int offset = tilemap.WorldToCell(worldPosition);

// 2. Offset → Cube 변환
HexCoordinates cube = HexCoordinates.OffsetToCube(offset);

// 3. 게임 로직 계산 (Cube 좌표 사용)
HexCoordinates[] neighbors = cube.GetNeighbors();
int distance = cube.GetDistance(otherCube);

// 4. Cube → Offset 변환 후 Unity API 호출
Vector3Int newOffset = HexCoordinates.CubeToOffset(cube);
tilemap.SetTile(newOffset, newTile);
```

---

## ✅ 결론

**HexCoordinates가 필요한 이유:**

1. **게임 로직의 효율성**
   - 인접 타일 탐색이 간단함 (홀수/짝수 구분 불필요)
   - 거리 계산이 한 줄로 끝남
   - 매칭 판정 로직이 명확함

2. **코드 가독성**
   - 복잡한 조건문 제거
   - 버그 발생 가능성 감소
   - 유지보수 용이

3. **확장성**
   - Line Clear (직선 경로) 구현 용이
   - Blast (범위 파괴) 구현 용이
   - A* 경로 찾기 알고리즘 적용 가능

4. **표준 관례**
   - 헥사곤 타일 게임에서 널리 사용되는 표준 방식
   - Red Blob Games 등 참고 자료가 많음

---

## 📚 참고 자료

- [Red Blob Games - Hexagonal Grids](https://www.redblobgames.com/grids/hexagons/)
- Unity Tilemap은 렌더링용, 게임 로직은 Cube 좌표계 사용이 베스트 프랙티스

---

**요약:** Unity Tilemap의 Offset 좌표는 **렌더링용**, HexCoordinates의 Cube 좌표는 **게임 로직용**입니다. 두 좌표계를 변환하여 각각의 장점을 활용하는 것이 최선의 방법입니다! 🎯
