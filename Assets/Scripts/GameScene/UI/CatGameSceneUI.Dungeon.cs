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
        var frame = MapDecoration(page, "Map Wooden Frame", Vector2.zero, Vector2.one, new Color(.25f, .19f, .14f));
        var map = MapDecoration(frame, "Dungeon Village Map", new Vector2(.015f, .008f), new Vector2(.985f, .992f), new Color(.58f, .43f, .28f));
        // 작은 색면과 나무를 배경에 먼저 그려 경로와 스테이지를 가리지 않습니다.
        for (int i = 0; i < 48; i++)
        {
            float x = .03f + (i * 37 % 93) / 100f;
            float y = .09f + (i * 19 % 72) / 100f;
            MapDecoration(map, "Fallen Leaves", new Vector2(x, y), new Vector2(x + .015f, y + .006f),
                i % 2 == 0 ? new Color(.72f, .51f, .28f) : new Color(.49f, .37f, .25f));
        }
        for (int i = 0; i < 10; i++)
        {
            float x = i % 2 == 0 ? .045f : .91f;
            float y = .16f + i / 2 * .14f;
            var tree = MapDecoration(map, "Autumn Tree", new Vector2(x - .035f, y), new Vector2(x + .035f, y + .065f), Color.clear);
            MapDecoration(tree, "Trunk", new Vector2(.42f, 0), new Vector2(.58f, .5f), new Color(.29f, .21f, .15f));
            MapDecoration(tree, "Crown Shadow", new Vector2(0, .35f), new Vector2(1, .82f), new Color(.43f, .29f, .16f));
            MapDecoration(tree, "Crown", new Vector2(.08f, .43f), new Vector2(.92f, .9f), i % 3 == 0 ? new Color(.66f, .34f, .20f) : new Color(.73f, .53f, .24f));
            MapDecoration(tree, "Crown Top", new Vector2(.24f, .85f), new Vector2(.76f, 1), new Color(.80f, .61f, .31f));
        }
        var header = MapDecoration(map, "Chapter Plaque", new Vector2(.025f, .855f), new Vector2(.975f, .985f), new Color(.24f, .29f, .24f));
        Label(header, "CHAPTER 1 · 고양이 마을", 28, new Vector2(.02f, .48f), new Vector2(.98f, .96f)).color = new Color(1, .91f, .70f);
        Label(header, $"고양이 2마리 · 클리어 {Mathf.Clamp(data.progress.dungeonClearedStage, 0, 15)} / 15", 18, new Vector2(.02f, .08f), new Vector2(.98f, .48f)).color = new Color(.87f, .88f, .77f);
        var footer = MapDecoration(map, "Map Legend", new Vector2(.025f, .014f), new Vector2(.975f, .065f), new Color(.32f, .26f, .19f));
        Label(footer, "길을 따라 도전하세요 · 5 / 10 / 15 보스", 17, Vector2.zero, Vector2.one).color = new Color(1, .89f, .68f);
        // 종횡비에 관계없이 5행으로 진행 경로를 배치합니다.
        var points = new Vector2[15];
        for (int i = 0; i < points.Length; i++)
        {
            int row = i / 3;
            int column = row % 2 == 0 ? i % 3 : 2 - i % 3;
            points[i] = new Vector2(.19f + column * .31f, .12f + row * .155f + (column == 1 ? .035f : 0));
        }
        for (int i = 0; i < 4; i++)
        {
            float x = i % 2 == 0 ? .025f : .88f;
            float y = .09f + (i / 2) * .43f;
            var house = Panel(map, "Village House", new Vector2(x, y), new Vector2(x + .09f, y + .07f), new Color(.83f, .66f, .40f));
            Panel(house, "Roof", new Vector2(-.1f, .7f), new Vector2(1.1f, 1.2f), new Color(.39f, .23f, .15f));
            Panel(house, "Door", new Vector2(.35f, 0), new Vector2(.65f, .5f), new Color(.25f, .17f, .12f));
            MapDecoration(house, "Lit Window", new Vector2(.10f, .36f), new Vector2(.27f, .58f), new Color(1, .80f, .41f));
        }
        for (int i = 1; i < points.Length; i++)
        {
            // 작은 타일을 이어 굽은 길을 표현합니다.
            for (int step = 0; step <= 18; step++)
            {
                Vector2 p = Vector2.Lerp(points[i - 1], points[i], step / 18f);
                MapDecoration(map, "Path Edge", p - new Vector2(.021f, .010f), p + new Vector2(.021f, .010f), new Color(.39f, .29f, .19f));
                var tile = Panel(map, "Path Tile", p - new Vector2(.014f, .007f), p + new Vector2(.014f, .007f),
                    i <= data.progress.dungeonClearedStage ? new Color(.93f, .76f, .46f) : new Color(.68f, .55f, .38f));
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
            bool current = stage == data.progress.dungeonClearedStage + 1;
            MapDecoration(map, "Stage Border", p - new Vector2(.099f, .038f), p + new Vector2(.099f, .038f),
                current ? new Color(1, .87f, .48f) : boss ? new Color(.55f, .27f, .15f) : new Color(.34f, .30f, .21f));
            var button = Button(map, unlocked ? $"1-{stage}" : "잠금", p - new Vector2(.09f, .033f), p + new Vector2(.09f, .033f), () =>
            {
                selectedDungeon = stage;
                DungeonPreparation();
            });
            CozyButtonStyle.SetColor(button, !unlocked ? new Color(.45f, .45f, .45f) : boss ? new Color(1, .74f, .2f) : CozyButtonStyle.Green);
            button.interactable = unlocked;
            var caption = MapDecoration(map, "Stage Caption", p + new Vector2(-.115f, -.065f), p + new Vector2(.115f, -.039f), new Color(.28f, .24f, .18f));
            Label(caption, cleared ? "★ 클리어" : current ? (boss ? "BOSS · 도전" : "도전 가능") : boss ? "BOSS" : "미개방", 14,
                Vector2.zero, Vector2.one).color = current ? new Color(1, .87f, .48f) : new Color(.94f, .89f, .77f);
        }
    }

    private RectTransform MapDecoration(Transform parent, string name, Vector2 min, Vector2 max, Color color)
    {
        var panel = Panel(parent, name, min, max, color);
        panel.GetComponent<Image>().raycastTarget = false;
        return panel;
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
            var portrait = Panel(monsters, "Monster " + i, new Vector2(left, .40f), new Vector2(left + .88f / kinds, .94f), CharacterRarityColor(DungeonStageRules.MonsterRarity(i)));
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
            var definition = owned == null ? null : catalog.FindCharacter(owned.characterId);
            var card = Panel(cats, "Cat Slot " + i, new Vector2(.06f + i * .47f, .25f), new Vector2(.47f + i * .47f, .94f), owned == null ? cream : CharacterRarityColor(definition == null ? ItemRarity.Common : definition.rarity));
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
        OpenTeamCatPicker(dungeonSlots, slot, "던전", DungeonPreparation);
    }

    private void OpenTeamCatPicker(OwnedCharacter[] slots, int slot, string title, Action refresh)
    {
        var overlay = Panel(page, "Owned Cat Picker", Vector2.zero, Vector2.one, cream);
        Label(overlay, $"{title} · 보유 고양이 선택", 28, new Vector2(0, .88f), Vector2.one);
        Button(overlay, "슬롯 비우기", new Vector2(.02f, .02f), new Vector2(.45f, .10f), () => { slots[slot] = null; refresh(); });
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
            bool otherSlot = false;
            for (int i = 0; i < slots.Length; i++)
                if (i != slot && slots[i]?.characterId == cat.characterId) otherSlot = true;
            var card = Panel(content, "Owned Cat", Vector2.zero, Vector2.one, CharacterRarityColor(definition.rarity));
            CharacterPortrait(Rect(card, "Portrait", new Vector2(0, .23f), Vector2.one), definition);
            Label(card, otherSlot ? "다른 슬롯 편성 중" : $"Lv.{cat.level}", 16, Vector2.zero, new Vector2(1, .23f));
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.interactable = !otherSlot;
            button.onClick.AddListener(() =>
            {
                if (saving) return;
                slots[slot] = cat;
                refresh();
            });
        }
    }
}
