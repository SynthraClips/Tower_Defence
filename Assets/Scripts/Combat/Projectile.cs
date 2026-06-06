using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    [Header("Stats")]
    public float speed = 10f;
    public int damage = 10;
    public float lifetime = 4f;
    public DamageType damageType = DamageType.Physical;

    [Header("Splash (optional)")]
    public bool splashDamage = false;
    public float splashRadius = 0.6f;
    public LayerMask enemyLayer;

    private Transform _target;
    private Rigidbody2D _rb;
    private Collider2D _collider;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _collider = GetComponent<Collider2D>();
        _collider.isTrigger = true;
    }

    public void Launch(Transform target)
    {
        _target = target;
        Invoke(nameof(SelfDestruct), lifetime);
    }

    private void OnEnable()
    {
        if (_rb) _rb.simulated = true;

        if (GameManager.Instance)
        {
            GameManager.Instance.OnGameOver += FreezeOnRunEnded;
            GameManager.Instance.OnVictory += FreezeOnRunEnded;
        }
    }

    private void OnDisable()
    {
        if (GameManager.Instance)
        {
            GameManager.Instance.OnGameOver -= FreezeOnRunEnded;
            GameManager.Instance.OnVictory -= FreezeOnRunEnded;
        }
    }

    private void FreezeOnRunEnded()
    {
        enabled = false;
        if (_rb) _rb.simulated = false;
    }

    private void Update()
    {
        if (_target == null) { SelfDestruct(); return; }

        Vector3 dir = (_target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        // Rotate to face movement
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(ang, Vector3.forward);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var e = other.GetComponent<Enemy>();
        if (e == null) return;

        if (!splashDamage)
        {
            e.TakeDamage(damage, damageType);
            PlayImpactSfx(false);
        }
        else
        {
            // Apply splash around impact:
            var hits = Physics2D.OverlapCircleAll(transform.position, splashRadius, enemyLayer);
            foreach (var h in hits)
            {
                var enemy = h.GetComponent<Enemy>();
                if (enemy) enemy.TakeDamage(damage, damageType);
            }

            FloatingPopupSystem.Instance.ShowWorldPopup(
                transform.position,
                "Splash",
                new Color(0.6f, 0.92f, 1f, 1f),
                Camera.main,
                0.4f);
            PlayImpactSfx(true);
        }

        SelfDestruct();
    }

    private void PlayImpactSfx(bool usedSplash)
    {
        AudioManager.Instance?.PlaySFX(usedSplash ? SFX.WaterSplash : SFX.Impact);
    }

    private void SelfDestruct()
    {
        CancelInvoke();
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (splashDamage)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, splashRadius);
        }
    }
}
