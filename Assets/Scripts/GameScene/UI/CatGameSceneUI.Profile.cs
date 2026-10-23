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
        var portrait = Panel(body, "Portrait Border", new Vector2(.05f, .32f), new Vector2(.35f, .79f), new Color(.40f, .27f, .20f));
        var inset = Panel(portrait, "Portrait Inset", new Vector2(.04f, .04f), new Vector2(.96f, .96f), new Color(.84f, .71f, .55f));
        CharacterPortrait(inset, catalog.FindCharacter(ProfilePortraitId));
        var name = Label(body, ProfileName, 27, new Vector2(.40f, .57f), new Vector2(.81f, .78f));
        name.supportRichText = false;
        name.alignment = TextAnchor.MiddleLeft;
        ProfileIconButton(body, "이름 변경", "edit", new Vector2(.82f, .57f), new Vector2(.96f, .79f), ShowProfileNameEditor, .5f);
        ProfileIconButton(body, "사진 변경", "photo", new Vector2(.25f, .27f), new Vector2(.39f, .49f), ShowProfilePortraitPicker, .5f);
        Label(body, $"Lv. {data.progress.accountLevel}", 23, new Vector2(.40f, .35f), new Vector2(.58f, .53f));
        long required = Math.Max(1, data.progress.accountLevel) * 100L;
        long experience = Math.Max(0, data.progress.accountExperience);
        var track = Panel(body, "Account EXP Track", new Vector2(.40f, .11f), new Vector2(.95f, .27f), new Color(.27f, .20f, .16f));
        var fill = Panel(track, "Account EXP Fill", new Vector2(.02f, .12f), new Vector2(.02f + .96f * Mathf.Clamp01((float)((double)experience / required)), .88f), new Color(.96f, .68f, .22f));
        fill.GetComponent<Image>().raycastTarget = false;
        Label(track, $"EXP {experience:N0} / {required:N0}", 17, Vector2.zero, Vector2.one).color = Color.white;
    }

    private RectTransform ProfileBody(string title, bool tall)
    {
        CloseProfilePopup();
        profilePopup = Panel(safeArea, "Profile Popup", Vector2.zero, Vector2.one, new Color(.06f, .04f, .03f, .72f));
        profilePopup.SetAsLastSibling();
        var body = PopupContent(profilePopup, "Profile Frame", "Profile Content",
            new Vector2(.045f, tall ? .20f : .36f), new Vector2(.955f, tall ? .82f : .70f), new Color(.35f, .23f, .17f), CloseProfilePopup, out _);
        body.GetComponent<Image>().color = new Color(.97f, .89f, .79f);
        Label(body, title, 28, new Vector2(.08f, .82f), new Vector2(.82f, .98f));
        ProfileIconButton(body, "닫기", "close", new Vector2(.85f, .81f), new Vector2(.98f, .99f), CloseProfilePopup);
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
        // 기존 버튼 스킨을 작은 정사각형에 적용하고 라벨 없이 아이콘만 표시합니다.
        button.targetGraphic = face.GetComponent<Image>();
        CozyButtonStyle.Apply(button, CozyButtonStyle.Cream);
        var drawing = Rect(face, "Icon", new Vector2(.22f, .22f), new Vector2(.78f, .78f));
        if (icon == "edit")
        {
            drawing.anchorMin = new Vector2(.10f, .10f);
            drawing.anchorMax = new Vector2(.90f, .90f);
        }
        Color dark = new Color(.28f, .18f, .12f);
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
