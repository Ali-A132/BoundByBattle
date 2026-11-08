using UnityEngine;

public class MahskController : PlayerController
{
    protected override void ApplyCharacterStats()
    {
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
    }

    protected override void PlayLaunchSound()
    {
        PlaySlidingStrikeSound();
    }

    protected override Vector2 GetHitEffectOffset(AttackType attack)
    {
        if (attack == AttackType.Kick)
        {
            return secondKick
                ? new Vector2(0f, 7f)
                : new Vector2(0f, 5.5f);
        }

        return base.GetHitEffectOffset(attack);
    }


}
