using System;
using System.Threading.Tasks;

// UI/SDK에 독립적인 비동기 저장 기능입니다. 전송 중 씬이 닫혀도 완료 결과를 처리합니다.
public class CatGameSession
{
    public async Task CompleteDungeonAsync(int stage)
    {
        if (IsSaving || HasPendingReward || stage < 1 || stage > 15 || stage != data.progress.dungeonClearedStage + 1) return;
        data.progress.dungeonClearedStage = stage;
        HasPendingReward = true;
        await SaveAsync();
    }
    public async Task GrantCrystalAdRewardAsync()
    {
        if (IsSaving || HasPendingReward || CrystalAdRewards.Remaining(data.wallet) == 0)
            return;
        if (data.wallet.gems > long.MaxValue - CrystalAdRewards.Amount)
            return;
        string today = CrystalAdRewards.Today;
        if (data.wallet.crystalAdDay != today)
            data.wallet.crystalAdCount = 0;
        data.wallet.crystalAdDay = today;
        data.wallet.crystalAdCount++;
        data.wallet.gems += CrystalAdRewards.Amount;
        HasPendingReward = true;
        await SaveAsync();
    }
    private readonly BackendUserData data;
    private readonly bool preview;
    private readonly Func<Task<bool>> saveData;
    private readonly CatGameCatalog catalog;
    private readonly Random drawRandom = new Random();
    public bool IsSaving
    {
        get; private set;
    }
    public bool HasPendingReward
    {
        get; private set;
    }
    private bool rewardLimitReached;
    private bool savingExperience;
    private long experienceRevision, savedExperienceRevision, queuedGold;

    public async Task SaveExperienceAsync()
    {
        if (IsSaving || HasPendingReward || experienceRevision == savedExperienceRevision) return;
        long snapshot = experienceRevision;
        savingExperience = IsSaving = true;
        bool success = false;
        try { success = preview || await saveData(); }
        catch (Exception) { success = false; }
        finally
        {
            if (success) savedExperienceRevision = snapshot;
            savingExperience = IsSaving = false;
        }
        // 경험치 저장 중 처치한 몬스터의 골드도 유실하지 않습니다.
        long gold = queuedGold;
        queuedGold = 0;
        if (gold > 0) await AwardGoldAsync(gold);
    }
    public event Action<bool> GameplayBlockedChanged;
    public event Action<bool> BusyChanged;
    public event Action<string> StatusChanged;

    public async Task AwardGoldAsync(long amount)
    {
        if (savingExperience && amount > 0)
        {
            queuedGold = checked(queuedGold + amount);
            return;
        }
        if (IsSaving || HasPendingReward || rewardLimitReached || amount < 0)
            return;
        if (data.wallet.gold > long.MaxValue - amount)
        {
            rewardLimitReached = true;
            GameplayBlockedChanged?.Invoke(true);
            StatusChanged?.Invoke("골드 한도에 도달해 전투를 중단했습니다.");
            return;
        }
        data.wallet.gold += amount;
        HasPendingReward = true;
        GameplayBlockedChanged?.Invoke(true);
        await SaveAsync();
    }

