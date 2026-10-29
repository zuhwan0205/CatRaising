using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class GameplayPatternChecks
{
    public static async void Run()
    {
        try
        {
            GameplayPatternMigration.Prepare();
            CheckStates(); CheckAreas(); CheckFrames();
            await CheckMutations();
            Debug.Log("GAMEPLAY_PATTERN_CHECKS_PASS: transitions, attack geometry parity, frame parity/cache, transaction rollback/success/concurrency.");
            EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
    private static void CheckStates()
    {
        var dungeon = new DungeonWaveState();
        Require(dungeon.CanPrepare && dungeon.StartWave() && !dungeon.StartWave(), "Wave start guard");
        Require(dungeon.ClearWave() && !dungeon.ClearWave() && dungeon.StartWave(), "Wave clear guard");
        Require(dungeon.Finish() && !dungeon.Finish() && !dungeon.StartWave(), "Dungeon terminal state");
        var encounter = new EncounterFlowState();
        Require(!encounter.BeginResult(), "No early result"); encounter.Ready();
        Require(encounter.Pause() && !encounter.Pause(), "Pause guard");
        Require(encounter.BeginReturn(), "Exit while paused"); encounter.CancelReturn();
        Require(encounter.Current == EncounterFlowState.Phase.Paused, "Failed return preserves pause");
        Require(encounter.Resume() && encounter.BeginResult() && !encounter.BeginResult() && !encounter.BeginReturn(), "Result guard");
        encounter.CompleteResult();
        Require(!encounter.BeginResult() && encounter.BeginReturn(), "One result, then return");
    }
    private static void CheckAreas()
    {
        foreach (var id in new[] { "claw_melee", "spin_melee", "trio_melee", "target_area", "forward_area", "breath_area", "unknown" })
            foreach (var target in new[] { Vector2.zero, Vector2.up * 2, new Vector2(1, -2) })
                for (int x = -12; x <= 12; x++) for (int y = -12; y <= 12; y++)
                {
                    Vector2 enemy = new Vector2(x * .25f, y * .25f);
                    bool expected = LegacyArea(id, Vector2.zero, target, enemy, 2);
                    Require(CharacterCombatRules.Hits(id, Vector2.zero, target, enemy, 2) == expected, "Area parity: " + id);
                }
        Require(ReferenceEquals(CharacterAttackFactory.Create("spin_melee"), CharacterAttackFactory.Create("spin_melee")), "Shared strategy");
        var catalog = Resources.Load<CatGameCatalog>("DefaultCatCatalog");
        Require(catalog.FindCharacter("cat_shadow").hitEffect == CharacterHitEffect.Slow, "Ice migration");
        Require(catalog.FindCharacter("cat_moon").hitEffect == CharacterHitEffect.ArmorBreak, "Mage migration");
        Require(catalog.FindCharacter("cat_dancer").grantsAttackAura, "Dancer migration");
        Require(catalog.FindCharacter("cat_ninja").delivery == AttackDelivery.Projectile, "Archer migration");
    }
    private static bool LegacyArea(string id, Vector2 origin, Vector2 target, Vector2 enemy, float range)
    {
        var offset = enemy - origin;
        if (id == "target_area") return (enemy - target).sqrMagnitude <= 1.5f * 1.5f;
        if (id == "spin_melee" || id == "trio_melee") return offset.sqrMagnitude <= range * range;
        var direction = (target - origin).normalized;
        if (direction.sqrMagnitude < .000001f) direction = Vector2.up;
        float along = Vector2.Dot(offset, direction);
        if (id == "breath_area") return offset.sqrMagnitude <= range * range && along >= 0 &&
            (offset.sqrMagnitude < .000001f || along >= offset.magnitude * .8660254f);
        if (id == "forward_area") return along >= 0 && along <= range && Mathf.Abs(direction.x * offset.y - direction.y * offset.x) <= 1;
        return false;
    }
    private static void CheckFrames()
    {
        var catalog = Resources.Load<CatVisualCatalog>("CatVisualCatalog");
        Require(catalog != null && catalog.characters.Length == 12, "Visual catalog");
        int count = 0;
        foreach (var entry in catalog.characters)
        {
            var old = LegacyCatFrameMigration.Get(entry.characterId);
            var current = CatAnimationFrames.Get(entry.characterId);
            Require(old.Length == current.Length, "Frame count: " + entry.characterId);
            Require(ReferenceEquals(current, CatAnimationFrames.Get(entry.characterId)), "Frame cache: " + entry.characterId);
            for (int i = 0; i < old.Length; i++)
            {
                Require(current[i] != null && current[i].texture == old[i].texture, "Frame texture");
                Require((current[i].rect.position - old[i].rect.position).sqrMagnitude < .001f &&
                    (current[i].rect.size - old[i].rect.size).sqrMagnitude < .001f &&
                    (current[i].pivot - old[i].pivot).sqrMagnitude < .001f &&
                    Mathf.Abs(current[i].pixelsPerUnit - old[i].pixelsPerUnit) < .001f, "Frame metadata: " + entry.characterId + "/" + i);
                count++;
            }
        }
        Debug.Log("Frame metadata parity: " + count + " frames across 12 characters.");
    }
    private static async Task CheckMutations()
    {
        int number = 3;
        try
        {
            using (var change = new ReversibleMutation())
            { change.Set(() => number, v => number = v, 5); change.Set(() => number, v => number = v, 7); throw new Exception("simulated"); }
        }
        catch { Require(number == 3, "Exception rolls back partial mutation in reverse order"); }
        var data = BackendUserData.CreateNew();
        data.wallet.gold = 100000000; data.wallet.gems = 10000;
        var cat = data.characters[0]; cat.fragments = 1000;
        var catalog = Resources.Load<CatGameCatalog>("DefaultCatCatalog");
        int saves = 0;
        var gate = new TaskCompletionSource<bool>();
        var session = new CatGameSession(data, false, () => { saves++; return gate.Task; }, catalog);
        string initial = JsonUtility.ToJson(data);
        var pending = session.LevelUpCharacterAsync(cat);
        await session.LevelUpCharacterAsync(cat);
        Require(saves == 1, "Concurrent mutation blocked");
        gate.SetResult(false); await pending;
        Require(JsonUtility.ToJson(data) == initial, "Star rollback");
        session.Events.Dispose();
        session = new CatGameSession(data, false, () => Task.FromResult(false), catalog);
        cat.skillRanks[0] = 1;
        initial = JsonUtility.ToJson(data);
        await session.ResetSkillsAsync(cat);
        Require(JsonUtility.ToJson(data) == initial, "Skill reset rollback");
        string buy = catalog.AllCharacters().First(c => c.id != cat.characterId && c.shop.CanSell).id;
        await session.BuyCharacterAsync(buy);
        Require(JsonUtility.ToJson(data) == initial, "Purchase rollback");
        var result = await session.DrawContentBatchAsync(true, 10);
        Require(result == null && JsonUtility.ToJson(data) == initial, "Ten character draws rollback");
        result = await session.DrawContentBatchAsync(false, 10);
        Require(result == null && JsonUtility.ToJson(data) == initial, "Ten relic draws rollback");
        session.Events.Dispose();
        session = new CatGameSession(data, false, () => Task.FromResult(true), catalog);
        long gems = data.wallet.gems;
        await session.ResetSkillsAsync(cat);
        Require(data.wallet.gems == gems - CatSkillTree.ResetCost && cat.skillRanks.All(r => r == 0), "Successful commit");
        session.Events.Dispose();
    }
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
