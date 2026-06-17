using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Cinemachine;

public class OnlineRoundManager : RoundManager
{
    public OnlineCountDownTimer onlineRoundTimer;
    public OnlineMatchSetup matchSetup;
    public GameObject digits;
    public AudioClip musicTrack;
    protected override void Start() {
        musicSpeaker.PlayOneShot(musicTrack);
    }

    public void BeginOnlineMatch()
    {
        if (!IsServer) return;
        musicSpeaker.Stop();
        StartCoroutine(OnlineDelayedStart());
    }

    IEnumerator OnlineDelayedStart()
    {
        musicSpeaker.Stop();
        while (player1 == null || player2 == null)
            yield return null;

        p1StartPos = player1.transform.position;
        p2StartPos = player2.transform.position;
        SyncStartPositionsClientRpc(p1StartPos, p2StartPos);

        StartCoroutine(OnlineBeginMatch());
    }

    IEnumerator OnlineBeginMatch()
    {
        FadeClientRpc(1f, 1f);
        yield return new WaitForSeconds(fadeDuration);
        currentRound = 1;
        yield return StartCoroutine(OnlineStartRoundSequence());
    }

    IEnumerator OnlineStartRoundSequence()
    {
        if (roundStarting) yield break;

        roundStarting = true;
        roundOver = true;
        SetRoundOverClientRpc(true);

        LockBothClientRpc();
        onlineRoundTimer.ResetTimer();

        FadeClientRpc(1f, 0f);
        yield return new WaitForSeconds(fadeDuration);

        PlayRoundIntroClientRpc(currentRound);
        float introDuration = roundWorldUI != null
            ? roundWorldUI.roundDisplayTime + roundWorldUI.startDisplayTime
            : 0f;
        yield return new WaitForSeconds(introDuration);

        matchSetup.UnfreezePlayersY();
        roundOver = false;
        SetRoundOverClientRpc(false);
        UnlockBothClientRpc();
        onlineRoundTimer.StartTimer();
        roundStarting = false;
    }

    protected override void EndRound()
    {
        roundOver = true;
        SetRoundOverClientRpc(true);
        onlineRoundTimer.StopTimer();
    }

    public override void OnPlayerKO(PlayerController loser)
    {
        if (!IsServer || roundOver) return;
        StartCoroutine(ProcessOnlineKO(loser));
    }

