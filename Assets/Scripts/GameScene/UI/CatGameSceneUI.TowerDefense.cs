using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    public event Action<bool> DungeonDefenseFinished;
    private RectTransform defenseRoot;
    private void CloseDefenseScreen()
    {
        if (defenseRoot != null) RemoveUI(defenseRoot.gameObject);
        defenseRoot = null;
    }

    public void BeginDungeonDefense(int stage, IList<OwnedCharacter> team)
    {
        if (stage < 1 || stage > 15 || stage > data.progress.dungeonClearedStage + 1 || team == null || team.Count != 3)
            throw new ArgumentException("서로 다른 고양이 3종을 선택해주세요.");
        var ids = new HashSet<string>();
        foreach (var cat in team)
            if (cat == null || !data.characters.Contains(cat) || !ids.Add(cat.characterId) || catalog.FindCharacter(cat.characterId) == null)
                throw new ArgumentException("보유 고양이 3종을 선택해주세요.");
        ClearPage();
        raidActive = true;
        CloseDefenseScreen();
        var root = Rect(safeArea.parent, "Dungeon Tower Defense", Vector2.zero, Vector2.one);
        defenseRoot = root;
        root.SetAsLastSibling();
        var map = MapDecoration(root, "Defense Map", Vector2.zero, Vector2.one, new Color(.48f, .64f, .30f));
        map.gameObject.AddComponent<RectMask2D>();
        var background = Rect(map, "Defense Garden", Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
        background.texture = Resources.Load<Texture2D>("GameUIArt/DungeonDefenseGarden");
        background.raycastTarget = false;
        var route = new[] { new Vector2(-.04f,.72f), new Vector2(.25f,.72f), new Vector2(.25f,.40f),
            new Vector2(.52f,.40f), new Vector2(.52f,.73f), new Vector2(.80f,.73f), new Vector2(.80f,.24f), new Vector2(1.04f,.24f) };
        var trail = Rect(map, "Monster Road", Vector2.zero, Vector2.one);
        trail.gameObject.AddComponent<CanvasRenderer>();
        trail.gameObject.AddComponent<DungeonMapTrail>().Initialize(route);
        var engine = root.gameObject.AddComponent<DungeonDefenseBattle>();
        int pendingSlot = -1;
        var slotLabels = new Text[8];
        var slotButtons = new UnityEngine.UI.Button[8];
        var shop = MapDecoration(root, "Deployment Popup", new Vector2(.04f,.20f), new Vector2(.96f,.65f), new Color(.22f,.18f,.15f,.98f));
        RoundCollectionPanel(shop);
        shop.GetComponent<Image>().raycastTarget = true;
        shop.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
        var info = Label(shop, "", 18, new Vector2(.03f,.90f), new Vector2(.97f,.99f));
        info.color = Color.white;
        var hint = Label(shop, "배치 칸을 누르면 고양이를 구매할 수 있어요", 18, new Vector2(0,.80f), new Vector2(1,.90f));
        hint.color = new Color(1,.92f,.73f);
        map.GetComponent<Image>().raycastTarget = true;
        var mapButton = map.gameObject.AddComponent<Button>();
        mapButton.transition = Selectable.Transition.None;
        mapButton.onClick.AddListener(() => { pendingSlot=-1; shop.gameObject.SetActive(true); });
        var choices = new UnityEngine.UI.Button[3];
        var definitions = new CharacterDefinition[3];
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            definitions[i] = catalog.FindCharacter(team[i].characterId);
            var card = CollectionFrame(shop, "Deploy " + i, CharacterRarityColor(definitions[i].rarity));
            card.anchorMin = new Vector2(.025f + i * .325f,.23f);
            card.anchorMax = new Vector2(.325f + i * .325f,.79f);
            CharacterPortrait(Rect(card, "Cat", new Vector2(.05f,.25f), new Vector2(.95f,.98f)), definitions[i]);
            Label(card, $"소환 {DungeonDefenseBattle.Cost(i)}", 18, new Vector2(0,0), new Vector2(1,.26f)).color = Color.white;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            choices[i] = button;
            button.onClick.AddListener(() =>
            {
                engine.Selected = index;
                if(pendingSlot >= 0 && engine.Place(pendingSlot))
                {
                    slotLabels[pendingSlot].text=""; slotButtons[pendingSlot].interactable=false;
                    shop.gameObject.SetActive(false);
                }
                else hint.text="맵의 빈 배치 칸을 먼저 눌러주세요";
            });
        }
        var wave = Button(shop, "웨이브 시작", new Vector2(.02f,.025f), new Vector2(.48f,.20f), () => { engine.StartWave(); if(!engine.CanStartWave) shop.gameObject.SetActive(false); });
        Button(shop, "닫기", new Vector2(.50f,.025f), new Vector2(.73f,.20f), () => shop.gameObject.SetActive(false));
        Button(shop, "포기", new Vector2(.75f,.025f), new Vector2(.98f,.20f), ExitRaid);
        var pads = new[] { new Vector2(.13f,.56f),new Vector2(.37f,.58f),new Vector2(.40f,.25f),new Vector2(.65f,.57f),
            new Vector2(.67f,.32f),new Vector2(.90f,.52f),new Vector2(.14f,.29f),new Vector2(.62f,.49f) };
        var anchors = new RectTransform[pads.Length];
        for (int i=0;i<pads.Length;i++)
        {
            int slot=i;
            var pad=MapOval(map,"Build Pad " + i,pads[i],new Vector2(.055f,.042f),new Color(.95f,.86f,.62f));
            pad.GetComponent<DungeonMapOval>().raycastTarget=true;
            var plus=Label(pad,"+",24,Vector2.zero,Vector2.one);
            var button=pad.gameObject.AddComponent<Button>();
            button.targetGraphic=pad.GetComponent<DungeonMapOval>();
            anchors[i]=Rect(map,"Tower " + i,pads[i],pads[i]);
            anchors[i].sizeDelta=new Vector2(85,100);
            anchors[i].pivot=new Vector2(.5f,.1f);
            slotLabels[i]=plus; slotButtons[i]=button;
            button.onClick.AddListener(() => { pendingSlot=slot; shop.gameObject.SetActive(true); hint.text="배치할 고양이를 선택하세요"; });
        }
        engine.Initialize(stage, map, route, anchors, team, definitions,
            text => { info.text=text; for(int i=0;i<3;i++) choices[i].interactable=engine.Coins>=DungeonDefenseBattle.Cost(i); wave.interactable=engine.CanStartWave; },
            message => { hint.text=message; }, won => { hint.text="결과 저장 중…"; DungeonDefenseFinished?.Invoke(won); });
        shop.SetAsLastSibling();
        shop.gameObject.SetActive(false);
    }
}
