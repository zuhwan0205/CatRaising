using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;

public partial class CatGameSceneUI
{
    private RectTransform profilePopup;
    private bool profileSaving;
    public Func<string, string, bool, Task<bool>> ProfileSaveRequested;
    public Func<string> ProfileSaveError;
    private string ProfilePortraitId => data == null ? CatGameCatalog.StarterCharacterId :
        string.IsNullOrEmpty(data.progress.profileCharacterId) ? data.loadout.characterId : data.progress.profileCharacterId;
    private string ProfileName => string.IsNullOrWhiteSpace(data.progress.profileName) ? "고양이 집사" : data.progress.profileName;

    private void ShowProfilePopup()
    {
        if (!Application.isPlaying || data == null || detailOverlay != null || drawOverlay != null || profilePopup != null) return;
        var body = ProfileBody("집사 프로필", false);
        var definition = catalog.FindCharacter(ProfilePortraitId);
        Color rarity = definition != null ? CharacterRarityColor(definition.rarity) : CozyButtonStyle.Cream;
        var portraitArea = Rect(body, "Portrait Area", new Vector2(.055f, .34f), new Vector2(.365f, .77f));
        // FitInParent는 자신의 앵커를 부모 전체로 바꾸므로 배치 슬롯과 분리합니다.
        var portraitSquare = Rect(portraitArea, "Portrait Square", Vector2.zero, Vector2.one);
        var square = portraitSquare.gameObject.AddComponent<AspectRatioFitter>();
        square.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        square.aspectRatio = 1;
        var portrait = CollectionFrame(portraitSquare, "Portrait Border", rarity);
        var inset = Panel(portrait, "Portrait Inset", new Vector2(.075f, .075f), new Vector2(.925f, .925f), new Color(.95f,.87f,.70f));
        RoundCollectionPanel(inset);
        CharacterPortrait(inset, definition);
        ProfileIconButton(portrait, "사진 변경", "photo", new Vector2(.73f, -.04f), new Vector2(1.08f, .31f), ShowProfilePortraitPicker);

        var namePlate = Panel(body, "Name Plate", new Vector2(.405f,.53f), new Vector2(.95f,.76f), new Color(.91f,.82f,.66f));
        RoundCollectionPanel(namePlate);
        var name = Label(namePlate, ProfileName, 30, new Vector2(.07f,.08f), new Vector2(.78f,.92f));
        name.supportRichText = false;
        name.alignment = TextAnchor.MiddleLeft;
        name.color = new Color(.28f,.22f,.16f);
        name.resizeTextForBestFit = true;
        name.resizeTextMinSize = 19;
        name.resizeTextMaxSize = 30;
        ProfileIconButton(namePlate, "이름 변경", "edit", new Vector2(.80f,.17f), new Vector2(.97f,.83f), ShowProfileNameEditor);
        var level = Panel(body, "Level Badge", new Vector2(.405f,.36f), new Vector2(.69f,.49f), new Color(.56f,.40f,.28f));
        RoundCollectionPanel(level);
        Label(level, $"Lv. {data.progress.accountLevel}", 24, Vector2.zero, Vector2.one).color = new Color(1,.94f,.77f);

        long required = Math.Max(1, data.progress.accountLevel) * 100L;
        long experience = Math.Max(0, data.progress.accountExperience);
        var expCard = Panel(body, "Experience Card", new Vector2(.055f,.055f), new Vector2(.945f,.29f), new Color(.93f,.85f,.71f));
        RoundCollectionPanel(expCard);
        var caption = Label(expCard, "EXP", 20, new Vector2(.04f,.58f), new Vector2(.52f,.96f));
        caption.alignment = TextAnchor.MiddleLeft;
        caption.color = new Color(.38f,.30f,.21f);
        var track = Panel(expCard, "Account EXP Track", new Vector2(.035f,.13f), new Vector2(.965f,.53f), new Color(.70f,.57f,.42f));
        RoundCollectionPanel(track);
        var fill = Panel(track, "Account EXP Fill", new Vector2(.012f,.13f), new Vector2(.988f,.87f), new Color(.93f,.67f,.25f));
        RoundCollectionPanel(fill);
        fill.pivot = new Vector2(0,.5f);
        fill.localScale = new Vector3(Mathf.Clamp01((float)((double)experience / required)), 1, 1);
        fill.GetComponent<Image>().raycastTarget = false;
        var value = Label(expCard, $"{experience:N0} / {required:N0}", 19, new Vector2(.50f,.58f), new Vector2(.96f,.96f));
        value.alignment = TextAnchor.MiddleRight;
        value.color = new Color(.38f,.30f,.21f);
    }

