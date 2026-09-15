# 데이터베이스 SDK 사용 기준

앞으로 신규 게임 데이터 저장/조회는 BACKND.Database를 사용합니다. Backend.GameData는 기존 USER_DATA에서 이전하기 위한 읽기 전용 LegacyGameDataReader에서만 사용합니다.

## 패키지

Packages/manifest.json에 공식 scoped registry와 com.backnd.database 0.0.18을 등록했습니다. com.backnd.tools 및 Newtonsoft JSON은 패키지 의존성입니다. 설치된 베이스 SDK는 5.18.3입니다. World SDK는 사용하지 않으며 Unity 6000.6의 SceneHandle 호환 오류 때문에 의존성에서 제외했습니다.

## 최초 연결

1. Unity가 Package Manager 설치 및 스크립트 컴파일을 완료할 때까지 기다립니다.
2. 기존 로딩 씬에서 로그인합니다. BackendManager의 Database Uuid 기본값은 사용자가 제공한 DB UUID로 설정했습니다.
3. 새 DB에 player_save가 없다면 조회 오류가 표시됩니다. Play 상태에서 BackendManager 컴포넌트의 메뉴를 열고 `Database/Create player_save table (once)`를 한 번 실행합니다. SDK 모델을 기준으로 테이블이 생성됩니다. 이미 있는 테이블은 수정/삭제하지 않습니다.
4. 생성 완료 안내 후 로그인 버튼을 다시 누릅니다. 새 DB에 행이 없으면 기존 USER_DATA를 읽어 이전하거나, 기존 행도 없으면 기본 데이터를 생성합니다.
5. 새 DB의 player_save 행과 재로그인/캐릭터 선택 저장을 확인합니다.

콘솔에서 생성할 경우 아래 스키마를 사용합니다. DB에서 테이블 생성 권한 오류가 나면 콘솔 Query Editor에서 schema.sql을 실행하세요. 실제 서버의 테이블 생성/이전 검증은 아직 수행하지 않았습니다.

## 저장 모델

player_save는 UserTable이며 CLIENT_ACCESS=true, READ=SELF, WRITE=SELF입니다. 현재 사용자 필터와 UUID 기반 기본 키로 조회합니다.

- save_id: string, Primary Key, DB Client.UserUUID 값
- schema_version: int32, NotNull
- revision: int64, NotNull
- progress, wallet, characters, equipment, relics, loadout, idle_reward, legacy_archive: 각각 json, NotNull

기존 playerDataJson 문자열을 그대로 넣지 않고, 구조별 JSON 컬럼으로 나눕니다. 한 행으로 원자적 저장하며 revision 조건으로 이전 버전의 덮어쓰기를 차단합니다. 보유 목록이 각 JSON 컬럼의 12KB 제한에 가까워지면 개별 보유 테이블로 분리해야 합니다. 분리 후 재화 차감/아이템 지급은 트랜잭션을 사용합니다.

## 코드 역할

- BackendGameData: DB 초기화, 현재 유저 조회, 신규 삽입, 비동기 저장, 버전 충돌 감지.
- PlayerSaveRow: SDK 테이블/컬럼 모델. Unity IL Weaver가 매핑 메서드를 생성합니다.
- LegacyGameDataReader: 이전 데이터 읽기와 변환. 기존 서버 행 쓰기/삭제 없음.
- BackendUserData: 게임 내 데이터 구조. UI는 이 구조를 그대로 사용합니다.
- CatGameSession: Task 기반 저장과 선택 실패 복원. 씬 종료 시에도 전송 중 저장은 완료 처리하고 UI 이벤트만 해제합니다.

조회 실패를 빈 데이터로 처리하지 않습니다. 이전 저장소가 오류를 반환해도 초기 데이터로 대체하지 않습니다. 저장 응답이 불확실하거나 버전 충돌이 발생하면 재로그인하여 서버 상태를 다시 읽습니다.

## 검증

실제 SDK 소스와 기존 SDK DLL을 참조한 컴파일, 비동기 저장 대기/성공/실패/예외/중복 요청/미보유 캐릭터/미리보기 단독 테스트를 확인했습니다. Unity 생성 Assembly-CSharp의 PlayerSaveRow에 Weaver 매핑 메서드가 생성된 것도 확인했습니다. 서버 왕복 검증은 테이블 생성 후 필요합니다.

## 공식 문서

- https://docs.backnd.com/sdk-docs/database/intro/
- https://docs.backnd.com/sdk-docs/database/data-modeling/
- https://docs.backnd.com/sdk-docs/database/insert/
- https://docs.backnd.com/sdk-docs/database/query/
- https://docs.backnd.com/sdk-docs/database/update-delete/
- https://docs.backnd.com/sdk-docs/database/transaction/
