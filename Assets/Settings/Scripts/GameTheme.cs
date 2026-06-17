using UnityEngine;
using UnityEngine.Audio;

public class GameTheme : MonoBehaviour
{
    public AudioSource musicSpeaker;
    public AudioClip musicTrack;

    void Start()
    {
        musicSpeaker.clip = musicTrack;
        musicSpeaker.loop = true;
        musicSpeaker.Play();
    }

    private void Update()
    {
        if (MatchData.musicEnabled)
            musicSpeaker.volume = 0.15f;
        else
            musicSpeaker.volume = 0f;
    }

}
