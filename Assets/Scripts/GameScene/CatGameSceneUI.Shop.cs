using System;
using UnityEngine;

// 상점 화면은 별도 파일로 관리하며 구매/저장은 세션에 요청합니다.
public partial class CatGameSceneUI
{
    public event Action<string> CharacterPurchaseRequested;
    public event Action RelicDrawRequested;
    public event Action CharacterDrawRequested;
    private string lastCharacterDrawResult="캐릭터를 뽑으면 결과가 여기에 표시됩니다.";
    private string lastDrawResult = "유물을 뽑으면 결과가 여기에 표시됩니다.";
    private UnityEngine.UI.Text lastDrawLabel;
    private bool characterShop = true;

    private void Shop()
    {
        var grid = Grid(characterShop ? "캐릭터 상점" : "유물 뽑기", .34f);
        var charactersTab = Button(page, "캐릭터", new Vector2(0,.79f), new Vector2(.48f,.88f), () => SelectShop(true));
        var relicsTab = Button(page, "유물", new Vector2(.52f,.79f), new Vector2(1,.88f), () => SelectShop(false));
        var inactive = new Color(.66f,.62f,.77f);
        charactersTab.image.color = characterShop ? accent : inactive;
        relicsTab.image.color = characterShop ? inactive : accent;
        if(characterShop)CharacterDrawPanel();
        int count = 0;
        if (characterShop) foreach (var definition in catalog.AllCharacters())
        {
            if ((definition.shop == null || !definition.shop.CanSell) && !definition.inCharacterDraw) continue;
            var item = definition;
            bool owned = data.characters.Exists(x => x != null && x.characterId == item.id);
            var drawPool=catalog.CharacterDrawPool();
            int tierCount=drawPool.FindAll(x=>x.rarity==item.rarity).Count;
            string odds=item.inCharacterDraw && catalog.characterDraw!=null && catalog.characterDraw.Validate(drawPool)?$" · {catalog.characterDraw.Percent(item.rarity)/(double)tierCount:0.###}%":"";
            string purchase=owned?"보유 · 중복은 조각":item.shop!=null && item.shop.CanSell?$"최초 해금 {item.shop.goldPrice:N0} 골드":"뽑기로 획득";
            Button(grid, $"{item.displayName}\n{Rarity(item.rarity)}{odds}\n" + purchase,
                Vector2.zero, Vector2.one, () => ShopDetail(item.id, true));
            count++;
        }
        if (!characterShop)
        {
            RelicDrawPanel(grid);
            return;
        }
        if (count == 0) Label(page, characterShop ? "판매 중인 캐릭터가 없습니다." : "판매 중인 유물이 없습니다.", 25, new Vector2(0,.3f), new Vector2(1,.7f));
    }

    private void CharacterDrawPanel()
    {
        var rules=catalog.characterDraw;
        bool valid=rules!=null && rules.Validate(catalog.CharacterDrawPool());
        Label(page,rules==null?"뽑기 설정 없음":$"Common {rules.commonPercent}% · Epic {rules.epicPercent}%\nUnique {rules.uniquePercent}% · Legendary {rules.legendaryPercent}%",23,new Vector2(0,.64f),new Vector2(1,.76f));
        Label(page,rules==null?"":$"최초 획득: 골드 해금 또는 뽑기\n중복: 해당 캐릭터 조각 +{rules.duplicateFragments} · 조각으로 강화",19,new Vector2(0,.54f),new Vector2(1,.64f));
        var button=Button(page,valid?$"캐릭터 1회 뽑기 · {rules.goldCost:N0} 골드":"뽑기 설정 확인 필요",new Vector2(0,.44f),new Vector2(1,.53f),()=>CharacterDrawRequested?.Invoke());
        button.interactable=valid;
        lastDrawLabel=Label(page,lastCharacterDrawResult,21,new Vector2(0,.35f),new Vector2(1,.44f));
    }

