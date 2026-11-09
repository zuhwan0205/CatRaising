using UnityEngine;
using UnityEngine.SceneManagement;

public enum CatSound
{
    Arrow, Button, Meow, Claw, Equip, Fire, Reward, Magnet, Hammer,
    Ice, Knife, Lose, Magic, Chest, Shield, Victory
}

// A bounded, shared voice pool keeps combat from creating an AudioSource per hit.
public sealed class CatSoundEffects : MonoBehaviour
{
    private static CatSoundEffects instance;
    private static bool combatAudible = true;
    private static readonly string[] Paths = {
        "Arrow_Attack", "Button", "Cat Meow", "Claw_Attack", "Equip", "Fire_Attack",
        "Get_Character", "Get_Item01", "Hammer_Attack", "Ice_Attack", "Knife_Attack",
        "Lose", "Magic_Attack", "PickUP_01", "Shield_Attack", "Victory"
    };
    private readonly AudioClip[] clips = new AudioClip[Paths.Length];
    private readonly bool[] loaded = new bool[Paths.Length];
    private readonly float[] nextPlay = new float[Paths.Length];
    private readonly AudioSource[] voices = new AudioSource[12];
    private int combatCursor, uiCursor;
    private AudioListener listener;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        combatAudible = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance == null) new GameObject("Cat Sound Effects").AddComponent<CatSoundEffects>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        // All current sounds are 2D. Keep their listener independent of scene cameras.
        listener = gameObject.AddComponent<AudioListener>();
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureSingleListener();
        for (int i = 0; i < voices.Length; i++)
        {
            var child = new GameObject(i < 8 ? "Combat Voice" : "UI Voice");
            child.transform.SetParent(transform, false);
            var source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.ignoreListenerPause = i >= 8;
            child.AddComponent<CatAudioChannelSource>();
            voices[i] = source;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureSingleListener();

    private void EnsureSingleListener()
    {
        foreach (var other in FindObjectsByType<AudioListener>(FindObjectsInactive.Include))
            if (other != listener) other.enabled = false;
        listener.enabled = true;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    public static void Play(CatSound sound, bool combat = false)
    {
        if (!Application.isPlaying || (combat && !combatAudible)) return;
        Initialize();
        instance.PlayInternal(sound, combat);
    }

    public static void SetCombatAudible(bool audible)
    {
        combatAudible = audible;
        if (audible || instance == null) return;
        // Cut off ongoing attack/pickup sounds too; leave the UI voices alone.
        for (int i = 0; i < 8; i++)
            if (instance.voices[i] != null) instance.voices[i].Stop();
    }

    private void PlayInternal(CatSound sound, bool combat)
    {
        int index = (int)sound;
        float now = Time.unscaledTime;
        if (now < nextPlay[index]) return;
        if (!loaded[index])
        {
            loaded[index] = true;
            clips[index] = Resources.Load<AudioClip>("SFX, BGM/" + Paths[index]);
            if (clips[index] == null) Debug.LogWarning("Missing sound: " + Paths[index]);
        }
        if (clips[index] == null) return;
        nextPlay[index] = now + (combat ? .075f : .04f);
        int start = combat ? 0 : 8, count = combat ? 8 : 4;
        int slot = -1;
        for (int i = start; i < start + count; i++)
            if (!voices[i].isPlaying) { slot = i; break; }
        if (slot < 0) slot = combat ? combatCursor++ % 8 : 8 + uiCursor++ % 4;
        var voice = voices[slot];
        voice.Stop();
        voice.GetComponent<CatAudioChannelSource>().BaseVolume = combat && sound != CatSound.Magnet ? .7f : 1f;
        voice.clip = clips[index];
        voice.Play();
    }

    public static void Attack(string characterId)
    {
        switch (characterId)
        {
            case "cat_starter": case "cat_spinner": case "cat_kitten_trio": case "cat_dancer": Play(CatSound.Claw, true); break;
            case "cat_knight": case "cat_celestial": Play(CatSound.Knife, true); break;
            case "cat_ninja": Play(CatSound.Arrow, true); break;
            case "cat_tiger": Play(CatSound.Hammer, true); break;
            case "cat_guardian": Play(CatSound.Shield, true); break;
            case "cat_shadow": Play(CatSound.Ice, true); break;
            case "cat_lion": Play(CatSound.Fire, true); break;
            case "cat_moon": Play(CatSound.Magic, true); break;
        }
    }
}
