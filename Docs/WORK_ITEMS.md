# 검증 및 후속 작업 목록

항목 작성·처리 기준은 루트 AGENTS.md를 따른다. 세션 01은 실행 기반, 세션 02는 퍼즐 좌표·입력, 세션 03은 씬 수명·일시정지·장애물 흐름, 세션 04는 보스·전투 계산, 세션 05는 아이템·색 선택·시간 정지, 세션 06은 안전 저장·경계 이어하기 점검이다. 게임 전체의 규칙 검증 결과가 아니다.
공통 등록일·최근 확인일: 아래 모든 항목은 2026-11-13. 환경: macOS 27.2 arm64, Unity 6000.3.14f1 Editor. 관련 커밋: 세션 01 당시 미커밋. 세션 02 시작 시 사용자 명시 승인으로 기존 전체 작업을 `c7354ba`에 보존하고 origin/main에 푸시함. 세션 02 결과는 사용자 요청으로 `47a43f8358e43d35392b0f420057d4b27f1e85fc`에 커밋하고 origin/main으로 푸시함. 세션 03은 사용자 요청으로 `a1b1f7f8ac2bbd38251ee492f1bd0c8315849027`에 커밋하고 origin/main으로 푸시함. 세션 04는 사용자 요청으로 `118b8f8c0036262dba76b658c03595c0347e2e65`에 커밋·푸시함. 세션 05는 사용자 요청으로 `de2a22c520b7c76e9967531c487ad57730cadeec`에 커밋·푸시함. 세션 06은 미커밋·미푸시.
근거 폴더: `output/playtests/2026-11-13-session01/`, `output/playtests/2026-11-13-session02/`, `output/playtests/2026-11-13-session03/`, `output/playtests/2026-11-13-session04/`, `output/playtests/2026-11-13-session05/`, `output/playtests/2026-11-13-session06/`. 상세 명령·복구 경로: 각 `Docs/Development/SESSION-번호-HANDOFF.txt`.

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
- 세션 03 확인: 가상 Mouse로 일시정지 메뉴·재시작·로비 왕복, 빨강/파랑/노랑 장애물 통과→BossPhase까지 자동 검증. 보스 승리까지의 전체 완주는 미검증(V-006).
- 세션 04 확인: 자동 마우스 입력으로 장애물3종→타임어택→그로기→승리/패배 Result 검증(V-007). 보드/HP 수치 보정이 포함되므로 실제 사람의 무보정 완주·난이도 검증은 미수행.
- 검증 방법: Main부터 직접 조작하여 보스 승리까지 무보정 완주. 후속 아이템/보상/성장 통합까지 완료한 것으로 해석하지 않음.
- 관련: I-001, I-002, I-005, D-001.

## 미해결

### I-003 — HUD 겹침과 한글 글리프 누락
- 관련: Game.unity, GameStateDebugPanel.cs, GameReadyPanel.cs, TextMesh Pro 폰트.
- 증상·영향: 디버그 패널이 상단 HUD와 겹치고 시작 버튼 한글 등이 사각형으로 표시됨. 실제 캡처 02/03에서 확인.
- 재현: Game View 1284×2778에서 Main→Play. 세션 01 `02-game-ready.png`, `03-game-running.png` 참고. 세션 02의 1080×1920/1284×2778 캡처에서도 상단 디버그 HUD 겹침이 남음.
- 원인: 테스트용 디버그 UI가 활성 상태이며 기본 LiberationSans SDF가 한글을 제공하지 않음.
- 세션 03 부분 조치: 기존 디버그 패널은 활성 플레이에서 제외. Pause 메뉴와 READY 표시는 추가했으나 경고/HP 간격이 좁은 현상과 한글 폰트 문제는 남음. 세션 03 03/04/05 PNG에서 확인.
- 세션 04 부분 조치: 비활성 BossHUD를 활성화하고 CanvasGroup 누락 오류 수정, 순서·HP·타이머/그로기·강화 표시 연결. 보스와 플레이어 HP 간격 조정, CHAIN/TIER 표시. 승리 CONTINUE는 실제로 RetryGame에 연결되어 있으므로 기능은 다시하기지만 문구는 세션 08에서 명확히 정비.
- 세션06 부분 조치: 준비 화면 CONTINUE/NEW GAME과 마지막 경계 정책을 영어로 표시하여 실제 선택 가능. 안내와 버튼 겹침 수정. 전체 한국어 폰트/결과 문구 정비는 남음.
- 다음 조치: 세션 08에서 한글 폰트·HUD·안전 영역 정비. 기존 UI와 리소스 보존.
- 관련: D-001.

