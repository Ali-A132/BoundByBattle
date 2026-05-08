using Unity.Netcode;
using UnityEngine;
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

    void Start() {
        StartCoroutine(ReverseTransitionDelay());
    }

    void Update() {
        if (isMoving) return;

        if (Input.GetKeyDown(KeyCode.S)) {
            MoveDown();
        }
        else if (Input.GetKeyDown(KeyCode.W)) {
            MoveUp();
        }

        if (Input.GetKeyDown(KeyCode.Return)) {
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

    void MoveDown() {
        if (isMoving) return;
        currentIndex = (currentIndex + 1) % (maxIndex + 1);
        animator.SetTrigger("Down");
        PlayMoveSound();
        StartCoroutine(MoveCooldown());
    }

    void MoveUp() {
        if (isMoving) return;
        currentIndex--;
        if (currentIndex < 0)
            currentIndex = maxIndex;

        animator.SetTrigger("Up");
        PlayMoveSound();
        StartCoroutine(MoveCooldown());
    }

    void SelectOption() {
        switch (currentIndex) {
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
                break;
            case 3:
                StartCoroutine(SlightDelay());
                settingsPanel.SetActive(true);
                mainMenuPanel.SetActive(false);
                break;
        }
    }

    public void OnBackToMainMenu() {
        PlayBackSound();
        animator.SetTrigger("Back");
    }

    public void OnBack()
    {
        PlayBackSound();
        animator.SetTrigger("Back");
        controlsPage.SetActive(false);
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

        yield return new WaitForSeconds(1.2f);

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
}