    private RectTransform ProfileBody(string title, bool tall)
    {
        CloseProfilePopup();
        profilePopup = Panel(safeArea, "Profile Popup", Vector2.zero, Vector2.one, new Color(.06f, .04f, .03f, .72f));
        profilePopup.SetAsLastSibling();
        var body = PopupContent(profilePopup, "Profile Frame", "Profile Content",
            new Vector2(.045f, tall ? .20f : .31f), new Vector2(.955f, tall ? .82f : .73f), new Color(.35f, .23f, .17f), CloseProfilePopup, out var frame);
        RoundCollectionPanel(frame);
        var shadow = frame.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(.10f,.07f,.04f,.45f);
        shadow.effectDistance = new Vector2(0,-8);
        DecorateCatPopup(body);
        var ribbon = Panel(body, "Profile Title Ribbon", new Vector2(.025f,.815f), new Vector2(.975f,.985f), new Color(.56f,.40f,.28f));
        RoundCollectionPanel(ribbon);
        var trim = Panel(ribbon, "Ribbon Gold Trim", new Vector2(.04f,.06f), new Vector2(.96f,.10f), new Color(.85f,.68f,.39f));
        trim.GetComponent<Image>().raycastTarget = false;
        Label(body, title, 29, new Vector2(.08f,.83f), new Vector2(.82f,.98f)).color = new Color(1,.94f,.78f);
        ProfileIconButton(body, "닫기", "close", new Vector2(.85f, .81f), new Vector2(.98f, .99f), CloseProfilePopup);
        UISelectionMotion.Play(frame, true);
        return body;
    }

    // UI 도형으로 그린 아이콘: 외부 폰트 기호에 의존하지 않고 작은 화면에서도 선명합니다.
    private void ProfileIconButton(Transform parent, string title, string icon, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action, float visualScale = 1f)
    {
        Vector2 center = (min + max) * .5f;
        Vector2 halfSize = (max - min) * (.5f * visualScale);
        var hit = Rect(parent, title, center - halfSize, center + halfSize);
        var hitImage = hit.gameObject.AddComponent<Image>();
        hitImage.color = Color.clear;
        var button = hit.gameObject.AddComponent<Button>();
        button.onClick.AddListener(() => { if (!profileSaving) action(); });
        var face = Panel(hit, "Icon Face", Vector2.zero, Vector2.one, new Color(.38f, .26f, .18f));
        var fit = face.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fit.aspectRatio = 1;
        // 프로필의 작은 아이콘은 같은 모카 색상으로 통일합니다.
        // 루트 이미지는 투명한 클릭 영역, 정사각형 face는 표시와 색상 전환 전용입니다.
        RoundCollectionPanel(face);
        face.GetComponent<Image>().color = Color.white;
        button.targetGraphic = face.GetComponent<Image>();
        CozyButtonStyle.SetColor(button, new Color(.56f,.40f,.28f));
        button.gameObject.AddComponent<ButtonTweenEffect>();
        var drawing = Rect(face, "Icon", new Vector2(.22f, .22f), new Vector2(.78f, .78f));
        if (icon == "edit")
        {
            drawing.anchorMin = new Vector2(.10f, .10f);
            drawing.anchorMax = new Vector2(.90f, .90f);
        }
        if (icon == "close")
        {
            // 투명한 루트는 클릭 영역으로 남기고 닫기 아이콘의 배경만 숨깁니다.
            face.GetComponent<Image>().enabled = false;
            button.targetGraphic = hitImage;
            button.transition = Selectable.Transition.None;
        }
        Color dark = new Color(1f, .94f, .82f);
        if (icon == "edit")
        {
            drawing.localRotation = Quaternion.Euler(0, 0, -40);
            ProfileIconStroke(drawing, new Vector2(.31f, .17f), new Vector2(.69f, .91f), dark);
            ProfileIconStroke(drawing, new Vector2(.38f, .30f), new Vector2(.62f, .78f), new Color(.96f, .70f, .27f));
            ProfileIconStroke(drawing, new Vector2(.38f, .78f), new Vector2(.62f, .89f), new Color(.91f, .48f, .39f));
            ProfileIconStroke(drawing, new Vector2(.37f, .10f), new Vector2(.63f, .28f), new Color(.98f, .87f, .64f));
            ProfileIconStroke(drawing, new Vector2(.44f, .02f), new Vector2(.56f, .14f), dark);
        }
        else if (icon == "photo")
        {
            drawing.anchorMin = new Vector2(.03f, .03f);
            drawing.anchorMax = new Vector2(.97f, .97f);
            var image = drawing.gameObject.AddComponent<RawImage>();
            image.texture = Resources.Load<Texture2D>("GameUIArt/ProfileRefreshWhite");
            image.raycastTarget = false;

        }
        else if (icon == "close")
        {
            ProfileIconLine(drawing, new Vector2(.18f, .18f), new Vector2(.82f, .82f), dark);
            ProfileIconLine(drawing, new Vector2(.18f, .82f), new Vector2(.82f, .18f), dark);
        }
        else if (icon == "check")
        {
            ProfileIconLine(drawing, new Vector2(.08f, .50f), new Vector2(.38f, .22f), dark);
            ProfileIconLine(drawing, new Vector2(.38f, .22f), new Vector2(.92f, .82f), dark);
        }
        else
        {
            ProfileIconStroke(drawing, new Vector2(.18f, .43f), new Vector2(.92f, .57f), dark);
            ProfileIconLine(drawing, new Vector2(.12f, .50f), new Vector2(.48f, .86f), dark);
            ProfileIconLine(drawing, new Vector2(.12f, .50f), new Vector2(.48f, .14f), dark);
        }
        foreach (var graphic in face.GetComponentsInChildren<Graphic>()) graphic.raycastTarget = false;
    }

