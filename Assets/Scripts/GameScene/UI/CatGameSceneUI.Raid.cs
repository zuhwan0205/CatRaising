using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public partial class CatGameSceneUI
{
    public event Action EncounterExitRequested;
    private bool standaloneEncounter;
    private RectTransform sceneTransition;

    public void SetSceneTransition(bool active)
    {
        if (sceneTransition != null) RemoveUI(sceneTransition.gameObject);
        sceneTransition = null;
        if (!active) return;
        sceneTransition = Panel(safeArea.parent, "Scene Transition", Vector2.zero, Vector2.one, new Color(.12f, .16f, .13f, .94f));
        Label(sceneTransition, "잠시만 기다려주세요…", 28, new Vector2(.05f, .4f), new Vector2(.95f, .6f)).color = cream;
        sceneTransition.SetAsLastSibling();
        if (joystick != null) joystick.ResetInput();
    }
    private RectTransform adventureMenu;
    private readonly List<OwnedCharacter> raidTeam = new List<OwnedCharacter>();

    private bool raidActive;
    private int selectedRaid;
    private Text raidHealth;





    private void Navigate(string title)
    {
        if (raidActive) { ShowStatus("레이드를 종료한 뒤 이동해주세요."); return; }
        if (detailOverlay == null && drawOverlay == null && tabs != null)
        {
            int index = title == "모험" ? 0 : title == "상점" ? 1 : title == "캐릭터" ? 2 : title == "유물" ? 3 : -1;
            if (index >= 0 && index < tabs.Length && tabs[index] != null)
                UISelectionMotion.Play(tabs[index].transform.Find("Navigation Icon"), false);
        }
        if (title != "모험") { Show(title); return; }
        if (adventureMenu != null) { CloseAdventureMenu(); return; }
        adventureMenu = Panel(safeArea, "Adventure Menu", new Vector2(.26f, .14f), new Vector2(.74f, .235f), new Color(.16f, .20f, .17f, .8f));
        adventureMenu.SetAsLastSibling();
        AdventureIconButton("전투", 0, .02f, .32f, () => Show("모험"));
        AdventureIconButton("던전", 2, .35f, .65f, () => Show("던전"));
        AdventureIconButton("레이드", 1, .68f, .98f, () => Show("레이드"));
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
        var background = Panel(page, "Formation Background", Vector2.zero, Vector2.one, new Color(.12f, .18f, .17f, .97f));
        RoundCollectionPanel(background);
        Button(page, "목록", new Vector2(0, .92f), new Vector2(.23f, 1), () => Show("레이드"));
        Label(page, EncounterTitle, 28, new Vector2(.25f, .92f), Vector2.one).color = cream;
        var boss = Panel(page, "Raid Boss Preview", new Vector2(.06f, .75f), new Vector2(.94f, .90f), new Color(.20f, .25f, .23f));
        BossImage(boss, selectedRaid);
        var bossPortrait = boss.Find("Boss Portrait Frame") as RectTransform;
        if (bossPortrait != null)
        {
            bossPortrait.anchorMin = new Vector2(.34f, .08f);
            bossPortrait.anchorMax = new Vector2(.66f, .92f);
        }
        FormationRow(true);
        FormationRow(false);
        for (int i = 0; i < raidSlots.Length; i++)
        {
            int slot = i;
            int row = i / 2;
            float top = .68f - row * .29f;
            float left = .12f + (i % 2) * .42f;
            var owned = raidSlots[i];
            if (owned != null && !data.characters.Contains(owned)) raidSlots[i] = owned = null;
            var definition = owned == null ? null : catalog.FindCharacter(owned.characterId);
            var roleColor = row == 0 ? new Color(.94f, .64f, .32f) : new Color(.40f, .71f, .92f);
            var card = CollectionFrame(page, "Raid Slot " + i,
                owned == null ? roleColor : CharacterRarityColor(definition == null ? ItemRarity.Common : definition.rarity));
            card.anchorMin = new Vector2(left, top - .205f);
            card.anchorMax = new Vector2(left + .34f, top);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.onClick.AddListener(() =>
            {
                if (!saving) OpenTeamCatPicker(raidSlots, slot, "", RaidFormation, compact: true);
            });
            if (owned != null)
            {
                CharacterPortrait(Rect(card, "Portrait", new Vector2(.08f, .25f), new Vector2(.92f, .95f)), definition);
                CollectionBadge(card, $"Lv.{owned.level}", new Vector2(.05f, .04f), new Vector2(.95f, .25f), roleColor);
            }
            else
            {
                Label(card, "+", 46, Vector2.zero, Vector2.one).color = roleColor;
            }
        }
        var enter = Button(page, "레이드 입장", new Vector2(.18f, .02f), new Vector2(.82f, .11f), () =>
        {
            raidTeam.Clear();
            raidTeam.AddRange(raidSlots); // 전위 0,1 / 후위 2,3 순서를 그대로 전달합니다.
            StartRaid();
        });
        var selectedIds = new HashSet<string>();
        foreach (var cat in raidSlots) if (cat != null) selectedIds.Add(cat.characterId);
        enter.interactable = selectedIds.Count == 4 && RaidBattle.CanEnter(selectedRaid, data.progress.accountLevel);
    }

    private void FormationRow(bool front)
    {
        float offset = front ? 0 : -.29f;
        Color color = front ? new Color(.94f, .64f, .32f) : new Color(.40f, .71f, .92f);
        var group = Panel(page, front ? "Vanguard" : "Rearguard", new Vector2(.045f, .46f + offset), new Vector2(.955f, .745f + offset), Color.Lerp(new Color(.10f, .13f, .17f), color, .20f));
        RoundCollectionPanel(group);
        var heading = Panel(page, "Formation Role", new Vector2(.07f, .685f + offset), new Vector2(.93f, .735f + offset), Color.Lerp(Color.black, color, .34f));
        RoundCollectionPanel(heading);
        var icon = Rect(heading, "Role Icon", new Vector2(.02f, .08f), new Vector2(.14f, .92f));
        var fit = icon.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 1;
        if (front)
        {
            var points = new[] { new Vector2(.2f, .83f), new Vector2(.8f, .83f), new Vector2(.75f, .35f), new Vector2(.5f, .12f), new Vector2(.25f, .35f) };
            for (int i = 0; i < points.Length; i++) ProfileIconLine(icon, points[i], points[(i + 1) % points.Length], color);
            ProfileIconLine(icon, new Vector2(.5f, .35f), new Vector2(.5f, .72f), color);
        }
        else
        {
            Vector2 previous = new Vector2(.30f, .15f);
            for (int i = 1; i <= 8; i++)
            {
                float t = i / 8f;
                var next = new Vector2(.30f + Mathf.Sin(t * Mathf.PI) * .32f, .15f + t * .70f);
                ProfileIconLine(icon, previous, next, color);
                previous = next;
            }
            ProfileIconLine(icon, new Vector2(.30f, .15f), new Vector2(.30f, .85f), color);
            ProfileIconLine(icon, new Vector2(.15f, .5f), new Vector2(.9f, .5f), color);
            ProfileIconLine(icon, new Vector2(.73f, .65f), new Vector2(.9f, .5f), color);
            ProfileIconLine(icon, new Vector2(.73f, .35f), new Vector2(.9f, .5f), color);
        }
        foreach (var graphic in group.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
        foreach (var graphic in heading.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
    }
    public event Action<int, IList<OwnedCharacter>> RaidRequested;
    public CatFieldController RaidField { get; set; }

    private void StartRaid()
    {
        if (saving) return;
        try
        {
            if (dungeonMode) { DungeonRequested?.Invoke(selectedDungeon, new List<OwnedCharacter>(raidTeam)); return; }
            else RaidRequested?.Invoke(selectedRaid, new List<OwnedCharacter>(raidTeam));
        }
        catch (ArgumentException error) { ShowStatus(error.Message); return; }
    }

    public void ShowRaidBattle(int index)
    {
        selectedRaid = index;
        dungeonMode = false;
        raidActive = true;
        ClearPage();
        var image = Rect(page, "Raid Field View", Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
        image.texture = fieldTexture;
        image.raycastTarget = false;
        image.gameObject.AddComponent<FieldViewportSize>().Target = fieldTexture;
        var header = Panel(page, "Raid HUD", new Vector2(0, .85f), Vector2.one, new Color(0, 0, 0, .65f));
        raidHealth = Label(header, "", 26, Vector2.zero, Vector2.one);
        raidHealth.rectTransform.offsetMax = new Vector2(-80, 0);
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
        BuildEncounterPauseButton(page);
        ShowStatus(dungeonMode ? "몰려오는 몬스터를 막고 3개 웨이브를 모두 처치하세요." : "직접 이동해 위험 범위를 피하세요. 공격은 자동으로 진행됩니다.");
    }

    private void UpdateRaid()
    {
        if (!raidActive || RaidField == null || !RaidField.RaidRunning || raidHealth == null) return;
        var boss = RaidField.RaidBoss;
        raidHealth.text = $"{EncounterTitle} · 남은 시간 {RaidField.RaidSeconds:0.0}초\nHP {boss.Health.Current:0}/{boss.Health.Maximum:0}\n{RaidField.RaidSkillMessage}";
    }

    public void ShowRaidResult(bool won, bool rewarded, bool pendingSave)
    {
        CloseDefenseScreen();
        ClearPage();
        UpgradeBackground();
        status.text = "";
        var backdrop = Panel(page, "Result Backdrop", Vector2.zero, Vector2.one,
            won ? new Color(.16f, .26f, .20f, .88f) : new Color(.21f, .23f, .32f, .88f));
        RoundCollectionPanel(backdrop);
        var trim = Panel(backdrop, "Result Gold Frame", new Vector2(.045f, .17f), new Vector2(.955f, .78f),
            won ? new Color(.83f, .64f, .33f) : new Color(.58f, .61f, .70f));
        RoundCollectionPanel(trim);
        var shadow = trim.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(.06f, .08f, .06f, .5f);
        shadow.effectDistance = new Vector2(0, -8);
        var body = Panel(trim, "Result Paw Paper", new Vector2(.018f, .014f), new Vector2(.982f, .986f), cream);
        DecorateCatPopup(body);
        var ribbon = Panel(body, "Result Ribbon", new Vector2(.06f, .77f), new Vector2(.94f, .93f),
            won ? new Color(.25f, .41f, .30f) : new Color(.39f, .42f, .53f));
        RoundCollectionPanel(ribbon);
        Label(ribbon, won ? $"{EncounterTab} 클리어!" : "다음 도전을 기다릴게요", 32, Vector2.zero, Vector2.one).color = new Color(1, .94f, .76f);
        Label(body, won ? "멋진 승리예요, 집사님!" : dungeonMode ? "기지가 무너졌어요. 다시 준비해볼까요?" : "시간 초과 또는 전멸 · 고양이들을 정비해보세요",
            22, new Vector2(.08f, .62f), new Vector2(.92f, .76f));
        var rewardCard = Panel(body, "Clear Reward Card", new Vector2(.13f, .19f), new Vector2(.87f, .58f), new Color(.93f, .85f, .69f));
        RoundCollectionPanel(rewardCard);
        Label(rewardCard, "클리어 보상", 22, new Vector2(.05f, .74f), new Vector2(.95f, .96f)).color = new Color(.43f, .32f, .20f);
        var gem = Rect(rewardCard, "Reward Crystal", new Vector2(.5f, .52f), new Vector2(.5f, .52f));
        gem.sizeDelta = new Vector2(48, 48);
        gem.localRotation = Quaternion.Euler(0, 0, 45);
        var gemEdge = Panel(gem, "Crystal Edge", Vector2.zero, Vector2.one,
            won && rewarded ? new Color(.18f, .43f, .58f) : new Color(.49f, .49f, .48f));
        Panel(gemEdge, "Crystal Face", new Vector2(.12f, .12f), new Vector2(.88f, .88f),
            won && rewarded ? new Color(.48f, .88f, 1f) : new Color(.73f, .74f, .72f));
        Panel(gemEdge, "Crystal Shine", new Vector2(.20f, .53f), new Vector2(.42f, .78f), new Color(1, 1, 1, .8f));
        string reward = !won ? "클리어 보상 없음" : !rewarded ? "보상을 지급하지 못했습니다. 저장 상태와 재화 한도를 확인해주세요." :
            $"다이아 +{CatGameSession.EncounterClearGems}";
        Label(rewardCard, reward, rewarded ? 30 : 21, new Vector2(.05f, .03f), new Vector2(.95f, .32f));
        Label(body, pendingSave ? "저장 대기 중 · 메인 상점에서 저장을 재시도해주세요." : won && rewarded ? "보상을 받았어요!" : "더 강해져서 다시 만나요!",
            19, new Vector2(.08f, .03f), new Vector2(.92f, .17f));
        Label(backdrop, won ? "VICTORY" : "TRY AGAIN", 40, new Vector2(.08f, .82f), new Vector2(.92f, .95f)).color = new Color(1, .89f, .61f);
        var back = Button(backdrop, standaloneEncounter ? "메인 전투로" : $"{EncounterTab} 목록으로", new Vector2(.20f, .045f), new Vector2(.80f, .13f), ExitRaid);
        CozyButtonStyle.Apply(back, CozyButtonStyle.Green);
    }

    private void ExitRaid()
    {
        if (saving) return;
        if (standaloneEncounter) { EncounterExitRequested?.Invoke(); return; }
        CloseDefenseScreen();
        raidActive = false;
        Show(EncounterTab);
    }

    public void ShowEncounterError(string message)
    {
        CloseDefenseScreen();
        ClearPage();
        Label(page, message, 26, new Vector2(.05f, .4f), new Vector2(.95f, .8f));
        Button(page, "메인으로 돌아가기", new Vector2(.15f, .2f), new Vector2(.85f, .32f), ExitRaid);
    }
}
