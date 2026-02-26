# Phase 3 시작 가이드

**작성일**: 2026-01-23  
**대상**: 새로운 에이전트 세션에서 Phase 3 작업을 시작하는 경우

---

## 📋 Phase 3 개요

### 목표
체인 길이에 따라 타일맵에 생성되는 특수 아이템 타일을 구현하고, 타일 제거 시 효과를 발동시킵니다.

### 예상 소요 시간
**4-6일**

---

## ✅ Phase 2 완료 상태 요약

### 완료된 핵심 기능
1. ✅ **벌집형 그리드 생성** (`PuzzleBoardManager`)
   - Unity Tilemap 기반 Pointy-top 헥사곤 그리드
   - `GridShapeData`를 이용한 스테이지별 그리드 쉐이프 지원
   - 홀수/짝수 열 교차 배치 로직
   - **Unity Tilemap Offset 좌표계 직접 사용** (HexCoordinates 제거됨)

2. ✅ **타일 인스턴스 시스템** (`TileInstance`)
   - Offset 좌표 기반 위치 관리
   - 타일 색상, 선택 상태, 활성화 상태 관리

3. ✅ **입력 처리** (`TileInputHandler`)
   - 터치/드래그 입력 처리
   - 마우스 이동 민감도 설정
   - 마지막 선택 타일 기준 인접 타일 검증
   - Back-track (되돌아가기) 처리

4. ✅ **타일 매칭 로직** (`TileMatcher`)
   - 인접 타일 매칭 판정
   - 동일 색상 2개 이상 연결 시 매칭
   - 다른 색상 선택 시 이전 선택 취소

5. ✅ **타일 제거 및 스폰** (`PuzzleBoardManager`)
   - 매칭된 타일 즉시 제거
   - 제자리 즉시 스폰 (중력 낙하 없음)
   - 새 타일 색상 랜덤 생성 (모든 색상: Red, Blue, Yellow, Purple, Orange, Cyan)

6. ✅ **시각적 피드백** (`TileSelectionVisualizer`)
   - LineRenderer를 사용한 연결선 표시
   - 타일 위에 표시되도록 렌더링 순서 조정

### 핵심 기술 결정
- **좌표계**: Unity Tilemap Offset 좌표계 (`Vector3Int`) 직접 사용
- **인접 타일 계산**: `HexOffsetUtils` 클래스 사용 (양방향 검증으로 대칭성 보장)
- **렌더링**: LineRenderer의 Z 위치 조정 및 View 모드로 타일 위에 표시

---

## 📁 Phase 2에서 생성된 핵심 파일

### Puzzle 시스템
```
Assets/_NyanLink/Scripts/Puzzle/
├── PuzzleBoardManager.cs      # 그리드 생성 및 관리
├── TileInstance.cs             # 타일 데이터 구조
├── HexOffsetUtils.cs           # Offset 좌표계 유틸리티
├── TileInputHandler.cs         # 입력 처리
├── TileMatcher.cs              # 타일 매칭 로직
├── TileSelectionVisualizer.cs  # 시각적 피드백
└── PuzzleBoardInitializer.cs   # 자동 초기화
```

### 데이터 구조
```
Assets/_NyanLink/Scripts/Data/
├── Definitions/
│   ├── GridShapeData.cs        # 그리드 쉐이프 정의 ✅ 완료
│   ├── TileEffectData.cs       # 타일 효과 수치 정의 ✅ 완료
│   └── BalanceData.cs          # 밸런싱 데이터 ⚠️ 비어있음 (Phase 3에서 구현 필요)
└── Enums/
    ├── TileColor.cs            # 타일 색상 (6가지) ✅ 완료
    └── ItemEffectType.cs       # 아이템 효과 타입 (6가지) ✅ 완료
```

### 유틸리티
```
Assets/_NyanLink/Scripts/Puzzle/
└── HexOffsetUtils.cs           # Offset 좌표계 유틸리티
    - GetNeighbors(Vector3Int): 인접 타일 6개 반환
    - GetDistance(Vector3Int, Vector3Int): 거리 계산 (BFS)
    - IsAdjacent(Vector3Int, Vector3Int): 인접성 검증
```

---

## 🎯 Phase 3 작업 내용

