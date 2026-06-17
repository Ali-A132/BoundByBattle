using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Netcode.Components;

[RequireComponent(typeof(NetworkTransform))]
public class OnlinePlayerController : PlayerController
{
    NetworkVariable<float> netHealth = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );
    NetworkVariable<float> netStamina = new NetworkVariable<float>(
        100f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server
    );

    bool isInDamageState = false;

    public override void OnNetworkSpawn()
    {
        netHealth.OnValueChanged += OnHealthChanged;
        netStamina.OnValueChanged += OnStaminaChanged;
        healthBar.SetHealth(netHealth.Value, maxHealth);
        staminaBar.SetStamina(netStamina.Value, maxStamina);
        LockControls();
    }

    public override void OnNetworkDespawn()
    {
        netHealth.OnValueChanged -= OnHealthChanged;
        netStamina.OnValueChanged -= OnStaminaChanged;
    }

    void OnHealthChanged(float prev, float curr) { healthBar.SetHealth(curr, maxHealth); }
    void OnStaminaChanged(float prev, float curr) { staminaBar.SetStamina(curr, maxStamina); }

    protected override void FixedUpdate()
    {
        if (!IsOwner) return;
        base.FixedUpdate();
    }

    protected override void Update()
    {
        if (!IsOwner) return;
        base.Update();
        SubmitStaminaServerRpc(currStamina);
    }

    [ServerRpc]
    void SubmitStaminaServerRpc(float stamina) { netStamina.Value = stamina; }


    protected override void EnterTired()
    {
        base.EnterTired();
        if (IsOwner) SyncTiredServerRpc(true);
    }

    protected override void ExitTired()
    {
        base.ExitTired();
        if (IsOwner) SyncTiredServerRpc(false);
    }

    [ServerRpc]
    void SyncTiredServerRpc(bool tired) { SyncTiredClientRpc(tired); }

    [ClientRpc]
    void SyncTiredClientRpc(bool tired)
    {
        if (IsOwner) return;
        animator.SetBool("Tired", tired);
        if (shadowAnimator != null) shadowAnimator.SetBool("Tired", tired);
    }

    public new void DrainStaminaEvent()
    {
        if (!IsOwner) return;
        base.DrainStaminaEvent();
    }

    public override void EnableHitbox()
    {
        if (!IsOwner) return;
        OnlineHitBox hitbox = GetComponentInChildren<OnlineHitBox>();
        if (hitbox != null) hitbox.EnableHitbox();
    }

    public override void DisableHitbox()
    {
        if (!IsOwner) return;
        OnlineHitBox hitbox = GetComponentInChildren<OnlineHitBox>();
        if (hitbox != null) hitbox.DisableHitbox();
    }

    public new void OnMoveOnline(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        base.OnMoveOnline(context);
    }

    public new void OnJabOnline(InputAction.CallbackContext context)
    {
        if (!IsOwner || !context.started || isTired) return;
        base.OnJabOnline(context);
        SyncAnimServerRpc("Jab");
    }

    public new void OnHeavyPunchOnline(InputAction.CallbackContext context)
    {
        if (!IsOwner || !context.started || isTired) return;
        bool up = moveInput.y > 0.5f;
        base.OnHeavyPunchOnline(context);
        SyncAnimServerRpc(up ? "Launch" : "HeavyPunch");
    }

    public new void OnKickOnline(InputAction.CallbackContext context)
    {
        if (!IsOwner || !context.started || isTired) return;
        bool up = moveInput.y > 0.5f;
        base.OnKickOnline(context);
        SyncAnimServerRpc(up ? "Special" : "Kick");
    }

    public new void OnLaunchOnline(InputAction.CallbackContext context)
    {
        if (!IsOwner || !context.started || isTired) return;
        if (moveInput.y <= 0.5f) return;
        base.OnLaunchOnline(context);
        SyncAnimServerRpc("Launch");
    }

    public new void OnSpecialOnline(InputAction.CallbackContext context)
    {
        if (!IsOwner || !context.started || isTired) return;
        if (moveInput.y <= 0.5f) return;
        base.OnSpecialOnline(context);
        SyncAnimServerRpc("Special");
    }

    public new void OnChainOnline(InputAction.CallbackContext context)
    {
        if (!IsOwner || !context.started || isTired) return;
        base.OnChainOnline(context);
        SyncAnimServerRpc("Chain");
    }

    public new void OnBlockOnline(InputAction.CallbackContext context)
    {
        if (!IsOwner || isTired)
            return;

        bool up = moveInput.y > 0.5f;

        if (up)
        {
            if (!context.started)
                return;

            base.OnBlockOnline(context);

            SyncAnimServerRpc("Taunt");
            return;
        }

        base.OnBlockOnline(context);

        if (context.started)
            SyncBlockServerRpc(true);

        if (context.canceled)
            SyncBlockServerRpc(false);
    }

    public void ApplyDamage(AttackType attackType, Vector3 hitPos, float damage, ulong attackerClientId)
    {
        if (!IsServer) return;
        if (roundManager.roundOver) return;

        if (isInvincible)
        {
            damage *= 0.4f;
            float newHealth = currHealth - damage;
            currHealth = Mathf.Max(newHealth, Mathf.Min(currHealth, 20f));
        }
        else
        {
            currHealth -= damage;
        }

        currHealth = Mathf.Clamp(currHealth, 0f, maxHealth);
        netHealth.Value = currHealth;
        ApplyDamageClientRpc((int)attackType, hitPos, currHealth, attackerClientId);
    }

    [ClientRpc]
    void ApplyDamageClientRpc(int attackTypeInt, Vector3 hitPos, float newHealth, ulong attackerClientId)
    {
        AttackType attack = (AttackType)attackTypeInt;
        if (roundManager.roundOver || knockedDown) return;

        isInDamageState = true;
        currHealth = newHealth;
        healthBar.SetHealth(currHealth, maxHealth);

        if (!isInvincible)
        {
            blockHeld = false;
            animator.SetBool("Block", false);
            if (shadowAnimator != null) shadowAnimator.SetBool("Block", false);
        }

        SpawnHitFX(hitPos, attack, isInvincible);

        if (currHealth <= 0) { KnockedOut(); return; }
        if (isInvincible) return;

        StopAllCoroutines();
        inputSequence.Clear();
        canMove = false;

        if (IsOwner)
        {
            if (attack == AttackType.Launch){
                float launchDirX = facingRight ? -1f : 1f;
                rb.linearVelocity = new Vector2(launchDirX * 4f, rb.linearVelocity.y);
                movementLockedInAir = true;
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        animator.SetFloat("xVelocity", 0f);
        if (shadowAnimator != null) shadowAnimator.SetFloat("xVelocity", 0f);

        switch (attack)
        {
            case AttackType.Launch:
                PlayDamageAnim("Falling Down");
                currStamina += 12;
                break;
            case AttackType.Heavy:
            case AttackType.Special:
                PlayDamageAnim("Damage 2");
                if (currStamina < 50f && isTired) EnterTired();
                else currStamina += 5;
                break;
            default:
                PlayDamageAnim("Damage 1");
                if (currStamina < 50f && isTired) EnterTired();
                else currStamina += 1;
                break;
        }

        speed = characterType == CharacterType.Mahsk ? 5.2f : 6.4f;
    }

    void PlayDamageAnim(string stateName)
    {
        animator.Play(stateName, 0, 0f);
        if (shadowAnimator != null) shadowAnimator.Play(stateName, 0, 0f);
    }

    public new void EndAttack()
    {
        isInDamageState = false;
        base.EndAttack();
    }

    protected override void KnockedOut()
    {
        canMove = false;
        animator.Play("Falling Down");
        if (shadowAnimator != null) shadowAnimator.gameObject.SetActive(false);
        if (IsServer) roundManager.OnPlayerKO(this);
    }

    [ServerRpc]
    void SyncAnimServerRpc(string trigger) { SyncAnimClientRpc(trigger); }

    [ClientRpc]
    void SyncAnimClientRpc(string trigger)
    {
        if (IsOwner) return;

        animator.ResetTrigger(trigger);
        animator.SetTrigger(trigger);

        if (shadowAnimator != null)
        {
            shadowAnimator.ResetTrigger(trigger);
            shadowAnimator.SetTrigger(trigger);
        }
    }

    [ServerRpc]
    void SyncBlockServerRpc(bool blocking) { SyncBlockClientRpc(blocking); }

    [ClientRpc]
    void SyncBlockClientRpc(bool blocking)
    {
        if (IsOwner) return;
        animator.SetBool("Block", blocking);
        if (shadowAnimator != null) shadowAnimator.SetBool("Block", blocking);
    }

    [ServerRpc]
    void SyncVelocityAnimServerRpc(float xVel) { SyncVelocityAnimClientRpc(xVel); }

    [ClientRpc]
    void SyncVelocityAnimClientRpc(float xVel)
    {
        if (IsOwner) return;
        if (isInDamageState) return;
        animator.SetFloat("xVelocity", xVel);
        if (shadowAnimator != null) shadowAnimator.SetFloat("xVelocity", xVel);
    }

    public override void FreezeBlockAnimation()
    {
        base.FreezeBlockAnimation();
        if (IsOwner) SyncBlockFreezeServerRpc(true);
    }

    protected override void ReleaseBlock()
    {
        base.ReleaseBlock();
        if (IsOwner) SyncBlockFreezeServerRpc(false);
    }

    [ServerRpc]
    void SyncBlockFreezeServerRpc(bool freeze) { SyncBlockFreezeClientRpc(freeze); }

    [ClientRpc]
    void SyncBlockFreezeClientRpc(bool freeze)
    {
        if (IsOwner) return;
        animator.speed = freeze ? 0f : 1f;
        if (shadowAnimator != null) shadowAnimator.speed = freeze ? 0f : 1f;
    }

    protected override void FixedUpdate_PostBase()
    {
        if (!IsOwner) return;
        SyncVelocityAnimServerRpc(Mathf.Abs(rb.linearVelocityX));
    }
}