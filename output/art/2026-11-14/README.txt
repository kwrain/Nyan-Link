냥링크 아트·사운드 교체 01 — 2026-11-14
기준: 확정 기획1.1, 기존 output/concepts/nyan-link-finished-game-concept.png 스타일.
이미지 생성물은 콘셉트 참고가 아니라 실제 Unity Resources에 연결한 에셋이다.
실제 게임 화면은 output/playtests/2026-11-14-art-audio/*.png 및 player/*.png와 구분한다.

이미지6개 / 스프라이트17개:
- cat-sheet: 2×2, Run A/B → Jump → Slide. Block/Attack/피격은 기존 상태를 읽어 틴트/위치만 표현.
- rat-sheet: 2×1, 경계 / 그로기. 공격·HP·타이머는 기존 BossController 소유.
- obstacle-sheet: 3×1, 빨강 가시 / 파랑 통과 빔 / 노랑 방패 상자. 기존 판정/콜라이더/예약 유지.
- tile-sheet: 3×2, 빨강·파랑·노랑 / 보라·주황·청록. 심볼은 현재 타일 색상 안내.
  아이템 종류는 색과 분리된 기존 ItemType 및 REC/FRZ/BLAST/LINE/PWR/COLOR 배지가 기준이다.
  아이템 Lv1 금색·Lv2 청록 흰색 테두리 추가. 재색칠 시 종류가 바뀌었다고 해석하지 말 것.
- title-logo: 투명 타이틀 로고.
- city-loop: 민트 도시/공원/잔디 배경. 2장 순환, 거리 기반 이동, 난수 비소모.

생성 프롬프트: prompts.json. 에셋 파일별 SHA256/해상도/오디오 길이·최대값: asset-manifest.json.
이미지는 OpenAI 이미지 생성 기능으로 제작. 음악은 AI가 직접 작성한 새 선율·화음과 합성 알고리즘이며 별도 오디오 생성 모델을 사용한 것은 아니다. 외부 음원·기존 곡 샘플 없음.
음원10개: adventure(108 BPM), boss(140 BPM), 매칭3단계, item, ui, clear, lose, hit.
Tools/generate_concept_audio.py로 동일 PCM WAV 재생성 가능. 기존 .meta 유지.
실제 스피커 청취·모바일 믹스는 미검증 V-012/D-001, 코드상 재생 상태와 청취 결과를 혼동하지 말 것.

교체 절차:
1. 기존 PNG/WAV/.meta 및 catalog.json을 별도 복사한 후 같은 파일명/그리드 순서로 새 리소스 교체.
2. 정확한 Unity6000.3.14f1에서 NyanLinkConceptArtPipeline.Prepare 실행.
3. 텍스처는 Texture2D/NPOT 원본 크기/Point(타일Bilinear)/mipmap없음/CPU복사없음.
   카탈로그는 alpha>220 영역에서 자른다. 그림 자체 수정은 하지 않음.
   타일 Sprite.bounds는1×1로 유지하여 기존39칸·히트 영역 계약 보존.
4. 캐릭터 같은 세트 내 공통PPU/바닥 피벗, 배경·로고 중앙 피벗. 카탈로그 수동 수정 대신 재생성.
5. CLI 그래픽 플레이 + 실제 캡처 확인, 격리 저장 사용. 씬이나 기존 리소스 삭제 불필요.
기존 아트/타일 데이터/Audio/Prototype은 보존. catalog가 없으면 새 그림 어댑터는 표시하지 않고 기존 타일을 사용한다. 오디오는 Inspector 연결이 우선이고 새 리소스, 기존 Prototype 순으로 null 기본값을 채운다.
