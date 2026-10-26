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
        var controls = MapDecoration(root, "Defense Controls", Vector2.zero, new Vector2(1,.13f), new Color(.31f,.23f,.17f));
        MapDecoration(controls, "Gold Trim", new Vector2(0,.96f), Vector2.one, new Color(.84f,.65f,.34f));
        var controlFace = MapDecoration(controls, "Cream Inlay", new Vector2(.015f,.04f), new Vector2(.985f,.94f), new Color(.98f,.91f,.77f));
        RoundCollectionPanel(controlFace);
        var instruction = Label(controls, "소환 버튼으로 고양이를 먼저 배치하세요", 17, new Vector2(.03f,.67f), new Vector2(.97f,.94f));
        instruction.color = new Color(.33f,.24f,.17f);
        var slotButtons = new UnityEngine.UI.Button[pads.Count];
        // 반투명 배경이 맵 입력을 차단하고, 바깥을 누르면 소환 창만 닫습니다.
        var shop = Panel(root, "Deployment Overlay", Vector2.zero, Vector2.one, new Color(.10f,.14f,.10f,.68f));
        var shopBody = PopupContent(shop, "Deployment Wood Frame", "Deployment Cream",
            new Vector2(.035f,.19f), new Vector2(.965f,.80f), new Color(.37f,.25f,.16f),
            () => shop.gameObject.SetActive(false), out var shopFrame);
        RoundCollectionPanel(shopFrame);
        var shopShadow = shopFrame.gameObject.AddComponent<Shadow>();
        shopShadow.effectColor = new Color(.08f,.05f,.03f,.55f);
        shopShadow.effectDistance = new Vector2(4,-7);
        DecorateCatPopup(shopBody);
        var ribbon = MapDecoration(shopBody, "Deployment Title Ribbon", new Vector2(.06f,.87f), new Vector2(.94f,.98f), new Color(.25f,.39f,.29f));
        RoundCollectionPanel(ribbon);
        Label(ribbon, "고양이 원정대 소환", 26, Vector2.zero, Vector2.one).color = new Color(1,.94f,.77f);
        var ledger = MapDecoration(shopBody, "Deployment Status", new Vector2(.06f,.75f), new Vector2(.94f,.85f), new Color(.91f,.81f,.62f));
        RoundCollectionPanel(ledger);
        var info = Label(ledger, "", 18, new Vector2(.02f,0), new Vector2(.98f,1));
        info.color = new Color(.29f,.21f,.15f);
        var hint = Label(shopBody, "고양이 선택 후 빈 잔디 타일에 배치하세요", 18, new Vector2(.04f,.66f), new Vector2(.96f,.74f));
        hint.color = new Color(.42f,.30f,.21f);
        map.GetComponent<Image>().raycastTarget = true;
        var choices = new UnityEngine.UI.Button[3];
        var definitions = new CharacterDefinition[3];
        for (int i = 0; i < 3; i++)
        {
            int index = i;
            definitions[i] = catalog.FindCharacter(team[i].characterId);
            var card = CollectionFrame(shopBody, "Deploy " + i, CharacterRarityColor(definitions[i].rarity));
            card.anchorMin = new Vector2(.035f + i * .32f,.24f);
            card.anchorMax = new Vector2(.325f + i * .32f,.64f);
            CharacterPortrait(Rect(card, "Cat", new Vector2(.05f,.28f), new Vector2(.95f,.87f)), definitions[i]);
            CollectionBadge(card, $"Lv.{team[i].level}", new Vector2(.08f,.86f), new Vector2(.60f,.97f), CharacterRarityColor(definitions[i].rarity));
            var price = MapDecoration(card, "Summon Cost", new Vector2(.06f,.06f), new Vector2(.94f,.25f), new Color(.96f,.81f,.48f));
            RoundCollectionPanel(price);
            Label(price, $"소환 {DungeonDefenseBattle.Cost(definitions[i].rarity)}", 20, Vector2.zero, Vector2.one).color = new Color(.29f,.20f,.12f);
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
        var summon = Button(controls, "소환", new Vector2(.76f,.13f), new Vector2(.96f,.66f), () =>
        {
            choosingPlacement=false;
            hint.text="고양이 선택 후 빈 잔디 타일에 배치하세요";
            shop.gameObject.SetActive(true);
        });
        CozyButtonStyle.Apply(summon, CozyButtonStyle.Peach);
        Button(shopBody, "닫기", new Vector2(.28f,.045f), new Vector2(.72f,.17f), () => shop.gameObject.SetActive(false));
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
        var updateBattleHud = BuildDefenseHud(root, engine, stage);
        engine.Initialize(stage, map, route, anchors, team, definitions,
            text => { info.text=text; updateBattleHud(); for(int i=0;i<3;i++) choices[i].interactable=engine.Coins>=DungeonDefenseBattle.Cost(definitions[i].rarity); wave.interactable=engine.CanStartWave; wave.GetComponentInChildren<Text>().text=engine.WaveNumber == 0 ? "전투 시작" : engine.CanStartWave ? "다음 웨이브" : "전투 중"; },
            message => { hint.text=message; instruction.text=message; }, won => { hint.text="결과 저장 중…"; DungeonDefenseFinished?.Invoke(won); });
        engine.WaveCleared = clearedWave =>
        {
            shop.gameObject.SetActive(false);
            choosingPlacement = false;
            var overlay = Panel(root, "Wave Clear Overlay", Vector2.zero, Vector2.one, new Color(0, 0, 0, .65f));
            Action dismiss = () => { if (overlay != null) RemoveUI(overlay.gameObject); };
            var body = PopupContent(overlay, "Wave Clear Frame", "Wave Clear Content",
                new Vector2(.07f, .32f), new Vector2(.93f, .70f), new Color(.56f, .37f, .17f), () => dismiss(), out _);
            DecorateCatPopup(body);
            Label(body, $"웨이브 {clearedWave} 클리어!", 32, new Vector2(.04f, .69f), new Vector2(.96f, .95f));
            Label(body, "고양이를 추가 소환하고\n다음 웨이브를 준비하세요", 23, new Vector2(.05f, .38f), new Vector2(.95f, .70f));
            Label(body, $"소환 재화 +{DungeonStageRules.WaveClearCoins}", 20, new Vector2(.05f, .25f), new Vector2(.95f, .40f)).color = new Color(.42f, .30f, .13f);
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
            DecorateCatPopup(body);
            Label(body,description,24,new Vector2(.05f,.20f),new Vector2(.95f,.96f));
            Button(body,"닫기",new Vector2(.25f,.04f),new Vector2(.75f,.17f),()=>close());
        };
        shop.SetAsLastSibling();
        shop.gameObject.SetActive(false);
    }

    private Action BuildDefenseHud(RectTransform root, DungeonDefenseBattle engine, int stage)
    {
        var area = Rect(root, "Defense HUD Safe Area", safeArea.anchorMin, safeArea.anchorMax);
        var panel = MapDecoration(area, "Battle Information", new Vector2(.025f,.89f), new Vector2(.975f,.985f), new Color(.19f,.24f,.18f,.83f));
        RoundCollectionPanel(panel);
        MapDecoration(panel, "Gold Trim", new Vector2(.03f,.04f), new Vector2(.97f,.065f), new Color(.86f,.69f,.40f,.8f));
        var waveText = Label(panel, "", 22, new Vector2(.02f,.43f), new Vector2(.33f,.93f));
        waveText.color = new Color(1,.94f,.77f);
        var phase = Label(panel, "", 16, new Vector2(.02f,.09f), new Vector2(.33f,.43f));
        phase.color = new Color(.83f,.88f,.76f);
        var health = Label(panel, "", 22, new Vector2(.35f,.43f), new Vector2(.68f,.93f));
        health.color = Color.white;
        var track = MapDecoration(panel, "Base HP Track", new Vector2(.37f,.18f), new Vector2(.66f,.35f), new Color(.10f,.13f,.10f,.9f));
        RoundCollectionPanel(track);
        var fill = MapDecoration(track, "Base HP Fill", Vector2.zero, Vector2.one, new Color(.48f,.82f,.46f));
        fill.pivot = new Vector2(0,.5f);
        RoundCollectionPanel(fill);
        var coins = Label(panel, "", 24, new Vector2(.70f,.42f), new Vector2(.98f,.93f));
        coins.color = new Color(1,.83f,.43f);
        Label(panel, "소환 재화", 16, new Vector2(.70f,.09f), new Vector2(.98f,.43f)).color = new Color(.94f,.87f,.71f);
        // 상단 표시 아래의 타일 선택과 드래그 입력을 가로채지 않습니다.
        foreach (var graphic in area.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
        int previousHealth = -1;
        return () =>
        {
            waveText.text = $"웨이브 {engine.WaveNumber}/{DungeonStageRules.WaveCount}";
            phase.text = $"1-{stage} · " + (engine.IsFinished ? "전투 종료" : engine.CanStartWave ? "소환 준비" : "전투 중");
            health.text = $"기지 HP {engine.BaseHealth}/{DungeonStageRules.BaseLives}";
            coins.text = engine.Coins.ToString("N0");
            if (previousHealth == engine.BaseHealth) return;
            float ratio = (float)engine.BaseHealth / DungeonStageRules.BaseLives;
            if (previousHealth < 0) fill.localScale = new Vector3(ratio,1,1);
            else HealthBarTween.Set(fill, ratio);
            fill.GetComponent<Image>().color = ratio <= .3f ? new Color(1,.37f,.32f) : new Color(.48f,.82f,.46f);
            previousHealth = engine.BaseHealth;
        };
    }
}
