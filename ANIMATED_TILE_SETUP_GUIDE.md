# AnimatedTile 설정 가이드

**작성일**: 2026-01-23

---

## 📋 개요

Phase 3 작업 전에 타일 시스템을 재구성하여 AnimatedTile을 사용하도록 변경합니다.

### 생성되는 타일
- **총 18개의 AnimatedTile 에셋**
- 6가지 색상 (Red, Blue, Yellow, Purple, Orange, Cyan)
- 각 색상별 3가지 상태:
  - `Normal`: 일반 상태 (아이템 효과 없음)
  - `ItemLv1`: 아이템 Lv1 효과 보유
  - `ItemLv2`: 아이템 Lv2 효과 보유

### 네이밍 규칙
```
HexagonTile_{Color}_{State}
예:
- HexagonTile_Red_Normal
- HexagonTile_Red_ItemLv1
- HexagonTile_Red_ItemLv2
- HexagonTile_Blue_Normal
- ...
```

---

## 🔧 사전 준비

### 1. Unity 2D Tilemap Extras 패키지 확인

AnimatedTile을 사용하려면 Unity의 **2D Tilemap Extras** 패키지가 필요합니다.

#### 패키지 설치 방법
1. Unity 에디터에서 **Window > Package Manager** 열기
2. 왼쪽 상단 드롭다운에서 **Unity Registry** 선택
3. 검색창에 `2d tilemap extras` 입력
4. **2D Tilemap Extras** 패키지 찾기
5. **Install** 버튼 클릭

#### 패키지 확인
- Unity 2021.2 이상: 기본 포함되어 있을 수 있음
- Unity 2020.x: 수동 설치 필요

### 2. 기본 헥사곤 스프라이트 생성

AnimatedTile 생성 전에 기본 헥사곤 스프라이트가 필요합니다.

#### 스프라이트 생성 방법
1. Unity 메뉴: **NyanLink > Setup > Create Phase 2 Test Assets**
2. 이 메뉴를 실행하면 `HexagonTexture.asset`과 `HexagonSprite`가 생성됩니다.

---

## 🚀 AnimatedTile 생성 방법

### 방법 1: Unity 메뉴 사용 (권장)

1. Unity 에디터에서 메뉴바 클릭: **NyanLink > Setup > Create Animated Tiles**
2. 자동으로 18개의 AnimatedTile이 생성됩니다.
3. 생성 완료 후 다이얼로그에서 확인 메시지를 볼 수 있습니다.

### 생성 위치
```
Assets/_NyanLink/Art/Tiles/
├── HexagonTile_Red_Normal.asset
├── HexagonTile_Red_ItemLv1.asset
├── HexagonTile_Red_ItemLv2.asset
├── HexagonTile_Blue_Normal.asset
├── HexagonTile_Blue_ItemLv1.asset
├── HexagonTile_Blue_ItemLv2.asset
├── ... (총 18개)
```

---

## 📝 Unity Tile Palette에 추가하기

### 1. Tile Palette 생성

1. Unity 메뉴: **Window > 2D > Tile Palette**
2. Tile Palette 창이 열리면 **Create New Palette** 클릭
3. 팔레트 이름 입력 (예: "NyanLink Hexagon Tiles")
4. 저장 위치 선택 (권장: `Assets/_NyanLink/Art/TilePalettes/`)

### 2. 타일을 팔레트에 추가

#### 방법 A: 드래그 앤 드롭
1. Project 창에서 생성된 AnimatedTile 에셋들을 선택
2. Tile Palette 창으로 드래그 앤 드롭
3. 타일이 팔레트에 추가됩니다.

#### 방법 B: 팔레트에서 직접 생성
1. Tile Palette 창에서 빈 슬롯 클릭
2. Inspector에서 생성된 AnimatedTile 에셋 할당

### 3. 타일 팔레트 사용

1. Tile Palette 창에서 원하는 타일 선택
2. Scene 뷰의 Tilemap에서 타일을 배치할 위치 클릭
3. 타일이 배치됩니다.

---

## 🎨 타일 상태별 사용 가이드

