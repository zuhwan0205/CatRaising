using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    public event Action<int, IList<OwnedCharacter>> DungeonRequested;
    private bool dungeonMode;
    private int selectedDungeon = 1;
    private int TeamSize => dungeonMode ? 2 : 4;
    private string EncounterTab => dungeonMode ? "던전" : "레이드";
    private string EncounterTitle => dungeonMode ? $"던전 1-{selectedDungeon}" : RaidBattle.Names[selectedRaid];

    private void DungeonMap()
    {
        dungeonMode = true;
        var map = Panel(page, "Dungeon Village Map", Vector2.zero, Vector2.one, new Color(.67f, .47f, .27f));
        Label(map, "CHAPTER 1 · 고양이 마을", 30, new Vector2(0, .91f), Vector2.one).color = Color.white;
        Label(map, "고양이 2마리 · 5 / 10 / 15 보스", 20, new Vector2(0, .85f), new Vector2(1, .92f)).color = Color.white;
        // 종횡비에 관계없이 5행으로 진행 경로를 배치합니다.
        var points = new Vector2[15];
        for (int i = 0; i < points.Length; i++)
        {
            int row = i / 3;
            int column = row % 2 == 0 ? i % 3 : 2 - i % 3;
            points[i] = new Vector2(.19f + column * .31f, .12f + row * .155f + (column == 1 ? .035f : 0));
        }
        for (int i = 0; i < 8; i++)
        {
            float x = i % 2 == 0 ? .025f : .88f;
            float y = .08f + (i / 2) * .21f;
            var house = Panel(map, "Village House", new Vector2(x, y), new Vector2(x + .09f, y + .07f), new Color(.83f, .66f, .40f));
            Panel(house, "Roof", new Vector2(-.1f, .7f), new Vector2(1.1f, 1.2f), new Color(.39f, .23f, .15f));
            Panel(house, "Door", new Vector2(.35f, 0), new Vector2(.65f, .5f), new Color(.25f, .17f, .12f));
        }
        for (int i = 1; i < points.Length; i++)
        {
            // 작은 타일을 이어 굽은 길을 표현합니다.
            for (int step = 0; step <= 18; step++)
            {
                Vector2 p = Vector2.Lerp(points[i - 1], points[i], step / 18f);
                var tile = Panel(map, "Path Tile", p - new Vector2(.014f, .007f), p + new Vector2(.014f, .007f), new Color(.92f, .77f, .48f));
                tile.GetComponent<Image>().raycastTarget = false;
            }
        }
        for (int i = 0; i < points.Length; i++)
        {
            int stage = i + 1;
            bool cleared = stage <= data.progress.dungeonClearedStage;
            bool unlocked = stage <= data.progress.dungeonClearedStage + 1;
            bool boss = stage % 5 == 0;
            Vector2 p = points[i];
            var button = Button(map, unlocked ? $"1-{stage}" : "잠금", p - new Vector2(.09f, .033f), p + new Vector2(.09f, .033f), () =>
            {
                selectedDungeon = stage;
                DungeonPreparation();
            });
            CozyButtonStyle.SetColor(button, !unlocked ? new Color(.45f, .45f, .45f) : boss ? new Color(1, .74f, .2f) : CozyButtonStyle.Green);
            button.interactable = unlocked;
            Label(map, cleared ? "★ 클리어" : boss ? "BOSS" : unlocked ? "도전 가능" : "", 15,
                p + new Vector2(-.12f, -.065f), p + new Vector2(.12f, -.034f)).color = Color.white;
        }
    }

    private readonly OwnedCharacter[] dungeonSlots = new OwnedCharacter[2];

    private void DungeonPreparation()
    {
        ClearPage();
        Button(page, "목록", new Vector2(0, .91f), new Vector2(.23f, 1), () => Show("던전"));
        Label(page, $"던전 1-{selectedDungeon}", 28, new Vector2(.25f, .91f), Vector2.one);
        var monsters = Panel(page, "Dungeon Monster Preview", new Vector2(.02f, .51f), new Vector2(.98f, .88f), new Color(.20f, .25f, .23f));
        int kinds = DungeonStageRules.KindCount(selectedDungeon);
        for (int i = 0; i < kinds; i++)
        {
            float left = .03f + i * .94f / kinds;
            var portrait = Panel(monsters, "Monster " + i, new Vector2(left, .40f), new Vector2(left + .88f / kinds, .94f), cream);
            var texture = Resources.Load<Texture2D>("MonsterArt/" + DungeonStageRules.MonsterArt[i]);
            if (texture != null)
            {
                var image = Rect(portrait, "Monster Image", new Vector2(.05f, .17f), new Vector2(.95f, .96f)).gameObject.AddComponent<RawImage>();
                image.texture = texture;
                image.uvRect = new Rect(0, .5f, .25f, .5f);
                image.raycastTarget = false;
            }
            Label(portrait, DungeonStageRules.MonsterNames[i], 15, Vector2.zero, new Vector2(1, .22f));
        }
        Label(monsters, "출현 몬스터", 25, new Vector2(0, .22f), new Vector2(1, .40f)).color = Color.white;
        Label(monsters, $"3 웨이브 · 총 60초 · 웨이브 사이 3초" + (selectedDungeon % 5 == 0 ? "\n마지막 웨이브에 보스 출현" : ""), 18,
            new Vector2(0, .02f), new Vector2(1, .22f)).color = Color.white;

        var cats = Panel(page, "Dungeon Team", new Vector2(.02f, .17f), new Vector2(.98f, .48f), new Color(.20f, .25f, .23f));
        for (int i = 0; i < 2; i++)
        {
            int slot = i;
            var owned = dungeonSlots[i];
            if (owned != null && !data.characters.Contains(owned)) dungeonSlots[i] = owned = null;
            var card = Panel(cats, "Cat Slot " + i, new Vector2(.06f + i * .47f, .25f), new Vector2(.47f + i * .47f, .94f), cream);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.onClick.AddListener(() => { if (!saving) DungeonCatPicker(slot); });
            if (owned != null)
            {
                CharacterPortrait(Rect(card, "Portrait", new Vector2(0, .20f), Vector2.one), catalog.FindCharacter(owned.characterId));
                Label(card, $"Lv.{owned.level} · 교체", 18, Vector2.zero, new Vector2(1, .23f));
            }
            else Label(card, "+\n고양이 선택", 24, Vector2.zero, Vector2.one);
        }
        Label(cats, "고양이 · 이미지를 눌러 교체", 22, Vector2.zero, new Vector2(1, .23f)).color = Color.white;
        var start = Button(page, "전투 시작", new Vector2(.18f, .02f), new Vector2(.82f, .12f), () =>
        {
            raidTeam.Clear();
            raidTeam.AddRange(dungeonSlots);
            StartRaid();
        });
        start.interactable = dungeonSlots[0] != null && dungeonSlots[1] != null && dungeonSlots[0].characterId != dungeonSlots[1].characterId;
    }

    private void DungeonCatPicker(int slot)
    {
        var overlay = Panel(page, "Owned Cat Picker", Vector2.zero, Vector2.one, cream);
        Label(overlay, $"{slot + 1}번 슬롯 · 보유 고양이", 28, new Vector2(0, .88f), Vector2.one);
        Button(overlay, "닫기", new Vector2(.70f, .02f), new Vector2(.98f, .10f), () => RemoveUI(overlay.gameObject));
        var viewport = Panel(overlay, "Picker Viewport", new Vector2(.02f, .13f), new Vector2(.98f, .86f), cream);
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Rect(viewport, "Owned Cats", new Vector2(0, 1), Vector2.one);
        content.pivot = new Vector2(.5f, 1);
        Canvas.ForceUpdateCanvases();
        var layout = content.gameObject.AddComponent<GridLayoutGroup>();
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 3;
        layout.cellSize = new Vector2(Mathf.Max(60, (viewport.rect.width - 24) / 3), 180);
        layout.spacing = new Vector2(8, 8);
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport;
        scroll.content = content;
        scroll.horizontal = false;
        scroll.scrollSensitivity = 60;
        var seen = new HashSet<string>();
        foreach (var owned in data.characters)
        {
            if (owned == null || !seen.Add(owned.characterId)) continue;
            var definition = catalog.FindCharacter(owned.characterId);
            if (definition == null) continue;
            var cat = owned;
            bool otherSlot = dungeonSlots[1 - slot]?.characterId == cat.characterId;
            var card = Panel(content, "Owned Cat", Vector2.zero, Vector2.one, CharacterRarityColor(definition.rarity));
            CharacterPortrait(Rect(card, "Portrait", new Vector2(0, .23f), Vector2.one), definition);
            Label(card, otherSlot ? "다른 슬롯 편성 중" : $"Lv.{cat.level}", 16, Vector2.zero, new Vector2(1, .23f));
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.interactable = !otherSlot;
            button.onClick.AddListener(() =>
            {
                if (saving) return;
                dungeonSlots[slot] = cat;
                DungeonPreparation();
            });
        }
    }
}
