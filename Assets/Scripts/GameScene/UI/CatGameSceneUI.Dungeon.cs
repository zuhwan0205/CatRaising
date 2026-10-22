using System;
using System.Collections.Generic;
using System.Linq;
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
        ClearPage();
        dungeonMode = true;
        var viewport = MapDecoration(page, "World Map Viewport", Vector2.zero, Vector2.one, new Color(.24f, .40f, .20f));
        viewport.GetComponent<Image>().raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        var map = MapDecoration(viewport, "Cat Village World Map", new Vector2(0, .5f), new Vector2(0, .5f), new Color(.65f, .78f, .36f));
        map.pivot = new Vector2(0, .5f);
        Canvas.ForceUpdateCanvases();
        float mapHeight = Mathf.Max(1200, viewport.rect.height);
        map.sizeDelta = new Vector2(mapHeight * 1.5f, mapHeight);
        DungeonArtwork(map, Vector2.zero, Vector2.one, new Rect(0, 0, 1, 1), Color.white);
        scroll.viewport = viewport;
        scroll.content = map;
        scroll.horizontal = true;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 55;
        scroll.decelerationRate = .08f;
        int clearedCount = Mathf.Clamp(data.progress.dungeonClearedStage, 0, 15);
        var points = new Vector2[15];
        for (int i = 0; i < points.Length; i++)
        {
            // 그림 중앙의 잔디를 따라 두 줄로 돌며 마지막에 마을 문으로 향합니다.
            float x = i < 8 ? .10f + i * .11f : .87f - (i - 8) * .12f;
            float y = i < 8 ? .43f + Mathf.Sin(i * 1.1f) * .035f : .59f + Mathf.Sin(i * 1.1f) * .025f;
            points[i] = new Vector2(x, y);
        }
        // 곡선을 촘촘하게 이어 밝은 흙길과 점선 발자국을 만듭니다.
        for (int i = 0; i < 14; i++)
        {
            for (int step = 0; step < 20; step++)
            {
                float t = step / 20f;
                Vector2 point = DungeonPathPoint(points, i, t);
                MapOval(map, "Sand Path", point, new Vector2(.007f, .012f), new Color(.94f, .82f, .51f, .85f));
            }
        }
        for (int i = 0; i < 140; i++)
        {
            var point = DungeonPathPoint(points, i / 10, (i % 10) / 10f);
            MapOval(map, "Trail Mark", point, new Vector2(.0017f, .0026f), new Color(.75f, .53f, .27f));
        }
        for (int i = 0; i < points.Length; i++)
        {
            int stage = i + 1;
            bool cleared = stage <= clearedCount;
            bool unlocked = stage <= clearedCount + 1;
            bool boss = stage % 5 == 0;
            bool current = stage == Mathf.Min(clearedCount + 1, 15);
            Vector2 point = points[i];
            Color rim = unlocked ? new Color(.75f, .43f, .10f) : new Color(.40f, .46f, .35f);
            Color gold = unlocked ? new Color(1, .79f, .20f) : new Color(.65f, .69f, .55f);
            var node = Rect(map, "Stage 1-" + stage, point, point);
            node.sizeDelta = new Vector2(boss ? 100 : 84, boss ? 80 : 68);
            MapOval(node, "Ground Shadow", new Vector2(.5f, .31f), new Vector2(.55f, .36f), new Color(.23f, .33f, .14f, .45f));
            var disk = MapOval(node, "Stage Rim", new Vector2(.5f, .52f), new Vector2(.5f, .43f), rim);
            MapOval(node, "Stage Gold", new Vector2(.5f, .59f), new Vector2(.43f, .34f), gold);
            MapOval(node, "Stage Shine", new Vector2(.5f, .64f), new Vector2(.30f, .21f), unlocked ? new Color(1, .94f, .56f) : new Color(.76f, .79f, .67f));
            var hit = node.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var button = node.gameObject.AddComponent<Button>();
            button.targetGraphic = disk.GetComponent<DungeonMapOval>();
            button.interactable = unlocked;
            button.onClick.AddListener(() => { selectedDungeon = stage; DungeonPreparation(); });
            Label(node, boss ? "BOSS" : cleared ? "✓" : unlocked ? "" : "잠금", boss ? 17 : 19,
                new Vector2(0, .35f), new Vector2(1, .85f)).color = new Color(.40f, .27f, .12f);
            var caption = Label(node, "1-" + stage, 21, new Vector2(-.2f, -.48f), new Vector2(1.2f, .02f));
            caption.color = Color.white;
            caption.gameObject.AddComponent<Shadow>().effectColor = new Color(.22f, .30f, .12f);
            if (current)
                Label(node, "▼", 29, new Vector2(0, 1.05f), new Vector2(1, 1.75f)).color = new Color(1, .98f, .72f);
            if (cleared)
                Label(node, "클리어", 15, new Vector2(-.2f, -.85f), new Vector2(1.2f, -.45f)).color = new Color(.25f, .39f, .17f);
        }
        var header = MapDecoration(page, "Chapter Plaque", new Vector2(.26f, .88f), new Vector2(.975f, .985f), new Color(.16f, .29f, .16f, .92f));
        Label(header, "CHAPTER 1 · 고양이 마을", 25, new Vector2(0, .40f), Vector2.one).color = new Color(1, .95f, .77f);
        Label(header, $"클리어 {clearedCount} / 15 · 고양이 2마리", 17, Vector2.zero, new Vector2(1, .43f)).color = new Color(.86f, .93f, .76f);
        Button(page, "챕터", new Vector2(.025f, .91f), new Vector2(.23f, .98f), () => Show("던전"));
        var footer = MapDecoration(page, "Map Guide", new Vector2(.1f, .015f), new Vector2(.9f, .08f), new Color(.16f, .29f, .16f, .85f));
        Label(footer, "지도를 밀어서 탐험하세요", 18, Vector2.zero, Vector2.one).color = Color.white;
        Canvas.ForceUpdateCanvases();
        float width = Mathf.Max(0, map.rect.width - viewport.rect.width);
        float target = points[Mathf.Min(clearedCount, 14)].x * map.rect.width - viewport.rect.width * .5f;
        scroll.horizontalNormalizedPosition = width > 0 ? Mathf.Clamp01(target / width) : 0;
        scroll.verticalNormalizedPosition = .5f;
    }

    private static Vector2 DungeonPathPoint(Vector2[] points, int segment, float t)
    {
        var start = points[segment];
        var end = points[segment + 1];
        return new Vector2(Mathf.Lerp(start.x, end.x, t), Mathf.Lerp(start.y, end.y, Mathf.SmoothStep(0, 1, t)));
    }

    private RectTransform MapOval(Transform parent, string name, Vector2 center, Vector2 radius, Color color)
    {
        var rect = Rect(parent, name, center - radius, center + radius);
        rect.gameObject.AddComponent<CanvasRenderer>();
        var graphic = rect.gameObject.AddComponent<DungeonMapOval>();
        graphic.color = color;
        graphic.raycastTarget = false;
        return rect;
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
        Button(page, "목록", new Vector2(0, .91f), new Vector2(.23f, 1), DungeonMap);
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
                var imageArea = Rect(portrait, "Monster Image Area", new Vector2(.06f, .25f), new Vector2(.94f, .94f));
                var image = Rect(imageArea, "Monster Image", Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
                image.texture = texture;
                image.uvRect = new Rect(0, .5f, .25f, .5f);
                var aspect = image.gameObject.AddComponent<AspectRatioFitter>();
                aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                aspect.aspectRatio = (texture.width * image.uvRect.width) / (texture.height * image.uvRect.height);
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
            RoundCollectionPanel(card);
            var slotFace = MapDecoration(card, "Slot Cushion", new Vector2(.025f, .035f), new Vector2(.975f, .965f),
                owned == null ? new Color(.98f, .88f, .71f) : Color.Lerp(CharacterRarityColor(definition == null ? ItemRarity.Common : definition.rarity), new Color(1, .95f, .85f), .75f));
            RoundCollectionPanel(slotFace);
            var paw = Rect(slotFace, "Paw Emblem", new Vector2(.5f, .58f), new Vector2(.5f, .58f));
            paw.sizeDelta = new Vector2(72, 78);
            Color pawColor = new Color(.67f, .43f, .28f, owned == null ? .35f : .15f);
            MapOval(paw, "Pad", new Vector2(.5f, .32f), new Vector2(.28f, .23f), pawColor);
            MapOval(paw, "Toe Left", new Vector2(.15f, .61f), new Vector2(.12f, .15f), pawColor);
            MapOval(paw, "Toe Top Left", new Vector2(.38f, .81f), new Vector2(.12f, .16f), pawColor);
            MapOval(paw, "Toe Top Right", new Vector2(.64f, .81f), new Vector2(.12f, .16f), pawColor);
            MapOval(paw, "Toe Right", new Vector2(.86f, .61f), new Vector2(.12f, .15f), pawColor);
            var caption = MapDecoration(card, "Slot Caption", new Vector2(.025f, .035f), new Vector2(.975f, .24f), new Color(.38f, .27f, .20f));
            RoundCollectionPanel(caption);
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.onClick.AddListener(() => { if (!saving) DungeonCatPicker(slot); });
            if (owned != null)
            {
                CharacterPortrait(Rect(card, "Portrait", new Vector2(0, .20f), Vector2.one), catalog.FindCharacter(owned.characterId));
                Label(caption, $"Lv.{owned.level} · 교체", 18, Vector2.zero, Vector2.one).color = new Color(1, .95f, .84f);
            }
            else
            {
                Label(paw, "+", 29, new Vector2(.15f, .05f), new Vector2(.85f, .60f)).color = new Color(.45f, .28f, .16f);
                Label(caption, "고양이 선택", 18, Vector2.zero, Vector2.one).color = new Color(1, .95f, .84f);
            }
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
        foreach (var owned in data.characters.Where(item => item != null).OrderBy(item => catalog.FindCharacter(item.characterId)?.rarity ?? ItemRarity.Common))
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

// 월드맵의 길과 원형 스테이지를 텍스처 없이 그립니다.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class DungeonMapOval : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        Vector2 center = rect.center;
        mesh.AddVert(center, color, Vector2.zero);
        const int sides = 24;
        for (int i = 0; i < sides; i++)
        {
            float angle = i * Mathf.PI * 2 / sides;
            mesh.AddVert(center + new Vector2(Mathf.Cos(angle) * rect.width * .5f, Mathf.Sin(angle) * rect.height * .5f), color, Vector2.zero);
        }
        for (int i = 0; i < sides; i++) mesh.AddTriangle(0, i + 1, (i + 1) % sides + 1);
    }
}
