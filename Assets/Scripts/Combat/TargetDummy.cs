using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TargetDummy : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 250f;
    [SerializeField] private float resetDelay = 2.5f;
    [SerializeField] private Renderer visualRenderer;
    [SerializeField] private Color healthyColor = new Color(0.1f, 0.75f, 0.95f);
    [SerializeField] private Color hitColor = new Color(1f, 0.9f, 0.15f);
    [SerializeField] private Color defeatedColor = new Color(0.05f, 0.05f, 0.05f);
    [SerializeField] private float hitFlashTime = 0.08f;

    [Header("Debug Readout")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float lastDamage;
    [SerializeField] private bool lastHitWasCritical;
    [SerializeField] private int currentLifeHits;
    [SerializeField] private int currentLifeCriticalHits;
    [SerializeField] private float currentLifeDamageTaken;
    [SerializeField] private int sessionHits;
    [SerializeField] private int sessionCriticalHits;
    [SerializeField] private float sessionDamageTaken;
    [SerializeField] private int sessionDefeats;
    [SerializeField] private float lastHitTime;
    [SerializeField] private float lastTimeToDefeat;
    [SerializeField] private int lastShotIndex;
    [SerializeField] private string lastWeaponName;
    [SerializeField] private string lastInstigatorName;
    [SerializeField] private bool defeated;

    private MaterialPropertyBlock propertyBlock;
    private float resetTimer;
    private float flashTimer;
    private float currentLifeStartTime;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDefeated => defeated;
    public float LastDamage => lastDamage;
    public bool LastHitWasCritical => lastHitWasCritical;
    public int CurrentLifeHits => currentLifeHits;
    public int CurrentLifeCriticalHits => currentLifeCriticalHits;
    public float CurrentLifeDamageTaken => currentLifeDamageTaken;
    public int SessionHits => sessionHits;
    public int SessionCriticalHits => sessionCriticalHits;
    public float SessionDamageTaken => sessionDamageTaken;
    public int SessionDefeats => sessionDefeats;
    public float LastHitTime => lastHitTime;
    public float LastTimeToDefeat => lastTimeToDefeat;
    public int LastShotIndex => lastShotIndex;
    public string LastWeaponName => lastWeaponName;
    public string LastInstigatorName => lastInstigatorName;

    private void Awake()
    {
        if (visualRenderer == null)
        {
            visualRenderer = GetComponentInChildren<Renderer>();
        }

        propertyBlock = new MaterialPropertyBlock();
        currentHealth = maxHealth;
        currentLifeStartTime = Time.time;
        ApplyColor(healthyColor);
    }

    private void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && !defeated)
            {
                ApplyColor(healthyColor);
            }
        }

        if (!defeated)
        {
            return;
        }

        resetTimer -= Time.deltaTime;
        if (resetTimer <= 0f)
        {
            ResetDummy();
        }
    }

    public void ApplyDamage(DamageInfo damageInfo)
    {
        if (defeated)
        {
            return;
        }

        lastDamage = damageInfo.Damage;
        lastHitWasCritical = damageInfo.IsCritical;
        lastHitTime = Time.time;
        lastShotIndex = damageInfo.ShotIndex;
        lastWeaponName = damageInfo.Weapon != null ? damageInfo.Weapon.displayName : string.Empty;
        lastInstigatorName = damageInfo.Instigator != null ? damageInfo.Instigator.name : string.Empty;
        currentLifeHits++;
        sessionHits++;
        if (damageInfo.IsCritical)
        {
            currentLifeCriticalHits++;
            sessionCriticalHits++;
        }

        currentLifeDamageTaken += damageInfo.Damage;
        sessionDamageTaken += damageInfo.Damage;
        currentHealth = Mathf.Max(0f, currentHealth - damageInfo.Damage);
        flashTimer = hitFlashTime;
        ApplyColor(hitColor);

        if (currentHealth <= 0f)
        {
            defeated = true;
            sessionDefeats++;
            lastTimeToDefeat = Time.time - currentLifeStartTime;
            resetTimer = resetDelay;
            ApplyColor(defeatedColor);
        }
    }

    private void ResetDummy()
    {
        defeated = false;
        currentHealth = maxHealth;
        lastDamage = 0f;
        lastHitWasCritical = false;
        currentLifeHits = 0;
        currentLifeCriticalHits = 0;
        currentLifeDamageTaken = 0f;
        currentLifeStartTime = Time.time;
        flashTimer = 0f;
        ApplyColor(healthyColor);
    }

    private void ApplyColor(Color color)
    {
        if (visualRenderer == null)
        {
            return;
        }

        visualRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor("_BaseColor", color);
        propertyBlock.SetColor("_Color", color);
        visualRenderer.SetPropertyBlock(propertyBlock);
    }
}
