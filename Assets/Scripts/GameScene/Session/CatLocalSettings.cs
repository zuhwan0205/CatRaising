using UnityEngine;

// 계정의 성장 데이터와 별개인 기기 설정입니다.
public static class CatLocalSettings
{
    private const string Prefix = "CatRaising.Settings.";
    public static event System.Action AudioChanged;
    public static float GetVolume(CatAudioChannel channel) => Mathf.Clamp01(PlayerPrefs.GetFloat(
        Prefix + "Audio." + channel, PlayerPrefs.GetFloat(Prefix + "Volume", 1f)));
    public static bool Muted => PlayerPrefs.GetInt(Prefix + "Muted", 0) != 0;
    public static int FrameRate => PlayerPrefs.GetInt(Prefix + "FrameRate", 60) == 30 ? 30 : 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Apply()
    {
        AudioListener.volume = Muted ? 0f : 1f;
        AudioChanged?.Invoke();
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = FrameRate;
    }

    public static void SetVolume(CatAudioChannel channel, float value)
    {
        PlayerPrefs.SetFloat(Prefix + "Audio." + channel, Mathf.Clamp01(value));
        AudioChanged?.Invoke();
    }

    public static void SetMuted(bool value)
    {
        PlayerPrefs.SetInt(Prefix + "Muted", value ? 1 : 0);
        Apply();
        Flush();
    }

    public static void SetFrameRate(int value)
    {
        PlayerPrefs.SetInt(Prefix + "FrameRate", value == 30 ? 30 : 60);
        Apply();
        Flush();
    }

    public static void Flush() => PlayerPrefs.Save();
}
