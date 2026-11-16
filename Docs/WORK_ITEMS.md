# 검증 및 후속 작업 목록

항목 작성·처리 기준은 루트 AGENTS.md를 따른다. 세션 01은 실행 기반, 세션 02는 퍼즐 좌표·입력, 세션 03은 씬 수명·일시정지·장애물 흐름, 세션 04는 보스·전투 계산, 세션 05는 아이템·색 선택·시간 정지, 세션 06은 안전 저장·경계 이어하기, 세션 07은 보상·장비·Lv.5 성장, 세션08은 화면·안전 영역·최소 사운드·통합/Player 검증이다. 게임 전체의 규칙 검증 결과가 아니다.
공통 등록일·최근 확인일: 별도 날짜를 표시하지 않은 기존 항목은 2026-11-13. 환경: macOS 27.2 arm64, Unity 6000.3.14f1 Editor. 관련 커밋: 세션 01 당시 미커밋. 세션 02 시작 시 사용자 명시 승인으로 기존 전체 작업을 `c7354ba`에 보존하고 origin/main에 푸시함. 세션 02 결과는 사용자 요청으로 `47a43f8358e43d35392b0f420057d4b27f1e85fc`에 커밋하고 origin/main으로 푸시함. 세션 03은 사용자 요청으로 `a1b1f7f8ac2bbd38251ee492f1bd0c8315849027`에 커밋하고 origin/main으로 푸시함. 세션 04는 사용자 요청으로 `118b8f8c0036262dba76b658c03595c0347e2e65`에 커밋·푸시함. 세션 05는 사용자 요청으로 `de2a22c520b7c76e9967531c487ad57730cadeec`에 커밋·푸시함. 세션 06은 사용자 요청으로 `fbda2526f941c4e9552866b72e0b4eed0d8b2482`에 커밋·푸시함. 세션07은 사용자 요청으로 cd4d74c3a294ff9cec3c01b517a8cd4ab2416c35에 커밋·푸시. 세션08 35bdffc96dc3cd3f9306ba7e4fc8a6c8e7b0ab63은 2026-11-14 사용자 요청으로 origin/main 푸시·원격 해시 일치 확인. 새 아트·사운드 교체는 검증 후 로컬 커밋(메시지 feat: apply concept pixel art and original chiptune audio), 미푸시.
근거 폴더: `output/playtests/2026-11-13-session01/`, `output/playtests/2026-11-13-session02/`, `output/playtests/2026-11-13-session03/`, `output/playtests/2026-11-13-session04/`, `output/playtests/2026-11-13-session05/`, `output/playtests/2026-11-13-session06/`, `output/playtests/2026-11-13-session07/`, `output/playtests/2026-11-13-session08/`. 상세 명령·복구 경로: 각 `Docs/Development/SESSION-번호-HANDOFF.txt`.

## 미검증

### V-021 — 다음 Android APK의 세로 고정과 기기 회전
- 등록일·최근 확인일: 2026-11-16. 관련: ProjectSettings.asset, NyanLinkAndroidPortrait.cs, NyanLinkAndroidBuild.cs, ANDROID-TEST-BUILD.txt. 관련 ID: I-011, D-001, V-020.
- 확인할 동작·기대 결과: 다음 APK를 기존 진행을 보존해 업데이트한 후 Lenovo TB320FC에서 세로·가로로 기기를 돌려도 게임이 세로 방향을 유지. 로비/준비/플레이/정지/홈 복귀·콜드 실행 각각 확인하고 실제 PNG 촬영.
- 구현: 기본 Portrait, 가로/거꾸로 세로 자동회전 금지, Android resizeableActivity=false. Gradle 후처리에서 Unity Activity의 portrait·비리사이즈와 제조사 방향/디스플레이 방향 override 거부 속성2개 적용. 다른 Activity/권한/런처 보존.
- 수행한 확인: Unity6000.3.14f1/Android 대상 CLI 컴파일·후처리 검사 종료0. 이전 실제 Gradle 매니페스트 복사본에 적용하여 세로·비리사이즈/속성2개/무관 Activity·권한·런처 보존/반복 적용 동일성을 확인. 근거 output/playtests/2026-11-16-android-portrait/{validation.log,results.txt,fixture/}. 관련 커밋은 이번 세로 고정 로컬 커밋(git log 참조). 플레이 테스트를 하지 않아 이번 변경의 새 실제 게임 캡처는 없음.
- 미검증 사유: 사용자가 다음 빌드부터 반영 요청. 현재 설치된 APK는 교체하지 않고 새 APK 생성·설치/물리 회전 검사는 다음 빌드 시 수행. 코드 컴파일과 이전 실제 Gradle 매니페스트 복사본의 후처리 검사는 별도 근거이며 기기 성공으로 해석하지 않음.
- 다음 조치: 다음 Android 빌드의 최종 merged manifest/APK 속성 확인, adb install -r -t로 업데이트 후 실제 회전·기존 저장 복원 검사. 제조사 호환 표시가 남으면 기기의 앱별 방향 호환 정책을 조사하고 원인을 확정. 기기 전체 자동회전 설정을 임의 변경하지 않음.

### V-019 — 회사 HTTPS 서버 게시·캐시·PC 브라우저 교차 검증
- 등록일·최근 확인일: 2026-11-16. 관련: NyanLinkWebBuild.cs, Assets/WebGLTemplates/NyanLink, Tools/WebHosting, Docs/Development/WEB-DEPLOYMENT.txt. 관련 ID: V-018, D-003.
- 확인할 동작·기대 결과: https://fairstudios.kr/nyan-link/에서 새 방문자의 게임 로드·입력·저장 복원이 성공하고, 빌드 교체 후 일반 새로고침으로 새 버전 실행. Mac Safari/Windows Edge에서도 같은 흐름 확인.
- 확인 환경: 공개 HTTPS 응답 기준 /legend-stone/는200·Server:nginx, /nyan-link/는404(2026-11-16). 로컬 브라우저 결과와 회사 서버 결과는 구분.
- 미검증 사유: 요청 범위는 웹 빌드와 배포 준비. 회사 호스팅 계정/실제 문서 루트/업로드 연결은 제공되지 않아 업로드·서버 변경 미수행. Safari/Windows 브라우저는 이번 자동 검사에서 실행하지 않음.
- 검증 방법: 배포 안내의 경로·MIME·캐시 설정에 맞춰 준비된 zip을 업로드한 뒤 실제 HTTPS 주소에서 로드/타일 입력/정지·재개/저장·새로고침/새 버전 교체 확인. 공개 주소의 단순200만으로 플레이 성공 처리하지 않음.
- 근거: output/playtests/2026-11-16-web/{reference-headers.txt,target-before-headers.txt}, 웹 플레이 결과는 V-018 참조.

한글 UI·폰트 V-013은 2026-11-16 완료 이력으로 이동. 이전 V-016/V-017의 미검증 언급은 당시 이력이며 현재 모바일 가독성은 D-001로 추적.

### V-001 — Player 빌드와 새 환경의 패키지 복구
- 2026-11-16 Android 추가 확인: 설치된 Android 지원 모듈/SDK36·NDK27.2·OpenJDK17로 ARM64 IL2CPP APK 빌드·서명 검사·실제 Lenovo Android15 설치/실행/터치/저장 복원 성공(V-020). 기존 패키지 캐시를 사용했으므로 새 환경 복구 성공으로 해석하지 않음.
- 2026-11-16 부분 확인: 기존 캐시에서 Unity WebGL Release 빌드 성공·오류0, Mac Chrome 실제 WebGL 실행/입력/저장·새로고침 복원은 V-018에서 확인. 새 캐시 복구/Native 모바일/실제 모바일 웹 성공으로 해석하지 않음.
- 관련: Packages/manifest.json, packages-lock.json, PuzzleBoardManager.cs, Game.unity의 `_tileEntries`.
- 확인할 동작·기대 결과: 캐시 없는 환경에서 패키지 복구 후 데스크톱/모바일 Player 컴파일 및 타일 렌더링 성공.
- 세션08 부분 확인: 현재 캐시에서 Mac Development Player 빌드·실제 실행·39칸/선택선 셰이더/입력·리필·격리 저장 성공. build-results/player/results.txt 및 V-014 참조.
- 남은 미검증 사유: 캐시 없는 복구와 iOS Player 빌드는 미수행. 원본 캐시 삭제 없이 별도 안전 폴더에서 확인 필요. 과거 모듈 미설치 기록은 당시 환경이며 현재 Android 모듈은 설치 확인.
- 검증 방법: 별도 복원 폴더에서 정확한 Unity 버전으로 의존성을 복구한 뒤 Player 빌드·실행. SSH Git 패키지 접근 권한 필요. 원본 Library 삭제 금지.
- 관련: D-001. Git 패키지 캐시 3개는 외부 백업의 보조 압축 파일에 보존.