### I-004 — 후속 성장 데이터 ID 공란
- 관련: Resources/Data/Characters/CharacterData.asset, Equipment/EquipmentData.asset, DataManager.cs.
- 증상·영향: 시작 시 CharacterData/EquipmentData ID 공란 경고. 해당 데이터는 ID 조회 사전에 등록되지 않음. Main→Game 진입은 가능.
- 재현: Main 플레이 시작 로그의 'ID가 없습니다'.
- 다음 조치: 세션 07에서 승인된 고양이·고정 장비 데이터 계약과 함께 ID를 정하고 연결. 임의 성장 데이터 선행 구현은 하지 않음.

## 실기기 테스트 필요

### D-001 — Android/iOS 세로 화면과 입력
- 관련: Main/Game 씬, New Input System, TileInputHandler, DualCameraRig, TMP, PlayerSettings.
- 대상: Android 및 iOS 실제 터치 기기, 노치·홈 바 기기, 9:16 및 더 긴 화면.
- 절차: Main→Game 진입, Play/시작 터치, HUD/안전 영역/39칸 전체 표시, 인접 드래그·되돌리기·오인식·보정 확인. 각 플랫폼의 관련 대기 항목을 함께 검증하고 실제 화면 촬영.
- 기대 결과: 화면 잘림·중복 입력·글리프 누락 없이 버튼과 타일 입력이 시각 위치와 일치.
- 기존 확인: 세션 01은 52칸/1284×2778 및 콜백 경로. 세션 02는 Mac Editor에서 39칸 전체 표시와 마우스/Enhanced Touch 가상 기기 입력, 터치 1.3배 겹침 영역의 가까운 중심 선택, 1080×1920 및 1284×2778 렌더링을 확인(V-005). 터치 상단 보정은 0으로 두고 실기기 튜닝 대기. 기기·시뮬레이터·모바일 빌드는 실행하지 않음.
- 세션 03 추가 대상: 실제 기기에서 장애물 경고 중 정지→재개, 실패 재도전 대기 중 정지, Boss 상태 복귀, 재시작·로비 반복. HP·예약·남은 시간이 보존되고 입력이 중복되지 않아야 함. Mac 자동 검증 V-006과 별개.
- 세션 04 추가 대상: 실제 보스 순서 입력, 그로기 공격/아이템 구분, 패턴·그로기 중 정지/재개, 타이머·강화·승패 표시. Mac 가상 마우스 검증 V-007과 별개.
- 세션05 추가 대상: 6종×2티어 단독 터치/같은색 체인·빠른 반복 입력·연쇄·종류별 한도·변색 후 종류 표시, 6색 창/중첩Pause/색 선택 뒤 같은 상태 복귀, TimeFreeze 중 실제 터치와 장애물 이동·앱 전환. 자동 Mouse 성공을 실기기 결과로 대체하지 않음.
- 대기 사유: 연결된 실기기를 사용하지 않았으며 세션 08 UI 정비가 남음.
- 관련: V-001, V-002, I-001, I-003.

