# 검증 및 후속 작업 목록

항목 작성·처리 기준은 루트 AGENTS.md를 따른다. 세션 01은 실행 기반, 세션 02는 퍼즐 좌표·입력 점검이다. 게임 전체의 규칙 검증 결과가 아니다.
공통 등록일·최근 확인일: 아래 모든 항목은 2026-11-13. 환경: macOS 27.2 arm64, Unity 6000.3.14f1 Editor. 관련 커밋: 세션 01 당시 미커밋. 세션 02 시작 시 사용자 명시 승인으로 기존 전체 작업을 `c7354ba`에 보존하고 origin/main에 푸시함. 세션 02 수정은 미커밋.
근거 폴더: `output/playtests/2026-11-13-session01/`, `output/playtests/2026-11-13-session02/`. 상세 명령·복구 경로: 각 `Docs/Development/SESSION-번호-HANDOFF.txt`.

## 미검증

### V-001 — Player 빌드와 새 환경의 패키지 복구
- 관련: Packages/manifest.json, packages-lock.json, PuzzleBoardManager.cs, Game.unity의 `_tileEntries`.
- 확인할 동작·기대 결과: 캐시 없는 환경에서 패키지 복구 후 데스크톱/모바일 Player 컴파일 및 타일 렌더링 성공.
- 미검증 사유: 이번 세션은 현재 Mac의 Editor 컴파일·플레이 검증. Player 빌드와 캐시 삭제는 실행하지 않음.
- 검증 방법: 별도 복원 폴더에서 정확한 Unity 버전으로 의존성을 복구한 뒤 Player 빌드·실행. SSH Git 패키지 접근 권한 필요. 원본 Library 삭제 금지.
- 관련: D-001. Git 패키지 캐시 3개는 외부 백업의 보조 압축 파일에 보존.

### V-002 — 실제 포인터·터치 입력과 전체 스테이지
- 관련: MainMenuController.cs, GameReadyPanel.cs, TileInputHandler.cs, Runner/Boss 스크립트.
- 확인할 동작·기대 결과: 실제 클릭/터치로 Play와 시작 버튼이 반응하고 드래그부터 장애물·보스·결과까지 진행.
- 세션 01 확인: 실제 Button.onClick 콜백 호출.
- 세션 02 확인: 가상 Mouse/Touchscreen에 New Input System 상태 이벤트를 주입하여 실제 UI/Update/Enhanced Touch 경로 검증 완료(V-005). 실제 사람의 마우스 조작·물리 터치 기기·전체 완주는 수행하지 않음.
- 검증 방법: I-001은 완료. I-002 및 후속 전투 기능을 각 담당 세션에서 정비한 뒤 Main부터 직접 조작하여 완주. 보스 전투·아이템·성장 구현 완료로 해석하지 않음.
- 관련: I-001, I-002, I-005, D-001.

## 미해결

### I-002 — Main↔Game 반복 진입의 싱글톤·씬 참조 유실
- 관련: 외부 NineMood.Services.Core.Singleton, Game.unity의 Managers, GameManager.cs, SaveManager.cs, PuzzleBoardInitializer.cs, BossController.cs.
- 증상·영향: 첫 Game 진입은 정상. Main으로 나갔다 다시 진입하면 기존 persistent Managers/BossController와 새 씬 인스턴스가 중복되고 SaveManager가 파괴된 씬 참조를 유지. 보드가 초기화되지 않음.
- 재현: CLI `NyanLinkSession01Validation.RunLifecycle` 또는 Main→Play→시작 후 SceneLoader.LoadMainMenu()→Play. `lifecycle-results.txt`, `lifecycle-focused.log`, `05-game-reentry.png` 참고.
- 원인: 여러 싱글톤과 PuzzleBoardInitializer가 동일한 DontDestroyOnLoad 루트에 있고, 외부 싱글톤은 중복 시 GameObject 전체를 파괴함. 파괴 예정인 중복 GameManager도 Awake 이후 이벤트를 잠시 구독할 수 있음.
- 다음 조치: 세션 03에서 영구 매니저/씬 소유 시스템 경계와 중복 Awake 가드, 씬 참조 재연결·이벤트 구독 수명 정비. 정확한 현재 검사 결과는 lifecycle-results.txt를 우선.
- 세션 02 확인: 기존 공유 DontDestroyOnLoad 루트와 참조 소유 구조는 수정하지 않음. 이번 반복 진입 재시험은 세션 03으로 이관하며 세션 01 재현 근거를 유지. 최초 진입은 새 입력·보드로 통과.
- 관련: V-002, I-005.