### V-002 — 실제 포인터·터치 입력과 전체 스테이지
- 관련: MainMenuController.cs, GameReadyPanel.cs, TileInputHandler.cs, Runner/Boss 스크립트.
- 확인할 동작·기대 결과: 실제 클릭/터치로 Play와 시작 버튼이 반응하고 드래그부터 장애물·보스·결과까지 진행.
- 세션 01 확인: 실제 Button.onClick 콜백 호출.
- 세션 02 확인: 가상 Mouse/Touchscreen에 New Input System 상태 이벤트를 주입하여 실제 UI/Update/Enhanced Touch 경로 검증 완료(V-005). 실제 사람의 마우스 조작·물리 터치 기기·전체 완주는 수행하지 않음.
- 세션 03 확인: 가상 Mouse로 일시정지 메뉴·재시작·로비 왕복, 빨강/파랑/노랑 장애물 통과→BossPhase까지 자동 검증. 보스 승리까지의 전체 완주는 미검증(V-006).
- 세션 04 확인: 자동 마우스 입력으로 장애물3종→타임어택→그로기→승리/패배 Result 검증(V-007). 보드/HP 수치 보정이 포함되므로 실제 사람의 무보정 완주·난이도 검증은 미수행.
- 세션08 자동 통합: 저장·아이템·보상/장비/성장/실패와 재도전까지 V-015 및 회귀에서 확인. Native smoke V-014와 구분.
- 검증 방법: Main부터 실제 사람의 손으로 무보정 완주·장기 성장 체감 기록. 자동 fixture 결과를 사람/실기기 검증으로 해석하지 않음.
- 관련: I-001, I-002, I-005, D-001.

### V-012 — 효과음 실제 청취와 출력·믹스
- 2026-11-16 실제 기기 부분 확인: Lenovo TB320FC/Android15의 실제 플레이에서 사용자가 배경음악과 타일 연결 효과음을 모두 들었다고 확인. 기본 음악/매칭 SFX 청취 근거이며, 3티어·아이템·피격·승패·중첩/무음/앱 전환의 전체 청취·믹스 검증을 뜻하지 않음. 관련 V-020, android/TEST-RESULTS.txt. 최근 확인일2026-11-16.
- 2026-11-14 최근 확인: ConceptPixel 새 BGM2곡/SFX8개 로드·비영점PCM, Native에서 BGM위치0.213→0.427초 진행·정지/재개·로비정지 확인(V-016). Editor/Native 출력 분석peak0 유지. 실제 청취·중첩 믹스/모바일 앱 전환은 여전히 미검증. 관련 Audio/ConceptPixel, AudioManager/PixelButtonAudio, art-audio/player/results.txt.
- 등록일: 2026-11-13 / 최근 확인일: 2026-11-16. 관련: AudioManager, Resources/Audio/Prototype, audio-probe.txt, player/results.txt, 2026-11-16-android/TEST-RESULTS.txt.
- 확인할 동작·기대: 스피커/헤드폰에서 매칭3티어·아이템·피격·승패가 구분되고 과도한 중첩·음량·잘림 없이 출력. 모바일 무음/앱 전환도 확인.
- 확인한 범위: PCM 음원 데이터 비영점·Resources 로드·실제 매칭의 SFX 카운트 증가·SourcePlaying=true·활성 Listener1개 확인. Editor 및 Native Player의 AudioListener 출력 분석값은 peak0으로 남음. 실제 청취 성공이나 실제 무음 원인을 단정하지 않음.
- 현재 남은 미검증 사유/방법: 기본 BGM/매칭음은 Android에서 사용자 청취 확인했으나 전체3티어·아이템·피격·승패·중첩/무음/앱 전환은 아직 청취 검사하지 않음. 해당 시나리오를 실제 장치에서 청취하고 필요 시 믹스/볼륨 조정. 과거 Mac 분석 API의0값 원인도 미확정이며 API 상태만으로 청취 성공 처리하지 않음.
- 관련: D-001.


## 미해결

### I-011 — 기존 Android APK의 태블릿 가로 화면 내 세로 앱 호환 표시
- 등록일·최근 확인일: 2026-11-16. 관련: Android 방향/창 호환 설정, V-020 실제 기기 캡처. 관련 ID: V-021, D-001.
- 증상·재현: Lenovo TB320FC Android15에서 현재 설치 APK의 manifest는 screenOrientation=portrait이지만, 가로2560×1600 화면 안 중앙 세로 앱/좌우 여백으로 표시. 사용자가 다음 빌드부터 항상 세로 표시를 요청.
- 영향·원인: 게임은 플레이 가능하나 사용자가 원하는 기기 화면 전체 세로 방향과 다름. 제조사 방향·창 호환 처리의 영향은 추정이며 확정하지 않음.
- 조치·다음 단계: 프로젝트 세로 고정/비리사이즈 및 Android 공식 override 거부 속성2개 적용. 기존 설치본은 그대로이며 V-021의 새 APK·실기기 회전 검사 전까지 해결 완료로 표시하지 않음.

### I-010 — Chrome WebGL 형식 조회 경고
- 등록일·최근 확인일: 2026-11-16. 관련: Unity WebGL 렌더러/Chrome157, output/playtests/2026-11-16-web/browser-console.json. 관련 ID: V-018, V-019, D-003.
- 증상·재현: Mac Chrome157 headless의 최초 게임 로드 중 `WebGL: INVALID_ENUM: getInternalformatParameter: invalid internalformat` 경고6회. 최종 실행에서 JS pageerror/console error0, 실제 타일/배경/선택선/픽셀폰트 렌더링·입력 성공. 확인한 플레이 범위에서는 시각 결함을 관찰하지 않음.
- 영향·원인: 현재 확인된 영향은 콘솔 경고. Unity의 지원 텍스처 형식 탐색/브라우저 조합일 가능성은 추정이며, 모든 브라우저에서 무해하다고 확정하지 않음.
- 다음 조치: 게시 후 일반 Chrome/Safari/Edge와 실제 모바일의 경고·렌더링 비교. 필요 시 WebGL 호출 스택과 지원 형식 검사 경로 확인. 경고 억제나 패키지/렌더러 임의 교체는 하지 않음.

### I-009 — Mac Development Player 종료 시 렌더 자원 정리 경고
- 2026-11-16 한글화 Native23/종료0 뒤 동일 ComputeBuffer·Development메모리 진단 재현. 이번 변경으로 해결 처리 안 함. 근거 2026-11-16-ko/player.log.
- 2026-11-16 최근 확인: 픽셀UI Native20 checks/종료0 뒤 동일 ComputeBuffer/Development메모리 진단 재현. UI 작업으로 해결 처리하지 않음. 근거 2026-11-16-ui/player.log, V-017.
- 2026-11-14 최근 확인: 새 아트 Native17 checks·실행 중 오류0/종료0 뒤 ComputeBuffer 자동정리 및 Development MemoryLeaks 경고 재현. 원인 미확정/기존 증상 유지. 이번 표현 변경에 원인을 단정하지 않고 URP·패키지 수명 추적 필요, 불필요한 패키지 제거/경고 억제 안 함. 근거 output/playtests/2026-11-14-art-audio/player.log, V-016.
- 등록일: 2026-11-13 / 최근 확인일: 2026-11-16. 관련: URP/Native Player 종료, output/playtests/2026-11-13-session08/player.log.
- 증상: 종료0 및 Main/입력/리필/저장 smoke 성공 뒤 프로세스 정리에서 ComputeBuffer 자동해제 경고/Development 메모리 진단 출력. 실행 중 프레임 누수나 게임 데이터 손상을 확인한 것은 아님.
- 재현: Session08Build.Build→Development Player -nyanSmoke 실행→Application.Quit. 종료 로그 말미.
- 이번 최초 Player 검사에서 확인했으며 세션08 변경으로 새로 발생했다고 단정하지 않음.
- 원인: 미확정. _NyanLink 게임 스크립트에 직접 ComputeBuffer/GraphicsBuffer 소유 코드는 없음. 패키지/렌더 파이프라인 수명과 관련됐을 가능성은 추정.
- 다음 조치: 종료 호출 스택과 반복 Player 실행/렌더 자원 수명을 조사, Release와 비교. 경고를 숨기거나 관련 패키지를 임의 삭제하지 않음.

