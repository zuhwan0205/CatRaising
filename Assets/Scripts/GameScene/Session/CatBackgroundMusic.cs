using UnityEngine;
using UnityEngine.SceneManagement;

// One persistent music source switches tracks when the active scene changes.
public sealed class CatBackgroundMusic : MonoBehaviour
{
    private static CatBackgroundMusic instance;
    private AudioSource source;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance == null)
            new GameObject("Cat Background Music").AddComponent<CatBackgroundMusic>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        var audioChannel = gameObject.AddComponent<CatAudioChannelSource>();
        audioChannel.Channel = CatAudioChannel.Bgm;
        audioChannel.BaseVolume = .3f;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void Start() => PlayScene(SceneManager.GetActiveScene());

    private void OnActiveSceneChanged(Scene previous, Scene next) => PlayScene(next);

    private void PlayScene(Scene scene)
    {
        string path;
        switch (scene.name)
        {
            case "LoadingScene": path = "BGM/LoadingBGM"; break;
            case "GameScene": path = "BGM/MainBGM"; break;
            case "DungeonScene": path = "BGM/DeongunBGM"; break;
            case "RaidScene": path = "BGM/RaidBGM"; break;
            default: source.Stop(); source.clip = null; return;
        }

        var clip = Resources.Load<AudioClip>(path);
        if (clip != null && source.clip == clip && source.isPlaying) return;
        source.Stop();
        source.clip = clip;
        if (clip != null) source.Play();
        else Debug.LogWarning("Missing background music: " + path);
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        if (instance == this) instance = null;
    }
}
