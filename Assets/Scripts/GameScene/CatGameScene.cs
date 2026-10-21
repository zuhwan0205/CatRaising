using UnityEngine;

// 씬 진입점: 데이터 기능과 UI를 생성하고 이벤트만 연결합니다.
public class CatGameScene : MonoBehaviour
{
    // 기존 씬/Inspector에 저장된 참조를 유지합니다.
    [SerializeField] private CatGameCatalog catalog;
    [SerializeField] private Font uiFont;
    [SerializeField] private bool previewSampleContent = true;

    private bool ownCatalog;
    [SerializeField] private GameObject sceneMap;
    [SerializeField] private Camera editorMapCamera;

    public void PrepareSceneUI(GameObject map, Camera previewCamera)
    {
        sceneMap = map;
        editorMapCamera = previewCamera;
        var definitions = catalog != null ? catalog : Resources.Load<CatGameCatalog>("DefaultCatCatalog");
        bool temporary = definitions == null;
        if (temporary) definitions = ScriptableObject.CreateInstance<CatGameCatalog>();
        var ui = GetComponent<CatGameSceneUI>();
        if (ui == null) ui = gameObject.AddComponent<CatGameSceneUI>();
        ui.Build(BackendUserData.CreateNew(), definitions, uiFont, true, previewCamera != null ? previewCamera.targetTexture : null);
        if (temporary) DestroyImmediate(definitions);
    }
    private CatGameSceneUI view;
    private CatGameSession session;
    private CatFieldController field;
    private bool watchingAd;
    private bool inRaid;
    private bool sessionBlocked;

    private void SetSessionBlocked(bool blocked)
    {
        sessionBlocked = blocked;
        if (field != null) field.SetGameplayBlocked(blocked || inRaid || watchingAd);
    }

    private void SetRaidActive(bool active)
    {
        if (!active && field != null) field.EndRaid();
        inRaid = active;
        if (field != null) field.SetGameplayBlocked(active || sessionBlocked || watchingAd);
    }
    private void BeginFieldRaid(int index, System.Collections.Generic.IList<OwnedCharacter> team)
    {
        dungeonStage = 0;
        SetRaidActive(true);
        try { field.BeginRaid(index, team, activeData, catalog); }
        catch { SetRaidActive(false); throw; }
    }
    private BackendUserData activeData;
    private int dungeonStage;
    private void BeginFieldDungeon(int stage, System.Collections.Generic.IList<OwnedCharacter> team)
    {
        if (session.IsSaving || session.HasPendingReward)
            throw new System.ArgumentException("진행 저장을 먼저 완료해주세요. 상점에서 저장을 재시도할 수 있습니다.");
        SetRaidActive(true);
        try
        {
            field.BeginDungeon(stage, team, activeData, catalog);
            dungeonStage = stage;
        }
        catch { SetRaidActive(false); throw; }
    }

    private async void OnEncounterFinished(bool won)
    {
        if (won && dungeonStage > 0) await session.CompleteDungeonAsync(dungeonStage);
        if (view == null) return;
        view.ShowRaidResult(won);
        view.PendingAdSave = session.HasPendingReward;
        if (session.HasPendingReward) view.ShowStatus("진행 저장 실패 · 상점의 보상 저장 재시도를 눌러주세요.");
    }

