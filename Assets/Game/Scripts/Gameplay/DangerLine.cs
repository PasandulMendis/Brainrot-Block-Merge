using UnityEngine;
using System.Collections.Generic;

public class DangerLine : MonoBehaviour
{
    public MergeGameController gameController;

    [Header("Danger Settings")]
    [Tooltip("How many Blocks must be touching the line to trigger Game Over?")]
    public int requiredBlocksForGameOver = 3;

    private float dangerTimer = 0f;
    public SpriteRenderer lineRenderer;

    private HashSet<Collider2D> blocksInDanger = new HashSet<Collider2D>();

    void Update()
    {
        if (gameController == null || gameController.isGameOver) return;

        if (Physics2D.simulationMode != SimulationMode2D.FixedUpdate) return;

        if (Physics2D.gravity.y > -4.0f)
        {
            ResetDanger();
            return;
        }

        blocksInDanger.RemoveWhere(col => col == null || col.gameObject == null);

        int blocksAtRestCount = 0;
        foreach (Collider2D col in blocksInDanger)
        {
            if (col.TryGetComponent(out Rigidbody2D rb))
            {
                if (rb.bodyType != RigidbodyType2D.Kinematic)
                {
                    if (rb.linearVelocity.sqrMagnitude < 0.25f)
                    {
                        blocksAtRestCount++;
                    }
                }
            }
        }

        bool isCurrentlyInDanger = (blocksAtRestCount >= requiredBlocksForGameOver);

        if (isCurrentlyInDanger)
        {
            dangerTimer += Time.deltaTime;

            if (lineRenderer) lineRenderer.color = Color.Lerp(Color.white, Color.red, dangerTimer / 2.0f);

            if (dangerTimer > 2.0f)
            {
                gameController.TriggerGameOver();
            }
        }
        else
        {
            ResetDanger();
        }
    }

    void ResetDanger()
    {
        dangerTimer = 0f;
        if (lineRenderer) lineRenderer.color = Color.white;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            blocksInDanger.Add(other);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            blocksInDanger.Remove(other);
        }
    }
}