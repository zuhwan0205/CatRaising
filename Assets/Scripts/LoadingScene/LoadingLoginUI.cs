using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// 씬에 저장된 UI를 사용하며, UI가 없는 씬에서는 기존 생성 방식을 지원합니다.
public class LoadingLoginUI : MonoBehaviour
{
    [SerializeField] private int textSizeVersion;
    [SerializeField] private int backgroundSizeVersion;
    public bool UpgradeBackgroundSize()
    {
        if (backgroundSizeVersion >= 2 || root == null) return false;
        var picture = root.transform.Find("Background/Cat Village Background");
        if (picture == null) return false;
        // 씬에 저장된 축소 상태도 원래 크기로 복원합니다.
        picture.localScale = Vector3.one;
        backgroundSizeVersion = 2;
        return true;
    }
    public bool UpgradeTextSize()
    {
        if (textSizeVersion >= 1 || root == null) return false;
        ReadableUIText.Upgrade(root.transform);
        textSizeVersion = 1;
        return true;
    }
    [SerializeField] private Font uiFont;
    private BackendManager manager;
    [SerializeField] private GameObject root;
    [SerializeField] private GameObject choice;
    [SerializeField] private GameObject form;
    [SerializeField] private Button retry;
    [SerializeField] private InputField idInput;
    [SerializeField] private InputField passwordInput;
    [SerializeField] private InputField confirmInput;
    [SerializeField] private Text heading;
    [SerializeField] private Text message;
    [SerializeField] private Text submitLabel;
    [SerializeField] private CanvasGroup formGroup;
    [SerializeField] private CanvasGroup choiceGroup;
    [SerializeField] private Button signUpButton, loginButton, googleButton, submitButton, backButton;
    private bool signUp;
    private bool busy;

