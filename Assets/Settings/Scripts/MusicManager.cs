using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    public AudioSource[] musicSources;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApplyMusicSetting();
    }

    public void ApplyMusicSetting()
    {
        float volume = MatchData.musicEnabled ? 1f : 0f;
        foreach (var source in musicSources)
        {
            if (source != null)
                source.volume = volume;
        }
    }

    public void SetMusicEnabled(bool enabled)
    {
        MatchData.musicEnabled = enabled;
        ApplyMusicSetting();
    }
}