## 실기기 테스트 필요

### D-005 — 러닝 접근 예고·입력 영역·동작 제한 가독성
- 등록일·최근 확인일: 2026-11-16. 관련: RunnerWarningHUD.cs, RunnerController.cs, ObstacleController.cs, PrototypePresentation.cs, ConceptPixelPresentation.cs. 관련 ID: D-001, D-004.
- 요청·변경: 두 번 표시되던 장애물 카운트다운 제거. 오른쪽에서 들어오는 장애물 아이콘/접근 중 표시와 점멸로 사전 예고. 장애물 생성 후 실제 입력 X 범위를 반투명하게 표시하고, 진입 시 요구 색 연결 안내·정답 입력 시 초록/입력 완료로 전환. 영역 밖/오답 매칭에서는 러너 동작을 생략하고 점수·회복·아이템·보라 가속은 유지.
- 대상·절차: 다음 APK로 Lenovo TB320FC Android15 및 Android/iOS 세로 기기에서 빨강/파랑/노랑 장애물의 예고→접근→영역 진입→정답/오답/실패 재도전, 예고/영역 내 정지·재개, 색 선택 모달 복귀를 테스트. 자유 러닝에서 각 색을 연결해 점프/방어 등 무관 동작이 없는지 확인. D-001의 손가락 가림·노치·성능 및 D-004 보스 아이콘도 함께 확인하고 PNG 촬영.
- 기대 결과: 예고를 입력 시점으로 오인하지 않고 공간 범위로 입력 타이밍을 판단. 입력 가능 영역과 실제 성공 예약 판정 일치, 정답 수락 후 완료 표시 유지. 작은 화면에서도 아이콘·문구·반투명 영역이 명확하고 퍼즐 입력을 차단하지 않음.
- 대기 사유·다음 조치: 이번 작업에서 APK 생성/설치·실기기 테스트 미수행. Editor/모의 안전 영역 확인과 실제 기기 결과를 구분하며 다음 기기 테스트에서 대비·점멸 속도·영역 인지성을 확인.

### D-004 — 보스 순서 아이콘·입력 완료 회색 표시 가독성
- 등록일·최근 확인일: 2026-11-16. 관련: BossHUD.cs, ConceptPixelPresentation.cs, Resources/BossCompletedIcon.shader. 관련 ID: D-001.
- 사용자 실기기 피드백: 기존 색상 이름으로 나열한 순서 안내는 가독성이 낮음. 보드와 동일한 색상 아이콘, 왼쪽부터 번호, 현재 입력의 금색 테두리·지금 표시, 완료한 아이콘의 회색 비활성 표시로 변경.
- 대상: Lenovo TB320FC Android15 및 후속 Android/iOS 세로 기기. 다음 APK 업데이트 후 보스 첫 패턴3칸→1/2칸 입력→그로기→강화4/5칸→오답·시간초과 재시도→일시정지/재개 확인.
- 기대 결과: 입력 순서·현재 위치가 명확하고 완료 색상은 회색으로 유지. 다음 패턴에서 원색·번호 초기화, UI와 보드·타이머 겹침 없음. 실제 기기에서 같은 색이 연속되는 순서도 구분 가능. D-001의 안전 영역·터치 항목도 함께 확인하고 실제 PNG 저장.
- 미수행 사유·다음 조치: 이번 작업은 코드·Editor 검증이며 기기 APK 생성·설치 미수행. 다음 실기기 테스트에서 크기·대비·회색 셰이더 렌더링을 확인. Editor 결과와 실제 기기 결과를 구분해 유지.

### D-003 — Android Chrome·iPhone Safari 웹 플레이
- 등록일·최근 확인일: 2026-11-16. 관련: WebGL 빌드·NyanLink 웹 템플릿·SaveStore·AudioManager·TileInputHandler. 관련 ID: D-001, D-002, V-012, V-019.
- 대상: 실제 Android 휴대폰 Chrome 및 iPhone Safari, 노치/홈 바 기기, 저사양·메모리 제한 기기.
- 절차: 회사 HTTPS /nyan-link/에서 최초 다운로드→게임 시작→39칸 드래그/아이템/보스/장비, 세로·가로·주소창 높이 변화, 일시정지/탭 전환/복귀, 저장 경계 뒤 새로고침·브라우저 재실행. 실제 스피커/헤드폰 청취와 장기 플레이 성능 확인, 실제 PNG 촬영.
- 기대 결과: 터치 위치·UI가 일치하고 스크롤/잘림/이중 입력이 없으며, 소리·저장·복귀가 의도대로 동작하고 메모리 부족 종료가 없어야 함. 기본 가로 화면에서도 세로 게임 영역 유지.
- 대기 사유: 실제 모바일 기기를 사용하지 않음. PC Chrome의 모바일 크기/터치 에뮬레이션은 실제 iOS/Android WebGL·오디오·메모리 검증을 대체하지 않음. 기존 D-001/D-002의 Native 기기 대기 항목도 유지.

### D-001 — Android/iOS 세로 화면과 입력
- 2026-11-16 러닝 표시/액션 규칙 변경: 기존 실기기에서 확인한 자유 매칭 점프는 변경 전 결과. 현재는 장애물 입력 영역 내 요구 색 매칭에만 러너 액션이 발생하며, 접근 예고/영역 표시는 D-005에서 새 APK 검증 필요.
- 2026-11-16 실제 Android 부분 확인(V-020): Lenovo TB320FC/Android15, 물리1600×2560 패널이 현재 가로2560×1600 상태에서 OS의 세로 앱 호환 영역/좌우 여백으로 표시. 실제39칸·한글/HUD/일시정지, ADB로 주입한 Android touch swipe의2칸 연결·20점/리필/점프, 홈 전환 뒤 정지 상태 유지 확인. 시뮬레이터/가상 Input System 검사가 아닌 실제 기기 실행. 손가락 수동 조작·세로로 직접 들기/노치폰·iOS·전체 아이템/보스/장비 스크롤은 남음. APK를 기기에 설치하고 마지막은 복원된 게임의 일시정지 상태로 남김.
- 2026-11-16 한글화 추가: 갈무리11·한글UI가 Editor3해상도/모의노치·Mac Native540×960에서 검증(V-013). 실제폰에서 작은한글/숫자·긴안내 줄바꿈/장비행·버튼·손가락가림·폰트메모리/추가글리프·성능 확인 필요. 모바일모듈미설치/실기기미실행 상태 유지.
- 2026-11-16 최근 확인: 픽셀 버튼/게이지/팝업/장비창·아이콘·긴목록 클리핑 적용(V-017). Mac CLI 가상Mouse/모의노치와 Native540×960 확인, 실제 터치검사 아님. 기기에서 눌림/비활성·6색선택·13개 이상 장비 스크롤·노치경계·작은글자·픽셀 리소스 메모리/성능 확인. Unity모바일모듈 없어 대기 유지.
- 2026-11-14 최근 확인: 새 고양이/보스/장애물·6색 심볼/아이템 링·배경/로고 및 BGM2/SFX8도 대상에 추가. Mac Editor3해상도/모의 노치·Native540×960은 V-016에서 확인, 실제 모바일 검증 아님. 기기에서 글·심볼·색 선택·손가락 가림/장애물 가독성·고해상도 텍스처 메모리·음량/무음/앱 전환 확인 필요. 관련 Presentation/, Art/ConceptPixel, Audio/ConceptPixel, V-012.
- 관련: Main/Game 씬, New Input System, TileInputHandler, DualCameraRig, TMP, PlayerSettings.
- 대상: Android 및 iOS 실제 터치 기기, 노치·홈 바 기기, 9:16 및 더 긴 화면.
- 절차: Main→Game 진입, Play/시작 터치, HUD/안전 영역/39칸 전체 표시, 인접 드래그·되돌리기·오인식·보정 확인. 각 플랫폼의 관련 대기 항목을 함께 검증하고 실제 화면 촬영.
- 기대 결과: 화면 잘림·중복 입력·글리프 누락 없이 버튼과 타일 입력이 시각 위치와 일치.
- 기존 확인: 세션 01은 52칸/1284×2778 및 콜백 경로. 세션 02는 Mac Editor에서 39칸 전체 표시와 마우스/Enhanced Touch 가상 기기 입력, 터치 1.3배 겹침 영역의 가까운 중심 선택, 1080×1920 및 1284×2778 렌더링을 확인(V-005). 터치 상단 보정은 0으로 두고 실기기 튜닝 대기. 기기·시뮬레이터·모바일 빌드는 실행하지 않음.
- 세션 03 추가 대상: 실제 기기에서 장애물 경고 중 정지→재개, 실패 재도전 대기 중 정지, Boss 상태 복귀, 재시작·로비 반복. HP·예약·남은 시간이 보존되고 입력이 중복되지 않아야 함. Mac 자동 검증 V-006과 별개.
- 세션 04 추가 대상: 실제 보스 순서 입력, 그로기 공격/아이템 구분, 패턴·그로기 중 정지/재개, 타이머·강화·승패 표시. Mac 가상 마우스 검증 V-007과 별개.
- 세션05 추가 대상: 6종×2티어 단독 터치/같은색 체인·빠른 반복 입력·연쇄·종류별 한도·변색 후 종류 표시, 6색 창/중첩Pause/색 선택 뒤 같은 상태 복귀, TimeFreeze 중 실제 터치와 장애물 이동·앱 전환. 자동 Mouse 성공을 실기기 결과로 대체하지 않음.
- 세션07 추가 대상: 3슬롯 보유품 스크롤·장착/해제·Lv5/코인부족 버튼, 저장 진행 잠금·명시적 NEW RUN SETUP, 전체 해상도에서 보상/능력치 가독성.
- 세션08: Editor3해상도·노치/홈 여백 모의·실제 가상Mouse 드래그, NativeMac540×960 smoke 확인. iOS/Android Unity 모듈과 실제 모바일 기기가 없어 실기기 상태 유지.
- 현재 남은 대기 사유: 최초 Android 태블릿 기본 동작만 이번에 검증. 실제 손가락으로 무보정 전체 플레이/여러 화면·회전·노치폰/iOS/장기 성능은 별도 기기·플레이 범위 필요. 기본 BGM/매칭음은 사용자 청취 확인(V-012)이며 모든 오디오 시나리오 완료는 아님.
- 관련: V-001, V-002, I-001, I-003.