### D-002 — 모바일 저장·앱 수명·파일 교체 검증
- 등록일·최근 확인일: 2026-11-13.
- 관련: SaveStore, SaveManager, GameReadyPanel, PlayerSettings, Application.persistentDataPath.
- 대상: Android/iOS 실제 Player와 앱별 저장 공간. 저장 직전/중/직후 백그라운드·강제 종료, OS 저장 실패/공간 부족, 앱 업데이트 후 구버전/미지원 파일.
- 절차·기대: 장애물 성공/실패·보스 성공/실패/그로기 경계에 반복 종료→재실행. 주/백업 중 온전한 한 문서로 코인/장비/영수증/보드/효과가 같은 revision으로 복원되고 원본 손상 파일은 보존. 미지원 파일 덮어쓰기 금지. New Game은 진행만 초기화.
- 기존 확인: Mac Editor 실제 File.Replace/Flush(true), temp 완료 직후 예외 주입, 손상·체크섬·백업·미지원 버전·독립 Unity 프로세스 재실행 V-010 통과. 모바일 IL2CPP/전원 차단/실제 디스크 장애 시험이 아님.
- 대기 사유: 연결 실기기와 모바일 Player 미사용. 세션08 또는 해당 플랫폼 환경 확보 시 실행. D-001 입력/UI 항목도 함께 확인.
- 관련: V-001, V-010, D-001.

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

### I-002 — 씬 재진입·싱글톤 참조 유실 해결 완료
- 등록일·최근 확인일·수행일: 2026-11-13. 기준선 커밋 `47a43f8`; 수정은 세션 03 미커밋 작업 트리.
- 관련: SceneSingleton.cs, Game/Score/Stamina/SaveManager, BossController, DataManager, SceneLoader, PuzzleBoardManager.
- 기존 증상: Main→Game 재진입 시 persistent Managers/BossController가 씬 객체를 참조하고 중복 루트를 파괴하여 보드 초기화 유실. 세션 01 lifecycle-results.txt에 실패 근거 보존.
- 조치: 씬 서비스는 씬과 함께 소멸하는 SceneSingleton으로 분리. 공유 루트 중복 시 해당 컴포넌트만 제거, Awake 소유자 가드, 이벤트 구독 해제, 씬 이동 시 timeScale 복원. DataManager만 전역 데이터 수명 유지.
- 검증: Unity 6000.3.14f1 Mac Editor Main→Game→Main 3회, 매회 39칸·서비스 각 1개·EventSystem 1개·Save 씬 참조 유효·DataManager 동일 인스턴스·파괴된/중복 이벤트 구독 0. 공유 Managers/Boss 루트에 중복 컴포넌트 생성해도 원래 루트 보존. Result 재시작도 통과.
- 근거: 세션 03 play-results.txt, play.log, 01-reentry-board.png, V-006. Player 빌드와 실기기 검증은 V-001/D-001 유지.

### V-006 — 세션 03 상태·장애물 자동 검증 완료
- 등록일·최근 확인일·수행일: 2026-11-13. 관련 커밋 기준선 `47a43f8`; 수정은 로컬 미커밋.
- 관련: GameManager, SceneSingleton, Runner/Obstacle 스크립트, GamePausePanel, BossController/BossPatternBase, Session03Validation, Session02PlayDriver.
- 환경: macOS 27.2 arm64, Unity 6000.3.14f1 Editor, 실제 렌더 1080×1920. 활성 대상 iOS이나 실제 iOS 실행 아님.
- 결과: CLI 컴파일 종료 0, 그래픽 플레이 225 assertion 통과·런타임 오류 0. 씬 왕복·중복 컴포넌트, Result 정지·재시작, 실제 UI Pause/Resume/Restart/Home 입력 확인.
- 보존: Runner/Boss 원래 상태, 보드 색·아이템·HP·거리·경과시간·행동/가속/재도전 타이머·장애물 위치/인스턴스·성공 예약이 정지 중 보존. 재시작은 전부 초기화. 보스는 격리된 기존 타이머/HP 재개 검사를 별도 통과.
- 장애물: 기본 StageData 250/5=50초 기본 이동, 2초 사전 경고, 입력 구간 밖 조기 매칭 거부, 정답 완료 예약, 애니메이션 종료 후에도 예약 성공, 오답 매칭 0.5초 잠금, 실패 20 피해·2초 후 같은 인덱스/새 인스턴스, 반복 실패 중 진행률 고정·자연 감소 유지, 보라 1.5배/3초, 노랑 성공 충돌 피해 0. 빨강→파랑→노랑→보스 진입 이벤트 1회 확인.
- 사용자 데이터: SaveManager.stageData는 메모리에서만 null로 격리. 사용자 저장 파일 SHA 목록 전후 일치. 입력 장치·설정/최고 점수/임시 Game View 복원. 씬 파일 저장 없음.
- 초기 검증 도구 실패: compile-attempt01은 StageData 새 필드 추가 누락 후 수정. attempt01/02는 입력 처리 전 값을 읽는 검사 순서 문제로 실패. 런타임 LateUpdate 검사로 수정하고 최종 성공. 실패 로그도 보존.
- 근거: output/playtests/2026-11-13-session03/play-results.txt, play.log, compile.log, 실제 PNG 6장. 01 재진입 보드, 02 Pause 메뉴, 03 Jump 사전 경고, 04 실패 재도전, 05 Block READY, 06 Boss 진입. 이미지 직접 열람 확인.
- 회귀 확인: 세션 03 코드에서 세션 02 좌표 9,855 및 Play 368 assertion 재실행 통과, 프로세스 종료 0. regression-session02/ 결과와 regression-session02.log. 기존 세션 02 결과 파일은 덮어쓰지 않음.
- 한계: 보스 내부 전투는 기본 비활성(세션 04). 전체 승리·아이템/성장/저장 정책·Player/실기기 검증이 아님. V-001/V-002/I-003/I-004/I-005/D-001 유지.