    private void Start()
    {
        bool preview = BackendGameData.userData == null;
        var data = preview ? BackendUserData.CreateNew() : BackendGameData.userData;
        adWallet = data.wallet;
        activeData = data;
        // 씬 단독 미리보기에서만 테스트 보유 목록을 구성합니다.
        if (preview && previewSampleContent)
        {
            data.characters.Add(new OwnedCharacter { characterId = "cat_spinner" });
            data.relics.Add(new OwnedRelic { relicId = "relic_claw" });
            data.relics.Add(new OwnedRelic { relicId = "relic_bell" });
        }

        if (catalog == null)
            catalog = Resources.Load<CatGameCatalog>("DefaultCatCatalog");
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CatGameCatalog>();
            ownCatalog = true;
        }
        session = new CatGameSession(data, preview, () => BackendGameData.Instance.GameDataUpdateAsync(), catalog, BackendGameData.Instance.RewardJournal);
        if (editorMapCamera != null) editorMapCamera.gameObject.SetActive(false);
        view = GetComponent<CatGameSceneUI>();
        if (view == null) view = gameObject.AddComponent<CatGameSceneUI>();
        field = gameObject.AddComponent<CatFieldController>();
        field.Build(data, catalog, sceneMap);
        field.ExperienceCrystals.Collect = CollectExperience;
        view.MovementModeChanged += field.SetAutomatic;
        view.StickChanged += field.SetStick;
        view.AdventureVisibilityChanged += field.SetVisible;
        view.Build(data, catalog, uiFont, preview, field.Output);
        view.ProfileSaveRequested = session.UpdateProfileAsync;
        session.NicknameUpdateRequested = BackendLogin.Instance.UpdateNicknameAsync;
        view.ProfileSaveError = () => session.LastProfileError;
        view.CharacterSelected += SelectCharacter;
        view.SkillRequested += LearnSkill;
        view.SkillResetRequested += ResetSkills;
        view.RelicSelected += SelectRelic;
        view.CharacterLevelUpRequested += LevelUpCharacter;
        view.RelicLevelUpRequested += LevelUpRelic;
        view.CharacterPurchaseRequested += BuyCharacter;
        view.RelicDrawRequested += DrawRelic;
        view.CharacterDrawRequested += DrawCharacter;
        view.BatchDrawRequested += DrawContent;
        view.CrystalAdRequested += WatchCrystalAd;
        session.BusyChanged += view.SetBusy;
        session.StatusChanged += view.ShowStatus;
        session.GameplayBlockedChanged += SetSessionBlocked;
        view.RaidActiveChanged += SetRaidActive;
        view.RaidRequested += BeginFieldRaid;
        view.RaidField = field;
        field.RaidFinished += OnEncounterFinished;
        view.DungeonRequested += BeginFieldDungeon;
        field.Combat.GoldEarned += AwardGold;
        field.Combat.MessageChanged += view.ShowCombatMessage;
        field.Combat.RelicMessageChanged += view.ShowRelicMessage;
        field.Combat.HealthChanged += view.ShowPlayerHealth;
        StartCoroutine(SaveCombatRewardsPeriodically());
    }

    private bool CollectExperience(long amount)
    {
        if (!session.TryAwardExperience(amount)) return false;
        RefreshExperienceDisplay();
        return true;
    }

    private void RefreshExperienceDisplay()
    {
        view.RefreshWallet();
        var cat = activeData.characters.Find(x => x.characterId == activeData.loadout.characterId);
        view.ShowCombatMessage($"경험치 획득 · Lv.{cat.level} · EXP {cat.experience}/{CatProgression.RequiredExperience(cat.level)}");
    }

    private System.Collections.IEnumerator SaveCombatRewardsPeriodically()
    {
        var wait = new WaitForSecondsRealtime(1);
        int ticks = 0;
        System.Threading.Tasks.Task pendingSave = null;
        while (true)
        {
            yield return wait;
            CheckpointCombatRewards();
            if (pendingSave != null && pendingSave.IsCompleted)
            {
                if (pendingSave.IsFaulted) Debug.LogException(pendingSave.Exception);
                pendingSave = null;
                if (view != null) view.RefreshWallet();
            }
            if (++ticks < 5) continue;
            ticks = 0;
            if (pendingSave == null) pendingSave = session.SaveCombatRewardsAsync();
        }
    }

    private void CheckpointCombatRewards()
    {
        try { session?.CheckpointRewards(); }
        catch (System.Exception exception) { Debug.LogException(exception); }
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) SaveBeforeLeaving();
    }

    private void OnApplicationFocus(bool focused)
    {
        if (!focused) SaveBeforeLeaving();
    }

    private void OnApplicationQuit() => SaveBeforeLeaving();

    private async void SaveBeforeLeaving()
    {
        CheckpointCombatRewards();
        if (session == null) return;
        try { await session.SaveCombatRewardsAsync(); }
        catch (System.Exception exception) { Debug.LogException(exception); }
    }

    private async void AwardGold(long amount)
    {
        var request = session.AwardGoldAsync(amount);
        view.RefreshWallet();
        await request;
        if(view != null && !session.HasPendingReward) view.PlayGoldReward();
    }
    private async void WatchCrystalAd()
    {
        if (watchingAd || session.IsSaving) return;
        watchingAd = true;
        view.SetBusy(true);
        field.SetGameplayBlocked(true);
        try
        {
            if (session.HasPendingReward)
                await session.SaveAsync();
            else if (CrystalAdRewards.Remaining(adWallet) > 0)
            {
                view.ShowStatus("테스트 광고를 불러오는 중…");
                if (await CrystalRewardedAd.ShowAsync())
                {
                    await session.GrantCrystalAdRewardAsync();
                    if (view != null && !session.HasPendingReward)
                        view.ShowStatus("크리스탈 10개를 받았습니다!");
                }
                else if (view != null)
                    view.ShowStatus("시청을 완료하지 않아 보상이 지급되지 않았습니다.");
            }
        }
        catch (System.Exception exception)
        {
            if (view != null) view.ShowStatus(exception.Message);
        }
        finally
        {
            watchingAd = false;
            if (view != null)
            {
                view.PendingAdSave = session.HasPendingReward;
                view.RefreshWallet();
                view.SetBusy(false);
                if (session.HasPendingReward) view.ShowStatus("저장 실패 · 상점의 보상 저장 재시도를 눌러주세요.");
            }
            if (field != null) field.SetGameplayBlocked(session.HasPendingReward);
        }
    }
    private PlayerWallet adWallet;
    private async void LearnSkill(OwnedCharacter cat, int node)
    {
        await session.LearnSkillAsync(cat, node);
        if (view != null) view.RefreshSkillAfterLearning(cat, node);
    }
    private async void ResetSkills(OwnedCharacter cat)
    {
        await session.ResetSkillsAsync(cat);
        if (view == null) return;
        view.RefreshWallet();
        view.ShowSkillTree(cat);
    }
    private async void SelectCharacter(OwnedCharacter character)
    {
        await session.SelectCharacterAsync(character);
    }
    private async void BuyCharacter(string id)
    {
        await session.BuyCharacterAsync(id);
        if (view != null)
            view.RefreshShop();
    }
    private void DrawRelic()
    {
        DrawContent(false);
    }
    private void DrawCharacter()
    {
        DrawContent(true);
    }
    private async void DrawContent(bool character, int count = 1)
    {
        if (view == null || !view.BeginRelicDraw(character, count))
            return;
        System.Collections.Generic.List<ContentDrawResult> results = null;
        try
        {
            results = await session.DrawContentBatchAsync(character, count);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
            if (view != null)
                view.ShowStatus("뽑기 처리 중 오류가 발생했습니다. 저장 상태를 확인해주세요.");
        }
        finally { if (view != null) view.CompleteRelicDrawBatch(results); }
    }
    private async void LevelUpCharacter(OwnedCharacter character)
    {
        int previousStars = character.ascension;
        await session.LevelUpCharacterAsync(character);
        if (view != null)
        {
            view.RefreshWallet();
            view.RefreshCharacterDetail(character);
            if(character.ascension > previousStars) view.PlayCharacterUpgrade();
        }
    }
    private async void LevelUpRelic(OwnedRelic relic)
    {
        await session.LevelUpRelicAsync(relic);
        if (view != null)
        {
            view.RefreshWallet();
            view.RefreshRelicDetail(relic);
        }
    }
    private async void SelectRelic(OwnedRelic relic)
    {
        await session.ToggleRelicAsync(relic);
        if (view != null)
            view.RefreshRelics();
    }

    private void OnDestroy()
    {
        // 전송 중인 저장은 완료까지 처리하고 UI 이벤트만 해제합니다.


        if (view != null)
        {
            view.CharacterSelected -= SelectCharacter;
            view.SkillRequested -= LearnSkill;
            view.SkillResetRequested -= ResetSkills;
            view.RelicSelected -= SelectRelic;
            view.CharacterLevelUpRequested -= LevelUpCharacter;
            view.RelicLevelUpRequested -= LevelUpRelic;
            view.CharacterPurchaseRequested -= BuyCharacter;
            view.RelicDrawRequested -= DrawRelic;
            view.CharacterDrawRequested -= DrawCharacter;
            view.BatchDrawRequested -= DrawContent;
            view.CrystalAdRequested -= WatchCrystalAd;
            view.RaidActiveChanged -= SetRaidActive;
            view.RaidRequested -= BeginFieldRaid;
            view.DungeonRequested -= BeginFieldDungeon;
            if (field != null) field.RaidFinished -= OnEncounterFinished;
            if (session != null)
            {
                session.BusyChanged -= view.SetBusy;
                session.StatusChanged -= view.ShowStatus;
                if (field != null)
                    session.GameplayBlockedChanged -= SetSessionBlocked;

            }
        }
        if (view != null && field != null)
        {
            view.MovementModeChanged -= field.SetAutomatic;
            view.StickChanged -= field.SetStick;
            view.AdventureVisibilityChanged -= field.SetVisible;
            field.Combat.GoldEarned -= AwardGold;
            field.Combat.MessageChanged -= view.ShowCombatMessage;
            field.Combat.RelicMessageChanged -= view.ShowRelicMessage;
            field.Combat.HealthChanged -= view.ShowPlayerHealth;
        }
        if (ownCatalog && catalog != null)
            Destroy(catalog);
    }
}
