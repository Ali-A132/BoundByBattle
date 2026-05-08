using System.Collections;
using Unity.VectorGraphics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterSelectController : MonoBehaviour {
    public GameObject reverseFade;
    public GameObject forwardFade;
    public Transform p1Cursor;
    public Transform p2Cursor;

    public GameObject mahskIdle;
    public GameObject mahskIdleFlipped;
    public GameObject payetIdle;
    public GameObject payetIdleFlipped;

    public GameObject mahskTitleCard;
    public GameObject payetTitleCard;
    public GameObject mahskTitleCardFlipped;
    public GameObject payetTitleCardFlipped;

    private Animator mahskAnim;
    private Animator mahskFlippedAnim;
    private Animator payetAnim;
    private Animator payetFlippedAnim;

    public AudioSource audioSource;
    public AudioSource musicSpeaker;
    public AudioClip[] moveSounds;
    private int moveSoundIndex = 0;
    public AudioClip selectSound1;
    public AudioClip selectSound2;
    public AudioClip backSounds;
    public AudioClip musicTrack;

    public Animator stageAnimator;

    public float[] positions = new float[3] { -1.414f, 0.051f, 1.483f };

    private int p1Index = 0;
    private int p2Index = 2;

    private bool p1Locked = false;
    private bool p2Locked = false;

    public GameObject chooseCharacterScreen;
    public GameObject stageSelectScreen;

    private int stageIndex = 0;
    private bool stageLocked = false;
    private bool inStageSelect = false;

    void Start() {
        if (MatchData.musicEnabled)
            musicSpeaker.volume = 0.15f;
        else
            musicSpeaker.volume = 0f;

        musicSpeaker.PlayOneShot(musicTrack);
        StartCoroutine(ReverseTransitionDelay());
        mahskAnim = mahskIdle.GetComponent<Animator>();
        mahskFlippedAnim = mahskIdleFlipped.GetComponent<Animator>();
        payetAnim = payetIdle.GetComponent<Animator>();
        payetFlippedAnim = payetIdleFlipped.GetComponent<Animator>();

        UpdateCursors();
        UpdateCharacterDisplay();

        stageSelectScreen.SetActive(false);
    }

    void Update() {
        if (!inStageSelect) {
            HandleP1Input();
            HandleP2Input();
        }
        else {
            if (!stageLocked)
                HandleStageInput();
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
        audioSource.PlayOneShot(selectSound1);
    }

    void PlayConfirm2Sound()
    {
        audioSource.PlayOneShot(selectSound2);
    }

    void HandleP1Input() {
        if (p1Locked) return;

        if (Input.GetKeyDown(KeyCode.A)) {
            PlayMoveSound();
            p1Index--;
            if (p1Index < 0) p1Index = 2;
            UpdateCursors();
            UpdateCharacterDisplay();
        }
        else if (Input.GetKeyDown(KeyCode.D)) {
            PlayMoveSound();
            p1Index = (p1Index + 1) % 3;
            UpdateCursors();
            UpdateCharacterDisplay();
        }

        if (Input.GetKeyDown(KeyCode.R)) {
            PlayConfirmSound();
            p1Locked = true;
            TriggerChosenAnimation(p1Index, true);
            Debug.Log("P1 Locked: " + p1Index);
            CheckBothLocked();
        }
    }

    void HandleP2Input() {
        if (p2Locked) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow)) {
            PlayMoveSound();
            p2Index--;
            if (p2Index < 0) p2Index = 2;
            UpdateCursors();
            UpdateCharacterDisplay();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow)) {
            PlayMoveSound();
            p2Index = (p2Index + 1) % 3;
            UpdateCursors();
            UpdateCharacterDisplay();
        }

        if (Input.GetKeyDown(KeyCode.P)) {
            PlayConfirm2Sound();
            p2Locked = true;
            TriggerChosenAnimation(p2Index, false);
            Debug.Log("P2 Locked: " + p2Index);
            CheckBothLocked();
        }
    }

    void UpdateCursors() {
        p1Cursor.localPosition = new Vector3(positions[p1Index], p1Cursor.localPosition.y, 0f);
        p2Cursor.localPosition = new Vector3(positions[p2Index], p2Cursor.localPosition.y, 0f);
    }


    void UpdateCharacterDisplay() {
        mahskIdle.SetActive(false);
        mahskIdleFlipped.SetActive(false);
        payetIdle.SetActive(false);
        payetIdleFlipped.SetActive(false);

        payetTitleCard.SetActive(false);
        mahskTitleCard.SetActive(false);
        payetTitleCardFlipped.SetActive(false);
        mahskTitleCardFlipped.SetActive(false);

        if (p1Index == 0) {
            mahskIdle.SetActive(true);
            mahskTitleCard.SetActive(true);
        }
        else if (p1Index == 2) {
            payetIdleFlipped.SetActive(true);
            payetTitleCardFlipped.SetActive(true);
        } if (p2Index == 0) {
            mahskIdleFlipped.SetActive(true);
            mahskTitleCardFlipped.SetActive(true);
        } else if (p2Index == 2) {
            payetIdle.SetActive(true);
            payetTitleCard.SetActive(true);
        }
    }

    void CheckBothLocked() {
        if (p1Locked && p2Locked) {
            StartCoroutine(HandleLockInDelay());
        }
    }

    IEnumerator HandleLockInDelay()
    {
        Debug.Log("Both players locked in!");

        int finalP1 = ResolveRandom(p1Index);
        int finalP2 = ResolveRandom(p2Index);

        Debug.Log("P1: " + finalP1);
        Debug.Log("P2: " + finalP2);

        yield return new WaitForSeconds(3f);

        foreach (Transform child in chooseCharacterScreen.transform)
        {
            child.gameObject.SetActive(false);
        }

        if (MatchData.stageIndex == 2)
        {
            MatchData.p1Character = (PlayerController.CharacterType)finalP1;
            MatchData.p2Character = (PlayerController.CharacterType)finalP2;
            SceneManager.LoadScene("TrainingRoom");
        }
        else
        {
            stageSelectScreen.SetActive(true);
            inStageSelect = true;
        }
    }

    int ResolveRandom(int index) {
        if (index == 1) {
            return Random.Range(0, 2) == 0 ? 0 : 2;
        }
        return index;
    }


    void HandleStageInput() {

        if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            int finalP1 = ResolveRandom(p1Index);
            int finalP2 = ResolveRandom(p2Index);
            MatchData.p1Character = (PlayerController.CharacterType)finalP1;
            MatchData.p2Character = (PlayerController.CharacterType)finalP2;
            Debug.Log("Cinematic Mode Triggered");
            musicSpeaker.Pause();
            StartCoroutine(LoadSceneWithTransition("CinematicMode"));
            return;
        }

        if (Input.GetKeyDown(KeyCode.W)) {
            if (stageIndex >= 2) {
                PlayMoveSound();
                stageIndex -= 2;
                stageAnimator.SetTrigger("Up");
            }
        }
        else if (Input.GetKeyDown(KeyCode.S)) {
            if (stageIndex <= 1) {
                PlayMoveSound();
                stageIndex += 2;
                stageAnimator.SetTrigger("Down");
            }
        }
        else if (Input.GetKeyDown(KeyCode.A)) {
            if (stageIndex % 2 == 1) {
                PlayMoveSound();
                stageIndex -= 1;
                stageAnimator.SetTrigger("Left");
            }
        }
        else if (Input.GetKeyDown(KeyCode.D)) {
            if (stageIndex % 2 == 0) {
                PlayMoveSound();
                stageIndex += 1;
                stageAnimator.SetTrigger("Right");
            }
        }

        if (Input.GetKeyDown(KeyCode.Return)) {
            PlayBackSound();
            LockStage();
        }
    }
    void TriggerChosenAnimation(int index, bool isP1)
    {

        if (index == 1) return;

        if (isP1) {
            if (index == 0) {
                mahskAnim.SetTrigger("Chosen");
            } else if (index == 2) {
                payetFlippedAnim.SetTrigger("Chosen");
            }
        } else {
            if (index == 0) {
                mahskFlippedAnim.SetTrigger("Chosen");
            } else if (index == 2) {
                payetAnim.SetTrigger("Chosen");
            }
        }
    }

    public void ExitButton()
    {
        StartCoroutine(LoadSceneWithTransition("MainMenu"));
    }

    System.Collections.IEnumerator ReverseTransitionDelay()
    {
        reverseFade.SetActive(true);
        yield return new WaitForSeconds(1.2f);
        reverseFade.SetActive(false);
    }

    System.Collections.IEnumerator LoadSceneWithTransition(string sceneName)
    {
        PlayBackSound();
        forwardFade.SetActive(true);

        yield return new WaitForSeconds(1.2f);

        SceneManager.LoadScene(sceneName);
    }

    void LockStage() {
        stageLocked = true;
        int finalP1 = ResolveRandom(p1Index);
        int finalP2 = ResolveRandom(p2Index);
        MatchData.p1Character = (PlayerController.CharacterType)finalP1;
        MatchData.p2Character = (PlayerController.CharacterType)finalP2;
        MatchData.stageIndex = stageIndex;

        string sceneName = stageIndex switch {
            0 => "MoonColony",
            1 => "HouseOfWaffles",
            2 => "RoomOfSpaceAndTime",
            3 => "DojoInTheSky",
            _ => "MoonColony"
        };
        musicSpeaker.Pause();
        StartCoroutine(LoadSceneWithTransition(sceneName));
    }
}