### I-007 — 기존 색상 효과 데이터 연결 오류 해결 완료
- 등록일·최근 확인일·수행일: 2026-11-13. 기준 커밋 a1b1f7f, 세션 04 로컬 수정.
- 관련: Resources/Data/ColorEffectMappingData.asset, TileMatcher, ItemEffectType.
- 증상·원인: 에셋의 effect0..5가 None 포함 열거형에 한 칸씩 잘못 연결. 노랑 아이템이 폭발 대신 TimeFreeze가 되어 확장 제거 검사가 실패. attempt02-play-results/log 근거.
- 조치: 기존 GUID/타입을 유지하고 Red→Recovery1, Blue→TimeFreeze2, Yellow→Blast3, Purple→LineClear4, Orange→PowerUp5, Cyan→ColorShift6으로 수정.
- 검증: 최종 세션 04에서 실제 노랑 아이템 체인 폭발 제거>직접 연결 수, 점수/회복은 실제 제거 수, 공격은 직접2개 기준4피해로 확인. 전체6종 2티어 기능 완료는 I-005/세션05로 유지.

### V-007 — 세션 04 보스·전투 계산 검증 완료
- 등록일·최근 확인일·수행일: 2026-11-13. 환경 Unity6000.3.14f1 macOS27.2 arm64 Editor, 1080×1920. 기준 커밋 a1b1f7f, 세션04 미커밋 작업 트리.
- 관련: BossController/TimeAttackPattern, BalanceData/BossBattleConfig, Score/StaminaManager, TileMatcher, RunnerController, BossHUD/GameHUD, Session04Validation.
- 최종 결과: CLI 컴파일 및 그래픽 Play 프로세스 종료0. 178 assertion 통과, 게임 경로 런타임 오류0. 실제 마우스 이벤트로 장애물3종→보스→그로기→승리/패배, 실제 PNG7장 촬영/열람.
- 공식: 체인 경계2/4/5/8/9/39, 명세 예시30피해, 실제 제거11의 점수220/440·회복3.3, 패턴 아이템 보존·생성/효과 금지·제거0 점수/회복0 입력, 성공 고정 피해0, 직접 연결·확장 제거 분리, 청록 방어권·무피해, 최대 버프·1회 소모/주행 이월.
- 상태: 순서3/10초/HP120/실패20/그로기5, 요구 색 인접 쌍, 보스 자연 감소 정지, 패턴/그로기 Pause·Resume, 시간 정지 API, 그로기 끝 강화(50%/20%, 단계마다 길이+1/시간-1 최소6), HP0 즉시 단일 승리(실제 매칭·직접 API), 실패 패배·Result 정지·재시작·씬 구독 정리.
- 회귀: 공개 시계/피해 계약에 맞춰 세션03 도구를 갱신하고 별도 폴더에서 273 assertion 통과·종료0. Main 왕복3회/중복 컴포넌트/재도전/예약/버프·정지 유지. 이전 세션03 출력은 보존.
- 세션02 회귀: Geometry9,855 / Play368 assertion 통과·종료0·런타임 오류0. 보드/마우스·Enhanced Touch/보정 API/두 해상도 검증. regression-session02/와 regression-session02.log 근거.
- 초기 실패: 비활성 BossHUD의 CanvasGroup 가짜 null 처리 수정(attempt01), 기존 효과 열거형 연결 수정(I-007, attempt02). pass01의165개 성공 뒤 주행 효과 이월 검사를 추가해 최종178개 통과.
- 한계: 입력/보드/HP의 격리된 테스트 배치 포함. 실제 사람 무보정 완주/실기기/전체 아이템·저장·성장 성공 아님. V-001/V-002/V-008/I-003/I-004/I-005/D-001 유지.
- 사용자 저장 파일 SHA 전후 일치, 최고 점수/입력/임시 Game View 복원, 씬 저장 없음. 근거 output/playtests/2026-11-13-session04/{play-results.txt,play.log,regression-session03/}, SESSION-04-HANDOFF.txt.

