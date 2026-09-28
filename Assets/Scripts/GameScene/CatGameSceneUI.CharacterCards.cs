using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    private Texture2D sharedCharacterPortrait;

    private Color CharacterRarityColor(ItemRarity rarity)
    {
        switch(rarity)
        {
            case ItemRarity.Epic: return new Color(.56f,.35f,.88f);
            case ItemRarity.Unique: return new Color(.95f,.42f,.22f);
            case ItemRarity.Legendary: return new Color(1,.78f,.20f);
            default: return new Color(.62f,.68f,.73f);
        }
    }

    private void CharacterCard(Transform grid,OwnedCharacter owned,CharacterDefinition definition)
    {
        Color rarity=CharacterRarityColor(definition==null?ItemRarity.Common:definition.rarity);
        var card=Panel(grid,"Character Card "+owned.characterId,Vector2.zero,Vector2.one,rarity);
        var button=card.gameObject.AddComponent<Button>();button.targetGraphic=card.GetComponent<Image>();
        button.onClick.AddListener(()=>{if(!saving && drawOverlay==null)CharacterDetail(owned);});
        var portrait=Panel(card,"Portrait",new Vector2(.04f,.30f),new Vector2(.96f,.96f),Color.Lerp(rarity,Color.white,.82f));
        CharacterPortrait(portrait,definition);
        var footer=Panel(card,"Growth",new Vector2(.04f,.04f),new Vector2(.96f,.29f),new Color(.16f,.14f,.22f));
        Label(footer,$"Lv. {owned.level}",18,new Vector2(0,0),new Vector2(.46f,1)).color=Color.white;
        Label(footer,$"강화 {owned.ascension}",18,new Vector2(.46f,0),Vector2.one).color=Color.white;
        if(data.loadout.characterId==owned.characterId)
        {
            var selected=Panel(card,"Selected",new Vector2(.45f,.79f),new Vector2(.95f,.95f),new Color(.18f,.16f,.27f,.9f));
            Label(selected,"선택 중",14,Vector2.zero,Vector2.one).color=Color.white;
        }
    }

    private void CharacterShopCard(Transform grid, CharacterDefinition definition, string purchase, string odds)
    {
        Color rarity = CharacterRarityColor(definition.rarity);
        var card = Panel(grid, "Shop Character " + definition.id, Vector2.zero, Vector2.one, rarity);
        var button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = card.GetComponent<Image>();
        button.onClick.AddListener(() =>
        {
            if (!saving && drawOverlay == null) ShopDetail(definition.id, true);
        });

        var portrait = Panel(card, "Portrait", new Vector2(.04f,.40f), new Vector2(.96f,.97f),
            Color.Lerp(rarity, Color.white, .55f));
        CharacterPortrait(portrait, definition);
        var footer = Panel(card, "Shop Info", new Vector2(.04f,.03f), new Vector2(.96f,.39f),
            new Color(.16f,.14f,.22f));
        Label(footer, definition.displayName, 18, new Vector2(.03f,.65f), new Vector2(.97f,1)).color = Color.white;
        Label(footer, Rarity(definition.rarity) + odds, 14, new Vector2(.03f,.36f), new Vector2(.97f,.65f)).color =
            Color.Lerp(rarity, Color.white, .45f);
        Label(footer, purchase, 15, new Vector2(.03f,0), new Vector2(.97f,.36f)).color = Color.white;
    }

    private void RelicIcon(Transform parent, RelicDefinition definition)
    {
        var rect = Rect(parent, "Relic Icon", new Vector2(.08f,.06f), new Vector2(.92f,.94f));
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
                case "relic_claw": assetName = "ClawRelic"; break;
                case "relic_bell": assetName = "BellRelic"; break;
                case "relic_tiger_fang": assetName = "TigerFangRelic"; break;
                case "relic_hunter_medal": assetName = "HunterMedalRelic"; break;
                case "relic_warm_scarf": assetName = "WarmScarfRelic"; break;
                case "relic_lucky_coin": assetName = "LuckyCoinRelic"; break;
                case "relic_catnip_pouch": assetName = "CatnipPouchRelic"; break;
                case "relic_moon_pendant": assetName = "MoonPendantRelic"; break;
                case "relic_lion_fang": assetName = "LionFangRelic"; break;
                case "relic_star_bell": assetName = "StarBellRelic"; break;
                case "relic_celestial_crown": assetName = "CelestialCrownRelic"; break;
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
            else Label(rect, "?", 28, Vector2.zero, Vector2.one);
        }
    }

    private void RelicCard(Transform grid, RelicDefinition definition, string name, string detail, UnityEngine.Events.UnityAction action)
    {
        var rarity = CharacterRarityColor(definition == null ? ItemRarity.Common : definition.rarity);
        var card = Panel(grid, "Relic Card " + name, Vector2.zero, Vector2.one, rarity);
        var button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = card.GetComponent<Image>();
        button.onClick.AddListener(() => { if (!saving && drawOverlay == null) action(); });
        var portrait = Panel(card, "Portrait", new Vector2(.04f,.38f), new Vector2(.96f,.97f), Color.Lerp(rarity, Color.white, .55f));
        RelicIcon(portrait, definition);
        var footer = Panel(card, "Info", new Vector2(.04f,.03f), new Vector2(.96f,.37f), new Color(.16f,.14f,.22f));
        Label(footer, name, 18, new Vector2(.02f,.55f), new Vector2(.98f,1)).color = Color.white;
        Label(footer, detail, 15, new Vector2(.02f,0), new Vector2(.98f,.55f)).color = Color.white;
    }

    private void CharacterPortrait(Transform parent,CharacterDefinition definition)
    {
        var rect=Rect(parent,"Character Image",new Vector2(.06f,.02f),new Vector2(.94f,.98f));
        if(definition!=null && definition.icon!=null)
        {
            var image=rect.gameObject.AddComponent<Image>();image.sprite=definition.icon;
            image.preserveAspect=true;image.raycastTarget=false;
        }
        else
        {
            if(sharedCharacterPortrait==null)sharedCharacterPortrait=Resources.Load<Texture2D>("CatStarterIsometric");
            if(sharedCharacterPortrait!=null)
            {
                var image=rect.gameObject.AddComponent<RawImage>();image.texture=sharedCharacterPortrait;image.raycastTarget=false;
                var fitter=rect.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode=AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio=(float)sharedCharacterPortrait.width/sharedCharacterPortrait.height;
            }
            else Label(rect,"?",36,Vector2.zero,Vector2.one);
        }
    }
}
