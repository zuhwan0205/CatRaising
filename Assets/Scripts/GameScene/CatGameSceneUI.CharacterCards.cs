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
