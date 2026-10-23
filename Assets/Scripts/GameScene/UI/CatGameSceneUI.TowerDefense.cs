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
        var map = MapDecoration(root, "Defense Map", new Vector2(0,.13f), Vector2.one, new Color(.48f, .64f, .30f));
        map.gameObject.AddComponent<RectMask2D>();
        var background = Rect(map, "Defense Garden", Vector2.zero, Vector2.one).gameObject.AddComponent<RawImage>();
        background.raycastTarget = false;
        var board=root.gameObject.AddComponent<DefenseTileBoard>();
        board.Build(background);
        var route=board.Route;
        var pads=new List<Vector2>();
        for(int y=0;y<DefenseTileBoard.Height;y++)
            for(int x=0;x<DefenseTileBoard.Width;x++)
                if(!board.Road.Contains(new Vector2Int(x,y))) pads.Add(DefenseTileBoard.Center(new Vector2Int(x,y)));
        var engine = root.gameObject.AddComponent<DungeonDefenseBattle>();
        bool choosingPlacement = false;
        var controls = MapDecoration(root, "Defense Controls", Vector2.zero, new Vector2(1,.13f), new Color(.19f,.23f,.16f));
        var instruction = Label(controls, "소환 버튼으로 고양이를 먼저 배치하세요", 17, new Vector2(.03f,.67f), new Vector2(.97f,.98f));
        instruction.color = new Color(1,.94f,.80f);
        var slotButtons = new UnityEngine.UI.Button[pads.Count];
        var shop = MapDecoration(root, "Deployment Popup", new Vector2(.04f,.20f), new Vector2(.96f,.65f), new Color(.22f,.18f,.15f,.98f));
        RoundCollectionPanel(shop);
        shop.GetComponent<Image>().raycastTarget = true;
        shop.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;
        var info = Label(shop, "", 18, new Vector2(.03f,.90f), new Vector2(.97f,.99f));
        info.color = Color.white;
        var hint = Label(shop, "고양이 선택 후 빈 잔디 타일에 배치하세요", 18, new Vector2(0,.80f), new Vector2(1,.90f));
        hint.color = new Color(1,.92f,.73f);
        map.GetComponent<Image>().raycastTarget = true;
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
            Label(card, $"소환 {DungeonDefenseBattle.Cost(definitions[i].rarity)}", 18, new Vector2(0,0), new Vector2(1,.26f)).color = Color.white;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            choices[i] = button;
            button.onClick.AddListener(() =>
            {
                engine.Selected = index;
                choosingPlacement = true;
                instruction.text = definitions[index].displayName + " · 빈 잔디 타일을 누르세요";
                shop.gameObject.SetActive(false);
            });
        }
        var wave = Button(controls, "전투 시작", new Vector2(.04f,.13f), new Vector2(.72f,.66f), engine.StartWave);
        Button(controls, "소환", new Vector2(.76f,.13f), new Vector2(.96f,.66f), () =>
        {
            choosingPlacement=false;
            hint.text="고양이 선택 후 빈 잔디 타일에 배치하세요";
            shop.gameObject.SetActive(true);
        });
        Button(shop, "닫기", new Vector2(.12f,.025f), new Vector2(.48f,.20f), () => shop.gameObject.SetActive(false));
        Button(shop, "포기", new Vector2(.52f,.025f), new Vector2(.88f,.20f), ExitRaid);
        var anchors = new RectTransform[pads.Count];
        Canvas.ForceUpdateCanvases();
        for (int i=0;i<pads.Count;i++)
        {
            int slot=i;
            var pad=MapDecoration(map,"Build Tile " + i,pads[i]-new Vector2(.05f,1f/28),pads[i]+new Vector2(.05f,1f/28),Color.clear);
            pad.GetComponent<Image>().raycastTarget=true;
            var button=pad.gameObject.AddComponent<Button>();
            button.targetGraphic=pad.GetComponent<Image>();
            anchors[i]=Rect(map,"Tower " + i,pads[i],pads[i]);
            anchors[i].sizeDelta=new Vector2(map.rect.width/10f*1.45f,map.rect.height/14f*1.75f);
            anchors[i].pivot=new Vector2(.5f,.1f);
            slotButtons[i]=button;
            button.onClick.AddListener(() =>
            {
                if(!choosingPlacement) { instruction.text="소환 버튼에서 고양이를 먼저 선택하세요"; return; }
                if(engine.Place(slot))
                {
                    slotButtons[slot].interactable=false;
                    choosingPlacement=false;
                    instruction.text="배치 완료 · 전투 시작 또는 추가 소환이 가능합니다";
                }
            });
        }
        engine.TowerMoved = (from, to) =>
        {
            slotButtons[from].interactable = true;
            slotButtons[to].interactable = false;
        };
        engine.Initialize(stage, map, route, anchors, team, definitions,
            text => { info.text=text; for(int i=0;i<3;i++) choices[i].interactable=engine.Coins>=DungeonDefenseBattle.Cost(definitions[i].rarity); wave.interactable=engine.CanStartWave; wave.GetComponentInChildren<Text>().text=engine.WaveNumber == 0 ? "전투 시작" : engine.CanStartWave ? "다음 웨이브" : "전투 중"; },
            message => { hint.text=message; instruction.text=message; }, won => { hint.text="결과 저장 중…"; DungeonDefenseFinished?.Invoke(won); });
        engine.WaveCleared = clearedWave =>
        {
            shop.gameObject.SetActive(false);
            choosingPlacement = false;
            var overlay = Panel(root, "Wave Clear Overlay", Vector2.zero, Vector2.one, new Color(0, 0, 0, .65f));
            Action dismiss = () => { if (overlay != null) RemoveUI(overlay.gameObject); };
            var body = PopupContent(overlay, "Wave Clear Frame", "Wave Clear Content",
                new Vector2(.07f, .32f), new Vector2(.93f, .70f), new Color(.56f, .37f, .17f), () => dismiss(), out _);
            Label(body, $"웨이브 {clearedWave} 클리어!", 32, new Vector2(.04f, .69f), new Vector2(.96f, .95f));
            Label(body, "고양이를 추가 소환하고\n다음 웨이브를 준비하세요", 23, new Vector2(.05f, .38f), new Vector2(.95f, .70f));
            Label(body, "소환 재화 +35", 20, new Vector2(.05f, .25f), new Vector2(.95f, .40f)).color = new Color(.42f, .30f, .13f);
            Button(body, "소환하기", new Vector2(.05f, .06f), new Vector2(.61f, .23f), () =>
            {
                dismiss();
                hint.text = "고양이 선택 후 빈 잔디 타일에 배치하세요";
                shop.gameObject.SetActive(true);
            });
            Button(body, "닫기", new Vector2(.65f, .06f), new Vector2(.95f, .23f), () => dismiss());
        };
        RectTransform statsPopup = null;
        engine.InspectCat = description =>
        {
            if(statsPopup != null) RemoveUI(statsPopup.gameObject);
            statsPopup=Panel(root,"Cat Stats Overlay",Vector2.zero,Vector2.one,new Color(0,0,0,.45f));
            Action close=()=> { if(statsPopup!=null) RemoveUI(statsPopup.gameObject); statsPopup=null; };
            var body=PopupContent(statsPopup,"Cat Stats Frame","Cat Stats",new Vector2(.10f,.27f),new Vector2(.90f,.75f),new Color(.43f,.29f,.17f),()=>close(),out _);
            Label(body,description,24,new Vector2(.05f,.20f),new Vector2(.95f,.96f));
            Button(body,"닫기",new Vector2(.25f,.04f),new Vector2(.75f,.17f),()=>close());
        };
        shop.SetAsLastSibling();
        shop.gameObject.SetActive(false);
    }
}
