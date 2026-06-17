using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(BoxCollider2D))]
public class OnlineHitBox : NetworkBehaviour
{
    public LayerMask opponentLayer;
    OnlinePlayerController attacker;
    bool hasHit;
    BoxCollider2D col;

    void Awake() {
        attacker = GetComponentInParent<OnlinePlayerController>();
        col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.enabled = false;
    }

    public void EnableHitbox() {
        hasHit = false;
        col.enabled = true;
    }

    public void DisableHitbox() {
        col.enabled = false;
        hasHit = false;
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (hasHit) return;
        if (!other.CompareTag("Player")) return;

        OnlinePlayerController defender = other.GetComponentInParent<OnlinePlayerController>();
        if (defender == null || attacker == null || defender == attacker) 
            return;

        hasHit = true;

        PlayerController.AttackType attackType = attacker.CurrentAttack;
        Vector3 hitPos = other.ClosestPoint(col.bounds.center);

        if (attacker.IsOwner)
            defender.SpawnHitFX(hitPos, attackType, defender.isInvincible);

        RegisterHitServerRpc(attacker.NetworkObject, defender.NetworkObject, (int)attackType, (int)attacker.characterType, hitPos);
        Invoke(nameof(ResetHit), 0.1f);
    }

    void ResetHit() { 
        hasHit = false; 
    }

    [ServerRpc(RequireOwnership = false)]
    void RegisterHitServerRpc(NetworkObjectReference attackerRef, NetworkObjectReference defenderRef, int attackTypeInt, int attackerCharTypeInt, Vector3 hitPos, ServerRpcParams rpcParams = default) {
        if (!attackerRef.TryGet(out NetworkObject attackerNetObj)) return;
        if (!defenderRef.TryGet(out NetworkObject defenderNetObj)) return;

        if (attackerNetObj.OwnerClientId != rpcParams.Receive.SenderClientId) return;

        OnlinePlayerController defender = defenderNetObj.GetComponent<OnlinePlayerController>();
        if (defender == null) return;

        if (defender.roundManager.roundOver) return;

        PlayerController.AttackType attackType = (PlayerController.AttackType)attackTypeInt;
        PlayerController.CharacterType attackerChar = (PlayerController.CharacterType)attackerCharTypeInt;

        float damage = GetDamageForCharacter(attackerChar, attackType);
        defender.ApplyDamage(attackType, hitPos, damage, rpcParams.Receive.SenderClientId);
    }

    static float GetDamageForCharacter(PlayerController.CharacterType charType, PlayerController.AttackType attackType) {
        return charType switch {
            PlayerController.CharacterType.Mahsk => attackType switch {
                PlayerController.AttackType.Jab => 4.5f,
                PlayerController.AttackType.Heavy => 7.5f,
                PlayerController.AttackType.Kick => 3f,
                PlayerController.AttackType.Special => 12f,
                PlayerController.AttackType.Launch => 1.5f,
                PlayerController.AttackType.Chain => 9f,
                _ => 0f
            },
            PlayerController.CharacterType.Payet => attackType switch {
                PlayerController.AttackType.Jab => 3.5f,
                PlayerController.AttackType.Heavy => 5.5f,
                PlayerController.AttackType.Kick => 4.5f,
                PlayerController.AttackType.Special => 15f,
                PlayerController.AttackType.Launch => 2.5f,
                PlayerController.AttackType.Chain => 8f,
                _ => 0f
            },
            _ => 0f
        };
    }
}