### I-003 — HUD 겹침과 한글 글리프 누락
- 관련: Game.unity, GameStateDebugPanel.cs, GameReadyPanel.cs, TextMesh Pro 폰트.
- 증상·영향: 디버그 패널이 상단 HUD와 겹치고 시작 버튼 한글 등이 사각형으로 표시됨. 실제 캡처 02/03에서 확인.
- 재현: Game View 1284×2778에서 Main→Play. 세션 01 `02-game-ready.png`, `03-game-running.png` 참고. 세션 02의 1080×1920/1284×2778 캡처에서도 상단 디버그 HUD 겹침이 남음.
- 원인: 테스트용 디버그 UI가 활성 상태이며 기본 LiberationSans SDF가 한글을 제공하지 않음.
- 다음 조치: 세션 08에서 한글 폰트·HUD·안전 영역 정비. 세션 01은 기존 UI와 리소스를 보존.
- 관련: D-001.

### I-004 — 후속 성장 데이터 ID 공란
- 관련: Resources/Data/Characters/CharacterData.asset, Equipment/EquipmentData.asset, DataManager.cs.
- 증상·영향: 시작 시 CharacterData/EquipmentData ID 공란 경고. 해당 데이터는 ID 조회 사전에 등록되지 않음. Main→Game 진입은 가능.
- 재현: Main 플레이 시작 로그의 'ID가 없습니다'.
- 다음 조치: 세션 07에서 승인된 고양이·고정 장비 데이터 계약과 함께 ID를 정하고 연결. 임의 성장 데이터 선행 구현은 하지 않음.

### I-005 — 기존 상태·전투·저장은 1.1 규칙 구현 완료 상태가 아님
- 관련: GameManager.ResumeGame, BossController, TileMatcher, SaveManager.
- 정적 확인된 남은 작업: ResumeGame은 정지 전 상태 대신 RunnerPhase로 복귀. BossController는 패턴 성공 고정 피해와 기존 그로기 계산을 사용. SaveManager는 보드/보스/지속 효과/버전/원자 쓰기를 저장하지 않으며 읽기 실패 파일을 삭제하는 기존 경로가 있음.
- 영향·재현 조건: 보스 중 정지/재개, 보스 패턴 성공·그로기 공격, 구간 저장·손상된 저장 파일 로드. 이 동작들은 이번 세션에서 실행하지 않았음. 코드 분기로 확인한 차이이며 실제 증상의 정도는 후속 실행 검증 필요.
- 다음 조치: 세션 03/04/05/06에서 순서대로 정비. 기존 저장 파일로 실험하지 말고 복사본 또는 격리 경로 사용. 이번 smoke는 SaveManager.stageData를 메모리에서만 null로 하여 사용자 체크포인트 읽기/쓰기를 차단.
- 관련: I-002, V-002.

## 실기기 테스트 필요