    public void Build(BackendManager owner)
    {
        manager = owner;
        if (root != null)
        {
            UpgradeTextSize();
            UpgradeBackgroundSize();
            BindButtons();
            PrepareInitialView();
            return;
        }
        var projectFont = Resources.Load<Font>("Fonts/Mona12TextKR");
        if (projectFont != null)
            uiFont = projectFont;
        if (uiFont == null)
            uiFont = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" }, 24);

        if (FindAnyObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("Login EventSystem", typeof(EventSystem));
            events.transform.SetParent(transform, false);
#if ENABLE_INPUT_SYSTEM
            events.AddComponent<InputSystemUIInputModule>();
#else
            events.AddComponent<StandaloneInputModule>();
#endif
        }

        root = new GameObject("Login Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(720, 960);
        scaler.matchWidthOrHeight = 0.5f;

        var background = Box("Background", root.transform, Vector2.zero, Vector2.zero);
        background.anchorMin = Vector2.zero;
        background.anchorMax = Vector2.one;
        background.sizeDelta = Vector2.zero;
        var backdrop = background.gameObject.AddComponent<Image>();
        backdrop.color = new Color(.22f, .29f, .20f);
        backdrop.raycastTarget = false;
        var artwork = Resources.Load<Texture2D>("LoginArt/CatVillageLogin");
        if (artwork != null)
        {
            var picture = Box("Cat Village Background", background, Vector2.zero, Vector2.zero);
            var image = picture.gameObject.AddComponent<RawImage>();
            image.texture = artwork;
            image.raycastTarget = false;
            var fit = picture.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = (float)artwork.width / artwork.height;
        }
        var title = Label("고양이 키우기", background, new Vector2(0, 290), new Vector2(600, 70), 44);
        title.fontStyle = FontStyle.Bold;
        title.color = new Color(1f, .95f, .78f);
        var titleShadow = title.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(.20f, .17f, .12f, .95f);
        titleShadow.effectDistance = new Vector2(3, -3);
        var headingPlate = Box("Heading Backplate", background, new Vector2(0, 210), new Vector2(600, 60));
        headingPlate.gameObject.AddComponent<Image>().color = new Color(.16f, .23f, .18f, .8f);
        headingPlate.GetComponent<Image>().raycastTarget = false;
        heading = Label("서버에 연결 중입니다…", background, new Vector2(0, 210), new Vector2(600, 60), 24);
        message = Label("", background, new Vector2(0, -330), new Vector2(600, 110), 21);
        var messageShadow = message.gameObject.AddComponent<Outline>();
        messageShadow.effectColor = new Color(.1f, .16f, .12f, .95f);
        messageShadow.effectDistance = new Vector2(1.5f, -1.5f);

        choice = Box("Choose Account Action", background, Vector2.zero, new Vector2(580, 300)).gameObject;
        choiceGroup = choice.AddComponent<CanvasGroup>();
        signUpButton = MakeButton("회원가입", choice.transform, new Vector2(0, 90));
        loginButton = MakeButton("로그인", choice.transform, Vector2.zero);
        googleButton = MakeButton("Google로 계속하기", choice.transform, new Vector2(0, -90));

        form = Box("Account Form", background, Vector2.zero, new Vector2(580, 440)).gameObject;
        formGroup = form.AddComponent<CanvasGroup>();
        idInput = MakeInput("아이디", form.transform, 120, false);
        passwordInput = MakeInput("비밀번호", form.transform, 35, true);
        confirmInput = MakeInput("비밀번호 확인", form.transform, -50, true);
        submitButton = MakeButton("로그인", form.transform, new Vector2(0, -140));
        submitLabel = submitButton.GetComponentInChildren<Text>();
        backButton = MakeButton("뒤로", form.transform, new Vector2(0, -215));
        retry = MakeButton("연결 재시도", background, Vector2.zero);
        BindButtons();
        PrepareInitialView();
        textSizeVersion = 1;
        UpgradeBackgroundSize();
    }

    public bool HasSceneUI => root != null;

    private void BindButtons()
    {
        // 재초기화에서도 런타임 이벤트가 중복되지 않도록 합니다.
        signUpButton.onClick.RemoveAllListeners();
        loginButton.onClick.RemoveAllListeners();
        googleButton.onClick.RemoveAllListeners();
        submitButton.onClick.RemoveAllListeners();
        backButton.onClick.RemoveAllListeners();
        retry.onClick.RemoveAllListeners();
        signUpButton.onClick.AddListener(() => SelectMode(true));
        loginButton.onClick.AddListener(() => SelectMode(false));
        googleButton.onClick.AddListener(() => { if (!busy) StartCoroutine(SubmitRoutine(true)); });
        submitButton.onClick.AddListener(Submit);
        backButton.onClick.AddListener(ShowChoice);
        retry.onClick.AddListener(manager.InitializeBackend);
    }

    private void PrepareInitialView()
    {
        busy = false;
        idInput.text = passwordInput.text = confirmInput.text = "";
        choiceGroup.interactable = formGroup.interactable = true;
        choice.SetActive(!Application.isPlaying);
        form.SetActive(false);
        retry.gameObject.SetActive(false);
        heading.text = Application.isPlaying ? "서버에 연결 중입니다…" : "회원가입 또는 로그인을 선택해주세요";
        message.text = "";
    }
    public void ShowInitialization(bool success)
    {
        retry.gameObject.SetActive(!success);
        if (success)
            ShowChoice();
        else
        {
            choice.SetActive(false);
            form.SetActive(false);
            heading.text = "서버 연결 실패";
            ShowMessage("네트워크를 확인한 후 다시 시도해주세요.");
        }
    }

    private void ShowChoice()
    {
        if (busy)
            return;
        passwordInput.text = "";
        confirmInput.text = "";
        choice.SetActive(true);
        form.SetActive(false);
        heading.text = "회원가입 또는 로그인을 선택해주세요";
        ShowMessage("");
    }

    private void SelectMode(bool createAccount)
    {
        signUp = createAccount;
        choice.SetActive(false);
        form.SetActive(true);
        confirmInput.gameObject.SetActive(signUp);
        passwordInput.text = "";
        confirmInput.text = "";
        heading.text = signUp ? "회원가입" : "로그인";
        submitLabel.text = heading.text;
        ShowMessage("");
        idInput.ActivateInputField();
    }

    private void Submit()
    {
        if (busy)
            return;
        if (string.IsNullOrWhiteSpace(idInput.text) || string.IsNullOrWhiteSpace(passwordInput.text))
        {
            ShowMessage("아이디와 비밀번호를 입력해주세요.");
            return;
        }
        if (signUp && passwordInput.text != confirmInput.text)
        {
            ShowMessage("비밀번호 확인이 일치하지 않습니다.");
            return;
        }
        StartCoroutine(SubmitRoutine());
    }

    private IEnumerator SubmitRoutine(bool google = false)
    {
        busy = true;
        formGroup.interactable = false;
        choiceGroup.interactable = false;
        ShowMessage("처리 중입니다…");
        // 동기 API 호출 전에 처리 중 안내를 화면에 표시합니다.
        yield return null;
        yield return null;
        try
        {
            var request = google ? manager.SubmitGoogleAsync() : manager.SubmitAsync(signUp, idInput.text, passwordInput.text);
            while (!request.IsCompleted)
                yield return null;
            if (request.IsFaulted)
            {
                Debug.LogException(request.Exception);
                ShowMessage("로그인 처리 중 오류가 발생했습니다. 다시 시도해주세요.");
            }
        }
        finally
        {
            busy = false;
            if (formGroup != null)
                formGroup.interactable = true;
            if (choiceGroup != null)
                choiceGroup.interactable = true;
        }
    }

    public void ShowLoginAfterSignUp()
    {
        SelectMode(false);
        ShowMessage("회원가입이 완료되었습니다. 비밀번호를 입력하고 로그인해주세요.");
    }

    public void ShowMessage(string text)
    {
        message.text = text;
    }

    public void ShowCompleted()
    {
        passwordInput.text = "";
        confirmInput.text = "";
        form.SetActive(false);
        choice.SetActive(false);
        heading.text = "로그인 완료";
        ShowMessage("유저 데이터 준비가 완료되었습니다.");
    }

    private RectTransform Box(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private Text Label(string value, Transform parent, Vector2 position, Vector2 size, int fontSize)
    {
        var text = Box("Label", parent, position, size).gameObject.AddComponent<Text>();
        text.font = uiFont;
        ReadableUIText.Configure(text, Mathf.RoundToInt(fontSize * 1.3f));
        text.text = value;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    private Button MakeButton(string title, Transform parent, Vector2 position)
    {
        var rect = Box(title, parent, position, new Vector2(520, 64));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(.25f, .36f, .26f, .97f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        // 이벤트는 Build에서 저장된 참조에 연결합니다.
        button.gameObject.AddComponent<ButtonTweenEffect>();
        Label(title, rect, Vector2.zero, new Vector2(500, 60), 24);
        return button;
    }

    private InputField MakeInput(string placeholder, Transform parent, float y, bool password)
    {
        var rect = Box(placeholder, parent, new Vector2(0, y), new Vector2(520, 64));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(.16f, .23f, .18f, .94f);
        var input = rect.gameObject.AddComponent<InputField>();
        var value = Label("", rect, Vector2.zero, new Vector2(484, 60), 24);
        value.alignment = TextAnchor.MiddleLeft;
        var hint = Label(placeholder, rect, Vector2.zero, new Vector2(484, 60), 24);
        hint.alignment = TextAnchor.MiddleLeft;
        hint.color = new Color(0.65f, 0.68f, 0.75f);
        input.targetGraphic = image;
        input.textComponent = value;
        input.placeholder = hint;
        input.contentType = password ? InputField.ContentType.Password : InputField.ContentType.Standard;
        input.lineType = InputField.LineType.SingleLine;
        return input;
    }
}
