using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    private RectTransform profilePopup;

    private void ShowProfilePopup()
    {
        if (!Application.isPlaying || data == null || saving || detailOverlay != null || drawOverlay != null || profilePopup != null) return;
        profilePopup = Panel(safeArea, "Profile Popup", Vector2.zero, Vector2.one, new Color(.04f, .06f, .05f, .65f));
        profilePopup.SetAsLastSibling();
        var backdrop = profilePopup.gameObject.AddComponent<Button>();
        backdrop.transition = Selectable.Transition.None;
        backdrop.onClick.AddListener(CloseProfilePopup);
        var frame = Panel(profilePopup, "Profile Frame", new Vector2(.10f, .32f), new Vector2(.90f, .72f), new Color(.39f, .27f, .16f));
        var intercept = frame.gameObject.AddComponent<Button>();
        intercept.transition = Selectable.Transition.None;
        var body = Panel(frame, "Profile Content", new Vector2(.025f, .025f), new Vector2(.975f, .975f), cream);
        Label(body, "프로필", 30, new Vector2(0, .82f), Vector2.one);
        CharacterPortrait(Rect(body, "Profile Cat", new Vector2(.30f, .43f), new Vector2(.70f, .82f)), catalog.FindCharacter(data.loadout.characterId));
        Label(body, "고양이 집사", 26, new Vector2(.05f, .30f), new Vector2(.95f, .44f));
        Label(body, $"계정 레벨 {data.progress.accountLevel}", 24, new Vector2(.05f, .18f), new Vector2(.95f, .31f));
        Button(body, "닫기", new Vector2(.25f, .03f), new Vector2(.75f, .16f), CloseProfilePopup);
    }

    private void CloseProfilePopup()
    {
        if (profilePopup == null) return;
        RemoveUI(profilePopup.gameObject);
        profilePopup = null;
    }
}
