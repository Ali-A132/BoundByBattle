using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour {
    public Animator animator;
    public int currentIndex = 0;
    private int maxIndex = 3;
    private bool isMoving = false;
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject controlsPage;
    public GameObject transitionFade;
    public GameObject reverseFade;

    public AudioSource audioSource;
    public AudioClip[] moveSounds;
    private int moveSoundIndex = 0;
    public AudioClip selectSounds;
    public AudioClip backSounds;
    private bool menuBusy = false;

    void Start() {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "FirstMainMenu")
        {
            StartCoroutine(AltReverseTransitionDelay());
        }
        else
        {
            StartCoroutine(ReverseTransitionDelay());
        }
    }

    void Update() {
        if (isMoving || menuBusy) return;

        if (Input.GetKeyDown(KeyCode.S))
        {
            MoveDown();
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            MoveUp();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            SelectOption();
        }
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


    void PlayConfirmSound()
    {
        audioSource.PlayOneShot(selectSounds);
    }

    private void OnEnable()
    {
        currentIndex = 0;
    }

    void MoveDown()
    {
        if (isMoving || menuBusy) return;

        animator.ResetTrigger("Up");
        animator.ResetTrigger("Down");
        currentIndex = (currentIndex + 1) % (maxIndex + 1);
        animator.SetTrigger("Down");
        PlayMoveSound();
        StartCoroutine(MoveCooldown());
    }

    void MoveUp()
    {
        if (isMoving || menuBusy) return;

        animator.ResetTrigger("Up");
        animator.ResetTrigger("Down");
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = maxIndex;
        animator.SetTrigger("Up");
        PlayMoveSound();
        StartCoroutine(MoveCooldown());
    }

    void SelectOption() {
        if (menuBusy) return;

        menuBusy = true;
        switch (currentIndex)
        {
            case 0:
                MatchData.stageIndex = 0;
                StartCoroutine(LoadSceneWithTransition("CharacterSelect"));
                break;

            case 1:
                currentIndex = 0;
                StartCoroutine(LoadSceneWithTransition("OnlineMoonColony"));
                break;

            case 2:
                PlayConfirmSound();
                controlsPage.SetActive(true);
                animator.SetTrigger("Enter");
                StartCoroutine(UnlockMenuAfterDelay(1f));
                break;

            case 3:
                StartCoroutine(OpenSettingsRoutine());
                break;
        }
    }
    System.Collections.IEnumerator OpenSettingsRoutine()
    {
        PlayConfirmSound();
        yield return new WaitForSeconds(0.01f);

        settingsPanel.SetActive(true);
        mainMenuPanel.SetActive(false);

        menuBusy = false;
    }

    System.Collections.IEnumerator UnlockMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        menuBusy = false;
    }


    public void OnBackToMainMenu() {
        PlayBackSound();
        animator.SetTrigger("Back");
        menuBusy = false;
    }

    public void OnBack() {
        PlayBackSound();
        animator.SetTrigger("Back");
        controlsPage.SetActive(false);
        menuBusy = false;
    }
    public void MusicOnOff()
    {
        MatchData.musicEnabled = !MatchData.musicEnabled;

        if (MatchData.musicEnabled)
            PlayConfirmSound();
        else
            PlayBackSound();
    }

    System.Collections.IEnumerator MoveCooldown()
    {
        isMoving = true;
        yield return new WaitForSeconds(0.2f);
        isMoving = false;
    }


    System.Collections.IEnumerator LoadSceneWithTransition(string sceneName)
    {
        PlayConfirmSound();
        isMoving = true;

        transitionFade.SetActive(true);

        yield return new WaitForSeconds(5.2f);

        SceneManager.LoadScene(sceneName);
    }

    System.Collections.IEnumerator SlightDelay()
    {
        PlayConfirmSound();
        yield return new WaitForSeconds(2.5f);

    }

    System.Collections.IEnumerator ReverseTransitionDelay()
    {
        reverseFade.SetActive(true);
        yield return new WaitForSeconds(1.2f);
        reverseFade.SetActive(false);
    }

    System.Collections.IEnumerator AltReverseTransitionDelay()
    {
        reverseFade.SetActive(true);
        yield return new WaitForSeconds(6.6f);
        reverseFade.SetActive(false);
    }
}