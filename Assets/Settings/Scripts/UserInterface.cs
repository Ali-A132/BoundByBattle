using UnityEngine;
using UnityEngine.UI;

public class UserInterface : MonoBehaviour
{
    public Image health;
    public Image stamina;
    public Sprite normalStaminaSprite;
    public Sprite tiredStaminaSprite;
    float healthTargetFill = 1f;
    float staminaTargetFill = 1f;
    private bool isTiredVisual = false;
    private bool enteredTired = false;

    void Update()
    {
        if (health != null) {
            health.fillAmount = Mathf.Lerp(
                health.fillAmount,
                healthTargetFill,
                Time.deltaTime * 10f
            );
        }

        if (stamina != null) {
            stamina.fillAmount = Mathf.Lerp(
                stamina.fillAmount,
                staminaTargetFill,
                Time.deltaTime * 10f
            );
        }
    }

    public void SetHealth(float curr, float max) {
        Debug.Log($"SetHealth called: {curr}/{max}");
        healthTargetFill = Mathf.Clamp01(curr / max);
    }

    public void SetStamina(float curr, float max)
    {
        staminaTargetFill = Mathf.Clamp01(curr / max);
        if (curr <= 0f)
        {
            enteredTired = true;
        }

        if (enteredTired && curr >= 5f && !isTiredVisual)
        {
            stamina.sprite = tiredStaminaSprite;
            isTiredVisual = true;
        }

        if (curr >= 50f)
        {
            stamina.sprite = normalStaminaSprite;
            isTiredVisual = false;
            enteredTired = false;
        }
    }

}