### D-001 — Android/iOS 세로 화면과 입력
- 관련: Main/Game 씬, New Input System, TileInputHandler, DualCameraRig, TMP, PlayerSettings.
- 대상: Android 및 iOS 실제 터치 기기, 노치·홈 바 기기, 9:16 및 더 긴 화면.
- 절차: Main→Game 진입, Play/시작 터치, HUD/안전 영역/39칸 전체 표시, 인접 드래그·되돌리기·오인식·보정 확인. 각 플랫폼의 관련 대기 항목을 함께 검증하고 실제 화면 촬영.
- 기대 결과: 화면 잘림·중복 입력·글리프 누락 없이 버튼과 타일 입력이 시각 위치와 일치.
- 기존 확인: 세션 01은 52칸/1284×2778 및 콜백 경로. 세션 02는 Mac Editor에서 39칸 전체 표시와 마우스/Enhanced Touch 가상 기기 입력, 터치 1.3배 겹침 영역의 가까운 중심 선택, 1080×1920 및 1284×2778 렌더링을 확인(V-005). 터치 상단 보정은 0으로 두고 실기기 튜닝 대기. 기기·시뮬레이터·모바일 빌드는 실행하지 않음.
- 대기 사유: 연결된 실기기를 사용하지 않았으며 세션 02/08 정비가 남음.
- 관련: V-001, V-002, I-001, I-003.

## 완료 이력

### V-003 — 작업 보존·복원 검증 완료
- 등록일·최근 확인일·수행일: 2026-11-13.
- 관련: 프로젝트 전체 소스/리소스/.meta/Docs/미추적·무시 파일, .git/index/로컬 이력.
- 확인: 외부 백업 503개 파일 SHA256 일치. 별도 폴더에 두 압축 파일을 실제 복원하여 Git staged/unstaged/untracked 상태가 백업 당시와 바이트 단위 일치. git fsck --full 종료 0(기존 dangling tree만 표시).
- 근거: 외부 백업 manifest.json, restore-verification.txt, git-status.txt. 복구 방법은 SESSION-01-HANDOFF.txt.

### V-004 — 컴파일·씬·참조 기준선 확인 완료
- 등록일·최근 확인일·수행일: 2026-11-13.
- 관련: Unity 6000.3.14f1, 패키지, Main/Game, 스크립트 GUID, Stage/Balance/Boss 데이터.
- 확인: 최초 CLI batch 컴파일 종료 0. 수정 후 smoke 실행 과정에서도 컴파일 성공. 에셋 GUID 중복/미해결 GUID/누락 스크립트 메타/Main·Game 로컬 fileID 누락 모두 0. Unity 씬 검사에서 missing script 0, 활성 EventSystem 각 1, 씬 내 싱글톤 타입별 1.
- 한계: 반복 진입 시 런타임 중복은 I-002로 유지. Player 빌드 검증은 V-001.
- 근거: compile-baseline-full.log, smoke.log, audit.txt, reference-audit.json.

### I-006 — 최초 진입의 씬·데이터 연결 정비 완료
- 등록일·최근 확인일·수행일: 2026-11-13.
- 관련: SceneLoader, DataManager, Main/Game, StageData/BossBattleConfig/LootTableData, 테스트 씬 생성 도구.
- 기존 문제: MainMenu라는 존재하지 않는 반환 씬 이름, 빈 Stage ID/참조, 공통 데이터 미로드, Game의 중복 EventSystem, 빌드용 타일 참조 배열 공란.
- 조치: Main 이름 정합, Resources 공통 데이터 로드와 중복 ID 경고, 테스트 데이터 연결, 활성 EventSystem 1개와 New Input 모듈, 기존 18종 타일 직렬화, 보류 보스 패턴 연결 해제·컴포넌트 비활성화. 모든 기존 .meta GUID 유지.
- 확인 환경·결과: 실제 Mac Unity 플레이에서 Main→Play→Briefing→시작→RunnerPhase 통과, 최초 경로 런타임 오류 0. 실제 화면 01/02/03 촬영. 실제 포인터/터치는 V-002.
- 근거: smoke-results.txt, smoke.log, lifecycle-focused.log. 보드 규격·화면 문제는 I-001/003에 남김.

### I-001 — 52칸 보드와 가로 잘림 해결 완료
- 관련: PuzzleBoardManager.CreateGrid, HexOffsetUtils/HexCoordinates, DefaultGridShape.asset, Game.unity.
- 기존 증상·영향: 첫 Game 진입에서 실제 생성 수 52. 확정 규격 7행 6/5 교차 총 39와 다름. 플레이 캡처에서 양끝 타일이 화면을 벗어남.
- 기존 재현: Unity에서 Main→Play→게임 시작. `03-game-running.png`, `smoke-results.txt`의 tile count=52 참고.
- 원인: 현재 CreateGrid는 width와 width+1 행을 생성하며 기본 데이터 width=7. 카메라 맞춤도 함께 확인 필요.
- 당시 다음 조치: 세션 02에서 odd-r 공통 좌표·마스크·39칸 생성과 카메라/입력 변환을 함께 검증. 세션 01에서는 변경하지 않음.
- 관련: V-002, D-001.

