using UnityEngine;

public class PayetController : PlayerController
{
    protected override void ApplyCharacterStats()
    {
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
    }

    protected override void PlayLaunchSound()
    {
        PlayWooshSound();
    }

    protected override Vector2 GetHitEffectOffset(AttackType attack)
    {
        if (attack == AttackType.Kick)
            return new Vector2(0f, -2.5f);

        if (attack == AttackType.Special)
            return new Vector2(0f, -0.5f);

        return base.GetHitEffectOffset(attack);
    }
}
