냥링크 픽셀 UI 리소스 — 2026-11-16
스타일: 기존 주황 고양이·민트 도시·네이비 테두리 콘셉트와 통일.
실제 PNG4장 / 런타임 UI Sprite18개. AI 이미지 생성 기능으로 제작, 생성 프롬프트 prompts.json / 파일별SHA256·해상도 asset-manifest.json.
이 폴더의 프롬프트와 Assets/Resources/Art/PixelUI PNG는 제작 리소스이며 실제 플레이 스크린샷이 아니다.
실제 화면은 output/playtests/2026-11-16-ui 아래 PNG와 구분.

buttons: 2×2, blue / mint / gold / red. 글자는 이미지에 굽지 않고 기존 TMP 문구 유지.
panels: 2×2, modal / inventory card / gold slot / reward. 반투명 모서리·짙은 중심의9-slice 패널.
meters: 1×4, empty frame / red HP / mint progress / gold boss HP. 기존 Slider 값·fillRect 연결 유지.
icons: 3×2, heart / coin / hat / outfit / weapon / unknown shield. 종류 설명용이며 소유·장착 상태는 기존 텍스트·데이터 기준.
청록 색 선택은6색 실제 심볼 아이콘과 색 이름을 유지. 선택값/수명/기반 상태 복귀 변경 없음.
아이템은 현재 타일 색과 별개인 기존 ItemType/레벨 배지가 권위 있는 표시.

교체 방법:
원래 PNG/.meta/catalog.json을 먼저 보존. 동일 파일명·셀 순서로 새 리소스를 넣고 Unity6000.3.14f1 CLI NyanLinkPixelUIPipeline.Prepare 실행.
기존 GUID 유지. Texture2D / NPOT 원본 크기 / 비압축 / Point / mipmap없음 / CPU 읽기 복사 없음.
각 셀의 가장 큰 alpha>220 연결 영역을 카탈로그Rect로 선택해 떨어진 생성 잡티를 제외. 원본 이미지 자체를 수정하지 않음.
Sprite.border로9-slice 제공, 버튼/패널은 pixelsPerUnitMultiplier3·게이지틀5. 기존RectTransform·Button/Slider·Canvas·리스너 유지.
장비 목록 새로고침 후에도 행·아이콘·비활성 버튼 스타일 적용. 알 수 없는 저장 장비는 기존 이름/상태를 보존하고 방패 대체 아이콘만 표시.
씬/기획/저장 schema/보상·성장 수치를 바꾸지 않음. 카탈로그가 없으면 기존 표현으로 안전하게 복귀.