### D-002 — 모바일 저장·앱 수명·파일 교체 검증
- 등록일: 2026-11-13 / 최근 확인일: 2026-11-16.
- 2026-11-16 Android 부분 확인(V-020): Lenovo TB320FC/Android15 IL2CPP에서 실제 profile revision2·backup 생성, 점수20/거리62.5/HP78.20675/39칸·ObstacleRetry 경계 저장. 앱 am force-stop→새 프로세스 실행→UI 이어하기→동일 보드/점수/경계 복원, 저장 문서 전체 바이트 불변 확인. 저장 이후 연속 진행분이 되돌아가는 정책에 따라 정지 직전 표시HP78과 복원 뒤 저장 경계의 표시HP79를 구분. 앱 삭제/데이터 지우기/저장 주입 없음.
- 관련: SaveStore, SaveManager, GameReadyPanel, PlayerSettings, Application.persistentDataPath.
- 대상: Android/iOS 실제 Player와 앱별 저장 공간. 저장 직전/중/직후 백그라운드·강제 종료, OS 저장 실패/공간 부족, 앱 업데이트 후 구버전/미지원 파일.
- 절차·기대: 장애물 성공/실패·보스 성공/실패/그로기 경계에 반복 종료→재실행. 주/백업 중 온전한 한 문서로 코인/장비/영수증/보드/효과가 같은 revision으로 복원되고 원본 손상 파일은 보존. 미지원 파일 덮어쓰기 금지. New Game은 진행만 초기화.
- 기존 확인: Mac Editor 실제 File.Replace/Flush(true), temp 완료 직후 예외 주입, 손상·체크섬·백업·미지원 버전·독립 Unity 프로세스 재실행 V-010 통과. 모바일 IL2CPP/전원 차단/실제 디스크 장애 시험이 아님.
- 세션07 추가 대상: 첫 클리어/이후 추첨/강화 비용 차감/장착 저장의 직전·직후 강제 종료를 반복해 영수증·코인·장비·레벨의 일관성 및 재지급 방지 확인.
- 현재 남은 대기 사유: 첫 Android 기기의 정상 저장 뒤 앱 종료/재실행 기본 검사만 수행. 실제 쓰기 직전·중·직후 중단/모든 보스·아이템·보상 경계/공간 부족/앱 업데이트·미지원 파일/iOS는 추가 검사 필요. 재현 가능한 별도 데이터 백업을 준비하고 D-001과 함께 진행.
- 관련: V-001, V-010, D-001.

## 완료 이력

### V-023 — 러닝 접근 예고·공간 입력 영역·동작 제한 Editor 검증
- 등록일·최근 확인일·수행일: 2026-11-16. 관련: RunnerWarningHUD.cs, RunnerController.cs, ObstacleController.cs, PrototypePresentation.cs, ConceptPixelPresentation.cs, NyanLinkSession03Validation.cs. 관련 커밋: `feat: clarify runner obstacle approach and input zones` 로컬 커밋. 실기기 잔여: D-005/D-001, 보스 D-004 유지.
- 변경: 사전 예고/입력 구간의 숫자 카운트다운과 시간 바 제거. 오른쪽에서 들어오는 기존 AI 장애물 아이콘과 접근 중/다시 접근 표시·점멸로 예고. 생성된 장애물의 실제 판정 시작X/끝X를 러너 카메라→부모 안전 영역 좌표로 투영한 반투명 영역 표시. 진입 전 대기 안내, 진입 후 요구 색 2개 이상 연결, 정답 후 초록/입력 완료로 구분. 장식은 raycast를 받지 않고 일시정지/색 선택 창 아래에 배치.
- 동작: 자유 러닝/사전 예고/영역 밖/오답 연결에는 러너 액션을 생략. 점수·회복·아이템 효과·보라 가속·보스 그로기 액션은 유지. 장애물 순서·스폰/이동 속도·입력 거리·실패 피해·재도전 시간·저장 스키마 변경 없음.
- 최종 검증: macOS Unity6000.3.14f1 Editor 실제1080×1920, 가상 Mouse/격리 저장. 러닝372 Play assertion/종료0/게임 경로 런타임 오류0. 자유 러닝5색 무동작, 보라 가속, 예고 아이콘 점멸·카운트다운 없음, 영역과 판정X 오차2px 미만(일반/모의 안전 영역), 빨강/파랑/노랑의 정답 액션·입력 완료, 조기 입력 거부·오답 잠금·20피해·재도전·정지/재개·씬 왕복·보스 진입 검사 통과. 사용자 저장 파일명/SHA 및 구독 정리 검사 통과.
- 회귀: 같은 소스의 Session04 기본 속도 장애물→보스/그로기/승패·아이템 보존/점수/회복/버프·보스 순서 아이콘 검사266개/종료0/게임 경로 런타임 오류0. output 하위 regression-boss/에 별도 보존.
- 캡처: `output/playtests/2026-11-16-runner-cues/` 실제 PNG13장 및 regression-boss/9장. 최신 03-jump-prewarning(오른쪽 예고)·04-window-Yellow(반투명 입력 범위) 직접 열람, 앞선 동일 소스 05-jump-reserved(초록 완료/점프)·04-window-safe-area도 열람. 기대 표시와 실제 결과 일치, 타이머/보드 겹침 없음.
- 테스트 한계: 캡처·오답 복구·동작 종료 후 예약 유지 검사를 위해 Session03의 마지막3종 검사에서만 영역 진입 후 장애물 속도를1로 낮추고 정답 후 원래 속도로 복원. 첫 실패/재도전·회귀 Session04는 기본 속도 검사. 보드 인접 쌍 보정·게임 배속·모의 안전 영역을 포함하므로 무보정 수동 난이도·실기기 검증으로 해석하지 않음.
- 초기 실패 이력: attempt01은 CanvasGroup 생성 참조 오류→불필요한 컴포넌트 제거 후 최종 검증. attempt02는 배속으로 예고 순간을 건너뛰는 검사, attempt03/04는 캡처/오답 대기 중 기한을 넘긴 fixture를 수정. attempt05-cloud-auth는372개 동작 검사 완료 후 UnityEditor.Connect Token Exchange 오류1건으로 종료1. 최종 재실행372개·보스 회귀266개 모두 종료0에서 해당 인증 오류 재발 없음. 게임 오류와 Editor 외부 인증 오류·초기 검사 실패를 구분해 로그 보존.
- 근거: 같은 폴더 play-results.txt/play.log/TEST-NOTES.txt 및 regression-boss/. 미검증·실기기: 새 APK 생성/설치 및 실제 기기 가독성은 D-005. 이번 기능의 확인된 미해결 게임 문제 없음.

