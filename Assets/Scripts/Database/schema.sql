-- 새 DB의 Query Editor에서 최초 1회 실행합니다. 기존 테이블은 삭제하지 않습니다.
CREATE TABLE player_save (
    save_id string PRIMARY KEY,
    schema_version int32 NOT NULL,
    revision int64 NOT NULL,
    progress json NOT NULL,
    wallet json NOT NULL,
    characters json NOT NULL,
    equipment json NOT NULL,
    relics json NOT NULL,
    loadout json NOT NULL,
    idle_reward json NOT NULL,
    legacy_archive json NOT NULL,
    USERTABLE (CLIENT_ACCESS = true, READ = (SELF), WRITE = (SELF))
);
