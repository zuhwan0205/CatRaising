using UnityEngine;

public enum CatAudioChannel { Bgm, Combat }

// AudioSource와 같은 오브젝트에 추가하여 소리 종류를 지정합니다.
[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public sealed class CatAudioChannelSource : MonoBehaviour
{
    [SerializeField] private CatAudioChannel channel = CatAudioChannel.Combat;
    [SerializeField, Range(0f, 1f)] private float baseVolume = 1f;
    private AudioSource source;

    public CatAudioChannel Channel
    {
        get => channel;
        set { channel = value; ApplyVolume(); }
    }

    private void OnEnable()
    {
        source = GetComponent<AudioSource>();
        CatLocalSettings.AudioChanged += ApplyVolume;
        ApplyVolume();
    }

    private void OnDisable() => CatLocalSettings.AudioChanged -= ApplyVolume;

    private void OnValidate()
    {
        if (Application.isPlaying) ApplyVolume();
    }

    private void ApplyVolume()
    {
        if (source == null) source = GetComponent<AudioSource>();
        if (source == null) return;
        source.ignoreListenerVolume = false;
        source.volume = baseVolume * CatLocalSettings.GetVolume(channel);
    }
}