### V-022 — 보스 색상 순서 아이콘·완료 회색 표시 Editor 검증
- 등록일·최근 확인일·수행일: 2026-11-16. 관련: BossHUD.cs, ConceptPixelPresentation.cs, BossCompletedIcon.shader, NyanLinkSession04Validation.cs. 관련 커밋: `feat: show boss color sequence as numbered icons` 로컬 커밋. 실기기 잔여: D-004/D-001.
- 변경: 보드의 기존 AI 생성 타일 스프라이트를 재사용. 왼쪽부터 번호·현재 금색 테두리/지금 표시·완료 회색 셰이더와 완료 표시. 다음 패턴은 원색으로 초기화하고 그로기·패턴 대기에서는 아이콘을 숨김. 보스 배경을 아이콘 행 높이만큼 확장, 표시 요소의 raycast 차단 없음. 전투/저장/패턴 순서 데이터 변경 없음.
- 환경·결과: macOS Unity6000.3.14f1 Editor 실제1080×1920, 가상 Mouse 입력·격리 저장으로 266 Play assertion/프로세스 종료0/게임 경로 런타임 오류0. 순서와 스프라이트 일치·좌→우 번호·완료한 칸만 회색 material·현재 칸 강조·그로기 숨김·강화4칸 초기화 검증. 기존 장애물→보스, 아이템 보존/점수/회복·정지/재개·시간정지·오답·실패·승패·재시작·씬 정리 회귀 통과. 원래 저장 파일명/SHA 불변 검사 통과.
- 실제 PNG9장: `output/playtests/2026-11-16-boss-icons/`. 01 입력 전, 01b 첫 입력, 01c 두 입력 완료, 02 패턴 정지, 03 그로기, 04 그로기 정지, 05 강화4칸, 06 승리, 07 패배. 01c/05 직접 열람: 완료 아이콘 회색·현재 원색/금색, 번호·타이머·보드 겹침 없음. 기대 동작과 실제 결과 일치.
- 근거: 같은 폴더 play-results.txt/play.log/TEST-NOTES.txt. 최초 샌드박스 라이선스 접근 실패 후 정상 권한으로 재실행. attempt01/02는 자동 검사가 LateUpdate 화면 갱신 전 상태를 읽어 실패했으며, 화면 프레임 대기를 추가한 최종 결과와 구분해 보존. 실제 기기·새 APK 생성/설치 검증은 미수행이며 D-004 유지.

### V-020 — Android APK 빌드·실제 태블릿 설치와 기본 플레이
- 등록일·최근 확인일·수행일: 2026-11-16. 관련: NyanLinkAndroidBuild.cs, build_android.sh, Android Gradle 템플릿/AndroidResolverDependencies.xml, ANDROID-TEST-BUILD.txt. 관련 커밋: 이번 Android 기기 빌드 로컬 커밋(최종 git log 참조), 원격 push 없음.
- 빌드 환경: macOS27.2 arm64 / Unity6000.3.14f1 / 기존 패키지 캐시 / Android SDK36·NDK27.2.12479018·JDK17. Development APK·IL2CPP·ARM64·ETC2·OpenGLES3·세로 앱, kr.fairstudios.nyanlink.prototype. New Input System만 임시 사용 후 원래 Both 및 프로젝트/클라우드 설정 복원. git diff 없음.
- 결과: Succeeded·오류0·기존 UGS 프로젝트 미연결 경고1, 빌드7분33초. APK79,398,044bytes, SHA256 f10f52f482550efb73d91c7bc02ecbbbc70ee27049e395264df6082718850fab. apksigner v2 검증/ZIP CRC/ARM64·API25최소·API36타깃 확인. report size1,164,777,873은 외부 개발 심볼 등 포함한 빌드 보고치로 APK 용량과 구분. 배포물은 APK와 폰트 LICENSE/NOTICE이며 DoNotShip 폴더를 배포하지 않음.
- 실기기 환경: Lenovo TB320FC, Android15/API35, ARM64, 패널1600×2560/400dpi, 실제 캡처2560×1600(기기를 가로로 둔 OS 세로 앱 호환 영역). 이전 동일 테스트 패키지 없음 확인 후 신규 설치 Success, 실제 GameActivity Cold launch Status:ok. 개인 앱/기존 Native 저장 삭제 없음.
- 실제 검증: 로비→준비→시작, 한글·픽셀 리소스·39칸, Android touch swipe로 빨강2개 연결→20점/리필/점프, 정지/재개. Home→게임 복귀에서 정지 유지, 실제 장애물 실패/재도전 저장, force-stop 후 새 프로세스에서 이어하기로 저장 경계 복원. HP/보드/점수/진행 수정이나 저장 주입 없음. 로그에서 Unity 오류/예외·Android fatal crash·저장 실패 미관찰.
- 청취: 사용자가 실제 태블릿에서 배경음악과 타일 연결 효과음을 모두 들었다고 확인. 모든 아이템/피격/승패/중첩음·백그라운드 청취까지 완료한 것은 아님(V-012).
- 실제 캡처: output/playtests/2026-11-16-android/01 로비,02 준비,03 실행,04/07 정지,05 드래그 도중,06 20점·리필·점프,08 홈 복귀,09 저장 뒤 정지,10 강제종료 뒤 이어하기,11 복원 뒤 정지. 각 환경·기대/실제 설명 TEST-RESULTS.txt, 앱별 logcat/서명/설치/원본 checkpoint 근거 보존.
- 한계: 실제 기기의 ADB 입력 검사이며 사람 손가락으로 전체 무보정 완주/난이도 확인은 아님. D-001/D-002의 남은 기기·경계·장기 검사 유지. 단순 재실행을 전원 차단/쓰기 도중 종료 검증으로 해석하지 않음. 새 Android 미해결 게임 문제는 확인하지 않음. 기존 I-009 Native Mac 종료·I-010 WebGL 경고는 별개로 유지.