### 1. 체인 티어 판정 (밸런스 조절 가능)
- **클래스**: `ChainEvaluator` — `BalanceData` 기반으로 티어 계산
- **기능**: 체인 길이에 따른 티어 계산. 수치는 **BalanceData**에서 조절 가능
  - Middle Chain: `middleChainMin`~`middleChainMax` (기본 5~8개) → Lv.1 효과
  - Long Chain: `longChainMin` 이상 (기본 9개 이상) → Lv.2 효과

### 2. 아이템 타일 생성 로직
- **위치**: `TileMatcher.cs` 또는 새로운 클래스
- **로직**:
  - 체인 조건 만족 시 **마지막 타일 위치에 동일 색상의 아이템 효과를 가진 타일** 생성
  - 예: 빨간색 체인으로 제거한 경우 → 마지막 위치에 Recovery 효과를 가진 빨간색 타일 생성
  - **나머지 제거된 공간에는 랜덤으로 효과 없는 일반 타일 생성**
  - **아이템 타일 제한 시스템**:
    - 전체 타일맵에서 아이템 효과를 가진 타일은 최대 2개까지 존재 가능 (조절 가능)
    - 타일맵에 정해진 수량 이상이 있으면 더 이상 아이템 효과를 가진 타일은 생성되지 않음
    - 아이템 종류와 관계없이 전체 합계로 제한 (예: Recovery 1개 + Blast 1개 = 최대 2개)

### 3. 아이템 효과 구현

#### 타일 색상 → 아이템 효과 매핑 (지정 가능)
- **ColorEffectMappingData** ScriptableObject로 관리. 추후 변경 시 에디터에서만 수정하면 됨
- 기본 매핑 (데이터 없을 때 폴백): Red→Recovery, Blue→TimeFreeze, Yellow→Blast, Purple→LineClear, Orange→PowerUp, Cyan→Rainbow

#### 타일 관련 효과 (Phase 3에서 완전 구현)
- **Yellow (Blast)**: 주변 범위 타일 파괴
- **Purple (LineClear)**: 가로/대각선 라인 제거
- **Cyan (Rainbow)**: Rainbow Tile 생성 또는 Rainbow Bomb 발동

#### 시스템 의존 효과 (이벤트 기반, Phase 4/5에서 실제 동작)
- **Red (Recovery)**: 스태미나 즉시 회복 → 이벤트 발행 (Phase 4에서 구독)
- **Blue (TimeFreeze)**: 스태미나 감소/타이머 정지 → 이벤트 발행 (Phase 4에서 구독)
- **Orange (PowerUp)**: 다음 보스 공격 대미지 증가 → 이벤트 발행 (Phase 5에서 구독)

**참고**: Phase 3에서는 이벤트 시스템(`ItemEffectEvents`)을 통해 효과를 발행하고, Phase 4/5에서 실제 시스템과 연결하여 동작하도록 설계

### 4. 아이템 타일 사용 로직
- 타일맵에 배치된 아이템 효과를 가진 타일을 **연결해서 제거할 때** 효과가 발생
- 체인에 포함된 모든 아이템 타일의 효과가 각각 발동
- 동일한 종류의 아이템 타일이 여러 개 포함되어도 각각 효과 발동
- 예: Recovery 타일 1개 + Blast 타일 1개를 동시에 연결 제거 → Recovery 효과 + Blast 효과 모두 발동

### 5. 아이템 타일 시각적 표현
- 아이템 효과를 가진 타일은 일반 타일과 구분되도록 시각적 표시
- 아이콘 오버레이 또는 특수 이펙트 표시
- 효과 레벨(Lv.1/Lv.2)에 따른 시각적 차별화

### 6. 연결선(LineRenderer) 티어·색상별 연출 (추후 변경 가능)
- **LineVisualEffectConfig** ScriptableObject로 티어별·색상별 연출 지정
- **Short** (아이템 미생성): 기본 연결선
- **Middle** (Lv.1): 약한 이펙트 (예: 약한 전기) — `tierMiddle` 색상/두께/effectIntensity/머티리얼
- **Long** (Lv.2): 강한 이펙트 (예: 강한 전기) — `tierLong` 색상/두께/effectIntensity/머티리얼
- **색상별 보정**: `colorOverrides`로 선택된 타일 색상에 따라 틴트·이펙트 강도 배율 적용
- 연출 변경 시: 전기 → 불꽃 등 에셋만 교체 (머티리얼, intensity 값 조정)
- 셰이더에서 `_EffectIntensity`, `_FlowOffset` 사용 시 자동 전달

---

## 📋 Phase 3 완료 기준

