# 아이템 운영 로그

## 콘솔 확인 결과 (2026-11-09)

- `item_audit` 유형과 아래 7개 필드를 콘솔에 등록하고 재진입해 저장을 확인했다. 보관 기간은 90일.
- 유저는 1234(Tory), qqqq, user1 총 3개. player_save에는 Tory와 qqqq의 저장 행 2개가 있다.
- USER_DATA에는 Tory의 이전 저장 1행, user1의 초기 테스트 저장 4행이 남아 있다.
- user1의 4행은 현행 LegacyGameDataReader의 중복 행 검사에 걸려 데이터 이전이 중단될 수 있다.
- qqqq와 user1은 테스트 계정으로 추정되지만 삭제 여부를 확정하지 않아 모든 계정과 데이터를 보존했다.
- 실제 게임 실행으로 생성된 item_audit 로그 수신은 아직 검증하지 않았다.

## 확인 위치

뒤끝 콘솔 → 로그 관리 → 신규 로그(InsertLogV2) → 행동 유형 `item_audit`.
스키마 필드는 `eventId`, `userUuid`, `source`, `saveRevision`, `occurredUtc`,
`appVersion`, `payloadJson`으로 등록한다. 스키마 등록 전에는 unknown_fields에서 확인한다.
콘솔 반영에는 최대 5분이 걸릴 수 있다. 보관 기간은 콘솔에서 확인하고 필요한 기록은 기간 내 내보낸다.

## 기록 범위

- 캐릭터·유물 신규 획득 및 제거, 중복 조각 증가·소모, 성급 변경, 유물 레벨 변경.
- `source`: character_draw_1/10, relic_draw_1/10, PurchaseAsync(캐릭터 첫 구매),
  LevelUpCharacterAsync(캐릭터 성급), AscendRelicAsync(유물 성급), LevelUpAsync(유물 레벨).
- payloadJson의 changes: kind, itemId, before, after. 같은 저장의 골드·다이아 전후 값도 포함.
- 기존 유저의 최초 저장에는 inventory_baseline을 별도로 기록한다. 과거 획득 시각·경로를 복원한 로그가 아니다.
- 전투 경험치·골드 변화만 있는 저장, 장착 변경은 아이템 로그를 만들지 않는다.
- 미구현 장비·재료의 지급 경로, 운영자가 콘솔에서 직접 바꾼 값은 이 로그 대상이 아니다.

## 오류 아이템 조사

1. 해당 기간의 item_audit 로그를 내보내 payloadJson의 changes에서 itemId를 찾는다.
2. kind=character/relic, before=0, after=1은 최초 획득이다. fragment 종류는 중복 획득·소모 기록이다.
3. userUuid 및 뒤끝이 자동 기록하는 유저 식별자로 대상 계정을 찾는다.
4. eventId가 같은 재전송 기록은 한 건으로 계산한다.
5. 현재 player_save의 실제 보유·성급·조각을 확인한 뒤 회수/보상 대상을 결정한다.
   로그만으로 자동 회수하지 않는다. 보상 지급 도구와 운영자 변경 로그는 별도 구현 대상이다.

## 저장·장애 처리

아이템 변경과 대기 로그를 동일한 player_save 업데이트에 저장한다.
저장 성공 후 비동기로 게임 로그를 전송한다. 전송 실패 시 대기 로그는 세이브에 남는다.
로그인/다음 저장에서 재시도하며 전송 성공한 항목은 다음 세이브에서 제거한다.
따라서 강제 종료·기기 전환 등으로 같은 eventId가 재전송될 수 있다(at-least-once).
새 컬럼은 필요 없으며 progress JSON에 itemAuditInitialized, itemAuditPending이 추가된다.
대기 기록이 24개 또는 기존 JSON 12KB 제한에 도달하면 새 저장을 거절하고 기존 로그를 재전송한다.
전송 장애가 지속되면 서버/콘솔 설정을 점검한다. 대기 로그를 수동 삭제하지 않는다.

클라이언트가 생성한 운영 추적 로그이며 조작 방지의 증거로 단독 사용하지 않는다.
출시 시 뽑기·지급 검증을 서버 함수로 옮기면 서버 권한의 감사 기록으로 강화할 수 있다.

## 데이터 정리 점검 결과 (코드 기준)

- player_save: 현재 사용하는 실제 세이브. 삭제 금지.
- USER_DATA: 기존 세이브가 없는 계정의 이전/신규 생성 시 LegacyGameDataReader가 조회한다.
  이전 완료 여부 확인과 읽기 경로 제거 없이 테이블을 삭제하면 가입/로그인이 실패할 수 있다.
- legacy_archive: 과거 데이터 원본 보존용. 이전 여부를 확인하기 전까지 유지.
- equipment, idle_reward, wallet.materials 및 progress의 일부 확장 필드는 현재 게임 동작에서
  활용하지 않거나 제한적으로 쓰지만 모델·검증·저장에서 참조한다. 콘솔 컬럼만 삭제하지 않는다.
- 유저 계정이 불필요한 테스트 계정인지 여부는 코드로 판별할 수 없다.

## 검증

에디터에서 1회/10회 캐릭터·유물 뽑기, 성급·유물 레벨 강화 후 로그를 확인한다.
저장 실패 시 확정 로그가 생성되지 않는지, 로그 전송 실패 후 재로그인 시 같은 eventId로
재전송되는지 확인한다. 실제 서버 콘솔 확인과 Unity 플레이 검증이 필요하다.

공식 문서: https://docs.backnd.com/sdk-docs/backend/base/game-log/insert/
