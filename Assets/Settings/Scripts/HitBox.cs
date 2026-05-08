using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class HitBox : MonoBehaviour
{
    public LayerMask opponentLayer;
    PlayerController attacker;
    bool hasHit;
    BoxCollider2D col;

    void Awake()
    {
        attacker = GetComponentInParent<PlayerController>();
        col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.enabled = false;
    }

    public void EnableHitbox()
    {
        hasHit = false;
        col.enabled = true;
    }

    public void DisableHitbox()
    {
        col.enabled = false;
        hasHit = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;
        if (!other.CompareTag("Player")) return;

        PlayerController defender = other.GetComponentInParent<PlayerController>();
        if (defender == null || attacker == null || defender == attacker) return;

        hasHit = true;

        PlayerController.AttackType attackType = attacker.CurrentAttack;
        Vector3 hitPos = other.ClosestPoint(col.bounds.center);

        float damage = GetDamageForCharacter(attacker.characterType, attackType);

        defender.ReceiveDamage(attackType, attacker, hitPos, damage);
    }

    static float GetDamageForCharacter(
        PlayerController.CharacterType charType,
        PlayerController.AttackType attackType)
    {
        return charType switch
        {
            PlayerController.CharacterType.Mahsk => attackType switch
            {
                PlayerController.AttackType.Jab => 4.5f,
                PlayerController.AttackType.Heavy => 7.5f,
                PlayerController.AttackType.Kick => 3f,
                PlayerController.AttackType.Special => 12f,
                PlayerController.AttackType.Launch => 1.5f,
                PlayerController.AttackType.Chain => 7f,
                _ => 0f
            },
            PlayerController.CharacterType.Payet => attackType switch
            {
                PlayerController.AttackType.Jab => 3.5f,
                PlayerController.AttackType.Heavy => 5.5f,
                PlayerController.AttackType.Kick => 4.5f,
                PlayerController.AttackType.Special => 15f,
                PlayerController.AttackType.Launch => 2.5f,
                PlayerController.AttackType.Chain => 10f,
                _ => 0f
            },
            _ => 0f
        };
    }
}