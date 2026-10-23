using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public partial class CatGameSceneUI
{
    public event Action<bool> RaidActiveChanged;
    private RectTransform adventureMenu;
    private readonly List<OwnedCharacter> raidTeam = new List<OwnedCharacter>();

    private bool raidActive;
    private int selectedRaid;
    private Text raidHealth;





    private void Navigate(string title)
    {
        if (raidActive) { ShowStatus("레이드를 종료한 뒤 이동해주세요."); return; }
        if (title != "모험") { Show(title); return; }
        if (adventureMenu != null) { CloseAdventureMenu(); return; }
        adventureMenu = Panel(safeArea, "Adventure Menu", new Vector2(.045f, .135f), new Vector2(.27f, .428f), new Color(.16f, .20f, .17f, .8f));
        adventureMenu.SetAsLastSibling();
        AdventureIconButton("전투", 0, .68f, .97f, () => Show("모험"));
        AdventureIconButton("던전", 2, .355f, .645f, () => Show("던전"));
        AdventureIconButton("레이드", 1, .03f, .32f, () => Show("레이드"));
    }

    private void CloseAdventureMenu()
    {
        if (adventureMenu != null) RemoveUI(adventureMenu.gameObject);
        adventureMenu = null;
    }

    private void BossImage(Transform parent, int index)
    {
        string[] names = { "DustBunnyMonster", "CheeseMouseMonster", "YarnSlimeMonster" };
        var texture = Resources.Load<Texture2D>("RaidArt/" + RaidBattle.ArtworkNames[index]);
        bool dedicatedArtwork = texture != null;
        if (texture == null) texture = Resources.Load<Texture2D>("MonsterArt/" + names[index]);
        if (texture == null) return;
        var rarityColor = CharacterRarityColor(RaidBattle.Rarities[index]);
        var portrait = Panel(parent, "Boss Portrait Frame", new Vector2(.65f, .08f), new Vector2(.97f, .92f), rarityColor);
        portrait.GetComponent<Image>().raycastTarget = false;
        var inset = Panel(portrait, "Portrait Background", Vector2.zero, Vector2.one,
            Color.Lerp(new Color(.10f, .14f, .14f), rarityColor, .20f));
        inset.offsetMin = new Vector2(3, 3);
        inset.offsetMax = new Vector2(-3, -3);
        inset.GetComponent<Image>().raycastTarget = false;
        var image = Rect(inset, "Boss Portrait", Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
        image.texture = texture;
        image.uvRect = dedicatedArtwork ? new Rect(0, 0, 1, 1) : new Rect(0, .5f, .25f, .5f);
        var aspect = image.gameObject.AddComponent<AspectRatioFitter>();
        aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspect.aspectRatio = dedicatedArtwork ? (float)texture.width / texture.height : (float)texture.width / (texture.height * 2);
        image.raycastTarget = false;
    }

    private void RaidList()
    {
        dungeonMode = false;
        BuildRaidListBackground();
        Label(page, $"레이드 · 계정 Lv.{data.progress.accountLevel}", 30,
            new Vector2(.04f, .90f), new Vector2(.96f, .99f)).color = new Color(1, .93f, .73f);
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            bool available = RaidBattle.CanEnter(i, data.progress.accountLevel);
            var card = Panel(page, "Raid " + i, new Vector2(.04f, .69f - i * .22f), new Vector2(.96f, .87f - i * .22f),
                available ? new Color(.22f, .30f, .26f) : new Color(.35f, .35f, .38f));
            var rarityColor = i < 3 ? CharacterRarityColor(RaidBattle.Rarities[i]) : Color.gray;
            var stripe = Panel(card, "Rarity Accent", Vector2.zero, new Vector2(.012f, 1), rarityColor);
            stripe.GetComponent<Image>().raycastTarget = false;
            if (i < 3) BossImage(card, i);
            var title = Label(card, i < 3 ? RaidBattle.Names[i] : "추후 공개", 25,
                new Vector2(.05f, .58f), new Vector2(.61f, .88f));
            title.alignment = TextAnchor.MiddleLeft;
            title.color = new Color(1, .96f, .85f);
            string requirement = i == 3 ? "아직 열리지 않은 레이드" :
                RaidBattle.IgnoreLevelRequirementForTesting ? "테스트 · 레벨 제한 해제" : $"필요 계정 레벨 {RaidBattle.RequiredLevels[i]}";
            var detail = Label(card, requirement, 18, new Vector2(.05f, .34f), new Vector2(.61f, .56f));
            detail.alignment = TextAnchor.MiddleLeft;
            detail.color = new Color(.82f, .87f, .82f);
            var action = Label(card, i == 3 || !available ? "잠금" : "4마리 편성  ›", 20,
                new Vector2(.05f, .10f), new Vector2(.61f, .32f));
            action.alignment = TextAnchor.MiddleLeft;
            action.color = new Color(1, .83f, .48f);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.interactable = available;
            button.onClick.AddListener(() => { if (!saving) { selectedRaid = index; raidTeam.Clear(); RaidFormation(); } });
        }
    }

    private void BuildRaidListBackground()
    {
        // 목록의 첫 자식으로 배치해 카드와 버튼 뒤에만 표시합니다.
        var frame = Panel(page, "Raid Window Frame", Vector2.zero, Vector2.one,
            new Color(.48f, .39f, .25f));
        frame.GetComponent<Image>().raycastTarget = false;
        var background = Panel(frame, "Raid Window Background", Vector2.zero, Vector2.one,
            new Color(.10f, .16f, .15f));
        background.offsetMin = new Vector2(4, 4);
        background.offsetMax = new Vector2(-4, -4);
        background.GetComponent<Image>().raycastTarget = false;
        // 낮은 대비의 블록 무늬로 기존 픽셀 UI와 어울리는 질감을 만듭니다.
        for (int row = 0; row < 12; row++)
        {
            float y = row / 12f;
            for (int column = 0; column < 4; column++)
            {
                float x = column * .25f;
                var tile = Panel(background, "Background Tile", new Vector2(x + .008f, y + .004f),
                    new Vector2(x + .242f, y + .078f),
                    (row + column) % 2 == 0 ? new Color(.12f, .18f, .17f) : new Color(.11f, .17f, .16f));
                tile.GetComponent<Image>().raycastTarget = false;
            }
        }
        var header = Panel(background, "Raid Header Background", new Vector2(.018f, .898f),
            new Vector2(.982f, .988f), new Color(.18f, .24f, .21f));
        header.GetComponent<Image>().raycastTarget = false;
        var divider = Panel(background, "Raid Header Divider", new Vector2(.04f, .89f),
            new Vector2(.96f, .893f), new Color(.64f, .52f, .31f));
        divider.GetComponent<Image>().raycastTarget = false;
    }

    private readonly OwnedCharacter[] raidSlots = new OwnedCharacter[4];

    private void RaidFormation()
    {
        ClearPage();
        Button(page, "목록", new Vector2(0, .92f), new Vector2(.23f, 1), () => Show("레이드"));
        Label(page, EncounterTitle, 28, new Vector2(.25f, .92f), Vector2.one);
        var boss = Panel(page, "Raid Boss Preview", new Vector2(.06f, .75f), new Vector2(.94f, .90f), new Color(.20f, .25f, .23f));
        BossImage(boss, selectedRaid);
        Label(boss, "보스 방향 ↑\n앞줄이 보스를 향합니다", 20, new Vector2(.02f, .05f), new Vector2(.53f, .95f)).color = Color.white;
        for (int i = 0; i < raidSlots.Length; i++)
        {
            int slot = i;
            int row = i / 2;
            float top = .69f - row * .29f;
            float left = .12f + (i % 2) * .42f;
            var owned = raidSlots[i];
            if (owned != null && !data.characters.Contains(owned)) raidSlots[i] = owned = null;
            var definition = owned == null ? null : catalog.FindCharacter(owned.characterId);
            var card = Panel(page, "Raid Slot " + i, new Vector2(left, top - .23f), new Vector2(left + .34f, top),
                owned == null ? cream : CharacterRarityColor(definition == null ? ItemRarity.Common : definition.rarity));
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.onClick.AddListener(() =>
            {
                if (!saving) OpenTeamCatPicker(raidSlots, slot, slot < 2 ? "앞줄" : "뒷줄", RaidFormation);
            });
            if (owned != null)
            {
                CharacterPortrait(Rect(card, "Portrait", new Vector2(0, .22f), Vector2.one), definition);
                Label(card, $"Lv.{owned.level} · 교체", 18, Vector2.zero, new Vector2(1, .22f));
            }
            else Label(card, "+\n고양이 선택", 23, Vector2.zero, Vector2.one);
        }
        Label(page, "앞줄 · 2마리", 22, new Vector2(0, .69f), new Vector2(1, .74f));
        Label(page, "뒷줄 · 2마리", 22, new Vector2(0, .40f), new Vector2(1, .45f));
        var enter = Button(page, "레이드 입장", new Vector2(.18f, .02f), new Vector2(.82f, .11f), () =>
        {
            raidTeam.Clear();
            raidTeam.AddRange(raidSlots); // 앞줄 0,1 / 뒷줄 2,3 순서를 그대로 전달합니다.
            StartRaid();
        });
        var selectedIds = new HashSet<string>();
        foreach (var cat in raidSlots) if (cat != null) selectedIds.Add(cat.characterId);
        enter.interactable = selectedIds.Count == 4 && RaidBattle.CanEnter(selectedRaid, data.progress.accountLevel);
    }
    public event Action<int, IList<OwnedCharacter>> RaidRequested;
    public CatFieldController RaidField { get; set; }

    private void StartRaid()
    {
        try
        {
            if (dungeonMode) { DungeonRequested?.Invoke(selectedDungeon, new List<OwnedCharacter>(raidTeam)); return; }
            else RaidRequested?.Invoke(selectedRaid, new List<OwnedCharacter>(raidTeam));
            if (RaidField == null || !RaidField.RaidRunning) return;
        }
        catch (ArgumentException error) { ShowStatus(error.Message); return; }
        raidActive = true;
        ClearPage();
        var image = Rect(page, "Raid Field View", Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
        image.texture = fieldTexture;
        image.raycastTarget = false;
        image.gameObject.AddComponent<FieldViewportSize>().Target = fieldTexture;
        var header = Panel(page, "Raid HUD", new Vector2(0, .85f), Vector2.one, new Color(0, 0, 0, .65f));
        raidHealth = Label(header, "", 26, Vector2.zero, Vector2.one);
        raidHealth.color = Color.white;
        if (!dungeonMode)
        {
            var pad = Panel(page, "Raid Movement Joystick", new Vector2(.03f, .12f), new Vector2(.30f, .32f), new Color(.12f, .16f, .20f, .55f));
            var handle = Panel(pad, "Handle", new Vector2(.35f, .35f), new Vector2(.65f, .65f), new Color(1, 1, 1, .85f));
            handle.GetComponent<Image>().raycastTarget = false;
            joystick = pad.gameObject.AddComponent<FieldJoystick>();
            joystick.Handle = handle;
            joystick.InputChanged = value => StickChanged?.Invoke(value);
            Label(page, "수동 이동 고정 · 4마리 함께 조작\nWASD / 방향키 / 조이스틱 · 공격은 자동", 18,
                new Vector2(.32f, .13f), new Vector2(.98f, .25f)).color = Color.white;
        }
        Button(page, $"{EncounterTab} 포기", new Vector2(.3f, .01f), new Vector2(.7f, .09f), ExitRaid);
        ShowStatus(dungeonMode ? "몰려오는 몬스터를 막고 3개 웨이브를 모두 처치하세요." : "직접 이동해 위험 범위를 피하세요. 공격은 자동으로 진행됩니다.");
    }

    private void UpdateRaid()
    {
        if (!raidActive || RaidField == null || !RaidField.RaidRunning || raidHealth == null) return;
        var boss = RaidField.RaidBoss;
        raidHealth.text = $"{EncounterTitle} · 남은 시간 {RaidField.RaidSeconds:0.0}초\nHP {boss.Health.Current:0}/{boss.Health.Maximum:0}\n{RaidField.RaidSkillMessage}";
    }

    public void ShowRaidResult(bool won)
    {
        CloseDefenseScreen();
        ClearPage();
        Label(page, won ? $"{EncounterTab} 승리!" : dungeonMode ? "던전 실패 · 기지가 무너졌어요" : $"{EncounterTab} 실패 · 시간 초과 또는 전멸", 32, new Vector2(0, .55f), new Vector2(1, .80f));
        Label(page, dungeonMode ? "클리어 진행은 저장됩니다. 재화 보상은 추후 추가됩니다." : "레이드 보상은 추후 추가됩니다.", 24, new Vector2(0, .4f), new Vector2(1, .55f));
        Button(page, $"{EncounterTab} 목록으로", new Vector2(.15f, .2f), new Vector2(.85f, .32f), ExitRaid);
    }

    private void ExitRaid()
    {
        CloseDefenseScreen();
        raidActive = false;
        RaidActiveChanged?.Invoke(false);
        Show(EncounterTab);
    }
}
