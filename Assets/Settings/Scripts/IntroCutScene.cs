using UnityEngine;
using System.Collections;

public class IntroCutsceneController : MonoBehaviour
{
    public PlayerController payet;
    public PlayerController mahsk;

    public GameObject uiCanvas; 
    public RoundManager roundManager;

    public float payetSpeechDuration = 4f;
    public float mahskSpeechDuration = 4f;

    void Start()
    {
        StartCoroutine(PlayCutscene());
    }

    IEnumerator PlayCutscene()
    {
        if (uiCanvas != null)
            uiCanvas.SetActive(false);

        LockPlayer(payet);
        LockPlayer(mahsk);

        yield return new WaitForSeconds(0.5f); 

        PlaySpeech(payet);
        yield return new WaitForSeconds(payetSpeechDuration);

        PlaySpeech(mahsk);
        yield return new WaitForSeconds(mahskSpeechDuration);

        if (uiCanvas != null)
            uiCanvas.SetActive(true);

        UnlockPlayer(payet);
        UnlockPlayer(mahsk);

        yield return new WaitForSeconds(1.5f);

        roundManager.gameObject.SetActive(true); 
    }

    void PlaySpeech(PlayerController player)
    {
        if (player == null) return;

        // player.animator.Play("Speech", 0, 0f);

        if (player.shadowAnimator != null)
            player.shadowAnimator.Play("Speech", 0, 0f);
    }

    void LockPlayer(PlayerController player)
    {
        if (player == null) return;

        player.canMove = false;
        player.controlsLocked = true;
    }

    void UnlockPlayer(PlayerController player)
    {
        if (player == null) return;

        player.controlsLocked = false;
        player.canMove = true;
    }
}