### V-018 — 웹 Release 빌드·로컬 Chrome 플레이·배포 패키징 확인
- 등록일·최근 확인일·수행일: 2026-11-16. 관련: NyanLinkWebBuild.cs, WebGLTemplates/NyanLink, MainMenuController.cs, Tools/build_web.sh/package_web.py/serve_web.py/WebHosting, WEB-DEPLOYMENT.txt. 관련 커밋: 이번 웹 배포 준비 로컬 커밋(최종 git log 참조), 원격 push 없음.
- 환경: macOS27.2 arm64, Unity6000.3.14f1, 기존 패키지 캐시, Chrome157.0.8103.51 headless 실제 WebGL2 렌더러. 격리된 브라우저 컨텍스트로 검사, 개인 브라우저/Native 사용자 저장에 접근하지 않음.
- 빌드: WebGL Release/단일스레드/Gzip+Unity fallback/파일 hash/일반 텍스처. 최종 Succeeded·오류0·기존 UGS 프로젝트 미연결 경고1, 33,875,068bytes. 재빌드+패키징+프로젝트/클라우드 설정 원복 스크립트 실행 성공(22.54초 빌드). 최종 settings의 git diff 없음.
- 패키지: Builds/Web/nyan-link-deploy.zip 33,128,753bytes, SHA256 f5d7e5ef5e4b88e5a63f6fbe61a679178dd3123d45d66148466d37f4bac6736b. ZIP 검사·내부 모든 파일 크기/SHA256 일치, nyan-link/ 접두사·폰트 라이선스·.htaccess 포함. 소스/디버그/툴체인 불포함. 웹 빌드와 zip은 ignored Builds/에 보존.
- 실제 브라우저: /nyan-link/ 경로로 다운로드→한글 로비→준비→시작,39칸 렌더링, 웹 종료 버튼 숨김. 실제 브라우저 Mouse down/move/up으로 빨강3개 선택선→릴리스30점/3칸 리필/점프 확인, 일시정지/재개 확인. 데이터·보드·HP를 보정하지 않음. 최종 콘솔 error/pageerror0, 형식 조회 경고6회는 I-010으로 유지.
- 저장: 첫 브라우저에서 실제 장애물 실패/재도전 경계가 반복되어 profile revision5와 .bak revision4가 IndexedDB에 자동 동기화. 새로고침·새 빌드 로드 후 UI 이어하기 표시, 동일 보드/거리62.5/HP 약15.3→표시16/재도전 상태 복원. profile와 backup은 browser-save-snapshot.json, 실제 화면05/06/07. 저장 데이터를 주입하지 않음. 전체 보스 승리/장기 경제/실제 폰 강제종료 성공은 미검증.
- 화면: 1280×900에서506.25×900,390×844에서390×693.33,844×390에서219.375×390 캔버스로9:16 유지·화면 안에 표시. 모바일 크기의 PC 브라우저이며 실제 터치/노치 기기 검증은 아님(D-003).
- 실제 캡처: 01 웹 진입,02 로비,03 준비,04 실행,05 정지,06 새로고침 뒤 이어하기,07 복원 뒤 정지,10 최종 실행,11 최종3칸 선택선,12 최종30점/리필/점프,13 세로 크기/14 가로 크기. 모두 실제 브라우저 촬영이며 render mockup 아님. screenshots와 results 설명은 같은 폴더 TEST-RESULTS.txt.
- 초기 시도 보존: attempt01 임시 경로104자 소켓 제한 실패→짧은 외장 임시경로 연결로 해결; attempt02 작업 중단으로 빌드 종료→원래 설정 백업 복원 후 재실행; attempt03 자동 클릭을 같은 프레임에 과도하게 전달해 원래 의도한 연결 검증 미성립→실제 입력 간격으로 최종11/12에서 확인. 초기 스크린샷을 최종 성공 근거로 사용하지 않음.
- 한계·후속: 회사 HTTPS 업로드/캐시 교체·Safari/Edge는 V-019, 모바일 실기기/소리 청취/성능은 D-003/V-012. 사용자 무보정 전체 완주는 V-002 유지. 로컬 웹 성공으로 기존 미검증·Native 종료 I-009를 완료 처리하지 않음.

### V-013 — 프로토타입 한글 UI·픽셀 폰트·글리프 검증 완료
- 등록일:2026-11-13 / 최근확인·수행일:2026-11-16. 기준6dff4ab. 기존 e883693/6dff4ab는 사용자 요청으로 origin/main에 푸시·6dff4ab54c58e0725cc0b8aaf537884de7e860f9 원격일치 확인. 새 한글화는 로컬 커밋 feat: localize prototype UI with Korean pixel typography, 미푸시.
- 이전 상태: 기본 LiberationSans/영문UI로 한국어폰트·현지화 미구현. 이번에 활성 프로토타입 경로를 한글화하고 정식 출처·라이선스 포함 폰트를 적용.
- 관련: UI/KoreanText·KoreanUIPresentation, FontPipeline, Resources/Fonts/Galmuri 및 TMP Settings, 기존UI·PixelUISkin/ConceptPixelPresentation, TimeAttackPattern 표시·ProgressionService.RewardSummary, Session06/07/08 기대문구/NativeQA.
- 출처/허가: quiple/Galmuri11 v2.40.4 공식배포 TTF 원본·OFL1.1 저작권/라이선스 전문 보존. URI/SHA font-provenance.json·NOTICE.txt. 기존 폰트/씬/.meta/데이터ID·이름/저장schema·규칙서명·게임수치 유지, 알려진3종 장비 표시명만 한글.
- 폰트:12pt RASTER_HINTED/Point/2048 Alpha8 atlas1개/11324문자,현대 한글11172전부 사전포함. Dynamic/sourceFont 참조/빌드캐시유지·기존LibSans fallback. UI표시만NFC정규화, 저장원본불변. Bitmap에 SDF-only outline설정 안 함.
- 적용: 시작/이어하기정책/일시정지/승패·보상/체력·연결/아이템·시간정지/장애물/보스순서·시간·그로기/6색선택/장비 슬롯·장착·Lv5·비용·잠금·오류 안내. 버튼/GameObject 이름·이벤트/ID는보존하고 한글표시와기존역할색 연결. 이미지브랜드NYAN LINK·미등록원본ID는 보존.
- Unity6000.3.14f1 Mac Editor CLI 통합1055/Cold64/아이템322/장비204 통과·각종료0·게임경로오류0. 실제UI·저장/재색칠아이템/효과·중복보상·장착/강화HP135/공격1.1·스크롤·눌림/비활성 유지.
- 추가폰트 검사: 현대11172기본글리프·Point/bitmap·활성TMP의폰트/누락/영문기능문구/넘침 확인. Main5/장비11/HUD12/Boss14 컴포넌트 확인, 실제PNG 직접확인. 빈문구검사도구오류 attempt01 보존후nullguard로해결.
- Native: Mac Development 빌드성공/오류0/기존UGS경고1·실행23 checks/종료0. 폰트원본·현대11172전부/Point·한글메뉴/장비/팝업·Mouse쌍/20점·리필/격리저장·음악정지/재개. 누락글리프 경고0·실제540×960 PNG5장.
- 보호: 작업트리시작깨끗함/앱파일9개외부SHA백업·재확인/원래saves부재유지/.meta311중복0. Unity자동Cloud설정diff보존후복원, TMP Settings 기본폰트는의도된변경으로유지.
- 근거: output/playtests/2026-11-16-ko/{play-results.txt,cold-results.txt,korean-ui-probe.txt,regression-session05/,regression-session07/,build-results.txt,player/,final-protection-audit.json,SCREENSHOTS.txt}, output/localization/2026-11-16-ko/, KO-UI-01-HANDOFF.txt.
- 한계: 프로토타입 활성UI 기준 완료. 실제모바일의글자/줄바꿈·터치·메모리·성능 D-001,모바일저장D-002/새캐시V-001/사람무보정V-002/실제청취V-012/기존종료경고I-009 유지. 향후추가기능·출시전체현지화 완료를 의미하지 않음.

