using System;
using System.Threading.Tasks;

// UI/SDK에 독립적인 비동기 저장 기능입니다. 전송 중 씬이 닫혀도 완료 결과를 처리합니다.
public class CatGameSession
{
    public const long EncounterClearGems = 10;

    // stage 0은 레이드입니다. 재도전 승리에도 같은 보상을 지급합니다.
    public async Task<bool> CompleteEncounterAsync(int stage)
    {
        if (MutationBlocked || stage < 0 || stage > 15 || stage > data.progress.dungeonClearedStage + 1 ||
            data.wallet.gems > long.MaxValue - EncounterClearGems) return false;
        if (stage > 0) data.progress.dungeonClearedStage = Math.Max(data.progress.dungeonClearedStage, stage);
        data.wallet.gems += EncounterClearGems;
        HasPendingReward = true;
        await SaveAsync();
        return true;
    }
    public async Task GrantCrystalAdRewardAsync()
    {
        if (MutationBlocked || CrystalAdRewards.Remaining(data.wallet) == 0)
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
    private readonly CombatRewardJournal rewardJournal;
    public void CheckpointRewards() => rewardJournal?.Checkpoint();
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
    private bool savingCombatRewards;
    public bool GameplayBlocked => HasPendingReward || rewardLimitReached || (IsSaving && !savingCombatRewards);
    private long experienceRevision, savedExperienceRevision;
    private long goldRevision, savedGoldRevision, queuedGold;

    // 네트워크 요청 시작 시점의 리비전만 완료 처리합니다.
    // 요청 중 들어온 보상은 다음 주기에 저장하므로 중복 지급/누락이 없습니다.
    public async Task SaveCombatRewardsAsync()
    {
        if (MutationBlocked || (experienceRevision == savedExperienceRevision && goldRevision == savedGoldRevision)) return;
        long experienceSnapshot = experienceRevision, goldSnapshot = goldRevision;
        savingCombatRewards = IsSaving = true;
        bool success = false;
        try { success = preview || await saveData(); }
        catch (Exception) { success = false; }
        finally
        {
            if (success)
            {
                savedExperienceRevision = experienceSnapshot;
                savedGoldRevision = goldSnapshot;
            }
            savingCombatRewards = IsSaving = false;
        }
        if (!success) StatusChanged?.Invoke("전투 보상 저장 실패 · 보상을 보관하고 자동 재시도합니다.");
    }
    public event Action<bool> GameplayBlockedChanged;
    public event Action<bool> BusyChanged;
    public event Action<string> StatusChanged;

    public Task AwardGoldAsync(long amount)
    {
        if (amount <= 0 || rewardLimitReached) return Task.CompletedTask;
        // 구매 등은 저장 실패 시 잔액을 복원하므로 그 사이의 보상을 별도로 보관합니다.
        if ((IsSaving && !savingCombatRewards) || HasPendingReward)
        {
            if (queuedGold > long.MaxValue - amount) StopAtGoldLimit();
            else queuedGold += amount;
        }
        else ApplyGold(amount);
        return Task.CompletedTask;
    }

    private void ApplyGold(long amount)
    {
        if (amount <= 0) return;
        if (data.wallet.gold > long.MaxValue - amount)
        {
            StopAtGoldLimit();
            return;
        }
        data.wallet.gold += amount;
        rewardJournal?.AddGold(amount);
        goldRevision++;
    }

    private void StopAtGoldLimit()
    {
        rewardLimitReached = true;
        GameplayBlockedChanged?.Invoke(true);
        StatusChanged?.Invoke("골드 한도에 도달해 전투를 중단했습니다.");
    }

    private void ApplyQueuedGold()
    {
        long gold = queuedGold;
        queuedGold = 0;
        ApplyGold(gold);
    }
    public CatGameSession(BackendUserData data, bool preview, Func<Task<bool>> saveData, CatGameCatalog catalog = null, CombatRewardJournal rewardJournal = null)
    {
        this.data = data ?? throw new ArgumentNullException(nameof(data));
        this.preview = preview;
        this.saveData = saveData ?? throw new ArgumentNullException(nameof(saveData));
        this.catalog = catalog;
        this.rewardJournal = preview ? null : rewardJournal;
        if (this.rewardJournal != null && this.rewardJournal.HasPending) goldRevision++;
    }

    public async Task LevelUpCharacterAsync(OwnedCharacter character)
    {
        if (MutationBlocked || character == null || !data.characters.Contains(character))
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
        if ((IsSaving && !savingCombatRewards) || HasPendingReward || amount <= 0) return false;
        var cat = data.characters.Find(x => x != null && x.characterId == data.loadout.characterId);
        if (cat == null) return false;
        CatProgression.AddExperience(cat, amount);
        rewardJournal?.AddExperience(cat.characterId, amount);
        experienceRevision++;
        return true;
    }

    public async Task ResetSkillsAsync(OwnedCharacter cat)
    {
        if (MutationBlocked || cat == null || !data.characters.Contains(cat) ||
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
        if (MutationBlocked || cat == null || !data.characters.Contains(cat) || !CatSkillTree.CanLearn(cat, node)) return;
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
        var results = await DrawContentBatchAsync(false, 1);
        return results == null ? null : results[0];
    }

    public async Task<ContentDrawResult> DrawCharacterAsync()
    {
        var results = await DrawContentBatchAsync(true, 1);
        return results == null ? null : results[0];
    }

    private bool MutationBlocked => IsSaving || HasPendingReward;

    public async Task<System.Collections.Generic.List<ContentDrawResult>> DrawContentBatchAsync(bool character, int count)
    {
        if (MutationBlocked || catalog == null || (count != 1 && count != 10)) return null;
        if (character)
        {
            var rules = catalog.characterDraw;
            var pool = catalog.CharacterDrawPool();
            if (rules == null || !rules.Validate(pool)) { StatusChanged?.Invoke("캐릭터 뽑기 설정을 확인해주세요."); return null; }
            return await DrawBatchAsync(pool, data.characters, count, rules.goldCost, rules.duplicateFragments,
                () => rules.Pick(pool, drawRandom.NextDouble(), drawRandom.NextDouble()),
                definition => definition.id, owned => owned.characterId,
                definition => new OwnedCharacter { characterId = definition.id },
                owned => owned.fragments, (owned, value) => owned.fragments = value,
                definition => new ContentDrawResult { character = true, id = definition.id, name = definition.displayName, rarity = definition.rarity });
        }
        else
        {
            var rules = catalog.relicDraw;
            var pool = catalog.RelicDrawPool();
            if (rules == null || !rules.Validate(pool)) { StatusChanged?.Invoke("유물 뽑기 설정을 확인해주세요."); return null; }
            return await DrawBatchAsync(pool, data.relics, count, rules.goldCost, rules.duplicateFragments,
                () => rules.Pick(pool, drawRandom.NextDouble(), drawRandom.NextDouble()),
                definition => definition.id, owned => owned.relicId,
                definition => new OwnedRelic { relicId = definition.id },
                owned => owned.fragments, (owned, value) => owned.fragments = value,
                definition => new ContentDrawResult { id = definition.id, name = definition.displayName, rarity = definition.rarity });
        }
    }

    private async Task<System.Collections.Generic.List<ContentDrawResult>> DrawBatchAsync<TDefinition, TOwned>(
        System.Collections.Generic.List<TDefinition> pool, System.Collections.Generic.List<TOwned> collection,
        int count, long cost, long fragmentCount, Func<TDefinition> pick,
        Func<TDefinition, string> definitionId, Func<TOwned, string> ownedId, Func<TDefinition, TOwned> create,
        Func<TOwned, long> fragments, Action<TOwned, long> setFragments, Func<TDefinition, ContentDrawResult> describe)
        where TDefinition : class where TOwned : class
    {
        if (cost > long.MaxValue / count || fragmentCount > long.MaxValue / count)
        { StatusChanged?.Invoke("뽑기 비용 또는 조각 설정이 너무 큽니다."); return null; }
        long totalCost = cost * count, maximumFragments = fragmentCount * count;
        if (data.wallet.gold < totalCost)
        { StatusChanged?.Invoke($"골드가 부족합니다. 필요 골드: {totalCost:N0}"); return null; }
        // 결과를 뽑기 전에 모든 후보의 조각 한도를 확인해 확률 편향을 막습니다.
        foreach (var definition in pool)
        {
            var existing = collection.Find(x => x != null && ownedId(x) == definitionId(definition));
            if (existing != null && fragments(existing) > long.MaxValue - maximumFragments)
            { StatusChanged?.Invoke("조각 보유 한도에 도달했습니다."); return null; }
        }
        var results = new System.Collections.Generic.List<ContentDrawResult>(count);
        var undo = new System.Collections.Generic.List<Action>(count);
        long previousGold = data.wallet.gold;
        Action rollback = () =>
        {
            data.wallet.gold = previousGold;
            for (int i = undo.Count - 1; i >= 0; i--) undo[i]();
        };
        try
        {
            for (int i = 0; i < count; i++)
            {
                var definition = pick();
                if (definition == null) throw new InvalidOperationException("뽑기 후보를 선택하지 못했습니다.");
                var owned = collection.Find(x => x != null && ownedId(x) == definitionId(definition));
                var result = describe(definition);
                result.duplicate = owned != null;
                if (owned != null)
                {
                    long before = fragments(owned);
                    undo.Add(() => setFragments(owned, before));
                    setFragments(owned, before + fragmentCount);
                    result.fragments = fragmentCount;
                }
                else
                {
                    owned = create(definition);
                    collection.Add(owned);
                    undo.Add(() => collection.Remove(owned));
                }
                results.Add(result);
            }
            data.wallet.gold -= totalCost;
        }
        catch { rollback(); throw; }
        // 10개 모두 한 번에 저장합니다. 실패하면 차감과 지급 전체를 복원합니다.
        return await SaveAsync(rollback) ? results : null;
    }
    private async Task PurchaseAsync(ShopOffer offer, bool alreadyOwned, Action grant, Action revoke)
    {
        if (MutationBlocked)
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
        if (MutationBlocked)
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

    public async Task<bool> SelectCharacterAsync(OwnedCharacter character)
    {
        if (MutationBlocked || character == null || !data.characters.Contains(character))
            return false;
        string previous = data.loadout.characterId;
        data.loadout.characterId = character.characterId;
        return await SaveAsync(() => data.loadout.characterId = previous);
    }

    public async Task ToggleRelicAsync(OwnedRelic relic)
    {
        if (MutationBlocked || relic == null || !data.relics.Contains(relic))
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

    public Func<string, Task<string>> NicknameUpdateRequested;
    public string LastProfileError { get; private set; }

    public async Task<bool> UpdateProfileAsync(string name, string portraitId, bool rename)
    {
        LastProfileError = null;
        name = name?.Trim();
        if (MutationBlocked || string.IsNullOrEmpty(name) || name.Length > 12 ||
            !data.characters.Exists(x => x != null && x.characterId == portraitId)) return false;
        foreach (char ch in name) if (char.IsControl(ch) || ch == '<' || ch == '>') return false;
        string oldName = data.progress.profileName, oldPortrait = data.progress.profileCharacterId;
        if (rename)
        {
            if (preview || NicknameUpdateRequested == null)
            {
                LastProfileError = "서버에 로그인한 후 이름을 변경해주세요.";
                return false;
            }
            IsSaving = true;
            try
            {
                BusyChanged?.Invoke(true);
                GameplayBlockedChanged?.Invoke(true);
                LastProfileError = await NicknameUpdateRequested(name);
            }
            catch (Exception) { LastProfileError = "이름 변경 결과를 확인하지 못했습니다. 다시 시도해주세요."; }
            finally
            {
                IsSaving = false;
                ApplyQueuedGold();
                BusyChanged?.Invoke(false);
                GameplayBlockedChanged?.Invoke(rewardLimitReached);
            }
            if (LastProfileError != null) return false;
        }
        data.progress.profileName = name;
        data.progress.profileCharacterId = portraitId;
        bool saved = await SaveAsync(() =>
        {
            if (!rename) data.progress.profileName = oldName;
            data.progress.profileCharacterId = oldPortrait;
        });
        if (!saved && rename) LastProfileError = "이름은 서버에서 변경됐지만 프로필 저장에 실패했습니다. 다시 로그인하면 변경된 이름이 표시됩니다.";
        return saved;
    }

    public async Task<bool> SaveAsync(Action rollback = null)
    {
        if (IsSaving)
            return false;
        long experienceSnapshot = experienceRevision, goldSnapshot = goldRevision;
        if (preview)
        {
            savedExperienceRevision = experienceSnapshot;
            savedGoldRevision = goldSnapshot;
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
                savedGoldRevision = goldSnapshot;
            }
            IsSaving = false;
            if (!HasPendingReward) ApplyQueuedGold();
            GameplayBlockedChanged?.Invoke(HasPendingReward || rewardLimitReached);
            BusyChanged?.Invoke(false);
        }
        StatusChanged?.Invoke(success ? "저장했습니다." : "저장 실패 · 전투가 대기 중이면 저장 버튼으로 재시도해주세요. 반복되면 다시 로그인해주세요.");
        return success;
    }
}
