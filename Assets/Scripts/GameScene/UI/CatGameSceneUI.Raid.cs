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
        Show("모험");
        adventureMenu = Panel(safeArea, "Adventure Menu", new Vector2(.04f, .10f), new Vector2(.30f, .27f), cream);
        Button(adventureMenu, "레이드", new Vector2(.03f, .53f), new Vector2(.97f, .97f), () => Show("레이드"));
        Button(adventureMenu, "던전", new Vector2(.03f, .03f), new Vector2(.97f, .47f), () => Show("던전"));
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
        var portrait = Panel(parent, "Boss Portrait Frame", new Vector2(.53f, .02f), new Vector2(.98f, .98f), CharacterRarityColor(RaidBattle.Rarities[index]));
        portrait.GetComponent<Image>().raycastTarget = false;
        var image = Rect(portrait, "Boss Portrait", Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
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
        Label(page, $"레이드 · 계정 Lv.{data.progress.accountLevel}", 30, new Vector2(0, .90f), Vector2.one);
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            bool available = RaidBattle.CanEnter(i, data.progress.accountLevel);
            var card = Panel(page, "Raid " + i, new Vector2(.04f, .69f - i * .22f), new Vector2(.96f, .87f - i * .22f),
                available ? new Color(.22f, .30f, .26f) : new Color(.35f, .35f, .38f));
            if (i < 3) BossImage(card, i);
            string title = i == 3 ? "잠금 · 추후 공개" : $"{RaidBattle.Names[i]}\n필요 계정 레벨: {RaidBattle.RequiredLevels[i]}" + (available ? "\n고양이 4마리 편성" : "\n잠금");
            if (i < 3 && RaidBattle.IgnoreLevelRequirementForTesting)
                title = $"{RaidBattle.Names[i]}\n테스트 · 레벨 제한 해제\n고양이 4마리 편성";
            Label(card, title, 23, new Vector2(.03f, .06f), new Vector2(.65f, .94f)).color = Color.white;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.interactable = available;
            button.onClick.AddListener(() => { if (!saving) { selectedRaid = index; raidTeam.Clear(); RaidFormation(); } });
        }
    }

    private void RaidFormation()
    {
        ClearPage();
        var grid = Grid($"{EncounterTitle} · 편성 {raidTeam.Count}/{TeamSize}", .76f);
        // 하단 입장 버튼과 목록 스크롤 영역을 분리합니다.
        var viewport = (RectTransform)grid.parent;
        viewport.anchorMin = new Vector2(0, .15f);
        Button(page, "목록", new Vector2(0, .80f), new Vector2(.25f, .89f), () => Show(EncounterTab));
        Label(page, $"보유 고양이 {TeamSize}마리를 선택하세요", 22, new Vector2(.27f, .80f), new Vector2(1, .89f));
        var seen = new HashSet<string>();
        foreach (var owned in data.characters)
        {
            if (owned == null || !seen.Add(owned.characterId)) continue;
            var definition = catalog.FindCharacter(owned.characterId);
            if (definition == null) continue;
            var cat = owned;
            bool selected = raidTeam.Contains(cat);
            var card = Panel(grid, "Raid Cat", Vector2.zero, Vector2.one, CharacterRarityColor(definition.rarity));
            var portrait = Rect(card, "Portrait", new Vector2(.05f, .24f), new Vector2(.95f, .98f));
            CharacterPortrait(portrait, definition);
            if (selected)
            {
                var badge = Panel(card, "Selected Badge", new Vector2(.45f, .78f), new Vector2(.98f, .98f), CozyButtonStyle.Green);
                badge.GetComponent<Image>().raycastTarget = false;
                Label(badge, "선택됨", 15, Vector2.zero, Vector2.one);
            }
            Label(card, $"Lv.{cat.level}" + (selected ? " · 선택됨" : ""), 18, Vector2.zero, new Vector2(1, .25f));
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.onClick.AddListener(() =>
            {
                if (saving) return;
                if (!raidTeam.Remove(cat))
                {
                    if (raidTeam.Count == TeamSize) { ShowStatus($"최대 {TeamSize}마리까지 선택할 수 있습니다."); return; }
                    raidTeam.Add(cat);
                }
                RaidFormation();
            });
        }
        var enter = Button(page, $"{EncounterTab} 입장 · {raidTeam.Count}/{TeamSize}", new Vector2(0, .02f), new Vector2(1, .12f), StartRaid);
        enter.interactable = raidTeam.Count == TeamSize && (dungeonMode ? selectedDungeon <= data.progress.dungeonClearedStage + 1 : RaidBattle.CanEnter(selectedRaid, data.progress.accountLevel));
    }

    public event Action<int, IList<OwnedCharacter>> RaidRequested;
    public CatFieldController RaidField { get; set; }

    private void StartRaid()
    {
        try
        {
            if (dungeonMode) DungeonRequested?.Invoke(selectedDungeon, new List<OwnedCharacter>(raidTeam));
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
        if (dungeonMode)
        {
            raidHealth.text = $"{EncounterTitle} · {RaidField.RaidSeconds:0.0}초\n웨이브 {RaidField.DungeonWave}/3 · 남은 적 {RaidField.DungeonEnemiesRemaining}" +
                (RaidField.DungeonWaveCountdown > 0 ? $" · 다음 웨이브 {RaidField.DungeonWaveCountdown:0.0}초" : "");
            return;
        }
        raidHealth.text = $"{EncounterTitle} · 남은 시간 {RaidField.RaidSeconds:0.0}초\nHP {boss.Health.Current:0}/{boss.Health.Maximum:0}\n{RaidField.RaidSkillMessage}";
    }

    public void ShowRaidResult(bool won)
    {
        ClearPage();
        Label(page, won ? $"{EncounterTab} 승리!" : $"{EncounterTab} 실패 · 시간 초과 또는 전멸", 32, new Vector2(0, .55f), new Vector2(1, .80f));
        Label(page, dungeonMode ? "클리어 진행은 저장됩니다. 재화 보상은 추후 추가됩니다." : "레이드 보상은 추후 추가됩니다.", 24, new Vector2(0, .4f), new Vector2(1, .55f));
        Button(page, $"{EncounterTab} 목록으로", new Vector2(.15f, .2f), new Vector2(.85f, .32f), ExitRaid);
    }

    private void ExitRaid()
    {
        raidActive = false;
        RaidActiveChanged?.Invoke(false);
        Show(EncounterTab);
    }
}