### V-017 — 픽셀 UI 리소스·기존 기능 유지 검증
- 등록일·최근 확인일·수행일: 2026-11-16. 기준 e883693, 로컬 커밋 메시지 feat: replace prototype UI with generated pixel art assets, 미푸시.
- 관련: Presentation/PixelUIResources·PixelUISkin·PixelUIPresentation, Editor/PixelUIPipeline, EquipmentPanel/PrototypePresentation/ConceptPixelPresentation, Art/PixelUI PNG4·catalog18, Session07/08Validation 및 PrototypePlayerSmoke.
- 적용: AI 생성 버튼4/패널4/게이지4/아이콘6. Main·준비·HP/진행/보스바·점수/아이템배지·경고·Pause/색선택·승패 결과·장비 창/행·아이콘.9-slice/Point/CPU텍스처복사없음, 동적 TMP문구/기존UGUI·리스너·Slider값 유지. 장식 raycast없음, 비활성/눌림색 구분. 색 선택은6색 심볼·색 이름/선택값 유지.
- 장비: 기존 ScrollRect/비용/슬롯/레벨·잠금/보상 계약 보존. 행 생성 뒤 표현만 연결, 미등록 장비 이름/기존 레코드 보존·방패 대체아이콘. viewport는 원래Mask를 삭제 없이 런타임 비활성화하고 RectMask2D로 장식 알파와 독립적인 사각형 클리핑. 동일 Sprite/색/형제순서 갱신 생략해 불필요 mesh dirty 방지.
- 환경/결과: Unity6000.3.14f1 Mac Editor CLI 통합896/독립Cold64/아이템322 통과·각 종료0·게임 경로 오류0. 실제20피해→HP Slider값·채움 일치,3세로해상도/모의노치/실제UI 포인터/기존 저장·전투·보상중복/성장 연결.
- 최종 장비204 checks 종료0·오류0: 실제 장착·해제/Lv5/비용/재도전 HP135·공격1.1/잠금,13개 격리 목록 Mouse wheel·비활성 버튼 저장불변·실제 눌림·창 밖release 취소, 임시10개 제거 후 원래 테스트3개/능력치 복원.4단계 원본PNG 제목 흰픽셀4978 동일.
- Native: 최종 Mac Development 빌드성공/오류0/기존UGS경고1,실행20 checks/종료0. 실제540×960 픽셀 장비창/팝업/HP바/Mouse 쌍·20점·리필/격리저장/BGM전환·정지/재개. actualPNG5장. 빌드는 로컬 Builds/PixelUI로 배포/커밋 안 함.
- 검사 이력: 동적 목록 배치 전 포인터좌표 읽기 실패→Canvas.ForceUpdateCanvases 후 sampling/raycast보강. LateUpdate의 CaptureScreenshotAsTexture 검사도구 오류→비동기 실제PNG 완료 후 읽기로 수정. 미리보기 제목 누락 추정은 이전/최종6PNG 독립디코드에서 모두4978픽셀로 정상 확인하여 정정, 실제 제목 누락을 고쳤다고 기록하지 않음.
- 보호: 시작 작업트리깨끗함/외부앱파일9개SHA 백업·재확인/원래saves부재유지/.meta302중복0. 기존씬·GUID·패키지/저장schema·규칙서명·수치/보류기능 불변. Unity자동Cloud설정diff보존 후복원. 검사는 별도폴더/입력·최고점·GameView복원.
- 근거: output/playtests/2026-11-16-ui/{play-results.txt,cold-results.txt,regression-session05/,regression-session07/,build-results.txt,player/,final-protection-audit.json,SCREENSHOTS.txt}, UI-ART-01-HANDOFF.txt. 생성프롬프트/에셋SHA output/art/2026-11-16-ui/.
- 한계: 통합896/Cold64/아이템322는 최초연결 버전, 이후 장비 클리핑/중복갱신 변경은 장비204와 최종Native20에서 재확인. 실제청취V-012/한국어폰트V-013/사람무보정V-002/새캐시·모바일V-001/실기기D-001·D-002/종료경고I-009 유지.

### V-016 — 콘셉트 픽셀 아트·새 음악 실제 연결 검증
- 등록일·최근 확인일·수행일: 2026-11-14. 기준35bdffc, 로컬 커밋 메시지 feat: apply concept pixel art and original chiptune audio.
- 관련: Presentation/{ConceptArtLibrary,ConceptPixelPresentation,PixelRunnerAnimator,PixelButtonAudio}, PuzzleBoardManager/AudioManager/PrototypePresentation, Art/ConceptPixel PNG6·catalog17, Audio/ConceptPixel WAV10, ConceptArtPipeline/Session02·05·07·08 검사/PlayerSmoke.
- 이미지: 기존 콘셉트의 주황 고양이·민트 도시/잔디·쥐 왕/그로기·장애물3종·6색 심볼·로고를 실제 Resources에 연결. 기존 코드/리소스/.meta/씬 참조 유지. 타일 bounds1×1·39칸/입력·ItemType 불변, 재색칠 배지·Lv링 보존. 새 난수/게임 수치·저장schema 변경 없음.
- 오디오: AI 작성 새 선율/합성 알고리즘의 주행/Boss BGM2곡, SFX8종. 외부 샘플·오디오 생성 모델 사용 없음. 생성 프롬프트/에셋SHA·크기/PCM길이·최대값·재생성 절차 output/art/2026-11-14/ 기록.
- Unity6000.3.14f1 Mac Editor CLI 통합797/새 프로세스Cold64·회귀 아이템322/장비190/좌표9855/입력368 통과, 모두 종료0·게임 경로 런타임 오류0. 아트17/타일39 연결·1×1히트계약/CPU텍스처복사없음/음원10비영점PCM·주행/보스곡/Pause/색선택 음악정지·재개 확인.
- Native Mac Development 빌드 Succeeded/오류0/기존UGS경고1·실행17 checks/종료0. 실제540×960의 새 그림·선택셰이더·마우스쌍/20점·리필/단일Listener/격리저장·음악 위치 진행/정지/재개·Main정지 확인. V-012 출력peak0/I-009 종료 경고는 완료로 처리하지 않음.
- 실제 PNG 직접 열람: Main/준비 로고·이어하기/보스 안내/그로기/아이템 재색칠·색선택/장비/실패·재시작/Native 선택·주행. 저장 경계 비교 fixture의 timeScale0으로 리필 중 크기 정지된 캡처도 있음. 무보정 사람 플레이 결과가 아님.
- 초기 도구 실패: TextureImporter 기본Cube→명시Texture2D, Play도중 도구재시작→검사중reload잠금, 복원직후 표식검사→모델즉시검사 유지/표식LateUpdate 대기. 최종 검증 성공. 실패와 이전 HUD 화면도 접두사/하위폴더로 보존.
- 보호: 시작 시 원래 saves 없음, 외부 백업13개SHA 재확인/게임저장 부재 유지, 검사 격리/최고점·입력·GameView 복원. 빌드 자동 ProjectSettings/InputSystem/Cloud 변경 patch보존 후 원복. 기존 테스트 결과 덮어쓰기 없음.
- 근거: output/playtests/2026-11-14-art-audio/play-results.txt·cold-results.txt·regression-exits.json·build-results.txt·static-audit.json·final-protection-audit.json, player/results.txt 및 PNG/로그, Docs/Development/ART-AUDIO-01-HANDOFF.txt.
- 남은 범위: V-001 새캐시/모바일, V-002 사람 무보정 완주/밸런스, V-012 실제 청취, V-013 한국어폰트, D-001/D-002 실기기. 장비 아이콘·UI 프레임은 2026-11-16 V-017에서 적용. 장착 외형·음량 설정/확장 애니메이션은 후속 범위이며 최종 출시 아트 전체 완료를 의미하지 않음.

### I-003 — 프로토타입 활성 화면 가독성 정비 완료
- 등록일·최근 확인일·수행일: 2026-11-13. 관련: PrototypePresentation/SafeArea, DualCameraRig, 기존 UGUI HUD/메뉴, ItemTileLabels.
- 기존 HUD/경고·HP 겹침과 중복 보드 노출, 준비/결과의 타일 배경 간섭을 수정. 활성 프로토타입은 기존 영문/기본 폰트로 확인, 한국어 폰트 자체를 구현한 것으로 간주하지 않음(V-013).
- 확인: 720×1280/1080×1920/1284×2778·모의 노치/홈 여백, 안전 영역 버튼/보드 경계·실제 드래그, 보스/시간 정지/결과·장비 화면 PNG 직접 확인. 실제 모바일 D-001은 유지.
- 근거: 세션08 layout-*.png, 06-groggy-saved.png, 10-reward-equipment-upgrade.png, 11-obstacle-loss.png, player/ PNG.

이전 I-003 기록(세션01~07, 완료 전의 이력):
- 관련: Game.unity, GameStateDebugPanel.cs, GameReadyPanel.cs, TextMesh Pro 폰트.
- 증상·영향: 디버그 패널이 상단 HUD와 겹치고 시작 버튼 한글 등이 사각형으로 표시됨. 실제 캡처 02/03에서 확인.
- 재현: Game View 1284×2778에서 Main→Play. 세션 01 `02-game-ready.png`, `03-game-running.png` 참고. 세션 02의 1080×1920/1284×2778 캡처에서도 상단 디버그 HUD 겹침이 남음.
- 원인: 테스트용 디버그 UI가 활성 상태이며 기본 LiberationSans SDF가 한글을 제공하지 않음.
- 세션 03 부분 조치: 기존 디버그 패널은 활성 플레이에서 제외. Pause 메뉴와 READY 표시는 추가했으나 경고/HP 간격이 좁은 현상과 한글 폰트 문제는 남음. 세션 03 03/04/05 PNG에서 확인.
- 세션 04 부분 조치: 비활성 BossHUD를 활성화하고 CanvasGroup 누락 오류 수정, 순서·HP·타이머/그로기·강화 표시 연결. 보스와 플레이어 HP 간격 조정, CHAIN/TIER 표시. 승리 CONTINUE는 실제로 RetryGame에 연결되어 있으므로 기능은 다시하기지만 문구는 세션 08에서 명확히 정비.
- 세션07 부분 조치: 승리 보상/RETRY·장비 최소 UGUI 연결, 보상과 최고점 문구 겹침/로비 중첩 캔버스 크기 및 준비 HUD의 장비HP 미리보기 수정. 전체 UI·한글 폰트/안전영역은 후속 범위 유지.
- 세션06 부분 조치: 준비 화면 CONTINUE/NEW GAME과 마지막 경계 정책을 영어로 표시하여 실제 선택 가능. 안내와 버튼 겹침 수정. 전체 한국어 폰트/결과 문구 정비는 남음.
- 다음 조치: 세션 08에서 한글 폰트·HUD·안전 영역 정비. 기존 UI와 리소스 보존.
- 관련: D-001.