    private void RelicDrawPanel(Transform grid)
    {
        var rules=catalog.relicDraw;
        var pool=catalog.RelicDrawPool();
        bool valid=rules!=null && rules.Validate(pool);
        string odds=rules==null?"뽑기 설정 없음":$"Common {rules.commonPercent}% · Epic {rules.epicPercent}%\nUnique {rules.uniquePercent}% · Legendary {rules.legendaryPercent}%";
        Label(page,odds,23,new Vector2(0,.64f),new Vector2(1,.76f));
        Label(page,rules==null?"":$"같은 등급 안에서는 동일 확률\n중복 유물: 해당 유물 조각 +{rules.duplicateFragments}",19,new Vector2(0,.54f),new Vector2(1,.64f));
        var draw=Button(page,valid?$"유물 1회 뽑기 · {rules.goldCost:N0} 골드":"뽑기 설정 확인 필요",new Vector2(0,.44f),new Vector2(1,.53f),()=>RelicDrawRequested?.Invoke());
        draw.interactable=valid;
        lastDrawLabel=Label(page,lastDrawResult,21,new Vector2(0,.35f),new Vector2(1,.44f));
        foreach(var definition in pool)
        {
            var item=definition;
            int count=pool.FindAll(x=>x.rarity==item.rarity).Count;
            string chance=valid?$"{rules.Percent(item.rarity)/(double)count:0.###}%":"확률 미설정";
            Button(grid,$"{item.displayName}\n{Rarity(item.rarity)} · {chance}",Vector2.zero,Vector2.one,()=>ShowStatus(item.description));
        }
    }

    public void SetDrawResult(ContentDrawResult result)
    {
        if(result==null)return;
        string message=$"최근 결과: [{Rarity(result.rarity)}] {result.name}\n"+(result.duplicate?$"중복 · 조각 +{result.fragments}":result.character?"새 캐릭터 획득!":"새 유물 획득!");
        if(result.character)lastCharacterDrawResult=message;else lastDrawResult=message;
        if(lastDrawLabel!=null && characterShop==result.character)lastDrawLabel.text=message;
    }

    private void SelectShop(bool character)
    {
        if (saving) return;
        characterShop = character;
        Show("상점");
    }

    private void ShopDetail(string id, bool character)
    {
        characterShop = character;
        foreach (Transform child in page) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        var cat = character ? catalog.FindCharacter(id) : null;
        var relic = character ? null : catalog.FindRelic(id);
        if (cat == null && relic == null) { Show("상점"); return; }
        var offer = character ? cat.shop : relic.shop;
        bool owned = character ? data.characters.Exists(x => x != null && x.characterId == id) :
            data.relics.Exists(x => x != null && x.relicId == id);
        string name = character ? cat.displayName : relic.displayName;
        string description = character ? cat.description : relic.description;
        string rarity = Rarity(character ? cat.rarity : relic.rarity);
        Button(page, "목록", new Vector2(0,.92f), new Vector2(.24f,1), () => Show("상점"));
        Label(page, name, 30, new Vector2(.26f,.92f), Vector2.one);
        Label(page, $"{rarity}\n{description}\n\n획득 레벨: 1\n획득 후 {(character ? "캐릭터 메뉴에서 선택" : "유물 메뉴에서 장착")}하세요.",
            25, new Vector2(.04f,.3f), new Vector2(.96f,.85f));
        bool available = offer != null && offer.CanSell;
        string title = owned ? "보유 중 · 뽑기로 조각 획득" : available ? $"{offer.goldPrice:N0} 골드로 구매" : "판매 종료";
        var purchase = Button(page, title, new Vector2(0,.08f), new Vector2(1,.2f), () =>
        {
            if (character) CharacterPurchaseRequested?.Invoke(id);

        });
        purchase.interactable = character && available && !owned;
    }

    public void RefreshShop()
    {
        string message = status.text;
        Show("상점");
        status.text = message;
    }
}