### V-008 — 시간 정지 API·아이템 통합·구간 이월 검증 완료
- 등록일·최근 확인일·수행일: 2026-11-13. 기준 커밋118b8f8, 세션05 로컬 수정.
- 관련: StaminaManager, BossController, ItemEffectEvents, TileMatcher, Session05Validation.
- 기존 미검증: 파랑 이벤트가 시계 API에 미연결, Stamina와 Boss의 독립 시계/주행→보스 이월 확인 필요.
- 조치: 실제 파랑3/7초 이벤트 연결. StaminaManager.DecayFreezeRemaining 하나를 사용하고 Boss는 같은 값을 읽음. 입력/연출/장애물은 전역 timeScale로 멈추지 않음.
- 검증 환경·결과: Unity6000.3.14f1 Mac Editor 실제 가상 Mouse 단독 사용, 짧은 효과 재사용 max/긴 값 보존, 자연 감소만 정지·경과/주행 지속·타일 행렬 변화·장애물 이동, 게임 Pause 잔여 보존, Boss 진입 동일 잔여·패턴/그로기 시간 정지. 색 선택/중첩Pause 동안 효과 시계도 보존. 재시작0 초기화.
- 근거: 세션05 play-results.txt/play.log, 05-frozen-obstacle.png, 07-items-groggy.png, V-009. 디스크 저장 이월은 I-005/세션06, 실기기 D-001 유지.

### I-008 — 기존 타일 애니메이션 미연결 해결 완료
- 등록일·최근 확인일·수행일: 2026-11-13. 관련: PuzzleBoardManager.Awake, TileAnimation, Game.unity의 tileAnimation:null.
- 증상·재현: SpawnTile 후 시간 경과에도 실제 Tilemap 행렬이 변하지 않음. 보드 GetComponent<TileAnimation>()도 null. 세션05 attempt02의 TimeFreeze 중 연출 검사에서 확인.
- 조치: 기존 TileAnimation 코드를 보드 씬 소유 컴포넌트로 런타임 연결, SetTransformMatrix에 필요한 LockTransform 해제. 기존 .meta/씬 연결은 보존.
- 확인: 새 생성 시 실제 행렬 변화와 Freeze 중 지속, Pause/색 선택 중 정지, 재시작 CancelAll 초기화. 새 생성과 이전 제거의 동일 셀 코루틴 취소 계약 유지. V-009 및 보스 회귀 통과.