### V-014 — Mac Player 빌드·실제 실행 확인
- 등록일·최근 확인일·수행일: 2026-11-13. 관련: Session08Build/PrototypePlayerSmoke, Main/Game, Resources 타일/LineFlow2D Material/Audio.
- 환경: Unity6000.3.14f1, macOS27.2 arm64, Development Mac Standalone, 실제540×960.
- 결과: 빌드 Succeeded/오류0/경고1(UGS 프로젝트 미연결 안내), 프로세스 종료0. Native Player10 checks 통과: Main/39칸·스탯·선택 셰이더·실제 Mouse 입력2개/손떼기20점·리필/단일Listener·사운드 호출/격리 저장/게임 경로 오류 없음.
- 실제 PNG3장으로 선택선과 렌더링 확인. Development Build 표식 포함. 임시 QA 옵션은 Development에서 명시한 경우만, 원래 사용자 저장·최고점 보존. Builds/Session08/NyanLink.app는 로컬 빌드이며 배포/커밋하지 않음.
- 한계: Native 전체 완주·물리 손 입력·모바일/Release/새 캐시는 아님(V-001/V-002/D-001/D-002). 청취 V-012 및 종료 정리 경고 I-009 유지.

### V-015 — 세션08 통합 플레이·새 프로세스 복원
- 등록일·최근 확인일·수행일: 2026-11-13. 기준 cd4d74c, 세션08 로컬 커밋 메시지는 상단 참조.
- 관련: PrototypePresentation/SafeArea/Feedback, DualCameraRig, AudioManager, PlayerCharacter/TileSelectionVisualizer/Stamina/SaveManager, Session08Validation, PROTOTYPE-VALIDATION.txt.
- Editor 통합602 / 완전히 새 프로세스 Cold64 assertion 통과·각 종료0·게임 경로 런타임 오류0. 화면 배치·카메라 중복 수정·모의 여백/실제 입력·저장·보스·영구 보상·실제 장비 UI/강화/재도전 HP110/패배와 재시작 확인.
- 외부 원래 앱 파일2개 SHA 백업, 검사는 GUID별 격리 저장. 원래 saves 디렉터리 부재 유지, 최고점/입력/화면/음소거 복원. 기존 씬·GUID·패키지 보존.
- 원본06 보존 계약의 가상321코인/미등록 장비/보드·HP·여행8배 배치가 포함됨. 현재 사람 무보정 완주/드롭 통계/밸런스 적정성으로 해석하지 않음.
- 초기 실패/수정은 attempt01~06 및 audio-import-attempt01/startup-attempt02 로그에 보존. Listener 부재/전체화면 카메라 중복·시각RNG소모/PCM 가져오기 수정. 출력 peak0은 미검증 V-012로 유지.
- 회귀 Geometry9855/입력368, 아이템322, 장비190 통과·각 종료0·게임 경로 오류0. 이전 출력 보존. 별도 실제 Mac Player10 checks V-014 확인. 전투 회귀188 통과·종료0. 상세 명령·복구·스크린샷/알려진 한계 SESSION-08-HANDOFF.txt.


### I-004 — 성장 데이터 ID 공란 해결 완료
- 등록일·최근 확인일·수행일: 2026-11-13. 기준 커밋 fbda252, 세션07 로컬 커밋(상단 메시지 참조).
- 관련: Resources/Data/Characters/CharacterData.asset, Equipment/*.asset, DataManager, ProgressionService.
- 기존 증상: 캐릭터·장비 ID 공란으로 조회 사전에서 제외되고 시작 경고 발생.
- 조치: 기존 에셋 GUID 유지, prototype-cat-01/prototype-hat-01과 신규 prototype-outfit-01/prototype-weapon-01 고정ID 등록, 슬롯별 Lv1~5 옵션 명시. 기존 미등록 저장 장비는 원본 보존·능력치 제외/장착 거부.
- 근거: Unity 실제 ID 조회 및 장비3종/5레벨 검사, 각 슬롯 Lv5 비용·장착·피해 반영 성공. output/playtests/2026-11-13-session07/, SESSION-07-HANDOFF.txt. Player/실기기 검증과 별개.


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

### V-011 — 세션07 보상·장비 성장 검증
- 등록일·최근 확인일·수행일: 2026-11-13. 기준 fbda252, 세션07 로컬 커밋(상단 메시지 참조).
- 관련: ProgressionService, EquipmentPanel, Game/Save/StaminaManager, BalanceData/BossController, Main/StageClear/GameReadyPanel, 고정 장비/캐릭터/전리품 데이터, Session07Validation.
- 환경: macOS27.2 arm64 / Unity6000.3.14f1 Mac Editor 실제1080×1920. 활성 대상iOS이나 Player/실기기 아님.
- 확인: 첫100코인/확정모자, 이후 Hat/Outfit/Weapon 각10% 및 없음70%의 고정 분기, Coin 엔트리 무시, 같은 runId 중복 완료/승리/Result/지급/로비 회복 무쓰기, 난수 비소모, temp flush 교체 중단 후 단일 회복 지급.
- 성장: 3슬롯 각 Lv1→5 비용100/150/200/250 총700, 3개2100. 부족/없는ID/미등록ID/Lv5 요청 파일 불변. 슬롯 교체1개만 장착, 해제·재장착과 저장 복원. 미지원 스키마/미등록 레코드는 원본 유지.
- 실제 입력: 첫 클리어→결과 장비창→장착/Lv2→재도전 HP110, 후속 클리어 자금으로 모자Lv5→3슬롯 HP135/공격1.1, 실제 빨강4.4 및 주황+50%13.2 피해, 다음 저장 경계/기존 진행 보호. 여행·보스HP·일부 runId/API자금은 격리 fixture이며 사람 무보정 완주 아님.
- 보호: 외부 저장 백업/manifest SHA 확인. 원래 게임 saves 폴더는 없었으며 원래 파일명·SHA 전후 일치. 최고점·입력 장치/설정·GameView 복원, 씬/.meta GUID 유지.
- 초기 실패/시각 결함: 검사 정리 콜백 누락으로 최초 도구 컴파일 실패→수정. 중간 기능 검사는 성공했으나 실제 PNG에서 결과 문구 겹침/로비 캔버스 크기0 발견→배치/stretch/정렬과 전체 크기·실제 열기/닫기 검사 보강. 중간 근거는 pass01/pass02-main-layout-issue와 compile-attempt01-validator.log 보존.
- 최종 전체 CLI 컴파일 종료0. 성장 Play182 / 새 프로세스 Cold118 통과·각 종료0·게임 경로 런타임 오류0. 원본06 예제 checksum/서명/진행 호환 및 읽기 후 바이트 불변, 실제 준비HP135/NEW GAME 문구 동기화 확인. 저장 회귀500 / 아이템·전투 회귀318 Play 통과·각 종료0·런타임 오류0(추가 UI 구독으로 이전 검사 수와 차이). 근거 output/playtests/2026-11-13-session07/{play-results.txt,cold-results.txt,play.log,cold.log,profile-example.json}, SESSION-07-HANDOFF.txt.
- 한계: V-001 Player/새 캐시, V-002 실제 물리 입력·무보정 완주, I-003 전체 UI/폰트, D-001/D-002 실기기 항목 유지. 임의 디스크 편집 방지/다중 프로세스 동시 저장/장기 경제 통계 검증은 아님.
