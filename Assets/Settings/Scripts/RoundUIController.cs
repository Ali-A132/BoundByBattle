using System.Collections;
using UnityEngine;

public class RoundUIController : MonoBehaviour {
    public GameObject[] roundObjects;
    public GameObject startObject;
    Coroutine currentRoutine;

    public AudioSource roundUiSoundSpeaker;
    public AudioClip roundStart;

    public float roundDisplayTime = 1.4f;
    public float startDisplayTime = 1f;
    bool isPlaying = false;

    public IEnumerator PlayRoundIntro(int roundNumber) {
        if (isPlaying) {
            if (currentRoutine != null)
                StopCoroutine(currentRoutine);
        }
        roundUiSoundSpeaker.PlayOneShot(roundStart);
        currentRoutine = StartCoroutine(PlaySequence(roundNumber));
        yield return currentRoutine;
    }

    IEnumerator PlaySequence(int roundNumber) {
        isPlaying = true;

        if (roundNumber < 1 || roundNumber > roundObjects.Length) {
            Debug.LogWarning("Invalid round number");
            isPlaying = false;
            yield break;
        }

        HideAll();

        yield return null;
        GameObject roundObj = roundObjects[roundNumber - 1];
        roundObj.SetActive(true);
        yield return null;
        RestartAnimator(roundObj);
        yield return new WaitForSeconds(roundDisplayTime);
        roundObj.SetActive(false);

        if (startObject != null) {
            startObject.SetActive(true);
            yield return null;
            RestartAnimator(startObject);
            yield return new WaitForSeconds(startDisplayTime);
            startObject.SetActive(false);
        }

        isPlaying = false;
        currentRoutine = null;
    }

    void HideAll() {
        foreach (var obj in roundObjects) {
            if (obj != null)
                obj.SetActive(false);
        }

        if (startObject != null)
            startObject.SetActive(false);
    }

    void RestartAnimator(GameObject obj) {
        if (!obj.TryGetComponent<Animator>(out var anim)) return;

        anim.enabled = false;
        anim.enabled = true;
        anim.Rebind();
        anim.Update(0f);
    }
}
