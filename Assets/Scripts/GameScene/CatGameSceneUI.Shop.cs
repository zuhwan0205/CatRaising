using System;
using UnityEngine;

// 상점 화면은 별도 파일로 관리하며 구매/저장은 세션에 요청합니다.
public partial class CatGameSceneUI
{
    public event Action<string> CharacterPurchaseRequested;
    public event Action<string> RelicPurchaseRequested;
    private bool characterShop = true;

    private void Shop()
    {
        var grid = Grid(characterShop ? "캐릭터 상점" : "유물 상점", .75f);
        var charactersTab = Button(page, "캐릭터", new Vector2(0,.79f), new Vector2(.48f,.88f), () => SelectShop(true));
        var relicsTab = Button(page, "유물", new Vector2(.52f,.79f), new Vector2(1,.88f), () => SelectShop(false));
        var inactive = new Color(.66f,.62f,.77f);
        charactersTab.image.color = characterShop ? accent : inactive;
        relicsTab.image.color = characterShop ? inactive : accent;
        int count = 0;
        if (characterShop) foreach (var definition in catalog.AllCharacters())
        {
            if (definition.shop == null || !definition.shop.CanSell) continue;
            var item = definition;
            bool owned = data.characters.Exists(x => x != null && x.characterId == item.id);
            Button(grid, $"캐릭터 · {item.displayName}\n{Rarity(item.rarity)}\n" + (owned ? "보유 중" : $"{item.shop.goldPrice:N0} 골드"),
                Vector2.zero, Vector2.one, () => ShopDetail(item.id, true));
            count++;
        }
        if (!characterShop) foreach (var definition in catalog.AllRelics())
        {
            if (definition.shop == null || !definition.shop.CanSell) continue;
            var item = definition;
            bool owned = data.relics.Exists(x => x != null && x.relicId == item.id);
            Button(grid, $"유물 · {item.displayName}\n{Rarity(item.rarity)}\n" + (owned ? "보유 중" : $"{item.shop.goldPrice:N0} 골드"),
                Vector2.zero, Vector2.one, () => ShopDetail(item.id, false));
            count++;
        }
        if (count == 0) Label(page, characterShop ? "판매 중인 캐릭터가 없습니다." : "판매 중인 유물이 없습니다.", 25, new Vector2(0,.3f), new Vector2(1,.7f));
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
        string title = owned ? "보유 중" : available ? $"{offer.goldPrice:N0} 골드로 구매" : "판매 종료";
        var purchase = Button(page, title, new Vector2(0,.08f), new Vector2(1,.2f), () =>
        {
            if (character) CharacterPurchaseRequested?.Invoke(id);
            else RelicPurchaseRequested?.Invoke(id);
        });
        purchase.interactable = available && !owned;
    }

    public void RefreshShop()
    {
        string message = status.text;
        Show("상점");
        status.text = message;
    }
}
