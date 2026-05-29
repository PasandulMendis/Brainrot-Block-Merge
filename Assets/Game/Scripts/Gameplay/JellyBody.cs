using UnityEngine;

public class JellyBody : MonoBehaviour
{
    [Header("Jelly Settings")]
    public float squashAmount = 0.2f;
    public float recoverSpeed = 10f;

    [Header("Butter Physics")]
    [Tooltip("Adds tiny rotation to stop shapes from locking together")]
    public float slipPower = 5.0f;

    private Vector3 originalScale;
    private Rigidbody2D rb;

    private bool isSquashing = false;
    private float squashPhase = 0f;
    private float currentIntensity = 0f;
    private Vector3 targetSquashScale;

    private bool wasInitialized = false;

    void Awake()
    {
        if (PlayerPrefs.GetInt("Setting_JellyBody", 1) == 0)
        {
            Destroy(this);
            return;
        }

        originalScale = transform.localScale;
        rb = GetComponent<Rigidbody2D>();
        wasInitialized = true;
    }

    void OnDestroy()
    {
        if (wasInitialized && isSquashing)
        {
            transform.localScale = originalScale;
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        if (rb.linearVelocity.sqrMagnitude < 1.0f)
        {
            float randomTurn = Random.Range(-1f, 1f) * slipPower;
            rb.AddTorque(randomTurn * Time.fixedDeltaTime);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (gameObject.TryGetComponent(out PopEffect pop)) return;

        float impactForceSqr = collision.relativeVelocity.sqrMagnitude;

        if (impactForceSqr > 4.0f)
        {
            float impactForce = Mathf.Sqrt(impactForceSqr);
            currentIntensity = Mathf.Clamp01(impactForce / 15f);

            float squashX = originalScale.x * (1f + (squashAmount * currentIntensity));
            float squashY = originalScale.y * (1f - (squashAmount * currentIntensity));
            targetSquashScale = new Vector3(squashX, squashY, 1f);

            isSquashing = true;
            squashPhase = 0f;
        }
    }

    void Update()
    {
        if (!isSquashing) return;

        if (squashPhase < 1f)
        {
            squashPhase += Time.deltaTime * (recoverSpeed * 2f);
            transform.localScale = Vector3.Lerp(originalScale, targetSquashScale, squashPhase);
        }
        else if (squashPhase < 2f)
        {
            float bouncePhase = squashPhase - 1f;
            squashPhase += Time.deltaTime * recoverSpeed;

            float bounce = Mathf.Sin(bouncePhase * Mathf.PI) * (0.2f * currentIntensity);
            Vector3 current = Vector3.Lerp(targetSquashScale, originalScale, bouncePhase);

            transform.localScale = current + new Vector3(-bounce, bounce, 0);
        }
        else
        {
            transform.localScale = originalScale;
            isSquashing = false;
        }
    }
}