- [ ] 체인 길이에 따라 올바른 티어 아이템 타일이 타일맵에 생성됨
- [ ] 전체 타일맵 아이템 타일 최대 개수 제한이 정확히 동작함
- [ ] 타일맵에 아이템 타일이 최대 개수 이상일 때 더 이상 생성되지 않음
- [ ] 아이템 타일을 연결 제거할 때 효과가 정확히 발동함
- [ ] 여러 아이템 타일이 동시에 제거될 때 모든 효과가 각각 발동함
- [ ] 동일 종류 아이템 타일도 각각 효과가 발동함
- [ ] 타일 관련 효과(Blast, LineClear, Rainbow)가 정확히 동작함
- [ ] 시스템 의존 효과(Recovery, TimeFreeze, PowerUp)가 이벤트를 정확히 발행함
- [ ] 아이템 타일 사용 시 시각적 피드백이 표시됨

**참고**: Recovery, TimeFreeze, PowerUp 효과는 Phase 3에서 이벤트만 발행하고, 실제 동작은 Phase 4/5에서 구현됨

---

## 🔧 Phase 3 시작 전 준비 사항

### 1. 데이터 구조 확인 및 구현

#### BalanceData.cs ✅ 구현됨 (Phase 3 시작 시)
- **위치**: `Assets/_NyanLink/Scripts/Data/Definitions/BalanceData.cs`
- **구현된 필드**:
  - 체인 티어 판정 (밸런스 조절): `middleChainMin`(5), `middleChainMax`(8), `longChainMin`(9)
  - 아이템 타일 최대 개수: `maxItemTilesOnBoard`(2)
  - `GetEffectLevelForChainLength(int)` 로 티어 계산

#### ColorEffectMappingData.cs ✅ 구현됨
- **위치**: `Assets/_NyanLink/Scripts/Data/Definitions/ColorEffectMappingData.cs`
- **역할**: 색상별 아이템 효과 지정 가능 (List&lt;ColorEffectEntry&gt;). 추후 변경 시 에디터에서만 수정

#### TileEffectData.cs 확인
- **위치**: `Assets/_NyanLink/Scripts/Data/Definitions/TileEffectData.cs`
- **상태**: ✅ 이미 구현됨
- **주요 메서드**:
  - `GetEffectLevel(int level)`: 레벨별 효과 수치 가져오기
  - `GetEffectValue(ItemEffectType, int level)`: 아이템 타입별 효과 수치 가져오기

#### ItemEffectType.cs 확인
- **위치**: `Assets/_NyanLink/Scripts/Data/Enums/ItemEffectType.cs`
- **상태**: ✅ 이미 구현됨
- **타입**: Recovery, TimeFreeze, Blast, LineClear, PowerUp, Rainbow

### 2. 기존 코드 이해

#### TileMatcher.cs 수정 필요
- **현재**: `ProcessMatch()` 메서드에서 타일 제거 및 새 타일 스폰만 처리
- **수정 필요**: 체인 티어 판정 및 특수 타일 생성 로직 추가

#### TileInstance.cs 확장 필요
- **현재**: `Color`, `IsSelected`, `IsActive` 속성만 있음
- **추가 필요**: `ItemEffectType?` 속성 (아이템 효과를 가진 타일인지 여부)

#### PuzzleBoardManager.cs 확장 필요
- **추가 필요**: 아이템 타일 개수 추적 메서드
- **추가 필요**: 특수 타일 생성 메서드 (아이템 효과 포함)

### 3. 이벤트 시스템 설계

#### ItemEffectEvents 클래스 생성 필요
- **목적**: 시스템 의존 효과(Recovery, TimeFreeze, PowerUp)를 이벤트로 발행
- **구조**: UnityEvent 또는 C# Action 기반
- **이벤트 타입**:
  - `OnRecoveryTriggered(float amount)`
  - `OnTimeFreezeTriggered(float duration)`
  - `OnPowerUpTriggered(float multiplier)`

---

## 🚀 Phase 3 구현 순서 권장

### 1단계: 데이터 구조 준비
1. ~~`BalanceData.cs` 구현~~ ✅ 완료 (체인 티어·아이템 제한 수치 조절 가능)
2. ~~`ColorEffectMappingData`~~ ✅ 완료 (색상별 효과 지정 가능)
3. `TileInstance.cs`는 이미 `TileState`(ItemLv1/ItemLv2) 보유
4. `ItemEffectEvents.cs` 클래스 생성

