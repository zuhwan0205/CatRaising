# 스크립트 구조

게임 동작과 클래스 이름은 유지하고 역할별로 소스 파일을 분류했습니다.

| 폴더 | 역할 |
| --- | --- |
| LoadingScene | 로그인 화면과 초기화 흐름 (`BackendManager`, `LoadingLoginUI`) |
| Backend/Authentication | 뒤끝 계정 인증 |
| Backend/Models | 저장할 유저 데이터 모델 |
| Backend/Storage | DB 읽기·저장·검증·기존 데이터 변환 |
| Content/Definitions | 캐릭터·유물 정의 및 카탈로그 클래스 |
| Content/Rules | 상점·뽑기·성장 규칙 |
| GameScene | 게임 씬 시작 및 연결 (`CatGameScene`) |
| GameScene/Session | 구매·성장·장착 및 저장 작업 |
| GameScene/UI | 게임 화면과 탭별 UI (동일 클래스의 partial 파일) |
| UI/Common | 공통 버튼 스타일 |
| Combat | 전투 필드 생성·연결 (`CatFieldController`) |
| Combat/Core | 공격·체력·몬스터·유물 효과 |
| Combat/Movement | 이동·조이스틱·장애물 경로 탐색 |
| Combat/Visuals | 캐릭터·공격 연출·카메라·화면 크기 |
| Combat/Map | 배경 생성과 맵 장애물 |
| Editor/Map | 편집용 맵 생성·전투 맵 적용 도구 |
| Editor/Art | 그림 임포트 설정 도구 |

## 유지한 경로와 참조

- 2026-11-10: `Assets/Scripts/Resources`의 에셋은 `.meta`와 함께 `Assets/Resources`로 이동했습니다. 에디터의 고정 경로도 갱신했습니다. Content의 별도 Resources와 MapDesign, UIArt는 유지합니다.
- Editor 도구는 계속 Editor 폴더 아래에 있어 기기 빌드에서 제외됩니다.
- 이동한 스크립트의 `.meta` 파일을 함께 이동하여 기존 GUID를 유지합니다.
- 기존 설명 문서는 작성 당시의 경로를 포함할 수 있습니다. 현재 소스 위치는 위 표를 기준으로 찾습니다.
- `.editorconfig`는 C# 들여쓰기와 줄바꿈 형식을 지정합니다.

## 현재 기능과 문서 (2026-11-10)

- `GameScene/Session/CatBackgroundMusic`: 로딩·메인·던전·레이드별 BGM 자동 전환과 무한 반복, 기본 음량 30%.
- `CatSoundEffects`: `SFX/` 로딩, 전투 8개·UI 4개 음원 풀.
- `CatAudioChannelSource`, `CatLocalSettings`: BGM·효과음 개별 볼륨, 음소거, FPS, 그림자 설정.
- `GameScene/UI/CatGameSceneUI.Tutorial`: 8종 안내, 완료 상태 저장, 설정에서 다시 보기.
- 전체 현황: [개발 현황](../../Docs/개발_현황.md), [게임 구조 설명](../../Docs/게임_구조_설명.md), [오디오·리소스](../../Docs/오디오_리소스.md).
