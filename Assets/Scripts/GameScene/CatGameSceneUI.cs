using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public partial class CatGameSceneUI : MonoBehaviour
{
    private CatGameCatalog catalog;
    private Font uiFont;
    private bool preview;
    public event Action SaveRequested;
    public event Action<OwnedCharacter> CharacterSelected;
    public event Action<OwnedRelic> RelicSelected;
    public event Action<OwnedCharacter> CharacterLevelUpRequested;
    public event Action<OwnedRelic> RelicLevelUpRequested;
    private bool saving;
    private BackendUserData data;
    private RectTransform page;
    private RectTransform safeArea;
    private Text status;
    private Text wallet;
    public event Action<bool> MovementModeChanged;
    public event Action<Vector2> StickChanged;
    public event Action<bool> AdventureVisibilityChanged;
    private RenderTexture fieldTexture;
    private bool automatic = true;
    private FieldJoystick joystick;
    private Text combatLabel;
    private Text relicEffectLabel;
    private Text playerHpLabel;
    private string playerHpText = "HP 준비 중";
    private string combatMessage = "자동 공격 준비 중";
    private Button[] tabs;
    private Rect lastSafeArea;
    private Vector2 lastScreen;
    private readonly Color ink = new Color(0.20f, 0.18f, 0.29f);
    private readonly Color accent = new Color(0.40f, 0.29f, 0.85f);
    private readonly Color cream = new Color(0.98f, 0.96f, 0.92f);

    public void Build(BackendUserData playerData, CatGameCatalog definitions, Font font, bool isPreview, RenderTexture fieldOutput)
    {
        data = playerData;
        fieldTexture = fieldOutput;
        catalog = definitions;
        uiFont = font;
        preview = isPreview;
        if (uiFont == null) uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" }, 24);
        if (Camera.main == null)
        {
            var camera = new GameObject("Game Camera", typeof(Camera)).GetComponent<Camera>();
            camera.transform.SetParent(transform, false);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = cream;
        }
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("Game EventSystem", typeof(EventSystem));
            events.transform.SetParent(transform, false);
#if ENABLE_INPUT_SYSTEM
            events.AddComponent<InputSystemUIInputModule>();
#else
            events.AddComponent<StandaloneInputModule>();
#endif
        }
        var canvas = new GameObject("Game Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.transform.SetParent(transform, false);
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 1280);
        scaler.matchWidthOrHeight = 0.5f;
        Panel(canvas.transform, "Background", Vector2.zero, Vector2.one, cream);
        safeArea = Rect(canvas.transform, "Safe Area", Vector2.zero, Vector2.one);
        ApplySafeArea();
        var header = Panel(safeArea, "Account Header", new Vector2(0.04f, .88f), new Vector2(.96f, .98f), Color.white);
        Label(header, "고양이 키우기", 28, new Vector2(.04f,.48f), new Vector2(.62f,.95f));
        wallet = Label(header, "", 22, new Vector2(.04f,.04f), new Vector2(.96f,.48f));
        Button(header, "저장", new Vector2(.73f,.53f), new Vector2(.96f,.92f), () => SaveRequested?.Invoke());
        page = Rect(safeArea, "Page", new Vector2(.04f,.16f), new Vector2(.96f,.86f));
        status = Label(safeArea, "", 18, new Vector2(.05f,.10f), new Vector2(.95f,.15f));
        var navigation = Rect(safeArea, "Navigation", new Vector2(.04f,.02f), new Vector2(.96f,.09f));
        string[] titles = { "모험", "상점", "캐릭터", "유물" };
        tabs = new Button[titles.Length];
        for (int i = 0; i < titles.Length; i++)
        {
            string title = titles[i];
            tabs[i] = Button(navigation, title, new Vector2(i*.25f,0), new Vector2(i*.25f+.235f,1), () => Show(title));
        }
        Show("모험");
    }

    private void Update() { if (safeArea != null) ApplySafeArea(); }
    private void ApplySafeArea()
    {
        var area = Screen.safeArea;
        var size = new Vector2(Screen.width, Screen.height);
        if (size.x <= 0 || size.y <= 0 || (area == lastSafeArea && size == lastScreen)) return;
        safeArea.anchorMin = area.position / size;
        safeArea.anchorMax = (area.position + area.size) / size;
        lastSafeArea = area; lastScreen = size;
    }

    private void Show(string tab)
    {
        if (saving || drawOverlay!=null) return;
        if (joystick != null) joystick.ResetInput();
        joystick = null;
        combatLabel = null;
        relicEffectLabel = null;
        playerHpLabel = null;
        foreach (Transform child in page) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        wallet.text = $"Lv. {data.progress.accountLevel}    골드 {data.wallet.gold:N0}    다이아 {data.wallet.gems:N0}";
        status.text = preview ? "씬 미리보기 · 서버 저장 없음" : "";
        string[] names = { "모험", "상점", "캐릭터", "유물" };
        for (int i = 0; i < tabs.Length; i++) tabs[i].image.color = names[i] == tab ? accent : new Color(.66f,.62f,.77f);
        AdventureVisibilityChanged?.Invoke(tab == "모험");
        if (tab == "모험") Adventure();
        else if (tab == "상점") Shop();
        else if (tab == "캐릭터") Characters();
        else Relics();
    }

    private void Adventure()
    {
        var field = Panel(page,"Field",Vector2.zero,Vector2.one,new Color(.30f,.42f,.30f));
        var image = Rect(field,"Field Camera View",Vector2.zero,Vector2.one).gameObject.AddComponent<RawImage>();
        image.texture=fieldTexture; image.raycastTarget=false;
        var fit=image.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent; fit.aspectRatio=.72f;
        Label(field,$"STAGE {data.progress.currentStage:00} · 2.5D 필드",25,new Vector2(.03f,.91f),new Vector2(.97f,1));
        var mode=Button(field,"",new Vector2(.52f,.80f),new Vector2(.96f,.89f),()=>{});
        var modeText=mode.GetComponentInChildren<Text>();
        modeText.text=automatic?"자동 이동 ON":"수동 이동";
        playerHpLabel=Label(field,playerHpText,22,new Vector2(.02f,.80f),new Vector2(.50f,.89f));
        var equippedNames = new System.Collections.Generic.List<string>();
        foreach (string id in data.loadout.relicIds)
        {
            var definition=catalog.FindRelic(id);
            bool owned=data.relics.Exists(x=>x!=null && x.relicId==id);
            equippedNames.Add(!owned?$"{id} (미보유)":definition==null?$"{id} (설정 없음)":definition.displayName);
        }
        string equippedText=equippedNames.Count==0?"장착 유물 없음 · 유물 메뉴에서 장착하세요":"장착: "+string.Join(" / ",equippedNames);
        Label(field,equippedText,18,new Vector2(.02f,.71f),new Vector2(.98f,.79f));
        relicEffectLabel=Label(field,"유물 발동 대기",20,new Vector2(.02f,.32f),new Vector2(.98f,.41f));
        relicEffectLabel.color=new Color(.9f,1,.65f);
        var pad=Panel(field,"Movement Joystick",new Vector2(.04f,.05f),new Vector2(.34f,.27f),new Color(.2f,.2f,.3f,.45f));
        var handle=Panel(pad,"Handle",new Vector2(.35f,.35f),new Vector2(.65f,.65f),new Color(1,1,1,.8f));
        handle.GetComponent<Image>().raycastTarget=false;
        joystick=pad.gameObject.AddComponent<FieldJoystick>(); joystick.Handle=handle;
        joystick.InputChanged=value=>StickChanged?.Invoke(value);
        pad.gameObject.SetActive(!automatic);
        mode.onClick.AddListener(()=>
        {
            if(saving)return;
            automatic=!automatic;
            joystick.ResetInput();
            pad.gameObject.SetActive(!automatic);
            modeText.text=automatic?"자동 이동 ON":"수동 이동";
            MovementModeChanged?.Invoke(automatic);
        });
        combatLabel = Label(field,combatMessage,20,new Vector2(.36f,.18f),new Vector2(.98f,.30f));
        Label(field,"공격은 두 모드 모두 자동\n수동 이동: WASD / 방향키 / 조이스틱",18,new Vector2(.36f,.03f),new Vector2(.98f,.17f));
        MovementModeChanged?.Invoke(automatic);
    }
    // 보유 목록은 스크롤 가능하며 시안의 3열 배치를 사용합니다.
    private Transform Grid(string title, float top = .87f)
    {
        Label(page, title, 32, new Vector2(0,.90f), Vector2.one);
        var viewport = Panel(page, "Inventory Viewport", new Vector2(0,.02f), new Vector2(1,top), new Color(.94f,.92f,.97f));
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Rect(viewport, "Inventory Items", new Vector2(0,1), Vector2.one);
        content.pivot = new Vector2(.5f,1);
        var grid = content.gameObject.AddComponent<GridLayoutGroup>();
        Canvas.ForceUpdateCanvases();
        float width = viewport.rect.width;
        grid.cellSize = new Vector2(Mathf.Max(80,(width-48)/3), 155);
        grid.spacing = new Vector2(12,12);
        grid.padding = new RectOffset(12,12,12,12);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 3;
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport; scroll.content = content;
        scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped;
        return content;
    }

    private void Characters()
    {
        var grid = Grid("캐릭터  ·  " + data.characters.Count);
        foreach (var owned in data.characters)
        {
            if (owned == null) continue;
            var character = owned;
            CharacterCard(grid, character, catalog.FindCharacter(character.characterId));
        }
    }

    private string CharacterName(string id)
    {
        var definition = catalog.FindCharacter(id);
        return definition == null ? id : definition.displayName;
    }

    private void CharacterDetail(OwnedCharacter owned)
    {
        if (joystick != null) joystick.ResetInput();
        joystick = null;
        foreach (Transform child in page) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        Button(page, "목록", new Vector2(0,.92f), new Vector2(.24f,1), () => Show("캐릭터"));
        Label(page, CharacterName(owned.characterId), 30, new Vector2(.26f,.92f), Vector2.one);
        var portrait = Panel(page, "Character Portrait", new Vector2(0,.49f), new Vector2(1,.88f), new Color(.94f,.87f,.77f));
        CharacterPortrait(portrait, catalog.FindCharacter(owned.characterId));
        var definition = catalog.FindCharacter(owned.characterId);
        string details = $"Lv. {owned.level}  ·  돌파 {owned.ascension}  ·  조각 {owned.fragments}\n";
        if (definition != null)
        {
            var b = definition.baseStats; var g = definition.statsPerLevel; int n = Mathf.Max(0,owned.level-1);
            details += $"{Rarity(definition.rarity)}  ·  기본 성장 스탯\nHP {b.maxHealth+g.maxHealth*n:0.#}    공격력 {b.attack+g.attack*n:0.#}\n방어력 {b.defense+g.defense*n:0.#}    공격 속도 {b.attackSpeed+g.attackSpeed*n:0.##}\n공격 방식: {definition.attackId}";
        }
        else details += "캐릭터 공통 설정을 연결해주세요.";
        Label(page, details, 23, new Vector2(0,.19f), new Vector2(1,.47f));
        Button(page, "이 고양이 선택 · 저장", new Vector2(0,.09f), new Vector2(1,.17f), () => CharacterSelected?.Invoke(owned));
        long fragmentCost=0;
        bool canGrow=definition?.fragmentGrowth!=null && definition.fragmentGrowth.TryGetCost(owned.level,out fragmentCost);
        var grow=Button(page,canGrow?$"Lv. {owned.level} → {owned.level+1} · 조각 {fragmentCost:N0}개":"최대 레벨 / 성장 설정 없음",Vector2.zero,new Vector2(1,.07f),()=>CharacterLevelUpRequested?.Invoke(owned));
        grow.interactable=canGrow;
    }

    private void Relics()
    {
        if (data.relics.Count == 0)
        {
            Label(page, "유물", 32, new Vector2(0,.90f), Vector2.one);
            Label(page, "아직 보유한 유물이 없어요.\n유물을 획득하면 이곳에 표시됩니다.", 25, new Vector2(.04f,.30f), new Vector2(.96f,.70f));
            return;
        }
        var grid = Grid("유물  ·  " + data.relics.Count);
        foreach (var owned in data.relics)
        {
            if (owned == null) continue;
            var relic = owned;
            var definition = catalog.FindRelic(relic.relicId);
            string name = definition == null ? relic.relicId : definition.displayName;
            string equipped = data.loadout.relicIds.Contains(relic.relicId) ? "\n장착 중" : "";
            Button(grid, name + $"\nLv. {relic.level}" + equipped, Vector2.zero, Vector2.one, () => RelicDetail(relic));
        }
    }

    private void RelicDetail(OwnedRelic owned)
    {
        foreach (Transform child in page) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
        var definition = catalog.FindRelic(owned.relicId);
        Button(page, "목록", new Vector2(0,.92f), new Vector2(.24f,1), () => Show("유물"));
        Label(page, definition == null ? owned.relicId : definition.displayName, 30, new Vector2(.26f,.92f), Vector2.one);
        string description = definition == null ? "유물 공통 설정을 연결해주세요." :
            $"{Rarity(definition.rarity)} · Lv. {owned.level} · 조각 {owned.fragments}\n{definition.description}\n{definition.trigger} · 확률 {definition.chance:P0}\n효과 {definition.effectValue+definition.effectPerLevel*Math.Max(0,owned.level-1):0.#} · 재사용 {definition.cooldownSeconds:0.#}초";
        Label(page, description, 25, new Vector2(.04f,.25f), new Vector2(.96f,.84f));
        bool equipped = data.loadout.relicIds.Contains(owned.relicId);
        var button = Button(page, equipped ? "유물 해제 · 저장" : "유물 장착 · 저장 (최대 3개)", new Vector2(0,.08f), new Vector2(1,.20f), () => RelicSelected?.Invoke(owned));
        button.interactable = definition != null || equipped;
        LevelUpButton(definition?.growth, owned.level, Vector2.zero, new Vector2(1,.07f), () => RelicLevelUpRequested?.Invoke(owned));
    }

    private void LevelUpButton(LevelUpRules rules, int level, Vector2 min, Vector2 max, UnityAction action)
    {
        long cost=0;
        bool available=rules!=null && rules.TryGetCost(level,out cost);
        string title=available?$"Lv. {level} → {level+1} · {cost:N0} 골드":rules!=null && level>=rules.maxLevel?"최대 레벨":"성장 설정 없음";
        var button=Button(page,title,min,max,action);
        button.interactable=available;
    }

    public void RefreshCharacterDetail(OwnedCharacter owned) { CharacterDetail(owned); }
    public void RefreshRelicDetail(OwnedRelic owned) { RelicDetail(owned); }

    public void RefreshRelics() { string message = status.text; Show("유물"); status.text = message; }

    public void RefreshWallet()
    {
        if (wallet != null) wallet.text = $"Lv. {data.progress.accountLevel}    골드 {data.wallet.gold:N0}    다이아 {data.wallet.gems:N0}";
    }

    public void SetBusy(bool value) { saving = value; }
    public void ShowCombatMessage(string message)
    {
        combatMessage = message;
        if (combatLabel != null) combatLabel.text = message;
    }
    public void ShowRelicMessage(string message)
    {
        if (relicEffectLabel != null) relicEffectLabel.text="최근 유물 효과: "+message;
    }
    public void ShowPlayerHealth(double hp,double maximum)
    {
        playerHpText=$"HP {hp:0.#} / {maximum:0.#}";
        if(playerHpLabel!=null)playerHpLabel.text=playerHpText;
    }
    public void ShowStatus(string message) { status.text = message; }
    private string Rarity(ItemRarity value) { return new[] { "Common", "Epic", "Unique", "Legendary" }[Mathf.Clamp((int)value,0,3)]; }
    private RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent,false); rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=rect.offsetMax=Vector2.zero;
        return rect;
    }
    private RectTransform Panel(Transform parent,string name,Vector2 min,Vector2 max,Color color)
    { var rect=Rect(parent,name,min,max); rect.gameObject.AddComponent<Image>().color=color; return rect; }
    private Text Label(Transform parent,string value,int size,Vector2 min,Vector2 max)
    {
        var text=Rect(parent,"Label",min,max).gameObject.AddComponent<Text>();
        text.font=uiFont; text.text=value; text.fontSize=size; text.color=ink;
        text.alignment=TextAnchor.MiddleCenter; text.raycastTarget=false;
        text.resizeTextForBestFit=true; text.resizeTextMinSize=12; text.resizeTextMaxSize=size;
        return text;
    }
    private Button Button(Transform parent,string title,Vector2 min,Vector2 max,UnityAction action)
    {
        var rect=Panel(parent,title,min,max,accent);
        var button=rect.gameObject.AddComponent<Button>(); button.targetGraphic=rect.GetComponent<Image>();
        button.onClick.AddListener(() => { if (!saving && drawOverlay==null) action(); });
        Label(rect,title,23,new Vector2(.04f,.04f),new Vector2(.96f,.96f)).color=Color.white;
        return button;
    }

}
