using UnityEngine;
using UnityEngine.Audio;

public class SettingsMenu : MonoBehaviour
{
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public Animator animator;
    public GameObject settingsButtons;
    public GameObject loreButton;
    public GameObject creditsButtons;
    public AudioSource audioSource;
    public AudioClip[] moveSounds;
    public AudioClip backSounds;
    private int moveSoundIndex = 0;

    private void Awake()
    {
        loreButton.SetActive(false);
        creditsButtons.SetActive(false);
    }
    void PlayMoveSound()
    {
        if (moveSounds.Length == 0 || audioSource == null) return;
        audioSource.PlayOneShot(moveSounds[moveSoundIndex]);
        moveSoundIndex = (moveSoundIndex + 1) % moveSounds.Length;
    }

    void PlayBackSound()
    {
        audioSource.PlayOneShot(backSounds);
    }

    public void OnCreditClick() {
        PlayMoveSound();
        settingsButtons.SetActive(false);
        creditsButtons.SetActive(true);
        animator.SetTrigger("Credits");
    }


    public void OnLoreClick() {
        PlayMoveSound();
        settingsButtons.SetActive(false);
        loreButton.SetActive(true);
        animator.SetTrigger("Lore");
    }

    public void OnMahskClick()
    {
        PlayMoveSound();
        animator.SetTrigger("Mahsk");
    }

    public void OnPayetClick()
    {
        PlayMoveSound();
        animator.SetTrigger("Payet");
    }


    public void OnBackClick()
    {
        PlayBackSound();
        animator.SetTrigger("Back");
        settingsButtons.SetActive(true);
        loreButton.SetActive(false);
        creditsButtons.SetActive(false);
    }


    public void OnSettingBackClick()
    {
        PlayBackSound();
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }

    public void MusicOnOff()
    {
        MatchData.musicEnabled = !MatchData.musicEnabled;

        if (MatchData.musicEnabled)
            PlayBackSound();
        else
            PlayBackSound();
    }

}
