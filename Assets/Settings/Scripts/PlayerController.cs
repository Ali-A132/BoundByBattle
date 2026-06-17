using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.Users;
using UnityEngine.Windows;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : NetworkBehaviour
{
    public Rigidbody2D rb;
    public Animator animator;
    protected Collider2D bodyCollider;
    public UserInterface healthBar;
    public UserInterface staminaBar;
    public RoundManager roundManager;
    protected Camera cam;
    protected Vector2 moveInput;
    protected List<AttackType> inputSequence = new List<AttackType>();
    public GameObject hitEffects;
    public Animator shadowAnimator;

    public GameObject dustEffectPrefab;
    public Vector2 dustOffset = new Vector2(-0.3f, -0.6f);
    public Transform groundPoint;

    public float speed;
    public float comboTimeout = 0.15f;
    public float maxHealth = 100;
    public float currHealth;
    public float maxStamina = 100f;
    public float currStamina = 0;
    public float idleStaminaRegen = 10f;
    public float tiredRecoveryThreshold = 50f;

    public float repeatAttackWindow = 0.35f;
    public float repeatAttackPenalty = 7f;
    private AttackType? lastAttackUsed = null;
    private float lastAttackTime = -999f;
    private bool applyRepeatPenalty = false;

    // Damage Mapping, WIP
    public float damageJab;
    public float damageHeavy;
    public float damageKick;
    public float damageSpecial;
    public float damageLaunch;
    public float damageChain;

    // Stamina Cost
    public float staminaJab;
    public float staminaKick;
    public float staminaHeavy;
    public float staminaLaunch;
    public float staminaSpecial;
    public float staminaChain;
    public float staminaBlockDrainPerSecond;

    public bool isTired;
    protected float comboTimer;
    protected float halfWidth;
    protected float camHalfWidth;

    public bool canMove = true;
    protected bool upHeld = false;
    public bool isInvincible = false;
    protected bool movementLockedInAir = false;
    public bool knockedDown = false;
    protected bool blockHeld = false;
    public bool controlsLocked = false;
    public bool facingRight = true;
    public bool secondKick = false;

    public AudioSource wooshAudioSource;
    public AudioSource hitAudioSource;
    public AudioClip[] wooshSounds;
    private int wooshSoundIndex = 0;
    public AudioClip[] hitSounds;
    private int hitSoundIndex = 0;
    public AudioClip[] blockSounds;
    private int blockSoundIndex = 0;
    public AudioClip groundHitSound;
    public AudioClip tiredSound;
    public AudioClip slidingStrike;

    public enum AttackType {
        Jab,
        Heavy,
        Kick,
        Launch,
        Block,
        Special,
        Chain
    }

    public enum CharacterType {
        Mahsk,
        Payet
    }

    public CharacterType characterType;
    public virtual void ApplyCharacterStats() {
        switch (characterType) {
            case CharacterType.Mahsk:
                speed = 5.2f;

                damageJab = 4.5f;
                damageHeavy = 7.5f;
                damageKick = 3f;
                damageSpecial = 12f;
                damageLaunch = 1.5f;
                damageChain = 9f;

                staminaJab = 10f;
                staminaKick = 20f;
                staminaHeavy = 26f;
                staminaLaunch = 15f;
                staminaSpecial = 55f;
                staminaChain = 12f;
                break;

            case CharacterType.Payet:
                speed = 6.5f;

                damageJab = 3.5f;
                damageHeavy = 5.5f;
                damageKick = 4.5f;
                damageSpecial = 14f;
                damageLaunch = 2.5f;
                damageChain = 9f;

                staminaJab = 10f;
                staminaKick = 20f;
                staminaHeavy = 22f;
                staminaLaunch = 15f;
                staminaSpecial = 60f;
                staminaChain = 15f;
                break;
        }
    }

    public AttackType CurrentAttack { get; private set; }

    private void Awake() {
        ApplyCharacterStats();
        currStamina = maxStamina;
        staminaBar.SetStamina(currStamina, maxStamina);
        currHealth = maxHealth;
        cam = Camera.main;
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        halfWidth = GetComponent<Collider2D>().bounds.extents.x;
        bodyCollider = GetComponent<Collider2D>();
        StartCoroutine(WarmUpAssets());
    }

    IEnumerator WarmUpAssets()
    {
        yield return null;
        EnableHitbox();
        animator.Play("Jab");
        shadowAnimator.Play("Jab");
        animator.Play("Movement", 0, 0f);
        shadowAnimator.Play("Movement", 0, 0f);
        Vector3 offScreen = new Vector3(-9999f, -9999f, -9999f);

        GameObject warmupObj = new GameObject("AudioWarmup");
        AudioSource warmupSource = warmupObj.AddComponent<AudioSource>();
        warmupSource.volume = 0f;

        foreach (var clip in wooshSounds)
            if (clip != null) warmupSource.PlayOneShot(clip);

        foreach (var clip in hitSounds)
            if (clip != null) warmupSource.PlayOneShot(clip);

        foreach (var clip in blockSounds)
            if (clip != null) warmupSource.PlayOneShot(clip);

        if (groundHitSound != null) warmupSource.PlayOneShot(groundHitSound);
        if (tiredSound != null) warmupSource.PlayOneShot(tiredSound);
        if (slidingStrike != null) warmupSource.PlayOneShot(slidingStrike);

        yield return new WaitForSeconds(0.2f);
        Destroy(warmupObj);

        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            sr.enabled = false;

        if (hitEffects != null)
        {
            GameObject fx = Instantiate(hitEffects, offScreen, Quaternion.identity);
            Animator anim = fx.GetComponent<Animator>();
            if (anim != null)
            {
                for (int i = 1; i <= 7; i++)
                {
                    anim.SetInteger("HitType", i);
                    yield return null;
                }
            }
            Destroy(fx);
        }

        if (dustEffectPrefab != null)
        {
            GameObject dust = Instantiate(dustEffectPrefab, offScreen, Quaternion.identity);
            yield return null;
            Destroy(dust);
        }

        foreach (var sr in GetComponentsInChildren<SpriteRenderer>(true))
            sr.enabled = true;
        DisableHitbox();
        staminaBar.SetStamina(maxStamina, maxStamina);
    }

    protected virtual void FixedUpdate() {
        if (blockHeld)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        if (!canMove) {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (moveInput.y > 0.50f) {
            upHeld = true;
        } else {
            upHeld = false;
        }

        if (movementLockedInAir) {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float x = Mathf.Abs(moveInput.x) > 0.01f
        ? Mathf.Sign(moveInput.x) * speed
        : 0f;

        rb.linearVelocity = new Vector2(x, rb.linearVelocity.y);

        if (x > 0.01f)
            facingRight = true;
        else if (x < -0.01f)
            facingRight = false;

        animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocityX));
        shadowAnimator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocityX));
        FixedUpdate_PostBase();
    }

    protected virtual void FixedUpdate_PostBase() { }

    protected virtual void Update() {
        if (controlsLocked)
            return;

        if (blockHeld) {
            canMove = false;
        }

        if (Mathf.Abs(rb.linearVelocityX) > 0.01f) {
            float halfWidth_cam = cam.orthographicSize * cam.aspect;
            float minX = cam.transform.position.x - halfWidth_cam + halfWidth;
            float maxX = cam.transform.position.x + halfWidth_cam - halfWidth;
            Vector3 pos = transform.position;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            transform.position = pos;
        }

        if (inputSequence.Count > 0) {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
                inputSequence.Clear();
        }

        float regenRate = idleStaminaRegen;

        if (!isTired && !blockHeld) {
            currStamina += regenRate * Time.deltaTime;
            currStamina = Mathf.Clamp(currStamina, 0f, maxStamina);
            staminaBar.SetStamina(currStamina, maxStamina);
        }


        if (isTired) {
            currStamina += regenRate * Time.deltaTime;
            if (currStamina >= tiredRecoveryThreshold) {
                currStamina = tiredRecoveryThreshold;
                ExitTired();
            }
            staminaBar.SetStamina(currStamina, maxStamina);
        }

    }

    void PlayWooshSound() {
        if (wooshSounds.Length == 0 || wooshAudioSource == null) return;
        wooshAudioSource.PlayOneShot(wooshSounds[wooshSoundIndex]);
        wooshSoundIndex = (wooshSoundIndex + 1) % wooshSounds.Length;
    }

    void PlayHitSound() {
        if (hitSounds.Length == 0 || hitAudioSource == null) return;
        hitAudioSource.PlayOneShot(hitSounds[hitSoundIndex]);
        hitSoundIndex = (hitSoundIndex + 1) % hitSounds.Length;
    }

    void PlayBlockSound()
    {
        if (blockSounds.Length == 0 || wooshAudioSource == null) return;
        wooshAudioSource.PlayOneShot(blockSounds[blockSoundIndex]);
        blockSoundIndex = (blockSoundIndex + 1) % blockSounds.Length;
    }

    void PlayGroundSound()
    {
        hitAudioSource.PlayOneShot(groundHitSound);
    }

    void PlayTiredSoundSound()
    {
        wooshAudioSource.PlayOneShot(tiredSound);
    }

    void PlaySlidingStrikeSound()
    {
        wooshAudioSource.PlayOneShot(slidingStrike);
    }

    void PlayAnim(string trigger)
    {
        animator.SetTrigger(trigger);

        if (shadowAnimator != null)
            shadowAnimator.SetTrigger(trigger);
    }

    public void OnMove(Vector2 input)
    {
        if (controlsLocked) return;

        moveInput = input;

        upHeld = input.y > 0.5f;
    }

    public void OnJab() {
        QueueInput(AttackType.Jab);
    }

    public void OnHeavyPunch() {
        QueueInput(AttackType.Heavy);
    }

    public void OnKick() {
        QueueInput(AttackType.Kick);
    }

    public void OnLaunch() {
        if (upHeld == true) {
            QueueInput(AttackType.Launch);
        }
    }

    public void OnSpecial() {
        if (upHeld == true) {
            QueueInput(AttackType.Special);
        }
    }

    public void OnChain() {
        QueueInput(AttackType.Chain);

    }

    public void OnBlock()
    {
        if (controlsLocked || isTired)
            return;

        if (currStamina <= 0f)
            return;

        if (upHeld)
        {
            canMove = false;
            StartTaunt();
            return;
        }

        blockHeld = true;
        canMove = false;

        inputSequence.Clear();
        CurrentAttack = AttackType.Block;

        shadowAnimator.SetFloat("xVelocity", 0f);

        animator.SetBool("Block", true);
        shadowAnimator.SetBool("Block", true);
    }

    public void OnBlockReleased()
    {
        blockHeld = false;
        ReleaseBlock();
    }

    public void OnMoveOnline(InputAction.CallbackContext context)
    {
        if (controlsLocked) return;
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnJabOnline(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        Debug.Log(gameObject.name + " Jab Pressed");
        QueueInput(AttackType.Jab);
    }

    public void OnHeavyPunchOnline(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        QueueInput(AttackType.Heavy);
    }

    public void OnKickOnline(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        QueueInput(AttackType.Kick);
    }

    public void OnLaunchOnline(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        if (upHeld == true)
        {
            QueueInput(AttackType.Launch);
        }
    }

    public void OnSpecialOnline(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        if (upHeld == true)
        {
            QueueInput(AttackType.Special);
        }
    }

    public void OnChainOnline(InputAction.CallbackContext context)
    {
        if (!context.started) return;
        QueueInput(AttackType.Chain);

    }

    public void OnBlockOnline(InputAction.CallbackContext context)
    {
        if (controlsLocked || isTired)
            return;

        if (context.started && currStamina > 0f)
        {
            if (upHeld)
            {
                canMove = false;
                StartTaunt();
                return;
            }

            blockHeld = true;
            canMove = false;

            inputSequence.Clear();
            CurrentAttack = AttackType.Block;
            shadowAnimator.SetFloat("xVelocity", 0f);
            animator.SetBool("Block", true);
            shadowAnimator.SetBool("Block", true);
        }
        else if (context.canceled)
        {
            blockHeld = false;
            ReleaseBlock();
        }
    }


    void QueueInput(AttackType attack) {
        if (blockHeld)
            return;

        if (isTired)
            return;

        if (inputSequence.Count >= 3) {
            inputSequence.RemoveAt(0);
        }

        inputSequence.Add(attack);
        comboTimer = comboTimeout;
        TryStartNextAttack();
    }

    void TryStartNextAttack() {
        if (blockHeld)
            return;

        if (!canMove) return;
        if (inputSequence.Count == 0) return;

        AttackType lastAttack = inputSequence[inputSequence.Count - 1];
        inputSequence.Clear();

        applyRepeatPenalty = false;

        if (lastAttackUsed.HasValue &&
            lastAttackUsed.Value == lastAttack &&
            Time.time - lastAttackTime <= repeatAttackWindow)
        {
            applyRepeatPenalty = true;
        }

        lastAttackUsed = lastAttack;
        lastAttackTime = Time.time;

        if (upHeld == true && lastAttack == AttackType.Heavy) {
            if (characterType == CharacterType.Mahsk) {
                PlaySlidingStrikeSound();
            }
            else {
                PlayWooshSound();
            }
            StartLaunch();
            return;
        }
        else if (upHeld == true && lastAttack == AttackType.Kick) {
            PlayWooshSound();
            StartSpecial();
            return;
        }


        switch (lastAttack) {
            case AttackType.Block:
                CurrentAttack = AttackType.Block;
                canMove = false;
                PlayAnim("Block");
                break;
            case AttackType.Chain:
                PlayWooshSound();
                CurrentAttack = AttackType.Chain;
                canMove = false;
                PlayAnim("Chain");
                break;
            case AttackType.Jab:
                PlayWooshSound();
                CurrentAttack = AttackType.Jab;
                canMove = false;
                PlayAnim("Jab");
                break;
            case AttackType.Heavy:
                PlayWooshSound();
                CurrentAttack = AttackType.Heavy;
                canMove = false;
                PlayAnim("HeavyPunch");
                break;
            case AttackType.Kick:
                PlayWooshSound();
                CurrentAttack = AttackType.Kick;
                canMove = false;
                PlayAnim("Kick");
                break;
        }
    }

    private void StartLaunch() {
        CurrentAttack = AttackType.Launch;
        canMove = false;
        PlayAnim("Launch");
    }
    private void StartSpecial() {
        CurrentAttack = AttackType.Special;
        canMove = false;
        speed = 12f;
        PlayAnim("Special");
    }
    private void StartTaunt() {
        speed = 0;
        canMove = false;
        PlayAnim("Taunt");
    }

    public void EndAttack() {
        rb.linearVelocity = Vector2.zero;
        canMove = true;

        if (characterType == CharacterType.Mahsk)
            speed = 5.2f;
        else
            speed = 6.5f;

        if (inputSequence.Count > 0)
            TryStartNextAttack();
    }

    public void SpeedBoost() {
        speed = 10f;
        canMove = true;
        rb.linearVelocity = new Vector2(rb.linearVelocityX, 0f);
        rb.AddForce(Vector2.up * 5f, ForceMode2D.Impulse);

        if (inputSequence.Count > 0)
            TryStartNextAttack();
    }

    public void FlyingSpeedBoost()
    {
        speed = 10f;
        canMove = true;
        rb.linearVelocity = new Vector2(rb.linearVelocityX, 0f);
        rb.AddForce(Vector2.up * 6f, ForceMode2D.Impulse);

        if (inputSequence.Count > 0)
            TryStartNextAttack();
    }

    public void LaunchJump() {
        canMove = true;
        rb.linearVelocity = new Vector2(rb.linearVelocityX, 0f);
        rb.AddForce(Vector2.up * 10f, ForceMode2D.Impulse);

    }

    public void BackUpJump() {
        canMove = true;
        rb.linearVelocity = new Vector2(rb.linearVelocityX, 0f);
        rb.AddForce(Vector2.up * 6f, ForceMode2D.Impulse);
    }

    public void LaunchSpeedBoost() {
        speed = 13.5f;
        canMove = true;

        if (inputSequence.Count > 0)
            TryStartNextAttack();
    }

    public void TrueEndAttack() {
        rb.linearVelocity = Vector2.zero;
        canMove = true;
        if (characterType == CharacterType.Mahsk)
            speed = 5.2f;
        else
            speed = 6.5f;
        rb.AddForce(Vector2.down * 10f, ForceMode2D.Impulse);

        if (inputSequence.Count > 0)
            TryStartNextAttack();
    }

    public void FallingDownPush() {
        rb.linearVelocity = Vector2.zero;
        canMove = true;
        if (characterType == CharacterType.Mahsk)
            speed = 5.2f;
        else
            speed = 6.5f;
        rb.AddForce(Vector2.down * 4f, ForceMode2D.Impulse);

        if (inputSequence.Count > 0)
            TryStartNextAttack();
    }

    public virtual void ReceiveDamage(AttackType attackType, PlayerController attacker, Vector3 hitPos, float damage) {
        if (roundManager.roundOver)
            return;

        if (knockedDown) {
            return;
        }

        if (!isInvincible)
        {
            blockHeld = false;
            animator.SetBool("Block", false);
            shadowAnimator.SetBool("Block", false);
        }

        //float damage = attackType switch { AttackType.Jab => damageJab, AttackType.Heavy => damageHeavy, AttackType.Kick => damageKick, AttackType.Special => damageSpecial, AttackType.Launch => damageLaunch, _ => 0f};

        if (isInvincible) {
            damage *= 0.4f;
            float newHealth = currHealth - damage;
            currHealth = Mathf.Max(newHealth, Mathf.Min(currHealth, 20f));
        } else
        {
            currHealth -= damage;
        }

        SpawnHitFX(hitPos, attackType, isInvincible);

        currHealth = Mathf.Clamp(currHealth, 0f, maxHealth);
        healthBar.SetHealth(currHealth, maxHealth);

        if (currHealth <= 0) {
            KnockedOut();
            return;
        }

        if (isInvincible) {
            return;
        }


        StopAllCoroutines();
        inputSequence.Clear();
        canMove = false;

        if (attackType == AttackType.Launch)
        {
            float launchDirX = attacker != null ? Mathf.Sign(transform.position.x - attacker.transform.position.x) : (facingRight ? -1f : 1f);
            rb.linearVelocity = new Vector2(launchDirX * 4f, rb.linearVelocity.y);
            movementLockedInAir = true;
        }
        else {
            rb.linearVelocity = Vector2.zero;
        }

        if (attacker != null) {
            Collider2D attackerCol = attacker.GetComponent<Collider2D>();
            if (attackerCol != null && attackType == AttackType.Launch) {
                Physics2D.IgnoreCollision(bodyCollider, attackerCol, true);
                StartCoroutine(ReenableCollision(attackerCol, 0.10f));
            }
            StartCoroutine(ReenableCollision(attackerCol, 0.10f));
        }

        animator.Play(animator.GetCurrentAnimatorStateInfo(0).shortNameHash, 0, 0f);

        switch (attackType) {
            case AttackType.Launch:
                animator.Play("Falling Down", 0, 0f);
                shadowAnimator.Play("Falling Down", 0, 0f);
                currStamina += 12;
                break;
            case AttackType.Heavy:
            case AttackType.Special:
                animator.Play("Damage 2", 0, 0f);
                shadowAnimator.Play("Damage 2", 0, 0f);
                if (currStamina < 50f && isTired)
                    EnterTired();
                else
                    currStamina += 5;
                break;
            default:
                animator.Play("Damage 1", 0, 0f);
                shadowAnimator.Play("Damage 1", 0, 0f);
                if (currStamina < 50f && isTired)
                    EnterTired();
                else
                    currStamina += 1;
                break;
        }
        if (characterType == CharacterType.Mahsk)
            speed = 5.2f;
        else
            speed = 6.5f;
    }

    public void SpawnHitFX(Vector3 pos, AttackType attack, bool wasBlocked)
    {

        Vector2 offset = new Vector2(0f, 0f);
        if (attack == AttackType.Kick)
        {
            offset = new Vector2(0f, -1.5f);
        }
        else if (attack == AttackType.Heavy)
        {
            offset = new Vector2(0f, -0.75f);
        }
        else if (attack == AttackType.Jab)
        {
            offset = new Vector2(0f, -0.75f);
        }
        else if (characterType == CharacterType.Payet && attack == AttackType.Kick)
        {
            offset = new Vector2(0f, -2.5f);
        }
        else if (characterType == CharacterType.Payet && attack == AttackType.Special)
        {
            offset = new Vector2(0f, -0.5f);
        }
        else if (characterType == CharacterType.Mahsk && attack == AttackType.Kick && secondKick)
        {
            offset = new Vector2(0f, 7f);
        }
        else if (characterType == CharacterType.Mahsk && attack == AttackType.Kick)
        {
            offset = new Vector2(0f, 5.5f);
        }

        Vector3 spawnPos = new Vector3(pos.x + offset.x, pos.y + offset.y, -1f);
        GameObject fx = Instantiate(hitEffects, spawnPos, Quaternion.identity);
        Animator anim = fx.GetComponent<Animator>();

        if (wasBlocked)
        {
            PlayBlockSound();
            anim.SetInteger("HitType", 6);
        }
        else
        {
            PlayHitSound();
            anim.SetInteger("HitType", GetHitTypeFromAttack(attack));
        }
    }


    public void SpawnDustFX()
    {
        if (dustEffectPrefab == null) return;

        Vector3 basePos = groundPoint.position;
        float xOffset = dustOffset.x;
        if (!facingRight)
            xOffset *= -1f;

        Vector3 spawnPos = new Vector3(
            basePos.x + xOffset,
            basePos.y + dustOffset.y,
            -1f
        );

        GameObject fx = Instantiate(dustEffectPrefab, spawnPos, Quaternion.identity);
        Vector3 scale = fx.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (facingRight ? 1 : -1);
        fx.transform.localScale = scale;

    }

    public void GroundSpawnDustFX()
    {
        if (dustEffectPrefab == null) return;

        PlayGroundSound();
        Vector3 basePos = groundPoint.position;
        float xOffset = dustOffset.x;
        if (!facingRight)
            xOffset *= -1f;

        Vector3 spawnPos = new Vector3(
            basePos.x + xOffset,
            basePos.y + dustOffset.y,
            -1f
        );

        GameObject fx = Instantiate(dustEffectPrefab, spawnPos, Quaternion.identity);
        Vector3 scale = fx.transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (facingRight ? 1 : -1);
        fx.transform.localScale = scale;

    }




    int GetHitTypeFromAttack(AttackType attack) {
        return attack switch {
            AttackType.Jab => 1,
            AttackType.Heavy => 2,
            AttackType.Kick => 3,
            AttackType.Launch => 4,
            AttackType.Special => 5,
            AttackType.Chain => 7,
            _ => 1
        };
    }


    public void ChainAssignment() {
        CurrentAttack = AttackType.Chain;
    }

    protected virtual void KnockedOut() {
        canMove = false;
        rb.linearVelocity = new Vector2(0f, -10f);
        animator.Play("Falling Down");
        if (shadowAnimator != null)
            shadowAnimator.gameObject.SetActive(false);
        roundManager.OnPlayerKO(this);
    }

    IEnumerator ReenableCollision(Collider2D attackerCol, float delay) {
        yield return new WaitForSeconds(delay);
        Physics2D.IgnoreCollision(bodyCollider, attackerCol, false);
    }

    public virtual void FreezeBlockAnimation() {
        if (blockHeld) {
            animator.speed = 0f;
            shadowAnimator.speed = 0f;
        }
    }

    protected virtual void ReleaseBlock() {
        blockHeld = false;
        isInvincible = false;

        canMove = true;
        animator.speed = 1f;
        shadowAnimator.speed = 1f;
        animator.SetBool("Block", false);
        shadowAnimator.SetBool("Block", false);
    }

    void SecondKickActivation() {
        secondKick = true;
    }

    void SecondKickDisable()
    {
        secondKick = false;
    }

    public void OnLanded() {
        movementLockedInAir = false;
        canMove = true;
    }

    public void CompleteStop() {
        rb.linearVelocity = Vector2.zero;
        canMove = false;
    }

    public void EnableInvincibility() {
        isInvincible = true;
    }

    public void DisableInvincibility() {
        isInvincible = false;
    }

    public void KnockedDownInvulnerability() {
        knockedDown = true;
    }

    public void KnockedDownInvulnerabilityOff() {
        knockedDown = false;
    }
    public void FreezeOnGround() {
        if (currHealth > 0f)
            return;

        rb.AddForce(Vector2.down * 200f, ForceMode2D.Impulse);
        animator.speed = 0f;
        canMove = false;
        knockedDown = true;
        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

    }

    protected virtual void EnterTired() {
        if (isTired) return;
        PlayTiredSoundSound();
        isTired = true;
        canMove = true;
        blockHeld = false;
        currStamina = 0f;
        staminaBar.SetStamina(currStamina, maxStamina);
        animator.SetBool("Block", false);
        shadowAnimator.SetBool("Block", false);
        animator.SetBool("Tired", true);
        shadowAnimator.SetBool("Tired", true);
    }

    protected virtual void ExitTired() {
        wooshAudioSource.Stop();
        isTired = false;
        animator.SetBool("Tired", false);
        shadowAnimator.SetBool("Tired", false);
    }


    public void DrainStaminaEvent() {
        float amount = GetStaminaCostForAttack(CurrentAttack);
        DrainStamina(amount);
    }

    void DrainStamina(float amount) {
        currStamina -= amount;

        applyRepeatPenalty = false;

        if (currStamina <= 0f)
        {
            currStamina = 0f;
            EnterTired();
        }

        staminaBar.SetStamina(currStamina, maxStamina);
    }

    float GetStaminaCostForAttack(AttackType attack) {
        float cost = attack switch
        {
            AttackType.Jab => staminaJab,
            AttackType.Kick => staminaKick,
            AttackType.Heavy => staminaHeavy,
            AttackType.Launch => staminaLaunch,
            AttackType.Special => staminaSpecial,
            AttackType.Chain => staminaChain,
            AttackType.Block => staminaBlockDrainPerSecond,
            _ => 0f
        };

        if (applyRepeatPenalty)
            cost += repeatAttackPenalty;

        Debug.Log(cost);
        return cost;
    }


    public virtual void EnableHitbox() {
        HitBox hitbox = GetComponentInChildren<HitBox>();
        if (hitbox != null)
            hitbox.EnableHitbox();
    }

    public virtual void DisableHitbox() {
        HitBox hitbox = GetComponentInChildren<HitBox>();

        // Possible change
        //if (characterType == CharacterType.Mahsk)
            //speed = 5.5f;
        //else
            //speed = 7f;

        if (hitbox != null)
            hitbox.DisableHitbox();
    }

    public void PlayVictoryTauntDelayed(float delay = 2f) {
        if (currHealth <= 0f) return;
        StartCoroutine(VictoryTauntRoutine(delay));
    }

    IEnumerator VictoryTauntRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (currHealth <= 0f) yield break;

        StopAllCoroutines();
        inputSequence.Clear();

        moveInput = Vector2.zero;

        canMove = false;
        blockHeld = false;
        movementLockedInAir = false;

        wooshAudioSource.Stop();
        animator.speed = 1f;
        shadowAnimator.speed = 1f;
        rb.linearVelocity = Vector2.zero;

        animator.Play("Taunt", 0, 0f);
        shadowAnimator.Play("Taunt", 0, 0f);

        yield return new WaitForSeconds(2.19f);

        rb.linearVelocity = Vector2.zero;

        animator.Play("Idle", 0, 0f);
        shadowAnimator.Play("Idle", 0, 0f);
    }

    public void FreezeMovementForSeconds(float seconds) {
        StartCoroutine(FreezeMovementRoutine(seconds));
    }

    IEnumerator FreezeMovementRoutine(float seconds) {
        canMove = false;
        yield return new WaitForSeconds(seconds);
        canMove = true;
    }

    public void ResetForNewRound() {
        StopAllCoroutines();

        currHealth = maxHealth;
        currStamina = maxStamina;

        healthBar.SetHealth(currHealth, maxHealth);
        staminaBar.SetStamina(currStamina, maxStamina);

        isInvincible = false;
        isTired = false;
        knockedDown = false;

        canMove = false;
        controlsLocked = true;
        blockHeld = false;
        movementLockedInAir = false;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = Vector2.zero;

        animator.speed = 1f;
        animator.Rebind();
        animator.Update(0f);

        if (shadowAnimator != null)
            shadowAnimator.gameObject.SetActive(true);

        shadowAnimator.speed = 1f;
        shadowAnimator.Rebind();
        shadowAnimator.Update(0f);
    }
    public void LockControls() {
        controlsLocked = true;
        canMove = false;
        rb.linearVelocity = Vector2.zero;
        moveInput = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }
    public void UnlockControls() {
        controlsLocked = false;
        canMove = true;
    }


}
