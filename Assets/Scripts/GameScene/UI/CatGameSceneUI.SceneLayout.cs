using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    [SerializeField] private GameObject adventureRoot;
    [SerializeField] private RawImage adventureImage;
    [SerializeField] private Button adventureMode;
    [SerializeField] private FieldJoystick adventureJoystick;
    [SerializeField] private Text adventureHp, adventureCombat, adventureRelic, adventureStage, adventureEquipment;

    private void RestoreAdventure()
    {
        adventureRoot.SetActive(true);
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
        adventureMode.GetComponentInChildren<Text>().text = automatic ? "자동 이동 ON" : "수동 이동";
        adventureMode.onClick.RemoveAllListeners();
        adventureMode.onClick.AddListener(() =>
        {
            if (saving) return;
            automatic = !automatic;
            adventureJoystick.ResetInput();
            adventureJoystick.gameObject.SetActive(!automatic);
            adventureMode.GetComponentInChildren<Text>().text = automatic ? "자동 이동 ON" : "수동 이동";
            MovementModeChanged?.Invoke(automatic);
        });
        MovementModeChanged?.Invoke(automatic);
    }
}
