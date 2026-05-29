using UnityEngine;

public class FellaLogic : MonoBehaviour
{
    public int fellaIndex;
    private MergeGameController gameController;
    public bool hasMerged = false;

    private Collider2D myCollider;
    private float scanTimer = 0f;

    public void Setup(int index, MergeGameController controller)
    {
        fellaIndex = index;
        gameController = controller;
        gameObject.tag = "Player";

        myCollider = GetComponent<Collider2D>();
    }

    void Update()
    {
        scanTimer += Time.deltaTime;
        if (scanTimer > 0.2f)
        {
            CheckProximity();
            scanTimer = 0f;
        }
    }

    void CheckProximity()
    {
        if (hasMerged || gameController == null || gameController.isGameOver) return;
        if (myCollider == null) return;

        float searchRadius = myCollider.bounds.extents.x + gameController.mergeGapDistance;

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, searchRadius);

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;

            FellaLogic other = hit.GetComponent<FellaLogic>();
            if (other != null)
            {
                TryMergeWith(other);
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        FellaLogic other = collision.gameObject.GetComponent<FellaLogic>();
        if (other) TryMergeWith(other);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        FellaLogic other = collision.gameObject.GetComponent<FellaLogic>();
        if (other) TryMergeWith(other);
    }

    void TryMergeWith(FellaLogic other)
    {
        if (hasMerged || other.hasMerged) return;
        if (gameController == null || gameController.isGameOver) return;

        if (other.fellaIndex == this.fellaIndex)
        {
            if (GetInstanceID() < other.GetInstanceID())
            {
                hasMerged = true;
                other.hasMerged = true;
                gameController.Merge(this, other);
            }
        }
    }
}