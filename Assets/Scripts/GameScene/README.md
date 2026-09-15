# Figma 기반 GameScene

원본: https://www.figma.com/design/qvUyXpBwSjxKCbrnTTkbSa/Untitled?node-id=0-1

## 실행

- 기존 로딩 씬에서 로그인하면 GameScene으로 자동 이동합니다.
- BackendManager의 On Login Ready에 기존 Inspector 이벤트가 있으면 자동 이동은 생략됩니다. 기존 씬 전환 이벤트를 정리하거나 GameScene으로 이동하도록 설정하세요.
- Assets/Scenes/GameScene.unity를 직접 열고 Play하면 서버 요청 없는 미리보기로 실행됩니다.
- 게임 씬은 빌드 목록에 추가했습니다. UI는 Play 중 CatGameScene이 생성합니다.
- 세로 720×1280 기준이며 모바일 안전 영역을 적용합니다.

## 구현 범위

상단 레벨/골드/다이아와 저장 버튼, 중앙 전투 미리보기, 하단 모험/상점/캐릭터/유물 탭입니다. Figma의 하단 세 메뉴에 전투 화면으로 돌아오는 모험 탭을 추가했습니다. 컬러는 임시 크림/보라색이며 캐릭터 아트는 텍스트 자리표시자입니다.

캐릭터 보유 목록은 3열 스크롤이고 상세 화면에 기본 성장 스탯과 공격 방식이 표시됩니다. 선택은 유저 데이터에 저장하며 저장 실패 시 이전 선택으로 복구합니다. 상세 스탯은 장비/유물/영구강화를 합산한 최종 스탯이 아닙니다.

유물 목록과 효과 요약은 저장 데이터 및 카탈로그에서 읽습니다. 구매/랜덤 획득/레벨업/자동 전투/보상 지급은 아직 구현하지 않아 준비 중 또는 미리보기로 표시됩니다.

GameScene의 GameScene 오브젝트에 있는 CatGameScene 컴포넌트에 CatGameCatalog 에셋과 한글 Font를 연결할 수 있습니다. 카탈로그 미지정 시 임시 기본 고양이 정의를 사용합니다. 모바일 배포 전 한글 폰트 에셋을 지정하세요.

## 검증

설치된 Unity/뒤끝 DLL을 참조하여 Assets/Scripts의 C# 컴파일을 확인했습니다. Unity 에디터에서 씬 임포트, 화면 표시 및 실제 로그인 후 전환/선택 저장은 추가 실행 확인이 필요합니다.

## 스크립트 역할 분리

- CatGameScene.cs: 씬 초기화, 카탈로그/폰트 참조 유지, UI와 기능 이벤트 연결 및 해제.
- CatGameSceneUI.cs: Canvas/버튼/목록/상세 화면 생성, 탭 전환, 안내 표시. 저장 요청과 캐릭터 선택은 이벤트로 전달하며 서버 API 호출이나 데이터 변경을 하지 않습니다.
- CatGameSession.cs: 보유 여부 확인, 캐릭터 선택, 저장 중 중복 요청 방지, 저장 실패/취소 시 선택 복구, 미리보기 처리. 저장 함수는 씬에서 전달합니다.

기존 GameScene의 CatGameScene 컴포넌트는 그대로 사용합니다. UI 컴포넌트는 실행 시 자동 추가되므로 Inspector에서 추가 연결할 필요가 없습니다. UI 배치를 수정할 때는 CatGameSceneUI, 기능을 수정할 때는 CatGameSession을 편집하세요.

분리 후 전체 Assets/Scripts 컴파일 및 기능 단독 테스트(성공, 실패, 예외, 미리보기, 미보유 선택 차단, 중복 요청, 취소)를 통과했습니다. Unity Play 화면 검증은 별도로 필요합니다.

## 데이터베이스 SDK 전환

현재 저장은 CatGameSession의 SaveAsync/SelectCharacterAsync에서 Task로 대기합니다. BackendGameData.GameDataUpdateAsync를 연결하며 코루틴 저장/CancelPendingSave는 사용하지 않습니다. 저장 중 씬이 닫히면 UI 이벤트만 해제하고 서버 요청 결과에 따라 선택을 유지하거나 복원합니다. 데이터베이스 설정은 ../Database/README.md를 참조하세요.
