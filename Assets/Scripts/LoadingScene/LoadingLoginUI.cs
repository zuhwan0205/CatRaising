using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// BackendManager가 실행 시 생성하므로 별도의 버튼 연결 없이 사용할 수 있습니다.
public class LoadingLoginUI : MonoBehaviour
{
    [SerializeField] private Font uiFont;
    private BackendManager manager;
    private GameObject root;
    private GameObject choice;
    private GameObject form;
    private Button retry;
    private InputField idInput;
    private InputField passwordInput;
    private InputField confirmInput;
    private Text heading;
    private Text message;
    private Text submitLabel;
    private CanvasGroup formGroup;
    private bool signUp;
    private bool busy;

    public void Build(BackendManager owner)
    {
        if (root != null) return;
        manager = owner;
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
        background.gameObject.AddComponent<Image>().color = new Color(0.08f, 0.10f, 0.16f, 1);
        Label("고양이 키우기", background, new Vector2(0, 290), new Vector2(600, 70), 36);
        heading = Label("서버에 연결 중입니다…", background, new Vector2(0, 210), new Vector2(600, 60), 24);
        message = Label("", background, new Vector2(0, -330), new Vector2(600, 110), 21);

        choice = Box("Choose Account Action", background, Vector2.zero, new Vector2(580, 300)).gameObject;
        MakeButton("회원가입", choice.transform, new Vector2(0, 55), () => SelectMode(true));
        MakeButton("로그인", choice.transform, new Vector2(0, -55), () => SelectMode(false));

        form = Box("Account Form", background, Vector2.zero, new Vector2(580, 440)).gameObject;
        formGroup = form.AddComponent<CanvasGroup>();
        idInput = MakeInput("아이디", form.transform, 120, false);
        passwordInput = MakeInput("비밀번호", form.transform, 35, true);
        confirmInput = MakeInput("비밀번호 확인", form.transform, -50, true);
        var submit = MakeButton("로그인", form.transform, new Vector2(0, -140), Submit);
        submitLabel = submit.GetComponentInChildren<Text>();
        MakeButton("뒤로", form.transform, new Vector2(0, -215), ShowChoice);
        retry = MakeButton("연결 재시도", background, Vector2.zero, manager.InitializeBackend);
        choice.SetActive(false);
        form.SetActive(false);
        retry.gameObject.SetActive(false);
    }

    public void ShowInitialization(bool success)
    {
        retry.gameObject.SetActive(!success);
        if (success) ShowChoice();
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
        if (busy) return;
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
        if (busy) return;
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

    private IEnumerator SubmitRoutine()
    {
        busy = true;
        formGroup.interactable = false;
        ShowMessage("처리 중입니다…");
        // 동기 API 호출 전에 처리 중 안내를 화면에 표시합니다.
        yield return null;
        yield return null;
        try
        {
            var request = manager.SubmitAsync(signUp, idInput.text, passwordInput.text);
            while (!request.IsCompleted) yield return null;
            if (request.IsFaulted)
            {
                Debug.LogException(request.Exception);
                ShowMessage("로그인 처리 중 오류가 발생했습니다. 다시 시도해주세요.");
            }
        }
        finally
        {
            busy = false;
            if (formGroup != null) formGroup.interactable = true;
        }
    }

    public void ShowLoginAfterSignUp()
    {
        SelectMode(false);
        ShowMessage("회원가입이 완료되었습니다. 비밀번호를 입력하고 로그인해주세요.");
    }

    public void ShowMessage(string text) { message.text = text; }

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
        text.fontSize = fontSize;
        text.text = value;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    private Button MakeButton(string title, Transform parent, Vector2 position, UnityAction action)
    {
        var rect = Box(title, parent, position, new Vector2(520, 64));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.25f, 0.36f, 0.62f);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        Label(title, rect, Vector2.zero, new Vector2(500, 60), 24);
        return button;
    }

    private InputField MakeInput(string placeholder, Transform parent, float y, bool password)
    {
        var rect = Box(placeholder, parent, new Vector2(0, y), new Vector2(520, 64));
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.18f, 0.21f, 0.29f);
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
