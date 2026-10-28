using System;
using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    public event Action<bool> EncounterPauseChanged;
    private RectTransform encounterPauseOverlay;

    private void BuildEncounterPauseButton(RectTransform parent)
    {
        var buttonArea = dungeonMode
            ? Rect(parent, "Pause Button Safe Area", safeArea.anchorMin, safeArea.anchorMax)
            : parent;
        var button = Button(buttonArea, "", Vector2.one, Vector2.one, OpenEncounterPause);
        button.name = "Encounter Pause Button";
        var rect = (RectTransform)button.transform;
        rect.sizeDelta = new Vector2(64, 64);
        rect.anchoredPosition = new Vector2(-44, -44);
        CozyButtonStyle.Apply(button, CozyButtonStyle.Cream);
        for (int i = 0; i < 3; i++)
        {
            float y = .20f + i * .23f;
            var edge = Panel(rect, "Menu Line Border", new Vector2(.17f, y), new Vector2(.83f, y + .17f), new Color(.28f,.21f,.15f));
            RoundCollectionPanel(edge);
            var face = Panel(edge, "Menu Line", new Vector2(.07f,.20f), new Vector2(.93f,.80f), new Color(1,.96f,.84f));
            RoundCollectionPanel(face);
        }
        // targetGraphic은 색상 전환용 내부 장식입니다. 실제 클릭 영역은 루트 이미지입니다.
        var hitArea = button.GetComponent<Image>();
        foreach (var graphic in button.GetComponentsInChildren<Graphic>())
            graphic.raycastTarget = graphic == hitArea;
    }

    private void OpenEncounterPause()
    {
        if (saving || encounterPauseOverlay != null) return;
        joystick?.ResetInput();
        StickChanged?.Invoke(Vector2.zero);
        EncounterPauseChanged?.Invoke(true);
        encounterPauseOverlay = Panel(safeArea.parent, "Encounter Pause", Vector2.zero, Vector2.one, new Color(.08f,.07f,.05f,.72f));
        encounterPauseOverlay.SetAsLastSibling();
        var frame = Panel(encounterPauseOverlay, "Pause Frame", new Vector2(.12f,.32f), new Vector2(.88f,.68f), new Color(.35f,.24f,.16f));
        RoundCollectionPanel(frame);
        var body = Panel(frame, "Pause Paper", new Vector2(.025f,.025f), new Vector2(.975f,.975f), cream);
        DecorateCatPopup(body);
        Label(body, "일시정지", 32, new Vector2(.08f,.72f), new Vector2(.92f,.93f));
        var resume = Button(body, "계속하기", new Vector2(.12f,.40f), new Vector2(.88f,.64f), () =>
        {
            CloseEncounterPause();
            EncounterPauseChanged?.Invoke(false);
        });
        CozyButtonStyle.Apply(resume, CozyButtonStyle.Green);
        Button(body, "나가기", new Vector2(.12f,.09f), new Vector2(.88f,.33f), ExitRaid);
    }

    public void CloseEncounterPause()
    {
        if (encounterPauseOverlay != null) RemoveUI(encounterPauseOverlay.gameObject);
        encounterPauseOverlay = null;
    }
}
