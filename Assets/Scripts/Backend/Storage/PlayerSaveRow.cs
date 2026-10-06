#if BACKND_SDK_INSTALLED
using System.Collections.Generic;
using BACKND.Database;

// SDK의 IL Weaver가 속성 기반 테이블 매핑 코드를 생성합니다.
[Table("player_save", TableType.UserTable, ClientAccess = true,
    ReadPermissions = new[] { TablePermission.SELF }, WritePermissions = new[] { TablePermission.SELF })]
public class PlayerSaveRow : BaseModel
{
    [PrimaryKey, Column("save_id", NotNull = true)] public string SaveId { get; set; }
    [Column("schema_version", NotNull = true)] public int SchemaVersion { get; set; }
    [Column("revision", NotNull = true)] public long Revision { get; set; }
    [Column("progress", DatabaseType.Json, NotNull = true)] public PlayerProgress Progress { get; set; }
    [Column("wallet", DatabaseType.Json, NotNull = true)] public PlayerWallet Wallet { get; set; }
    [Column("characters", DatabaseType.Json, NotNull = true)] public List<OwnedCharacter> Characters { get; set; }
    [Column("equipment", DatabaseType.Json, NotNull = true)] public List<OwnedEquipment> Equipment { get; set; }
    [Column("relics", DatabaseType.Json, NotNull = true)] public List<OwnedRelic> Relics { get; set; }
    [Column("loadout", DatabaseType.Json, NotNull = true)] public PlayerLoadout Loadout { get; set; }
    [Column("idle_reward", DatabaseType.Json, NotNull = true)] public IdleRewardState IdleReward { get; set; }
    [Column("legacy_archive", DatabaseType.Json, NotNull = true)] public Dictionary<string, object> LegacyArchive { get; set; }
}
#endif
