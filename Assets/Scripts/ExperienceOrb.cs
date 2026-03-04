using UnityEngine;

public class ExperienceOrb : BaseCollectible
{
    private int experienceValue = 10;

    public void SetExperienceValue(int value)
    {
        experienceValue = value;
    }

    public override void SetAttractionRange(float range)
    {
        attractionRange = range;
    }

    public override void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    public void SetOrbColor(Color color)
    {
        originalColor = color;

        if (objectRenderer == null)
            objectRenderer = GetComponent<Renderer>();

        if (objectRenderer != null)
        {
            if (materialInstance == null && objectRenderer.material != null)
            {
                materialInstance = objectRenderer.material;
            }

            if (materialInstance != null)
            {
                materialInstance.color = color;

                if (materialInstance.HasProperty("_BaseColor"))
                    materialInstance.SetColor("_BaseColor", color);
                if (materialInstance.HasProperty("_Color"))
                    materialInstance.SetColor("_Color", color);
                if (materialInstance.HasProperty("_EmissionColor"))
                    materialInstance.SetColor("_EmissionColor", color);
            }
        }
    }

    protected override void UpdateConfiguration()
    {

        if (PlayerStatsManager.Instance != null)
        {
            attractionRange = PlayerStatsManager.Instance.GetModifiedMagnetRange();
        }
        else if (GameBalanceConfig.Instance != null)
        {
            attractionRange = GameBalanceConfig.Instance.OrbAttractionRange;
        }
        else
        {
            attractionRange = 5f;
        }

        if (GameBalanceConfig.Instance != null)
        {
            lifeTime = GameBalanceConfig.Instance.OrbLifetime;
        }
        else
        {
            lifeTime = 30f;
        }
    }

    protected override void OnCollected(GameObject playerObject)
    {

        PlayerExperience playerExp = playerObject.GetComponent<PlayerExperience>();
        if (playerExp != null)
        {
            playerExp.AddExperience(experienceValue);
            GameEvents.TriggerExperienceGained(experienceValue);
        }

        if (FloatingTextManager.Instance != null)
        {
            Vector3 textPosition = transform.position + Vector3.up * 0.5f;
            FloatingTextManager.Instance.ShowExperience(experienceValue, textPosition);
        }

        if (PickupAudioManager.Instance != null)
        {
            PickupAudioManager.Instance.PlayExperienceOrbSound(experienceValue);
        }
    }
}
