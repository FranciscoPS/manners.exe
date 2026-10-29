using UnityEngine;
using TMPro;

public class PlayerStatsHUD : MonoBehaviour
{
    [Header("Textos editables por idioma")]
    [SerializeField] private LocalizedString damageLabel = new LocalizedString("Damage", "Daño");
    [SerializeField] private LocalizedString fireRateLabel = new LocalizedString("Fire rate", "Vel. de disparo");
    [SerializeField] private LocalizedString rangeLabel = new LocalizedString("Range", "Rango");
    [SerializeField] private LocalizedString speedLabel = new LocalizedString("Speed", "Velocidad");
    [SerializeField] private LocalizedString magnetLabel = new LocalizedString("Magnet range", "Rango magnético");
    [SerializeField] private LocalizedString multiLabel = new LocalizedString("Multishot", "Multi disparo");
    [SerializeField] private LocalizedString explosiveLabel = new LocalizedString("Explosive shots", "Explosivas");
    [SerializeField] private LocalizedString chainLabel = new LocalizedString("Impact chain", "Cadena de impacto");

    [Header("Text References - Assign in Inspector")]
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI attackSpeedText;
    [SerializeField] private TextMeshProUGUI attackRangeText;
    [SerializeField] private TextMeshProUGUI moveSpeedText;
    [SerializeField] private TextMeshProUGUI magnetRangeText;
    [SerializeField] private TextMeshProUGUI multiShotText;
    [SerializeField] private TextMeshProUGUI explosiveShotText;
    [SerializeField] private TextMeshProUGUI knockbackText;

    private void Start()
    {
        if (PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.OnUpgradeApplied += OnUpgradeApplied;
        }

        UpdateAllStats();
    }

    private void OnDestroy()
    {
        if (PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.OnUpgradeApplied -= OnUpgradeApplied;
        }
    }

    private void OnUpgradeApplied(UpgradeType upgradeType, int newLevel)
    {
        UpdateAllStats();
    }

    private void UpdateAllStats()
    {
        if (PlayerStatsManager.Instance == null) return;

        UpdateDamage();
        UpdateAttackSpeed();
        UpdateAttackRange();
        UpdateMoveSpeed();
        UpdateMagnetRange();
        UpdateMultiShot();
        UpdateExplosiveShot();
        UpdateKnockback();
    }

    private void UpdateDamage()
    {
        if (damageText == null) return;

        float damage = PlayerStatsManager.Instance.GetModifiedDamage();
        int level = PlayerStatsManager.Instance.GetUpgradeLevel(UpgradeType.Damage);

        damageText.text = FormatStatLine(damageLabel.Value, damage.ToString("F1"), level);
    }

    private void UpdateAttackSpeed()
    {
        if (attackSpeedText == null) return;

        float cooldown = PlayerStatsManager.Instance.GetModifiedAttackCooldown();
        float fireRate = 1f / cooldown;
        int level = PlayerStatsManager.Instance.GetUpgradeLevel(UpgradeType.AttackSpeed);

        attackSpeedText.text = FormatStatLine(fireRateLabel.Value, $"{fireRate:F2}/s", level);
    }

    private void UpdateAttackRange()
    {
        if (attackRangeText == null) return;

        float range = PlayerStatsManager.Instance.GetModifiedAttackRange();
        int level = PlayerStatsManager.Instance.GetUpgradeLevel(UpgradeType.AttackRange);

        attackRangeText.text = FormatStatLine(rangeLabel.Value, range.ToString("F1"), level);
    }

    private void UpdateMoveSpeed()
    {
        if (moveSpeedText == null) return;

        float moveSpeed = GameBalanceConfig.Instance != null
            ? GameBalanceConfig.Instance.PlayerMoveSpeed
            : 5f;

        int level = PlayerStatsManager.Instance.GetUpgradeLevel(UpgradeType.MoveSpeed);
        if (level > 0 && UpgradeDatabase.Instance != null)
        {
            UpgradeData upgrade = UpgradeDatabase.Instance.allUpgrades.Find(u => u.upgradeType == UpgradeType.MoveSpeed);
            if (upgrade != null)
            {
                float percentageBonus = upgrade.CalculateValueAtLevel(level);
                moveSpeed *= (1f + percentageBonus / 100f);
            }
        }

        moveSpeedText.text = FormatStatLine(speedLabel.Value, moveSpeed.ToString("F1"), level);
    }

    private void UpdateMagnetRange()
    {
        if (magnetRangeText == null) return;

        float magnetRange = PlayerStatsManager.Instance.GetModifiedMagnetRange();
        int level = PlayerStatsManager.Instance.GetUpgradeLevel(UpgradeType.MagnetRange);

        magnetRangeText.text = FormatStatLine(magnetLabel.Value, magnetRange.ToString("F1"), level);
    }

    private void UpdateMultiShot()
    {
        if (multiShotText == null) return;

        int level = PlayerStatsManager.Instance.GetUpgradeLevel(UpgradeType.MultiShot);
        if (level == 0)
        {
            multiShotText.text = FormatStatLine(multiLabel.Value, "0%", 0);
            return;
        }

        float probability = PlayerStatsManager.Instance.GetMultiShotProbability();
        int extraBullets = PlayerStatsManager.Instance.GetMultiShotExtraBullets();

        multiShotText.text = FormatStatLine(multiLabel.Value, $"{probability:F0}% +{extraBullets}", level);
    }

    private void UpdateExplosiveShot()
    {
        if (explosiveShotText == null) return;

        int level = PlayerStatsManager.Instance.GetUpgradeLevel(UpgradeType.ExplosiveShot);
        if (level == 0)
        {
            explosiveShotText.text = FormatStatLine(explosiveLabel.Value, "0%", 0);
            return;
        }

        float probability = PlayerStatsManager.Instance.GetExplosiveShotProbability();

        explosiveShotText.text = FormatStatLine(explosiveLabel.Value, $"{probability:F0}%", level);
    }

    private void UpdateKnockback()
    {
        if (knockbackText == null) return;

        int level = PlayerStatsManager.Instance.GetUpgradeLevel(UpgradeType.Knockback);
        if (level == 0)
        {
            knockbackText.text = FormatStatLine(chainLabel.Value, "0%", 0);
            return;
        }

        float probability = PlayerStatsManager.Instance.GetKnockbackProbability();
        float force = PlayerStatsManager.Instance.GetKnockbackForce();

        knockbackText.text = FormatStatLine(chainLabel.Value, $"{probability:F0}% [{force:F1}F]", level);
    }

    private string FormatStatLine(string label, string value, int level)
    {
        if (level > 0)
        {
            return $"<color=#CCCCCC>{label}</color>: <color=#66FF88>{value}</color> <color=#FFD700>[lvl.{level}]</color>";
        }
        else
        {
            return $"<color=#CCCCCC>{label}</color>: <color=#66FF88>{value}</color>";
        }
    }
    private void OnEnable() => GameLocalization.LanguageChanged += UpdateAllStats;
    private void OnDisable() => GameLocalization.LanguageChanged -= UpdateAllStats;
}