    private void ProfileIconStroke(Transform parent, Vector2 min, Vector2 max, Color color)
    {
        Panel(parent, "Icon Stroke", min, max, color).GetComponent<Image>().raycastTarget = false;
    }

    private void ProfileIconLine(RectTransform parent, Vector2 start, Vector2 end, Color color)
    {
        // 정사각형 아이콘 안에서 비율 좌표로 선을 배치합니다.
        Vector2 center = (start + end) * .5f;
        float length = Vector2.Distance(start, end);
        var line = Panel(parent, "Icon Line", center - new Vector2(length * .5f, .07f), center + new Vector2(length * .5f, .07f), color);
        line.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg);
        line.GetComponent<Image>().raycastTarget = false;
    }

    private void ShowProfileNameEditor()
    {
        if (profileSaving) return;
        var body = ProfileBody("이름 변경", false);
        var box = Panel(body, "Name Input", new Vector2(.08f, .43f), new Vector2(.92f, .68f), Color.white);
        var text = Label(box, ProfileName, 25, new Vector2(.05f, .05f), new Vector2(.95f, .95f));
        text.supportRichText = false;
        var input = box.gameObject.AddComponent<InputField>();
        input.targetGraphic = box.GetComponent<Image>();
        input.textComponent = text;
        input.characterLimit = 12;
        input.lineType = InputField.LineType.SingleLine;
        input.text = ProfileName;
        var message = Label(body, "1~12자까지 입력할 수 있어요", 18, new Vector2(.05f, .27f), new Vector2(.95f, .41f));
        ProfileIconButton(body, "저장", "check", new Vector2(.62f, .04f), new Vector2(.83f, .26f), () => SaveProfileChanges(input.text, ProfilePortraitId, message, true));
        ProfileIconButton(body, "취소", "back", new Vector2(.17f, .04f), new Vector2(.38f, .26f), ReturnToProfile);
        input.ActivateInputField();
    }

    private void ShowProfilePortraitPicker()
    {
        if (profileSaving) return;
        var body = ProfileBody("프로필 사진 선택", true);
        var message = Label(body, "보유한 고양이를 선택하세요", 18, new Vector2(.05f, .04f), new Vector2(.95f, .13f));
        var viewport = Panel(body, "Portrait Viewport", new Vector2(.04f, .16f), new Vector2(.96f, .79f), new Color(1, 1, 1, .18f));
        viewport.gameObject.AddComponent<RectMask2D>();
        var content = Rect(viewport, "Portrait Choices", new Vector2(0, 1), Vector2.one);
        content.pivot = new Vector2(.5f, 1);
        int count = data.characters.Count;
        content.sizeDelta = new Vector2(0, Mathf.CeilToInt(count / 3f) * 155);
        var scroll = viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
        scroll.scrollSensitivity = 45; scroll.movementType = ScrollRect.MovementType.Clamped;
        for (int i = 0; i < count; i++)
        {
            var owned = data.characters[i];
            if (owned == null) continue;
            string id = owned.characterId;
            var button = Button(content, "", new Vector2(i % 3 / 3f + .015f, 1), new Vector2((i % 3 + 1) / 3f - .015f, 1), () => SaveProfileChanges(ProfileName, id, message), true);
            var rect = (RectTransform)button.transform;
            rect.pivot = new Vector2(.5f, 1);
            rect.anchoredPosition = new Vector2(0, -(i / 3) * 155);
            rect.sizeDelta = new Vector2(0, 145);
            CharacterPortrait(Rect(rect, "Portrait", new Vector2(.06f, .22f), new Vector2(.94f, .96f)), catalog.FindCharacter(id));
            foreach (var graphic in rect.GetComponentsInChildren<Graphic>()) if (graphic.gameObject != rect.gameObject) graphic.raycastTarget = false;
            Label(rect, id == ProfilePortraitId ? "선택 중" : CharacterName(id), 16, new Vector2(.03f, .02f), new Vector2(.97f, .23f));
        }
    }

    private async void SaveProfileChanges(string name, string portrait, Text message, bool rename = false)
    {
        if (profileSaving) return;
        name = name?.Trim();
        if (string.IsNullOrEmpty(name) || name.Length > 12 || name.IndexOfAny(new[] { '<', '>', '\n', '\r', '\t' }) >= 0)
        { message.text = "이름을 1~12자로 입력해주세요"; return; }
        if (saving || ProfileSaveRequested == null) { message.text = "저장이 끝나면 다시 눌러주세요"; return; }
        profileSaving = true;
        message.text = "저장 중…";
        bool success = false;
        try { success = await ProfileSaveRequested(name, portrait, rename); }
        catch (Exception exception) { Debug.LogException(exception); }
        finally { profileSaving = false; }
        if (this == null) return;
        RefreshProfile();
        if (success) ReturnToProfile();
        else if (message != null)
        {
            string error = ProfileSaveError?.Invoke() ?? "저장하지 못했어요. 다시 시도해주세요";
            message.text = error;
            ShowProfileErrorPopup(error);
        }
    }

    private void ShowProfileErrorPopup(string error)
    {
        if (profilePopup == null) return;
        var overlay = Panel(profilePopup, "Profile Error", Vector2.zero, Vector2.one, new Color(0, 0, 0, .65f));
        UnityEngine.Events.UnityAction close = () => { if (overlay != null) RemoveUI(overlay.gameObject); };
        var body = PopupContent(overlay, "Error Frame", "Error Content", new Vector2(.09f, .37f), new Vector2(.91f, .66f), new Color(.35f, .23f, .17f), close, out _);
        Label(body, error.Contains("중복") ? "이름 중복" : "프로필 변경 안내", 27, new Vector2(.05f, .73f), new Vector2(.95f, .95f));
        Label(body, error, 22, new Vector2(.07f, .32f), new Vector2(.93f, .72f));
        Button(body, "확인", new Vector2(.28f, .06f), new Vector2(.72f, .27f), close, true);
    }

    private void ReturnToProfile()
    {
        if (profileSaving) return;
        CloseProfilePopup();
        ShowProfilePopup();
    }

    private void CloseProfilePopup()
    {
        if (profileSaving || profilePopup == null) return;
        RemoveUI(profilePopup.gameObject);
        profilePopup = null;
    }
}
