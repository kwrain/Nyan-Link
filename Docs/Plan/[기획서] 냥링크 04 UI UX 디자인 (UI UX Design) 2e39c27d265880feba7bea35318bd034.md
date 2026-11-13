# [기획서] 냥링크: 04. UI/UX 디자인 (UI/UX Design)

Unity **UGUI (Canvas / Panel)** 기반의 반응형 UI 설계를 지향하며, 물리적으로 분할된 화면(상단/하단) 구조를 따릅니다.

## 1. 디자인 컨셉 및 가이드 (Design Guidelines)

- **해상도 전략**:
    - **Target**: 9:16 Portrait (1080x1920) 기준.
    - **Safe Area**: 노치(Notch) 및 하단 홈 바 영역을 침범하지 않도록 `Padding` 처리.
    - **Scale Mode**: `Scale With Screen Size`.
- **테마 (Theme)**:
    - **Top View (Runner)**: 레트로 감성의 **픽셀 아트 HUD**. (비트맵 폰트 사용)
    - **Bottom View (System)**: 가독성을 최우선으로 한 **모던 플랫 디자인**. (벡터 아이콘, 둥근 모서리)
    - **Popup**: 화면 전체를 덮는 반투명 딤드(Dimmed) 배경 위 모달 창.

## 2. 화면 계층 구조 (Screen Hierarchy)

UGUI Canvas 계층 구조를 활용하여 화면 그룹 단위로 관리합니다.

```
Canvas (Screen Space - Camera)
 ├── ScreenContainer
 │    ├── LobbyGroup (Active: false)      // 로비, 상점, 장비 등
 │    └── InGameGroup (Active: true)      // 인게임 화면
 │         ├── TopRunnerView (height: 40%)
 │         └── BottomPuzzleView (height: 60%)
 └── PopupLayer (Stretch Full)            // 최상위 팝업 컨테이너
```

## 3. 상세 화면 리스트 (Screen List)

### 3.1. 진입 및 로비 (Lobby)

| **화면 ID** | **화면명** | **주요 기능 및 구성** |
| --- | --- | --- |
| **UI_Title** | 타이틀 | - 로고, "Touch to Start", 버전/UID 표시. |
| **UI_Lobby** | 메인 로비 | - **Top**: 재화(코인), 설정 버튼.
- **Center**: 대표 캐릭터(Idle), **[게임 시작]** 버튼.
- **Bottom**: [상점] - [장비] - **[홈]** - [아케이드] 탭 메뉴. |
| **UI_StageSelect** | 스테이지 선택 | - 챕터 리스트 (스크롤).
- **보스 스테이지 강조** (해골 아이콘 + 대표 드롭 장비 아이콘).
- 입장 시 소모 스태미나 확인 팝업. |
| **UI_Equipment** | 장비 관리 | - **장착 슬롯**: 모자/옷/무기 (3슬롯).
- **보유 목록**: 그리드 리스트, 등급별 테두리 색상 적용.
- **[상세 팝업]**: 레벨업(코인 소모), 승급 버튼 포함. |
| **UI_Shop** | 상점 | - 광고 보상(무료 코인/뽑기) 버튼.
- 유료 재화 상품 리스트. |

### 3.2. 인게임 (In-Game HUD)

상하 40:60 비율이 고정된 핵심 플레이 화면입니다.

| **영역** | **구성 요소 및 기능** |
| --- | --- |
| **Top View**
(Runner)
*Flex: 4* | **[HUD]**
- HP(스태미나) 게이지, 스테이지 진행률 바 (Start...Boss).
- 일시정지 버튼.
**[Boss Overlay]**
- 보스 조우 시 활성화.
- 보스 체력 바, **패턴 UI** (노트 레일, 시퀀스 아이콘, 타이머). |
| **Bottom View**
(Puzzle)
*Flex: 6* | **[Puzzle Grid]**
- **Honeycomb Layout**: 벌집형 교차 배치가 적용된 타일맵 컨테이너.
- 배경: 그리드 패턴 혹은 단순 음영.
**[Control]**
- 보유 아이템 슬롯 (최대 2개, 터치 시 사용). |

3.3. 결과 및 공통 팝업 (Common)

| **화면 ID** | **화면명** | **주요 기능 및 구성** |
| --- | --- | --- |
| **UI_Result_Win** | 승리 | - 클리어 별(Star), 점수.
- **[Loot List]**: 획득한 코인 및 장비.
- **[Jackpot Effect]**: **보스 전용 장비** 획득 시 특수 연출.
- 버튼: [다음], [다시하기], [홈]. |
| **UI_Result_Lose** | 패배 | - 실패 사유 (Time Over / HP Zero).
- **[광고 보고 부활]** 버튼 (강조). |
| **UI_Pause** | 일시정지 | - 게임 중단, 옵션(사운드), 포기하기. |

## 4. 연출 및 피드백 (VFX & Feedback Plan)

구현은 후순위(Polish 단계)이나, 기획상 필요한 시각적 피드백 리스트입니다.

### 4.1. 퍼즐 피드백 (Puzzle VFX)

- **Tile Select**: 드래그 시 선택된 타일 발광(Glow) + 연결선(Line Renderer) 표시.
- **Match Pop**: 타일 제거 시 펑 터지는 파티클.
- **Item Create**: 아이템 생성 시 반짝임 + 아이콘 팝업 애니메이션.
- **Grid Refill**: 타일이 사라진 자리에 새 타일이 '뿅' 하고 나타나는 스케일 애니메이션.

### 4.2. 액션 피드백 (Runner VFX)

- **Action Text**: 점프/공격 발동 시 캐릭터 머리 위 텍스트 (JUMP!, BLOCK!) 출력.
- **Boss Phase**: 보스 페이즈 전환 시 화면 흔들림(Shake) + 경고(Warning) 오버레이.
- **Damage**: 피격 시 화면 붉은색 점멸(Vignette).