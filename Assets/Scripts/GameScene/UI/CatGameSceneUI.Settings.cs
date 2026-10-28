using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public partial class CatGameSceneUI
{
    public Func<Task> LogoutRequested;
    public Func<Task<bool>> SettingsSaveRequested;
    private RectTransform settingsPopup;
    private bool settingsBusy;

    private void ConfigureSettingsButton(Transform header)
    {
        var existing = header.Find("Settings");
        if (existing != null)
        {
            var button = existing.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(ShowSettings);
            BuildSettingsGear(button.transform);
            return;
        }
        var gear = Button(header, "", new Vector2(.88f, -.64f), new Vector2(.98f, -.06f), ShowSettings);
        gear.name = "Settings";
        BuildSettingsGear(gear.transform);
    }

    private void BuildSettingsGear(Transform button)
    {
        if (button.GetComponent<SettingsGearTween>() == null)
            button.gameObject.AddComponent<SettingsGearTween>();
        var old = button.Find("Gear Icon");
        if (old != null && old.GetComponent<SettingsGearGraphic>() != null) return;
        if (old != null)
        {
            old.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject);
        }
        var icon = Rect(button, "Gear Icon", new Vector2(.17f, .17f), new Vector2(.83f, .83f));
        var graphic = icon.gameObject.AddComponent<SettingsGearGraphic>();
        graphic.color = Color.black;
        graphic.raycastTarget = false;
    }
    private void CloseSettings()
    {
        if (settingsBusy) return;
        CatLocalSettings.Flush();
        if (settingsPopup != null) RemoveUI(settingsPopup.gameObject);
        settingsPopup = null;
    }

    private RectTransform SettingsBody(string title)
    {
        CloseSettings();
        settingsPopup = Panel(safeArea, "Settings Popup", Vector2.zero, Vector2.one, new Color(.07f, .10f, .08f, .76f));
        settingsPopup.SetAsLastSibling();
        var body = PopupContent(settingsPopup, "Settings Wood Frame", "Settings Paper",
            new Vector2(.045f, .16f), new Vector2(.955f, .87f), new Color(.39f, .27f, .18f), CloseSettings, out _);
        DecorateCatPopup(body);
        Label(body, title, 32, new Vector2(.1f, .86f), new Vector2(.78f, .98f));
        Button(body, "닫기", new Vector2(.78f, .88f), new Vector2(.96f, .97f), CloseSettings);
        return body;
    }

    private void ShowSettings()
    {
        if (!Application.isPlaying || settingsBusy || detailOverlay != null || drawOverlay != null || profilePopup != null) return;
        var body = SettingsBody("설정");
        AudioVolumeRow(body, CatAudioChannel.Bgm, "BGM", .84f);
        AudioVolumeRow(body, CatAudioChannel.Combat, "전투 효과음", .69f);
        var mute = Button(body, CatLocalSettings.Muted ? "음소거 켜짐" : "음소거 꺼짐", new Vector2(.25f, .465f), new Vector2(.75f, .535f), () => { });
        mute.onClick.AddListener(() =>
        {
            CatLocalSettings.SetMuted(!CatLocalSettings.Muted);
            mute.GetComponentInChildren<Text>().text = CatLocalSettings.Muted ? "음소거 켜짐" : "음소거 꺼짐";
            CozyButtonStyle.SetColor(mute, CatLocalSettings.Muted ? CozyButtonStyle.Peach : CozyButtonStyle.Green);
        });
        CozyButtonStyle.SetColor(mute, CatLocalSettings.Muted ? CozyButtonStyle.Peach : CozyButtonStyle.Green);
        Label(body, "화면 부드러움", 24, new Vector2(.10f, .39f), new Vector2(.90f, .455f));
        Button low = null, high = null;
        Action refresh = () =>
        {
            StyleFrameRateOption(low, CatLocalSettings.FrameRate == 30, "30 FPS · 절전");
            StyleFrameRateOption(high, CatLocalSettings.FrameRate == 60, "60 FPS · 부드럽게");
        };
        low = Button(body, "30 FPS · 절전", new Vector2(.10f, .305f), new Vector2(.48f, .385f), () => { CatLocalSettings.SetFrameRate(30); refresh(); });
        high = Button(body, "60 FPS · 부드럽게", new Vector2(.52f, .305f), new Vector2(.90f, .385f), () => { CatLocalSettings.SetFrameRate(60); refresh(); });
        refresh();
        var message = Label(body, "설정은 이 기기에 저장됩니다.", 18, new Vector2(.06f, .03f), new Vector2(.94f, .12f));
        Button(body, "진행 저장", new Vector2(.10f, .18f), new Vector2(.48f, .28f), async () =>
        {
            if (settingsBusy || saving) return;
            settingsBusy = true;
            message.text = "저장 중…";
            try
            {
                bool success = SettingsSaveRequested != null && await SettingsSaveRequested();
                if (message != null) message.text = success ? "저장했습니다." : "저장에 실패했습니다. 다시 시도해주세요.";
            }
            catch (Exception) { if (message != null) message.text = "저장에 실패했습니다. 다시 시도해주세요."; }
            finally { settingsBusy = false; }
        });
        var logout = Button(body, "로그아웃", new Vector2(.52f, .18f), new Vector2(.90f, .28f), () => { if (!settingsBusy && !saving) ShowLogoutConfirmation(); });
        CozyButtonStyle.Apply(logout, CozyButtonStyle.Peach);
        Label(body, "v" + Application.version, 16, new Vector2(.10f, 0), new Vector2(.90f, .035f));
    }

    private static void StyleFrameRateOption(Button button, bool applied, string title)
    {
        // 포커스가 이동해도 실제 저장된 프레임 설정을 기준으로 표시합니다.
        var color = applied ? new Color(.96f, .72f, .29f) : new Color(.72f, .70f, .66f);
        CozyButtonStyle.SetColor(button, color);
        var colors = button.colors;
        colors.selectedColor = color;
        button.colors = colors;
        var label = button.GetComponentInChildren<Text>();
        label.text = applied ? title + "\n적용 중" : title;
        label.fontStyle = applied ? FontStyle.Bold : FontStyle.Normal;
        label.color = applied ? new Color(.24f, .16f, .06f) : new Color(.30f, .29f, .27f);
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 16;
        label.resizeTextMaxSize = 23;
    }

    private void AudioVolumeRow(RectTransform body, CatAudioChannel channel, string title, float top)
    {
        var volumeText = Label(body, $"{title} · {CatLocalSettings.GetVolume(channel):P0}", 25, new Vector2(.10f, top - .06f), new Vector2(.90f, top));
        var area = Rect(body, channel + " Volume Slider", new Vector2(.12f, top - .13f), new Vector2(.88f, top - .065f));
        var track = Panel(area, "Track", new Vector2(0, .32f), new Vector2(1, .68f), new Color(.70f, .61f, .47f));
        RoundCollectionPanel(track);
        var fillArea = Rect(area, "Fill Area", new Vector2(.02f, .32f), new Vector2(.98f, .68f));
        var fill = Panel(fillArea, "Fill", Vector2.zero, Vector2.one, CozyButtonStyle.Green);
        var handleArea = Rect(area, "Handle Area", new Vector2(.02f, 0), new Vector2(.98f, 1));
        var handle = Panel(handleArea, "Handle", Vector2.zero, Vector2.one, new Color(1, .87f, .57f));
        handle.sizeDelta = new Vector2(32, 0);
        RoundCollectionPanel(handle);
        var slider = area.gameObject.AddComponent<Slider>();
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.SetValueWithoutNotify(CatLocalSettings.GetVolume(channel));
        slider.onValueChanged.AddListener(value =>
        {
            CatLocalSettings.SetVolume(channel, value);
            volumeText.text = $"{title} · {value:P0}";
        });
    }

    private void ShowLogoutConfirmation()
    {
        var body = SettingsBody("로그아웃");
        var message = Label(body, "진행 상황을 저장하고\n로그인 화면으로 돌아갈까요?\n\n계정과 고양이들은 삭제되지 않습니다.", 25,
            new Vector2(.09f, .34f), new Vector2(.91f, .79f));
        Button(body, "취소", new Vector2(.10f, .18f), new Vector2(.48f, .29f), () => { if (!settingsBusy) ShowSettings(); });
        Button(body, "로그아웃", new Vector2(.52f, .18f), new Vector2(.90f, .29f), async () =>
        {
            if (settingsBusy || saving) return;
            settingsBusy = true;
            message.text = "진행 상황을 저장하고 있습니다…";
            try
            {
                if (LogoutRequested == null) throw new InvalidOperationException("로그아웃을 준비하지 못했습니다.");
                await LogoutRequested();
            }
            catch (Exception error) { if (message != null) message.text = error.Message; }
            finally { settingsBusy = false; }
        });
    }
}
