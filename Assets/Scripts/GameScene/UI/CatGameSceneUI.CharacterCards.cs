using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    private Texture2D sharedCharacterPortrait;

    private Color CharacterRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Epic:
                return new Color(.56f, .35f, .88f);
            case ItemRarity.Unique:
                return new Color(.95f, .42f, .22f);
            case ItemRarity.Legendary:
                return new Color(1, .78f, .20f);
            default:
                return new Color(.62f, .68f, .73f);
        }
    }

    private void CharacterCard(Transform grid, OwnedCharacter owned, CharacterDefinition definition,
        UnityEngine.Events.UnityAction onSelected = null, string badge = null, bool interactable = true, bool showEquipped = true)
    {
        Color rarity = CharacterRarityColor(definition == null ? ItemRarity.Common : definition.rarity);
        var card = CollectionFrame(grid, "Character Card " + owned.characterId, rarity);
        var button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = card.GetComponent<Image>();
        button.gameObject.AddComponent<ButtonTweenEffect>();
        button.interactable = interactable;
        button.onClick.AddListener(() =>
        {
            if (saving || drawOverlay != null) return;
            if (onSelected != null) onSelected(); else CharacterDetail(owned);
        });
        CharacterPortrait(Rect(card, "Large Portrait", new Vector2(.07f, .23f), new Vector2(.93f, .94f)), definition);

        if (!string.IsNullOrEmpty(badge))
            CollectionBadge(card, badge, new Vector2(.08f, .79f), new Vector2(.92f, .94f),
                interactable ? new Color(.28f, .64f, .42f) : new Color(.44f, .41f, .48f));
        else if (showEquipped && data.loadout.characterId == owned.characterId)
            CollectionBadge(card, "선택", new Vector2(.65f, .81f), new Vector2(.92f, .94f), new Color(.28f, .64f, .42f));
        var footer = Panel(card, "Growth", new Vector2(.055f, .06f), new Vector2(.945f, .30f), new Color(.06f, .05f, .10f, .92f));
        Label(footer, $"Lv.{owned.level}", 18, new Vector2(.04f, .05f), new Vector2(.49f, .95f)).color = Color.white;
        Label(footer, $"{owned.ascension}성", 17, new Vector2(.50f, .05f), new Vector2(.98f, .95f)).color = new Color(1, .82f, .36f);
    }
    private void CharacterShopCard(Transform grid, CharacterDefinition definition, string purchase, string odds)
    {
        Color rarity = CharacterRarityColor(definition.rarity);
        var card = Panel(grid, "Shop Character " + definition.id, Vector2.zero, Vector2.one, rarity);
        var button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = card.GetComponent<Image>();
        button.gameObject.AddComponent<ButtonTweenEffect>();
        button.onClick.AddListener(() =>
        {
            if (!saving && drawOverlay == null)
                ShopDetail(definition.id, true);
        });

        var portrait = Panel(card, "Portrait", new Vector2(.04f, .40f), new Vector2(.96f, .97f),
            Color.Lerp(rarity, Color.white, .55f));
        CharacterPortrait(portrait, definition);
        var footer = Panel(card, "Shop Info", new Vector2(.04f, .03f), new Vector2(.96f, .39f),
            new Color(.16f, .14f, .22f));
        Label(footer, definition.displayName, 18, new Vector2(.03f, .65f), new Vector2(.97f, 1)).color = Color.white;
        Label(footer, Rarity(definition.rarity) + odds, 14, new Vector2(.03f, .36f), new Vector2(.97f, .65f)).color =
            Color.Lerp(rarity, Color.white, .45f);
        Label(footer, purchase, 15, new Vector2(.03f, 0), new Vector2(.97f, .36f)).color = Color.white;
    }

    private void RelicIcon(Transform parent, RelicDefinition definition)
    {
        var rect = Rect(parent, "Relic Icon", new Vector2(.08f, .06f), new Vector2(.92f, .94f));
        if (definition != null && definition.icon != null)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = definition.icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }
        else
        {
            // 이전 카탈로그에 Icon 연결이 없어도 동일 ID의 제작 이미지를 사용합니다.
            string assetName = null;
            switch (definition?.id)
            {
                case "relic_claw":
                    assetName = "ClawRelic";
                    break;
                case "relic_bell":
                    assetName = "BellRelic";
                    break;
                case "relic_tiger_fang":
                    assetName = "TigerFangRelic";
                    break;
                case "relic_hunter_medal":
                    assetName = "HunterMedalRelic";
                    break;
                case "relic_warm_scarf":
                    assetName = "WarmScarfRelic";
                    break;
                case "relic_lucky_coin":
                    assetName = "LuckyCoinRelic";
                    break;
                case "relic_catnip_pouch":
                    assetName = "CatnipPouchRelic";
                    break;
                case "relic_moon_pendant":
                    assetName = "MoonPendantRelic";
                    break;
                case "relic_lion_fang":
                    assetName = "LionFangRelic";
                    break;
                case "relic_star_bell":
                    assetName = "StarBellRelic";
                    break;
                case "relic_celestial_crown":
                    assetName = "CelestialCrownRelic";
                    break;
            }
            var texture = assetName == null ? null : Resources.Load<Texture2D>("RelicIcons/" + assetName);
            if (texture != null)
            {
                var image = rect.gameObject.AddComponent<RawImage>();
                image.texture = texture;
                image.raycastTarget = false;
                var fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = (float)texture.width / texture.height;
            }
            else
                Label(rect, "?", 28, Vector2.zero, Vector2.one);
        }
    }

    private void RelicCard(Transform grid, RelicDefinition definition, string name, string detail, UnityEngine.Events.UnityAction action)
    {
        var rarity = CharacterRarityColor(definition == null ? ItemRarity.Common : definition.rarity);
        var card = CollectionFrame(grid, "Relic Card " + name, rarity);
        var button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = card.GetComponent<Image>();
        button.gameObject.AddComponent<ButtonTweenEffect>();
        button.onClick.AddListener(() => { if (!saving && drawOverlay == null) action(); });
        RelicIcon(Rect(card, "Large Relic", new Vector2(.08f, .29f), new Vector2(.92f, .94f)), definition);
        CollectionBadge(card, "유물", new Vector2(.08f, .81f), new Vector2(.36f, .94f), rarity);
        var footer = Panel(card, "Info", new Vector2(.055f, .06f), new Vector2(.945f, .32f), new Color(.06f, .05f, .10f, .92f));
        Label(footer, name, 17, new Vector2(.03f, .48f), new Vector2(.97f, 1)).color = Color.white;
        Label(footer, detail, 15, new Vector2(.03f, .02f), new Vector2(.97f, .50f)).color = new Color(1, .84f, .45f);
    }
    private void CharacterPortrait(Transform parent, CharacterDefinition definition)
    {
        var rect = Rect(parent, "Character Image", new Vector2(.06f, .02f), new Vector2(.94f, .98f));
        var frames = definition != null ? CatAnimationFrames.Get(definition.id) : null;
        if (frames != null && frames[0] != null)
        {
            var portrait = rect.gameObject.AddComponent<Image>();
            portrait.sprite = frames[0];
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            return;
        }
        if (definition != null && definition.icon != null)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = definition.icon;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }
        else
        {
            if (sharedCharacterPortrait == null)
                sharedCharacterPortrait = Resources.Load<Texture2D>("CatStarterIsometric");
            if (sharedCharacterPortrait != null)
            {
                var image = rect.gameObject.AddComponent<RawImage>();
                image.texture = sharedCharacterPortrait;
                image.raycastTarget = false;
                var fitter = rect.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = (float)sharedCharacterPortrait.width / sharedCharacterPortrait.height;
            }
            else
                Label(rect, "?", 36, Vector2.zero, Vector2.one);
        }
    }
}

