using UnityEngine;

// 씬 진입점: 데이터 기능과 UI를 생성하고 이벤트만 연결합니다.
public class CatGameScene : MonoBehaviour
{
    // 기존 씬/Inspector에 저장된 참조를 유지합니다.
    [SerializeField] private CatGameCatalog catalog;
    [SerializeField] private Font uiFont;
    [SerializeField] private bool previewSampleContent = true;

    private bool ownCatalog;
    private CatGameSceneUI view;
    private CatGameSession session;
    private CatFieldController field;

    private void Start()
    {
        bool preview = BackendGameData.userData == null;
        var data = preview ? BackendUserData.CreateNew() : BackendGameData.userData;
        // 씬 단독 미리보기에서만 테스트 보유 목록을 구성합니다.
        if (preview && previewSampleContent)
        {
            data.characters.Add(new OwnedCharacter { characterId = "cat_spinner" });
            data.relics.Add(new OwnedRelic { relicId = "relic_claw" });
            data.relics.Add(new OwnedRelic { relicId = "relic_bell" });
        }

        if (catalog == null) catalog = Resources.Load<CatGameCatalog>("DefaultCatCatalog");
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<CatGameCatalog>();
            ownCatalog = true;
        }
        session = new CatGameSession(data, preview, () => BackendGameData.Instance.GameDataUpdateAsync());
        view = gameObject.AddComponent<CatGameSceneUI>();
        field = gameObject.AddComponent<CatFieldController>();
        field.Build(data, catalog);
        view.MovementModeChanged += field.SetAutomatic;
        view.StickChanged += field.SetStick;
        view.AdventureVisibilityChanged += field.SetVisible;
        view.Build(data, catalog, uiFont, preview, field.Output);
        view.SaveRequested += Save;
        view.CharacterSelected += SelectCharacter;
        view.RelicSelected += SelectRelic;
        session.BusyChanged += view.SetBusy;
        session.StatusChanged += view.ShowStatus;
        session.GameplayBlockedChanged += field.SetGameplayBlocked;
        field.Combat.GoldEarned += AwardGold;
        field.Combat.MessageChanged += view.ShowCombatMessage;
        field.Combat.RelicMessageChanged += view.ShowRelicMessage;
        field.Combat.HealthChanged += view.ShowPlayerHealth;
    }

    private async void Save() { await session.SaveAsync(); }
    private async void AwardGold(long amount)
    {
        var request = session.AwardGoldAsync(amount);
        view.RefreshWallet();
        await request;
    }
    private async void SelectCharacter(OwnedCharacter character) { await session.SelectCharacterAsync(character); }
    private async void SelectRelic(OwnedRelic relic)
    {
        await session.ToggleRelicAsync(relic);
        if (view != null) view.RefreshRelics();
    }

    private void OnDestroy()
    {
        // 전송 중인 저장은 완료까지 처리하고 UI 이벤트만 해제합니다.


        if (view != null)
        {
            view.SaveRequested -= Save;
            view.CharacterSelected -= SelectCharacter;
            view.RelicSelected -= SelectRelic;
            if (session != null)
            {
                session.BusyChanged -= view.SetBusy;
                session.StatusChanged -= view.ShowStatus;
                if (field != null) session.GameplayBlockedChanged -= field.SetGameplayBlocked;

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
        if (ownCatalog && catalog != null) Destroy(catalog);
    }
}