### 2단계: 체인 티어 판정
1. ~~`ChainEvaluator.cs` 클래스 생성~~ ✅ 완료 (`BalanceData` 기반)
2. ~~체인 길이에 따른 티어 계산~~ ✅ `ChainEvaluator.GetEffectLevel(balanceData, chainLength)`

### 3단계: 특수 타일 생성 로직
1. `TileMatcher.cs`의 `ProcessMatch()` 수정
2. 체인 티어 판정 후 마지막 타일 위치에 특수 타일 생성
3. 아이템 타일 개수 제한 로직 구현
4. 나머지 공간에 일반 타일 생성

### 4단계: 아이템 효과 구현
1. 타일 관련 효과 구현 (Blast, LineClear, Rainbow)
2. 시스템 의존 효과 이벤트 발행 (Recovery, TimeFreeze, PowerUp)
3. 아이템 타일 제거 시 효과 발동 로직 구현

### 5단계: 시각적 표현
1. 아이템 타일 시각적 구분 (아이콘 오버레이 또는 특수 이펙트)
2. 효과 레벨에 따른 시각적 차별화

### 6단계: 테스트 및 검증
1. 각 완료 기준별 테스트
2. 버그 수정 및 최적화

---

## 📚 참고 문서

### 필수 읽기 문서
1. **`PROJECT_ROADMAP.md`** - Phase 3 섹션 (145-205줄)
2. **`PHASE2_COMPLETION_SUMMARY.md`** - Phase 2 완료 내역
3. **`PHASE2_FINAL_CHECK.md`** - Phase 2 최종 확인

### 참고 문서
- `PHASE1_CORE_CONCEPTS.md` - 프로젝트 핵심 개념
- `PHASE1_UNDERSTANDING_GUIDE.md` - Phase 1 이해 가이드

---

## ⚠️ 주의사항

### Phase 2에서 제거된 내용
- **HexCoordinates 시스템**: 완전히 제거됨, Unity Offset 좌표계만 사용
- **타일 애니메이션**: Phase 2에서 제거됨, Phase 3에서 재검토 예정

### Phase 3에서 구현하지 않을 내용
- **타일 풀링 시스템**: Phase 8에서 구현 예정
- **성능 최적화**: Phase 8에서 구현 예정
- **Recovery, TimeFreeze, PowerUp 실제 동작**: Phase 4/5에서 구현 예정

---

## 💡 구현 팁

### 1. 타일 색상 → 아이템 효과 매핑 (데이터 기반)
```csharp
// ColorEffectMappingData 사용 (DataManager에서 로드)
var mapping = DataManager.Instance.ColorEffectMapping;
if (mapping != null)
    ItemEffectType effect = mapping.GetEffectForColor(color);
```

### 2. 체인 티어 판정 (밸런스 데이터 사용)
```csharp
var balance = DataManager.Instance.BalanceData;
int effectLevel = ChainEvaluator.GetEffectLevel(balance, selectedChain.Count);
TileState state = ChainEvaluator.EffectLevelToTileState(effectLevel);
```

### 3. 아이템 타일 개수 추적
- `PuzzleBoardManager`에서 `TileInstance.State != Normal` 개수로 추적
- `BalanceData.maxItemTilesOnBoard`로 제한

### 4. 이벤트 시스템 구조
```csharp
public static class ItemEffectEvents
{
    public static event System.Action<float> OnRecoveryTriggered;
    public static event System.Action<float> OnTimeFreezeTriggered;
    public static event System.Action<float> OnPowerUpTriggered;
    
    public static void TriggerRecovery(float amount)
    {
        OnRecoveryTriggered?.Invoke(amount);
    }
    // ... 다른 이벤트들
}
```

---

## ✅ 시작 전 체크리스트

- [ ] `PROJECT_ROADMAP.md`의 Phase 3 섹션 읽음
- [ ] `PHASE2_COMPLETION_SUMMARY.md` 읽음
- [ ] `PHASE2_FINAL_CHECK.md` 읽음
- [ ] Phase 2 핵심 파일 구조 이해
- [ ] `HexOffsetUtils` 사용법 이해
- [ ] `TileMatcher.cs`의 현재 구조 확인
- [ ] `TileInstance.cs`의 현재 구조 확인
- [ ] `PuzzleBoardManager.cs`의 현재 구조 확인
- [ ] `TileEffectData.cs` 구조 확인
- [ ] `ItemEffectType.cs` Enum 확인

---

**다음 단계**: Phase 3 개발 시작 준비 완료! 🚀