- 수행일·최근 확인일: 2026-11-13. 관련 커밋 기준선 `c7354ba`; 수정은 세션 02 미커밋 작업 트리.
- 조치: 마스크/생성을 0부터 시작하는 odd-r 6/5 교차 7행 39칸으로 통일. 실제 스프라이트 경계와 뷰포트 비율로 카메라를 맞추고 해상도 변경 시 재계산.
- 검증: Unity 6000.3.14f1 Mac Editor 실제 플레이 1080×1920 및 1284×2778에서 모든 타일 경계가 퍼즐 뷰 안에 들어옴. 각 39칸 중심의 마우스/터치 좌표 왕복과 실제 6방향 드래그 통과. 실제 캡처 01/02/03 직접 열람 확인.
- 근거: 세션 02 geometry-results.txt, play-results.txt, play.log, 01-board39-1080x1920.png, 03-board39-1284x2778.png. 실기기 검증 D-001은 유지.

### V-005 — 세션 02 좌표·입력·보정 API 자동 검증 완료
- 등록일·최근 확인일·수행일: 2026-11-13.
- 관련: HexCoordinates/HexUtils/HexOffsetUtils, GridShapeData, PuzzleBoardManager, TileInputHandler, TileMatcher, Session02Validation/PlayDriver.
- 환경: macOS 27.2 arm64, Unity 6000.3.14f1 Editor. 활성 빌드 대상은 iOS이나 검증 실행은 Mac Editor이며 iOS 기기/Player 실행 아님.
- 결과: CLI 좌표·마스크 검사 9,855 assertion 및 실제 게임 프레임의 Play 검사 368 assertion 통과, 최종 프로세스 종료 0, 해당 플레이 경로 런타임 오류 0.
- 좌표: 음수 행 포함 왕복·Cube 합 0·정확한 6이웃/대칭성·알려진 7이웃 오류·거리·이동된 중심의 반경·라인·39칸 마스크/경계.
- 화면·입력: 실제 UI 버튼을 가상 Mouse 이벤트로 조작하여 Main→Game→Start. 6방향·대각선·되돌리기·다른 색 취소/즉시 재시작·Release 단일 처리·제자리 리필·외부 잠금·가상 Enhanced Touch 사용/취소 및 연결된 마우스와 중복 처리 방지 확인.
- 보정: 요구 색 전환·리필 뒤 재검사, 최소 재색칠, 아이템/선택/보호 타일 보존, 보정 불가 시 부분 변경 없음. 실게임의 장애물/보스 호출 연결은 세션 03/04에서 수행.
- 화면: 타일 앵커/이동/회전/비균일 배율에서 중심 선택, 1.3배 터치 겹침 경계 양측의 가까운 중심 판정. 두 세로 해상도에서 렌더 경계/선택선 확인.
- 검증 도구 초기 실패(컴파일 인수/백그라운드/해상도/편집기 Screen 문맥)는 수정 후 재실행. 과거 실패 로그와 최종 성공 로그를 구분해 보존함. 검증 도구의 성공은 실제 사람·기기 조작을 대체하지 않음(V-002/D-001).
- 사용자 저장 보호: SaveManager.stageData를 플레이 메모리에서만 null로 두고, 최고 점수 PlayerPrefs와 원래 입력 설정/장치를 테스트 종료 시 복원. 씬·저장 파일에 테스트 배치를 저장하지 않음.
- 근거: `output/playtests/2026-11-13-session02/geometry-results.txt`, `play-results.txt`, `play.log`, 실제 PNG 3장. 추가 자동 커밋/푸시 없음.