### Normal (일반 상태)
- **사용 시점**: 아이템 효과를 보유하지 않은 일반 타일
- **에셋**: `HexagonTile_{Color}_Normal`
- **예시**: `HexagonTile_Red_Normal`, `HexagonTile_Blue_Normal`

### ItemLv1 (아이템 Lv1 효과 보유)
- **사용 시점**: Middle Chain (5~8개)으로 생성된 특수 타일
- **에셋**: `HexagonTile_{Color}_ItemLv1`
- **예시**: `HexagonTile_Red_ItemLv1`, `HexagonTile_Yellow_ItemLv1`

### ItemLv2 (아이템 Lv2 효과 보유)
- **사용 시점**: Long Chain (9개 이상)으로 생성된 특수 타일
- **에셋**: `HexagonTile_{Color}_ItemLv2`
- **예시**: `HexagonTile_Red_ItemLv2`, `HexagonTile_Purple_ItemLv2`

---

## 🔄 코드에서 AnimatedTile 사용하기

### PuzzleBoardManager 수정 필요

현재 `PuzzleBoardManager.cs`는 일반 `TileBase`를 사용하고 있습니다. AnimatedTile을 사용하도록 수정해야 합니다.

#### 수정 예시

```csharp
// 현재 코드
public TileBase redTile;
public TileBase blueTile;
// ...

// 수정 후 (AnimatedTile 사용)
public AnimatedTile redTileNormal;
public AnimatedTile redTileItemLv1;
public AnimatedTile redTileItemLv2;
// ... (각 색상별로 3가지 상태)
```

또는 딕셔너리 구조 사용:

```csharp
private Dictionary<TileColor, Dictionary<TileState, AnimatedTile>> _tilesByColorAndState;

// 타일 가져오기
public AnimatedTile GetTileByColorAndState(TileColor color, TileState state)
{
    return _tilesByColorAndState[color][state];
}
```

---

## ⚠️ 주의사항

### 1. 스프라이트 아트 에셋

현재는 기본 헥사곤 스프라이트만 사용합니다. 추후 각 상태별로 다른 스프라이트를 사용하려면:

1. 각 상태별 스프라이트 아트 에셋 준비
2. `NyanLinkAnimatedTileCreator.cs`의 `GetSpritesForState()` 메서드 수정
3. AnimatedTile을 다시 생성

### 2. 애니메이션 설정

현재 AnimatedTile은 단일 스프라이트만 사용합니다. 추후 애니메이션 효과를 추가하려면:

1. 여러 프레임의 스프라이트 시퀀스 준비
2. `GetSpritesForState()` 메서드에서 스프라이트 배열 반환
3. AnimatedTile의 `m_MinSpeed`, `m_MaxSpeed` 조정

### 3. 기존 타일과의 호환성

기존 `TileBase` 타일들은 그대로 유지됩니다. AnimatedTile로 전환할 때:

1. 기존 타일 참조를 AnimatedTile로 교체
2. `GetTileByColor()` 메서드를 `GetTileByColorAndState()`로 변경
3. 타일 상태에 따라 적절한 AnimatedTile 선택

---

## 📚 참고 문서

- Unity 공식 문서: [AnimatedTile](https://docs.unity3d.com/Packages/com.unity.2d.tilemap.extras@latest)
- Unity 공식 문서: [Tile Palette](https://docs.unity3d.com/Manual/Tilemap-Palette.html)

---

## ✅ 체크리스트

- [ ] Unity 2D Tilemap Extras 패키지 설치 확인
- [ ] 기본 헥사곤 스프라이트 생성 (`Create Phase 2 Test Assets` 실행)
- [ ] AnimatedTile 생성 (`Create Animated Tiles` 실행)
- [ ] 18개의 AnimatedTile 에셋 생성 확인
- [ ] Unity Tile Palette 생성 및 타일 추가
- [ ] `PuzzleBoardManager.cs` 수정 (AnimatedTile 사용)
- [ ] 타일 상태에 따른 타일 선택 로직 구현

---

**다음 단계**: Phase 3 개발 시작 전에 타일 시스템 재구성 완료! 🚀
