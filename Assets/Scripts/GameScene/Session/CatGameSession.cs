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
    public bool IsUiBusy => IsSaving && !savingCombatRewards;
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
        PublishSavedState(success);
        if (!success) PublishMessage("전투 보상 저장 실패 · 보상을 보관하고 자동 재시도합니다.");
    }
    public SessionEventBus Events { get; } = new SessionEventBus(UnityEngine.Debug.LogException);
    private long publishedGold, publishedGems;
    private string publishedCharacter;
    private readonly System.Collections.Generic.Dictionary<string, (int level, int stars, long xp, long fragments, int skills)> publishedProgress =
        new System.Collections.Generic.Dictionary<string, (int, int, long, long, int)>();
    private void PublishBusy(bool value) => Events.Publish(new SessionBusyChanged(value));
    private void PublishBlock(bool value) => Events.Publish(new GameplayBlockChanged(value));
    private void PublishMessage(string text) => Events.Publish(new SessionMessage(text));
    private void PublishWallet()
    {
        if (publishedGold == data.wallet.gold && publishedGems == data.wallet.gems) return;
        publishedGold = data.wallet.gold; publishedGems = data.wallet.gems;
        Events.Publish(new WalletChanged(publishedGold, publishedGems));
    }
    private void PublishCharacter(OwnedCharacter cat, bool notify = true)
    {
        if (cat == null) return;
        var snapshot = (cat.level, cat.ascension, cat.experience, cat.fragments, CatSkillTree.Signature(cat));
        if (publishedProgress.TryGetValue(cat.characterId, out var previous) && previous.Equals(snapshot)) return;
        publishedProgress[cat.characterId] = snapshot;
        if (notify) Events.Publish(new CharacterProgressChanged(cat.characterId));
    }
    private void PublishSavedState(bool success)
    {
        // 롤백/보상 보류 처리가 모두 끝난 실제 데이터를 알립니다.
        PublishWallet();
        if (publishedCharacter != data.loadout.characterId)
        {
            publishedCharacter = data.loadout.characterId;
            Events.Publish(new SelectedCharacterChanged(publishedCharacter));
        }
        foreach (var cat in data.characters) PublishCharacter(cat);
        Events.Publish(new SessionSaveFinished(success, HasPendingReward));
    }

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
        PublishWallet();
    }

    private void StopAtGoldLimit()
    {
        rewardLimitReached = true;
        PublishBlock(true);
        PublishMessage("골드 한도에 도달해 전투를 중단했습니다.");
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
        publishedGold = data.wallet.gold; publishedGems = data.wallet.gems;
        publishedCharacter = data.loadout.characterId;
        foreach (var cat in data.characters) PublishCharacter(cat, false);
        if (this.rewardJournal != null && this.rewardJournal.HasPending) goldRevision++;
    }

    public async Task LevelUpCharacterAsync(OwnedCharacter character)
    {
        if (MutationBlocked || character == null || !data.characters.Contains(character))
            return;
        if (!CatProgression.TryStarCost(character.ascension, out long cost))
        {
            PublishMessage("최대 성급(5성)이거나 성급 정보가 올바르지 않습니다.");
            return;
        }
        if (character.fragments < cost)
        {
            PublishMessage($"해당 캐릭터 조각이 부족합니다. 필요 조각: {cost:N0}");
            return;
        }
        await ExecuteMutationAsync(change =>
        {
            change.Set(() => character.ascension, value => character.ascension = value, character.ascension + 1);
            change.Set(() => character.fragments, value => character.fragments = value, character.fragments - cost);
        });
    }

    public bool TryAwardExperience(long amount)
    {
        if ((IsSaving && !savingCombatRewards) || HasPendingReward || amount <= 0) return false;
        var cat = data.characters.Find(x => x != null && x.characterId == data.loadout.characterId);
        if (cat == null) return false;
        CatProgression.AddExperience(cat, amount);
        rewardJournal?.AddExperience(cat.characterId, amount);
        experienceRevision++;
        PublishCharacter(cat);
        return true;
    }

    public async Task ResetSkillsAsync(OwnedCharacter cat)
    {
        if (MutationBlocked || cat == null || !data.characters.Contains(cat) ||
            !CatSkillTree.HasLearnedSkills(cat) || data.wallet.gems < CatSkillTree.ResetCost) return;
        await ExecuteMutationAsync(change =>
        {
            change.Set(() => cat.skillRanks, value => cat.skillRanks = value, new int[CatSkillTree.Count]);
            change.Set(() => data.wallet.gems, value => data.wallet.gems = value, data.wallet.gems - CatSkillTree.ResetCost);
        });
    }

    public async Task LearnSkillAsync(OwnedCharacter cat, int node)
    {
        if (MutationBlocked || cat == null || !data.characters.Contains(cat) || !CatSkillTree.CanLearn(cat, node)) return;
        var ranks = new int[CatSkillTree.Count];
        for (int i = 0; i < ranks.Length; i++) ranks[i] = CatSkillTree.Rank(cat, i);
        ranks[node]++;
        await ExecuteMutationAsync(change => change.Set(() => cat.skillRanks, value => cat.skillRanks = value, ranks));
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
            if (rules == null || !rules.Validate(pool)) { PublishMessage("캐릭터 뽑기 설정을 확인해주세요."); return null; }
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
            if (rules == null || !rules.Validate(pool)) { PublishMessage("유물 뽑기 설정을 확인해주세요."); return null; }
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
        { PublishMessage("뽑기 비용 또는 조각 설정이 너무 큽니다."); return null; }
        long totalCost = cost * count, maximumFragments = fragmentCount * count;
        if (data.wallet.gold < totalCost)
        { PublishMessage($"골드가 부족합니다. 필요 골드: {totalCost:N0}"); return null; }
        // 결과를 뽑기 전에 모든 후보의 조각 한도를 확인해 확률 편향을 막습니다.
        foreach (var definition in pool)
        {
            var existing = collection.Find(x => x != null && ownedId(x) == definitionId(definition));
            if (existing != null && fragments(existing) > long.MaxValue - maximumFragments)
            { PublishMessage("조각 보유 한도에 도달했습니다."); return null; }
        }
        var results = new System.Collections.Generic.List<ContentDrawResult>(count);
        bool saved = await ExecuteMutationAsync(change =>
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
                    change.Set(() => fragments(owned), value => setFragments(owned, value), fragments(owned) + fragmentCount);
                    result.fragments = fragmentCount;
                }
                else
                {
                    owned = create(definition);
                    change.Apply(() => collection.Add(owned), () => collection.Remove(owned));
                }
                results.Add(result);
            }
            change.Set(() => data.wallet.gold, value => data.wallet.gold = value, data.wallet.gold - totalCost);
        });
        return saved ? results : null;
    }
    private async Task PurchaseAsync(ShopOffer offer, bool alreadyOwned, Action grant, Action revoke)
    {
        if (MutationBlocked)
            return;
        if (offer == null || !offer.CanSell)
        {
            PublishMessage("현재 판매하지 않는 상품입니다.");
            return;
        }
        if (alreadyOwned)
        {
            PublishMessage("이미 보유하고 있습니다.");
            return;
        }
        if (data.wallet.gold < offer.goldPrice)
        {
            PublishMessage($"골드가 부족합니다. 필요 골드: {offer.goldPrice:N0}");
            return;
        }
        await ExecuteMutationAsync(change =>
        {
            change.Set(() => data.wallet.gold, value => data.wallet.gold = value, data.wallet.gold - offer.goldPrice);
            change.Apply(grant, revoke);
        });
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
            PublishMessage("최대 레벨이거나 성장 설정이 없습니다.");
            return;
        }
        if (data.wallet.gold < cost)
        {
            PublishMessage($"골드가 부족합니다. 필요 골드: {cost:N0}");
            return;
        }
        await ExecuteMutationAsync(change =>
        {
            change.Set(() => data.wallet.gold, value => data.wallet.gold = value, data.wallet.gold - cost);
            change.Apply(() => setLevel(level + 1), () => setLevel(level));
        });
    }

    public async Task<bool> SelectCharacterAsync(OwnedCharacter character)
    {
        if (MutationBlocked || character == null || !data.characters.Contains(character))
            return false;
        return await ExecuteMutationAsync(change =>
            change.Set(() => data.loadout.characterId, value => data.loadout.characterId = value, character.characterId));
    }

    public async Task ToggleRelicAsync(OwnedRelic relic)
    {
        if (MutationBlocked || relic == null || !data.relics.Contains(relic))
            return;
        var next = new System.Collections.Generic.List<string>(data.loadout.relicIds);
        if (next.Contains(relic.relicId)) next.RemoveAll(id => id == relic.relicId);
        else
        {
            if (next.Count >= 3) { PublishMessage("유물은 최대 3개까지 장착할 수 있습니다."); return; }
            next.Add(relic.relicId);
        }
        await ExecuteMutationAsync(change => change.Set(() => data.loadout.relicIds, value => data.loadout.relicIds = value, next));
    }

    private async Task<bool> ExecuteMutationAsync(Action<ReversibleMutation> apply)
    {
        if (MutationBlocked) return false;
        using (var change = new ReversibleMutation())
        {
            apply(change);
            bool saved = await SaveAsync(change.Rollback);
            if (saved) change.Commit();
            return saved;
        }
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
                PublishBusy(true);
                PublishBlock(true);
                LastProfileError = await NicknameUpdateRequested(name);
            }
            catch (Exception) { LastProfileError = "이름 변경 결과를 확인하지 못했습니다. 다시 시도해주세요."; }
            finally
            {
                IsSaving = false;
                ApplyQueuedGold();
                PublishBusy(false);
                PublishBlock(rewardLimitReached);
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
            PublishBlock(rewardLimitReached);
            PublishSavedState(true);
            PublishMessage("미리보기 변경입니다. 서버에는 저장하지 않습니다.");
            return true;
        }
        IsSaving = true;
        PublishBlock(true);
        bool success = false;
        try
        {
            PublishBusy(true);
            PublishMessage("저장 중…");
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
            PublishBlock(HasPendingReward || rewardLimitReached);
            PublishBusy(false);
        }
        PublishSavedState(success);
        PublishMessage(success ? "저장했습니다." : "저장 실패 · 전투가 대기 중이면 저장 버튼으로 재시도해주세요. 반복되면 다시 로그인해주세요.");
        return success;
    }
}