### V-009 — 세션05 아이템·상태·전투 자동 검증 완료
- 등록일·최근 확인일·수행일: 2026-11-13. 기준 커밋118b8f8, 세션05 미커밋 작업 트리.
- 관련: TileMatcher/TileInstance/TileInputHandler/PuzzleBoardManager/TileAnimation, ColorSelectionPanel/ItemTileLabels, GameManager/GamePausePanel/GameHUD, Score/StaminaManager/BossController, GameEvents, Session05Validation.
- 환경: macOS27.2 arm64 Unity6000.3.14f1 Editor, 실제1080×1920. 활성 iOS 대상이지만 Player/실기기 아님.
- 결과: CLI 컴파일 종료0, 최종 Play316 assertion 통과·종료0·게임 경로 런타임 오류0. 실제 Mouse 이벤트로 6종×2티어 생성/단독 효과, 마지막 셀·종류별2개·총11개, 한도 시 점수/제거 유지.
- 연쇄: 기존6종 재귀·겹침은 셀별1회, 실제 고유 제거수 점수/회복, 7/19 반경·5/17 라인, 동일종류2개 소비 후 새 생성, 새 타일/아이템은 연쇄 재발동 없음. 모든 효과의 기존 보드39개 관찰 후 단일 집계/리필.
- 선택/보존: Color/ItemType 분리로 전체 변색 후 종류/레벨/개수 보존·원래 효과 발동. Lv2 실제6색창, 퍼즐 차단·Runner/그로기 복귀·중첩Pause/같은 장애물 유지·다중선택 큐. 단독 사용은 MatchCompleted와 별도 이벤트.
- 보스: 패턴 아이템 효과/생성 금지·기존 아이템 보존·실제7개 점수280/회복2.1·0제거 무보상·단독 입력 거부. 실제50/100% 최대 버프·다음 공격1회 소모, 치명 연쇄에서 청록 선택 취소/단일 승리, Restart/Home 초기화·구독 정리.
- 시간 정지 상세: V-008. 보드/HP 검사 배치를 포함하며 수동 무보정 완주/난이도 검증이 아님.
- 보스 회귀: 현재 소스에서 세션04 Play180 assertion 통과·종료0·런타임 오류0. 새 ItemUse 이벤트로 구독 검사 수가 늘어 기존178보다2개 많음. 기존 세션04 출력은 별도 폴더로 보존.
- 보드/입력 회귀: 세션02 Geometry9,855 / Play368 통과·종료0·런타임 오류0. 새 단독 입력/타일 연출 후 기존39칸/6방향/보정/마우스·가상EnhancedTouch/두 해상도 유지. regression-session02/ 근거.
- 초기 실패: 검사 지역변수 이름 충돌 수정(compile-attempt01-validator), 잘못된 반경 밖 검사 배치 수정(attempt01), 실제 미연결 애니메이션 수정(I-008, attempt02). 실패 증거 보존.
- 사용자 저장 파일명/SHA 전후 일치, 최고 점수/입력 장치·설정/임시GameView 복원, 씬 파일 저장 없음. 실제PNG8장.
- 근거: output/playtests/2026-11-13-session05/{play-results.txt,play.log,regression-session04/}, SESSION-05-HANDOFF.txt. V-001/V-002/I-003/I-004/I-005/D-001 유지.

### I-005 — 저장·구간 이어하기 정비 완료
- 등록일·최근 확인일·수행일: 2026-11-13. 기준 커밋de2a22c, 수정은 세션06 미커밋.
- 관련: Core/Persistence/SaveData/SaveStore, SaveManager, GameManager, 보드/러너/보스 복원 API.
- 기존 문제: 구형 체크포인트는 보드/아이템 정체성/효과/보스 상태 부족, 읽기 실패 파일 삭제·직접 덮어쓰기. 이전 상태/전투/아이템 차이는 세션03~05에서 완료.
- 조치: 버전1·체크섬·명시적 진행유무·단일 진행/영구 문서, temp Flush→원자 교체·유효 이전 문서 백업, 손상 원본 보존·미지원 버전 거부. 구형 checkpoint 파일은 원본 유지하며 불완전 자동 변환 안 함.
- 확인: 실제 장애물/보스 안정 경계 저장·동일39칸/재색칠된 ItemType/HP·점수/거리·재도전/가속·시간 정지·보스 단계/다음 순서/버프·방어권 복원. 그로기 종료에서 피해·강화 저장. Pause와 경계저장이 겹쳐도 기반 실행 단계 보존. V-010 근거.
- 원래 게임 저장 폴더는 없었고 앱 메타데이터2개를 외부 백업 후 검사는 별도 sandbox에서 수행. 실제 기존 사용자 진행이 있었다고 간주하지 않음. 경로·해시·복구 절차 SESSION-06-HANDOFF.txt.
- 한계: 모바일/Player D-002/V-001, 실제 보상/장비 능력치/성장 UI는 세션07. 영구 데이터 계약을 성장 구현 완료로 해석하지 않음.

