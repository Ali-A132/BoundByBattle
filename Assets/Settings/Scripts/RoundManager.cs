using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RoundManager : NetworkBehaviour
{
    protected Vector3 p1StartPos;
    protected Vector3 p2StartPos;
    public PlayerController player1;
    public PlayerController player2;
    public Image[] p1_Rounds;
    public Image[] p2_Rounds;
    public Image fadeImage;
    public CountDownTimer roundTimer;
    public RoundUIController roundWorldUI;
    public GameObject blackScreen;
    public GameObject exit;
    public AudioSource timeoutSpeaker;
    public AudioSource musicSpeaker;
    public AudioClip[] musicTracks;
    public AudioClip pauseMenu;
    public AudioClip timeOutSound;

    public Transform victoryCenterPoint;
    public GameObject victoriousHomeScreen;
    public Unity.Cinemachine.CinemachineTargetGroup targetGroup;
    public GameObject inGameUI;

    public float fadeDuration = 2f;
    public int roundsToWin = 3;
    public float roundResetDelay = 2f;
    protected int p1Wins = 0;
    protected int p2Wins = 0;
    protected int currentRound = 1;
    public bool roundOver = false;
    protected bool roundStarting = false;
    protected bool tieGame = false;
    protected PlayerController matchWinner = null;

    protected virtual void Start()
    {
        if (MatchData.musicEnabled)
            musicSpeaker.volume = 0.15f;
        else
            musicSpeaker.volume = 0f;
        StartCoroutine(PlayMusicPlaylist());
        DelayedStart();
    }

    IEnumerator PlayMusicPlaylist()
    {
        if (musicTracks.Length == 0 || musicSpeaker == null) yield break;

        int index = 0;

        while (true)  {
            AudioClip clip = musicTracks[index];

            musicSpeaker.clip = clip;
            musicSpeaker.Play();

            yield return new WaitForSeconds(clip.length);

            index = (index + 1) % musicTracks.Length;
        }
    }

    protected virtual void DelayedStart()
    {
        // yield return null;
        p1StartPos = player1.transform.position;
        p2StartPos = player2.transform.position;
        StartCoroutine(BeginMatch());
    }

    protected virtual IEnumerator BeginMatch()
    {
        yield return StartCoroutine(Fade(1f, 1f));
        currentRound = 1;
        yield return StartCoroutine(StartRoundSequence());
    }

    protected virtual IEnumerator StartRoundSequence()
    {
        if (roundStarting) yield break;

        roundStarting = true;
        roundOver = true;

        player1.LockControls();
        player2.LockControls();

        roundTimer.ResetTimer();
        yield return StartCoroutine(Fade(1f, 0f));
        if (roundWorldUI != null)
            yield return StartCoroutine(roundWorldUI.PlayRoundIntro(currentRound));

        player1.UnlockControls();
        player2.UnlockControls();
        roundOver = false;
        roundTimer.StartTimer();
        roundStarting = false;
    }

    protected virtual void EndRound()
    {
        roundOver = true;
        roundTimer.StopTimer();
    }

    public virtual void OnPlayerKO(PlayerController loser)
    {
        if (roundOver) return;
        StartCoroutine(ProcessKO(loser));
    }

    protected virtual IEnumerator ProcessKO(PlayerController loser)
    {
        yield return null;
        if (roundOver) yield break;

        EndRound();
        bool isTie = player1.currHealth <= 0f && player2.currHealth <= 0f;

        if (isTie)
        {
            tieGame = true;
        }
        else
        {
            tieGame = false;
            PlayerController winner = loser == player1 ? player2 : player1;

            if (loser == player1) { p2Wins++; UpdateRoundUI(p2_Rounds, p2Wins); }
            else { p1Wins++; UpdateRoundUI(p1_Rounds, p1Wins); }

            winner?.PlayVictoryTauntDelayed(2f);
        }
        CheckMatchEnd();
    }

    public virtual void OnTimeOver()
    {
        if (roundOver) return;
        EndRound();
        timeoutSpeaker.PlayOneShot(timeOutSound);
        PlayerController winner = null;
        if (player1.currHealth > player2.currHealth) winner = player1;
        else if (player2.currHealth > player1.currHealth) winner = player2;

        if (winner != null)
        {
            tieGame = false;
            winner.PlayVictoryTauntDelayed(2.5f);
            if (winner == player1) { p1Wins++; UpdateRoundUI(p1_Rounds, p1Wins); }
            else { p2Wins++; UpdateRoundUI(p2_Rounds, p2Wins); }
        }
        else
        {
            tieGame = true;
            player1.PlayVictoryTauntDelayed(2f);
            player2.PlayVictoryTauntDelayed(2f);
        }
        CheckMatchEnd();
    }

    protected virtual void CheckMatchEnd()
    {
        if (p1Wins >= roundsToWin || p2Wins >= roundsToWin)
        {
            matchWinner = p1Wins >= roundsToWin ? player1 : player2;
            EndMatch();
        }
        else
        {
            if (!tieGame) currentRound++;
            StartCoroutine(RoundTransition());
        }
    }

    protected virtual void EndMatch()
    {
        roundTimer.StopTimer();
        StartCoroutine(VictorySequence());
    }

    IEnumerator VictorySequence()
    {
        exit.SetActive(true);
        yield return new WaitForSeconds(5f);

        yield return StartCoroutine(Fade(0f, 1f));

        blackScreen.SetActive(true);
        yield return new WaitForSeconds(1f);

        PlayerController loser = matchWinner == player1 ? player2 : player1;
        DisablePlayerVisuals(loser);

        if (targetGroup != null)
        {
            targetGroup.Targets = new System.Collections.Generic.List<Unity.Cinemachine.CinemachineTargetGroup.Target>
            {
                new Unity.Cinemachine.CinemachineTargetGroup.Target
                {
                    Object = matchWinner.transform,
                    Weight = 1f,
                    Radius = 1f
                }
            };
        }

        if (inGameUI != null) inGameUI.SetActive(false);

        if (victoryCenterPoint != null)
            matchWinner.transform.position = victoryCenterPoint.position;

        matchWinner.LockControls();
        Animator winnerAnim = matchWinner.GetComponent<Animator>();
        if (winnerAnim != null)
        {
            winnerAnim.speed = 1f;
            winnerAnim.Play("Idle", 0, 0f);
        }

        yield return StartCoroutine(Fade(1f, 0f));

        victoriousHomeScreen.SetActive(true);
        blackScreen.SetActive(false);
    }

    void DisablePlayerVisuals(PlayerController player)
    {
        foreach (var sr in player.GetComponentsInChildren<SpriteRenderer>(true))
            sr.enabled = false;

        foreach (var col in player.GetComponentsInChildren<Collider2D>(true))
            col.enabled = false;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;
    }

    protected virtual IEnumerator RoundTransition()
    {
        float delay = tieGame ? 3f : roundResetDelay;
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(Fade(0f, 1f));

        player1.LockControls();
        player2.LockControls();

        ResetPlayersPosition();
        player1.ResetForNewRound();
        player2.ResetForNewRound();

        yield return new WaitForSeconds(0.25f);
        yield return StartCoroutine(StartRoundSequence());
    }

    protected void UpdateRoundUI(Image[] rounds, int wins)
    {
        for (int i = 0; i < rounds.Length; i++)
            rounds[i].enabled = i < wins;
    }

    protected void ResetPlayersPosition()
    {
        player1.transform.position = p1StartPos;
        player2.transform.position = p2StartPos;
        var rb1 = player1.GetComponent<Rigidbody2D>();
        var rb2 = player2.GetComponent<Rigidbody2D>();
        if (rb1) rb1.linearVelocity = Vector2.zero;
        if (rb2) rb2.linearVelocity = Vector2.zero;
    }

    public void ExitButton() {
        musicSpeaker.PlayOneShot(pauseMenu);
        SceneManager.LoadScene("MainMenu");
    }

    protected IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        Color c = fadeImage.color;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(from, to, t / fadeDuration);
            fadeImage.color = new Color(c.r, c.g, c.b, a);
            yield return null;
        }
        fadeImage.color = new Color(c.r, c.g, c.b, to);
    }


}