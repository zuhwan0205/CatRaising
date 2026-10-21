using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    [SerializeField] private GameObject adventureRoot;
    [SerializeField] private RawImage adventureImage;
    [SerializeField] private Button adventureMode;
    [SerializeField] private FieldJoystick adventureJoystick;
    [SerializeField] private Text adventureHp, adventureCombat, adventureRelic, adventureStage, adventureEquipment;

    public bool UpgradeFullScreenBattle()
    {
        if (safeArea == null || adventureImage == null || adventureRoot == null) return false;
        if (adventureImage.transform.parent == safeArea.parent) return false;
        // 전투 영상은 화면 끝까지, 조작 UI는 노치 안전 영역 안에 유지합니다.
        var rect = adventureImage.rectTransform;
        rect.SetParent(safeArea.parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.SetSiblingIndex(safeArea.GetSiblingIndex());
        adventureRoot.GetComponent<Image>().color = Color.clear;
        adventureRoot.GetComponent<Image>().raycastTarget = false;
        var header = safeArea.Find("Currency Bar");
        if (header != null) header.SetAsLastSibling();
        var navigation = safeArea.Find("Navigation");
        if (navigation != null) navigation.SetAsLastSibling();
        return true;
    }

    private void RestoreAdventure()
    {
        var header = safeArea.Find("Currency Bar");
        if (header != null) header.gameObject.SetActive(true);
        adventureRoot.SetActive(true);
        UpgradeFullScreenBattle();
        adventureImage.gameObject.SetActive(true);
        adventureImage.texture = fieldTexture;
        adventureImage.GetComponent<FieldViewportSize>().Target = fieldTexture;
        joystick = adventureJoystick;
        playerHpLabel = adventureHp;
        combatLabel = adventureCombat;
        relicEffectLabel = adventureRelic;
        playerHpLabel.text = playerHpText;
        combatLabel.text = combatMessage;
        adventureStage.text = $"STAGE {data.progress.currentStage:00} · 2.5D 필드";
        var names = new System.Collections.Generic.List<string>();
        foreach (var id in data.loadout.relicIds)
            names.Add(catalog.FindRelic(id)?.displayName ?? id);
        adventureEquipment.text = names.Count == 0 ? "장착 유물 없음 · 유물 메뉴에서 장착하세요" : "장착: " + string.Join(" / ", names);
        adventureJoystick.InputChanged = value => StickChanged?.Invoke(value);
        adventureJoystick.gameObject.SetActive(!automatic);
        adventureMode.onClick.RemoveAllListeners();
        adventureMode.onClick.AddListener(() =>
        {
            if (saving) return;
            automatic = !automatic;
            adventureJoystick.ResetInput();
            adventureJoystick.gameObject.SetActive(!automatic);
            MovementModeChanged?.Invoke(automatic);
            RefreshMovementButton();
        });
        MovementModeChanged?.Invoke(automatic);
        foreach (var text in adventureRoot.GetComponentsInChildren<Text>(true)) text.enabled = false;
        RefreshMovementButton();
    }

    private void RefreshMovementButton()
    {
        var icon = adventureMode.transform.Find("Movement Mode Icon");
        if (icon != null) icon.gameObject.SetActive(false);
        var label = adventureMode.GetComponentInChildren<Text>(true);
        label.gameObject.SetActive(true);
        label.enabled = true;
        label.text = "AUTO";
        label.fontStyle = FontStyle.Bold;
        label.color = automatic ? Color.white : new Color(.65f, .65f, .62f);
        CozyButtonStyle.SetColor(adventureMode, automatic ? CozyButtonStyle.Green : new Color(.28f, .29f, .27f));
    }
}