### V-010 — 세션06 저장·복원·독립 프로세스 검증 완료
- 등록일·최근 확인일·수행일: 2026-11-13. 환경 macOS27.2 arm64 Unity6000.3.14f1 Editor, 실제1080×1920. 기준de2a22c, 세션06 미커밋.
- 관련: SaveData/SaveStore/SaveManager, GameManager, PuzzleBoardManager, Runner/Obstacle, BossController/TimeAttackPattern, GameReadyPanel/GameHUD, Session06Validation.
- 최종 결과: CLI 컴파일 종료0. 최종486 Play assertion + 완전히 새 Unity 프로세스58 assertion 통과, 각 종료0·게임 경로 런타임 오류0.
- 파일: 실제 temp/Flush/Replace/백업, 미커밋 temp 무시, 정상JSON 체크섬 불일치/구문 손상·유효 백업 복구/원본 보존·둘다 손상/미지원 버전 거부, 잘못된 보드·NaN·규칙 서명 거부. 음수 코인 거래 거부 시 기존 문서 불변.
- 게임: 실제 마우스 UI/매칭으로 성공·재도전·보스 실패/성공/그로기 진입·종료,39칸 색/레벨/ItemType·HP/점수/거리/시간·가속/시간정지·재도전/행동 잔여·보스HP/단계/다음순서/버프·방어권·난수 복원. 복원 첫 시뮬레이션 틱 전에 정확 비교. 피해/패턴 결과/완료 이벤트 중복 없음.
- 영구 계약: 가상321→421코인/장비2개/클리어·첫보상 플래그/영수증·PendingClear를 원자 커밋. 중복 완료/같은 runId 지급은 문서 변경 없이 거부. New Game은 영구기록 유지. 실제 보상 선정/장비UI·능력치 연동은07 미구현.
- 독립 프로세스: 실제 강화 단계1(화면Phase2), HP48/120·재색칠된 파란PowerUp2·효과/가속·계획된 다음순서·영구기록/첫보상 플래그 복원. 실제 UI Retry는 진행만 초기화.
- 아이템 회귀: 세션05 Play316 통과·종료0·런타임 오류0. 기존 출력 보존, regression-session05/ 근거.
- 사용자 보호: 원래 게임 saves 경로의 파일명/SHA 전후 일치, Editor 전용 경로 override·입력/최고점/GameView 복원. Unity 내부 메타데이터와 게임 파일 구분. 실제 스크린샷9장.
- 보드/입력 회귀: Geometry9,855 / Play368 통과·종료0·런타임 오류0. regression-session02/ 근거. 기능 검증 후 표식의 밝은 프레임 대비를 어두운 배지로 보완하고 최종Cold58에서 표식 활성/실제 화면/UI 재시작 확인.
- 초기 실패·중간 성공: null 클래스의 기본 객체 직렬화→hasCheckpoint, 첫 프레임 뒤 비교→복원 콜백 즉시 비교, 보스 비활성 전환 타이머 정규화, 검사 오답 입력 시간 초과 배치 수정. attempt01~04/pass01~02 접두사로 로그/PNG 보존.
- 근거: output/playtests/2026-11-13-session06/{play-results.txt,cold-results.txt,play.log,cold.log,schema-example.json,environment.txt}, SESSION-06-HANDOFF/SCHEMA.txt.
- 한계: 격리된 파일·메모리 배치, Mac Editor·정상 프로세스 재실행/중단 주입. 실제 사용자 무보정 플레이/성장·모바일 저장의 증거가 아님. V-001/V-002/I-003/I-004/D-001/D-002 유지.