    IEnumerator ProcessOnlineKO(PlayerController loser)
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
            if (loser == player1)
            {
                p2Wins++;
                UpdateRoundUIClientRpc(false, p2Wins);
                PlayVictoryClientRpc(false, 2f);
            }
            else
            {
                p1Wins++;
                UpdateRoundUIClientRpc(true, p1Wins);
                PlayVictoryClientRpc(true, 2f);
            }
        }
        CheckOnlineMatchEnd();
    }

    public override void OnTimeOver()
    {
        if (!IsServer || roundOver) return;
        EndRound();

        if (player1.currHealth > player2.currHealth)
        {
            tieGame = false;
            p1Wins++;
            UpdateRoundUIClientRpc(true, p1Wins);
            PlayVictoryClientRpc(true, 1.5f);
        }
        else if (player2.currHealth > player1.currHealth)
        {
            tieGame = false;
            p2Wins++;
            UpdateRoundUIClientRpc(false, p2Wins);
            PlayVictoryClientRpc(false, 1.5f);
        }
        else
        {
            tieGame = true;
            PlayVictoryClientRpc(true, 2f);
            PlayVictoryClientRpc(false, 2f);
        }

        CheckOnlineMatchEnd();
    }

    void CheckOnlineMatchEnd()
    {
        if (p1Wins >= roundsToWin || p2Wins >= roundsToWin)
        {
            bool p1Wins = this.p1Wins >= roundsToWin;
            EndOnlineMatch(p1Wins);
        }
        else
        {
            if (!tieGame) currentRound++;
            StartCoroutine(OnlineRoundTransition());
        }
    }

    void EndOnlineMatch(bool p1Won)
    {
        onlineRoundTimer.StopTimer();
        VictorySequenceClientRpc(p1Won);
    }

    [ClientRpc]
    void VictorySequenceClientRpc(bool p1Won)
    {
        PlayerController winner = p1Won ? player1 : player2;
        PlayerController loser = p1Won ? player2 : player1;
        StartCoroutine(OnlineVictorySequence(winner, loser));
    }

    IEnumerator OnlineVictorySequence(PlayerController winner, PlayerController loser)
    {
        if (exit != null) exit.SetActive(true);

        yield return new WaitForSeconds(5f);

        yield return StartCoroutine(FadeRoutine(0f, 1f));

        if (blackScreen != null) blackScreen.SetActive(true);

        yield return new WaitForSeconds(3f);
        digits.SetActive(false);

        DisableOnlinePlayerVisuals(loser);

        if (targetGroup != null)
        {
            targetGroup.Targets = new List<CinemachineTargetGroup.Target>
            {
                new CinemachineTargetGroup.Target
                {
                    Object = winner.transform,
                    Weight = 1f,
                    Radius = 1f
                }
            };
        }

        if (inGameUI != null) inGameUI.SetActive(false);

        if (victoryCenterPoint != null)
            winner.transform.position = victoryCenterPoint.position;

        winner.LockControls();
        Animator winnerAnim = winner.GetComponent<Animator>();
        if (winnerAnim != null)
        {
            winnerAnim.speed = 1f;
            winnerAnim.Play("Idle", 0, 0f);
        }

        yield return StartCoroutine(FadeRoutine(1f, 0f));

        if (victoriousHomeScreen != null) victoriousHomeScreen.SetActive(true);
        if (blackScreen != null) blackScreen.SetActive(false);
    }

    void DisableOnlinePlayerVisuals(PlayerController player)
    {
        foreach (var sr in player.GetComponentsInChildren<SpriteRenderer>(true))
            sr.enabled = false;
        foreach (var col in player.GetComponentsInChildren<Collider2D>(true))
            col.enabled = false;
        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;
    }

    IEnumerator OnlineRoundTransition()
    {
        float delay = tieGame ? 3f : roundResetDelay;
        yield return new WaitForSeconds(delay);

        FadeClientRpc(0f, 1f);
        yield return new WaitForSeconds(fadeDuration);

        LockBothClientRpc();
        ResetPositionsClientRpc(p1StartPos, p2StartPos);
        ResetBothClientRpc();

        yield return new WaitForSeconds(0.25f);
        yield return StartCoroutine(OnlineStartRoundSequence());
    }


    [ClientRpc]
    void SyncStartPositionsClientRpc(Vector3 p1Pos, Vector3 p2Pos)
    {
        p1StartPos = p1Pos;
        p2StartPos = p2Pos;
    }

    [ClientRpc]
    void SetRoundOverClientRpc(bool value) { roundOver = value; }

    [ClientRpc]
    void FadeClientRpc(float from, float to)
    {
        StartCoroutine(FadeRoutine(from, to));
    }

    IEnumerator FadeRoutine(float from, float to)
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

    [ClientRpc]
    void LockBothClientRpc()
    {
        player1?.LockControls();
        player2?.LockControls();
    }

    [ClientRpc]
    void UnlockBothClientRpc()
    {
        player1?.UnlockControls();
        player2?.UnlockControls();
    }

    [ClientRpc]
    void PlayRoundIntroClientRpc(int round)
    {
        if (roundWorldUI != null)
            StartCoroutine(roundWorldUI.PlayRoundIntro(round));
    }

    [ClientRpc]
    void UpdateRoundUIClientRpc(bool isP1, int wins)
    {
        Image[] rounds = isP1 ? p1_Rounds : p2_Rounds;
        for (int i = 0; i < rounds.Length; i++)
            rounds[i].enabled = i < wins;
    }

    [ClientRpc]
    void PlayVictoryClientRpc(bool isP1, float delay)
    {
        PlayerController winner = isP1 ? player1 : player2;
        winner?.PlayVictoryTauntDelayed(delay);
    }

    [ClientRpc]
    void ResetPositionsClientRpc(Vector3 p1Pos, Vector3 p2Pos)
    {
        player1.transform.position = p1Pos;
        player2.transform.position = p2Pos;
        var rb1 = player1.GetComponent<Rigidbody2D>();
        var rb2 = player2.GetComponent<Rigidbody2D>();
        if (rb1) rb1.linearVelocity = Vector2.zero;
        if (rb2) rb2.linearVelocity = Vector2.zero;
    }

    [ClientRpc]
    void ResetBothClientRpc()
    {
        player1?.ResetForNewRound();
        player2?.ResetForNewRound();
    }

}