    public CatGameSession(BackendUserData data, bool preview, Func<Task<bool>> saveData, CatGameCatalog catalog = null)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.preview = preview;
        this.saveData = saveData ?? throw new ArgumentNullException(nameof(saveData));
        this.catalog = catalog;
    }

    public async Task LevelUpCharacterAsync(OwnedCharacter character)
    {
        if (IsSaving || HasPendingReward || character == null || !data.characters.Contains(character))
            return;
        if (!CatProgression.TryStarCost(character.ascension, out long cost))
        {
            StatusChanged?.Invoke("최대 성급(5성)이거나 성급 정보가 올바르지 않습니다.");
            return;
        }
        if (character.fragments < cost)
        {
            StatusChanged?.Invoke($"해당 캐릭터 조각이 부족합니다. 필요 조각: {cost:N0}");
            return;
        }
        int oldStars = character.ascension;
        long oldFragments = character.fragments;
        character.ascension++;
        character.fragments -= cost;
        await SaveAsync(() => { character.ascension = oldStars; character.fragments = oldFragments; });
    }

    public bool TryAwardExperience(long amount)
    {
        if ((IsSaving && !savingExperience) || HasPendingReward || amount <= 0) return false;
        var cat = data.characters.Find(x => x != null && x.characterId == data.loadout.characterId);
        if (cat == null) return false;
        CatProgression.AddExperience(cat, amount);
        experienceRevision++;
        return true;
    }

    public async Task ResetSkillsAsync(OwnedCharacter cat)
    {
        if (IsSaving || HasPendingReward || cat == null || !data.characters.Contains(cat) ||
            !CatSkillTree.HasLearnedSkills(cat) || data.wallet.gems < CatSkillTree.ResetCost) return;
        var previousRanks = cat.skillRanks;
        long previousGems = data.wallet.gems;
        cat.skillRanks = new int[CatSkillTree.Count];
        data.wallet.gems -= CatSkillTree.ResetCost;
        await SaveAsync(() =>
        {
            cat.skillRanks = previousRanks;
            data.wallet.gems = previousGems;
        });
    }

    public async Task LearnSkillAsync(OwnedCharacter cat, int node)
    {
        if (IsSaving || HasPendingReward || cat == null || !data.characters.Contains(cat) || !CatSkillTree.CanLearn(cat, node)) return;
        var previous = cat.skillRanks;
        var ranks = new int[CatSkillTree.Count];
        for (int i = 0; i < ranks.Length; i++) ranks[i] = CatSkillTree.Rank(cat, i);
        ranks[node]++;
        cat.skillRanks = ranks;
        await SaveAsync(() => cat.skillRanks = previous);
    }
    public Task BuyCharacterAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || catalog == null)
            return Task.CompletedTask;
        var definition = catalog.FindCharacter(id);
        var item = new OwnedCharacter { characterId = id };
        return PurchaseAsync(definition?.shop, data.characters.Exists(x => x != null && x.characterId == id),
            () => data.characters.Add(item), () => data.characters.Remove(item));
    }

    public async Task<ContentDrawResult> DrawRelicAsync()
    {
        if (IsSaving || HasPendingReward || catalog == null)
            return null;
        var rules = catalog.relicDraw;
        var pool = catalog.RelicDrawPool();
        if (rules == null || !rules.Validate(pool))
        {
            StatusChanged?.Invoke("뽑기 확률 또는 등급별 유물 목록을 확인해주세요.");
            return null;
        }
        if (data.wallet.gold < rules.goldCost)
        {
            StatusChanged?.Invoke($"골드가 부족합니다. 필요 골드: {rules.goldCost:N0}");
            return null;
        }
        // 조각 한도로 인해 특정 당첨 결과만 취소되는 편향을 막습니다.
        foreach (var entry in pool)
        {
            var existing = data.relics.Find(x => x != null && x.relicId == entry.id);
            if (existing != null && existing.fragments > long.MaxValue - rules.duplicateFragments)
            {
                StatusChanged?.Invoke("유물 조각 보유 한도에 도달했습니다.");
                return null;
            }
        }
        var definition = rules.Pick(pool, drawRandom.NextDouble(), drawRandom.NextDouble());
        var owned = data.relics.Find(x => x != null && x.relicId == definition.id);
        bool duplicate = owned != null;
        long previousGold = data.wallet.gold, previousFragments = owned == null ? 0 : owned.fragments;
        if (!duplicate)
        {
            owned = new OwnedRelic { relicId = definition.id };
            data.relics.Add(owned);
        }
        else
            owned.fragments += rules.duplicateFragments;
        data.wallet.gold -= rules.goldCost;
        bool saved = await SaveAsync(() =>
        {
            data.wallet.gold = previousGold;
            if (duplicate)
                owned.fragments = previousFragments;
            else
                data.relics.Remove(owned);
        });
        return saved ? new ContentDrawResult { id = definition.id, name = definition.displayName, rarity = definition.rarity, duplicate = duplicate, fragments = duplicate ? rules.duplicateFragments : 0 } : null;
    }

    public async Task<ContentDrawResult> DrawCharacterAsync()
    {
        if (IsSaving || HasPendingReward || catalog == null)
            return null;
        var rules = catalog.characterDraw;
        var pool = catalog.CharacterDrawPool();
        if (rules == null || !rules.Validate(pool))
        {
            StatusChanged?.Invoke("뽑기 확률 또는 등급별 캐릭터 목록을 확인해주세요.");
            return null;
        }
        if (data.wallet.gold < rules.goldCost)
        {
            StatusChanged?.Invoke($"골드가 부족합니다. 필요 골드: {rules.goldCost:N0}");
            return null;
        }
        // 조각 한도로 인해 특정 당첨 결과만 취소되는 편향을 막습니다.
        foreach (var entry in pool)
        {
            var existing = data.characters.Find(x => x != null && x.characterId == entry.id);
            if (existing != null && existing.fragments > long.MaxValue - rules.duplicateFragments)
            {
                StatusChanged?.Invoke("캐릭터 조각 보유 한도에 도달했습니다.");
                return null;
            }
        }
        var definition = rules.Pick(pool, drawRandom.NextDouble(), drawRandom.NextDouble());
        var owned = data.characters.Find(x => x != null && x.characterId == definition.id);
        bool duplicate = owned != null;
        long previousGold = data.wallet.gold, previousFragments = owned == null ? 0 : owned.fragments;
        if (!duplicate)
        {
            owned = new OwnedCharacter { characterId = definition.id };
            data.characters.Add(owned);
        }
        else
            owned.fragments += rules.duplicateFragments;
        data.wallet.gold -= rules.goldCost;
        bool saved = await SaveAsync(() =>
        {
            data.wallet.gold = previousGold;
            if (duplicate)
                owned.fragments = previousFragments;
            else
                data.characters.Remove(owned);
        });
        return saved ? new ContentDrawResult { character = true, id = definition.id, name = definition.displayName, rarity = definition.rarity, duplicate = duplicate, fragments = duplicate ? rules.duplicateFragments : 0 } : null;
    }

    private async Task PurchaseAsync(ShopOffer offer, bool alreadyOwned, Action grant, Action revoke)
    {
        if (IsSaving || HasPendingReward)
            return;
        if (offer == null || !offer.CanSell)
        {
            StatusChanged?.Invoke("현재 판매하지 않는 상품입니다.");
            return;
        }
        if (alreadyOwned)
        {
            StatusChanged?.Invoke("이미 보유하고 있습니다.");
            return;
        }
        if (data.wallet.gold < offer.goldPrice)
        {
            StatusChanged?.Invoke($"골드가 부족합니다. 필요 골드: {offer.goldPrice:N0}");
            return;
        }
        long previousGold = data.wallet.gold;
        data.wallet.gold -= offer.goldPrice;
        grant();
        await SaveAsync(() => { data.wallet.gold = previousGold; revoke(); });
    }

    public Task LevelUpRelicAsync(OwnedRelic relic)
    {
        if (relic == null || !data.relics.Contains(relic))
            return Task.CompletedTask;
        var definition = catalog == null ? null : catalog.FindRelic(relic.relicId);
        return LevelUpAsync(relic.level, definition?.growth, value => relic.level = value);
    }

    private async Task LevelUpAsync(int level, LevelUpRules rules, Action<int> setLevel)
    {
        if (IsSaving || HasPendingReward)
            return;
        if (rules == null || !rules.TryGetCost(level, out long cost))
        {
            StatusChanged?.Invoke("최대 레벨이거나 성장 설정이 없습니다.");
            return;
        }
        if (data.wallet.gold < cost)
        {
            StatusChanged?.Invoke($"골드가 부족합니다. 필요 골드: {cost:N0}");
            return;
        }
        long previousGold = data.wallet.gold;
        data.wallet.gold -= cost;
        setLevel(level + 1);
        await SaveAsync(() => { data.wallet.gold = previousGold; setLevel(level); });
    }

    public async Task SelectCharacterAsync(OwnedCharacter character)
    {
        if (IsSaving || HasPendingReward || character == null || !data.characters.Contains(character))
            return;
        string previous = data.loadout.characterId;
        data.loadout.characterId = character.characterId;
        await SaveAsync(() => data.loadout.characterId = previous);
    }

    public async Task ToggleRelicAsync(OwnedRelic relic)
    {
        if (IsSaving || HasPendingReward || relic == null || !data.relics.Contains(relic))
            return;
        var previous = new System.Collections.Generic.List<string>(data.loadout.relicIds);
        if (data.loadout.relicIds.Contains(relic.relicId))
            data.loadout.relicIds.RemoveAll(id => id == relic.relicId);
        else
        {
            if (data.loadout.relicIds.Count >= 3)
            {
                StatusChanged?.Invoke("유물은 최대 3개까지 장착할 수 있습니다.");
                return;
            }
            data.loadout.relicIds.Add(relic.relicId);
        }
        await SaveAsync(() => data.loadout.relicIds = previous);
    }

    public async Task<bool> SaveAsync(Action rollback = null)
    {
        if (IsSaving)
            return false;
        long experienceSnapshot = experienceRevision;
        if (preview)
        {
            savedExperienceRevision = experienceSnapshot;
            HasPendingReward = false;
            GameplayBlockedChanged?.Invoke(rewardLimitReached);
            StatusChanged?.Invoke("미리보기 변경입니다. 서버에는 저장하지 않습니다.");
            return true;
        }
        IsSaving = true;
        GameplayBlockedChanged?.Invoke(true);
        bool success = false;
        try
        {
            BusyChanged?.Invoke(true);
            StatusChanged?.Invoke("저장 중…");
            success = await saveData();
        }
        catch (Exception) { success = false; }
        finally
        {
            if (!success)
                rollback?.Invoke();
            if (success)
            {
                HasPendingReward = false;
                savedExperienceRevision = experienceSnapshot;
            }
            IsSaving = false;
            GameplayBlockedChanged?.Invoke(HasPendingReward || rewardLimitReached);
            BusyChanged?.Invoke(false);
        }
        StatusChanged?.Invoke(success ? "저장했습니다." : "저장 실패 · 전투가 대기 중이면 저장 버튼으로 재시도해주세요. 반복되면 다시 로그인해주세요.");
        return success;
    }
}
