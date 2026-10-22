using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    private void DungeonChapters()
    {
        dungeonMode = true;
        var background = MapDecoration(page, "Chapter Book", Vector2.zero, Vector2.one, new Color(.19f, .24f, .20f));
        var hero = Rect(background, "Village Panorama", new Vector2(.025f, .68f), new Vector2(.975f, .985f));
        DungeonArtwork(hero, Vector2.zero, Vector2.one, new Rect(.02f, .54f, .96f, .42f), Color.white);
        var ribbon = MapDecoration(background, "Journey Banner", new Vector2(.055f, .655f), new Vector2(.945f, .735f), new Color(.55f, .22f, .16f));
        RoundCollectionPanel(ribbon);
        Label(ribbon, "집사의 여정", 30, Vector2.zero, Vector2.one).color = new Color(1, .94f, .76f);
        var viewport = MapDecoration(background, "Chapter List Viewport", new Vector2(.025f, .025f), new Vector2(.975f, .635f), Color.clear);
        viewport.GetComponent<Image>().raycastTarget = true;
        viewport.gameObject.AddComponent<RectMask2D>();
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        var list = Rect(viewport, "Chapter List", new Vector2(0, 1), Vector2.one);
        list.pivot = new Vector2(.5f, 1);
        const float cardHeight = 170f;
        const float gap = 18f;
        const float padding = 10f;
        list.sizeDelta = new Vector2(0, 5 * cardHeight + 4 * gap + 2 * padding);
        scroll.viewport = viewport;
        scroll.content = list;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 55f;
        scroll.verticalNormalizedPosition = 1f;
        int cleared = Mathf.Clamp(data.progress.dungeonClearedStage, 0, 15);
        for (int i = 0; i < 5; i++)
        {
            bool available = i == 0;
            float top = padding + i * (cardHeight + gap);
            var card = MapDecoration(list, "Chapter " + (i + 1), new Vector2(.02f, 1), new Vector2(.98f, 1),
                available ? new Color(.95f, .79f, .46f) : new Color(.42f, .45f, .40f));
            card.pivot = new Vector2(.5f, 1);
            card.sizeDelta = new Vector2(0, cardHeight);
            card.anchoredPosition = new Vector2(0, -top);
            RoundCollectionPanel(card);
            var face = MapDecoration(card, "Chapter Face", new Vector2(.012f, .055f), new Vector2(.988f, .945f),
                available ? new Color(1, .94f, .80f) : new Color(.70f, .71f, .65f));
            RoundCollectionPanel(face);
            var thumbnail = Rect(face, "Chapter Thumbnail", new Vector2(.015f, .08f), new Vector2(.24f, .92f));
            thumbnail.gameObject.AddComponent<RectMask2D>();
            DungeonArtwork(thumbnail, Vector2.zero, Vector2.one, new Rect(.08f + i * .12f, .60f, .24f, .34f),
                available ? Color.white : new Color(.40f, .43f, .40f));
            Label(face, available ? "1. 고양이 마을" : $"{i + 1}. 미지의 지역", 29,
                new Vector2(.28f, .43f), new Vector2(.83f, .96f)).alignment = TextAnchor.MiddleLeft;
            var description = Label(face, available ? $"클리어 {cleared} / 15  ·  탐험하기" : "아직 열리지 않은 챕터", 22,
                new Vector2(.28f, .06f), new Vector2(.83f, .45f));
            description.alignment = TextAnchor.MiddleLeft;
            description.color = new Color(.40f, .36f, .27f);
            if (available)
                Label(face, "▶", 26, new Vector2(.86f, .1f), new Vector2(.98f, .9f)).color = new Color(.63f, .36f, .12f);
            else
            {
                var padlock = Rect(face, "Locked Chapter", new Vector2(.87f, .25f), new Vector2(.96f, .80f));
                MapOval(padlock, "Lock Loop", new Vector2(.5f, .72f), new Vector2(.36f, .28f), new Color(.32f, .35f, .30f));
                MapOval(padlock, "Lock Opening", new Vector2(.5f, .72f), new Vector2(.19f, .16f), new Color(.70f, .71f, .65f));
                var body = MapDecoration(padlock, "Lock Body", Vector2.zero, new Vector2(1, .65f), new Color(.43f, .40f, .30f));
                RoundCollectionPanel(body);
                MapDecoration(body, "Keyhole", new Vector2(.43f, .23f), new Vector2(.57f, .63f), new Color(.20f, .23f, .19f));
            }
            card.GetComponent<Image>().raycastTarget = true;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            button.interactable = available;
            if (available) button.onClick.AddListener(DungeonMap);
        }
    }

    private void DungeonArtwork(Transform parent, Vector2 min, Vector2 max, Rect crop, Color tint)
    {
        var picture = Rect(parent, "Chapter Artwork", min, max).gameObject.AddComponent<RawImage>();
        picture.texture = Resources.Load<Texture2D>("GameUIArt/DungeonChapterOne");
        picture.uvRect = crop;
        picture.color = tint;
        picture.raycastTarget = false;
    }
}
