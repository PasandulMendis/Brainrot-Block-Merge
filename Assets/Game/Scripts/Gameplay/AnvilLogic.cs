using UnityEngine;

public class AnvilLogic : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject crushEffect;

    private bool hasHitFloor = false;
    private HapticManager hapticManager;

    void Start()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb)
        {
            rb.mass = 50f;
            rb.freezeRotation = true;
        }

        hapticManager = FindObjectOfType<HapticManager>();
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHitFloor) return;

        GameObject hitObj = collision.gameObject;

        if (hitObj.CompareTag("Player"))
        {
            if (crushEffect != null)
                Instantiate(crushEffect, hitObj.transform.position, Quaternion.identity);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayGenericPop();

            if (hapticManager != null)
                hapticManager.TriggerMergeHaptic();

            Destroy(hitObj);
        }

        string objName = hitObj.name.ToLower();

        if (objName.Contains("floor") || objName.Contains("ground") || objName.Contains("bottom") || objName.Contains("wall"))
        {
            hasHitFloor = true;

            if (hapticManager != null) hapticManager.TriggerGameOverHaptic();

            Destroy(gameObject, 0.1f);
        }
    }
}