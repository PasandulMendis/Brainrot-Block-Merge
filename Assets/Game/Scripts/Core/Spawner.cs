using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Spawner : MonoBehaviour
{
    [Header("Configuration")]
    public GameObject[] fellaPrefabs;
    public GameObject anvilPrefab;
    public Transform spawnPoint;

    [Header("UI References")]
    public Image nextPreviewImage;

    [Header("Visuals")]
    public Transform guideLight;

    [Header("Motion Feel")]
    public float smoothSpeed = 20f;
    public float spawnAnimationSpeed = 12f;

    [Header("Laser Settings")]
    public LayerMask laserMask;

    [Header("Bad Blocks (Level Modifiers)")]
    [Tooltip("Drag your bad block prefabs (stones, un-mergables, etc) here")]
    public GameObject[] badBlockPrefabs;

    private Sprite[] activeSkinSprites;

    public void SetSkin(Sprite[] newSprites)
    {
        if (newSprites != null && newSprites.Length >= fellaPrefabs.Length)
        {
            activeSkinSprites = newSprites;
        }
    }

    public Sprite GetSkinSprite(int index)
    {
        if (activeSkinSprites != null && index < activeSkinSprites.Length)
        {
            return activeSkinSprites[index];
        }
        return null;
    }

    private GameObject currentFella;
    private int nextFellaIndex;
    private int currentFellaIndex;
    private float targetX;

    public bool IsAnvilMode { get; private set; } = false;
    
    public bool isHoldingBadBlock { get; private set; } = false;
    private bool isNextBadBlock = false;
    private GameObject nextBadBlockPrefab = null;
    private GameObject currentBadBlockPrefab = null;

    public void SwapPrefabs(GameObject[] newPrefabs)
    {
        if (newPrefabs != null && newPrefabs.Length > 0)
        {
            fellaPrefabs = newPrefabs;
        }
    }

    public void Init()
    {
        if (spawnPoint != null) targetX = spawnPoint.position.x;

        if (fellaPrefabs.Length > 0)
        {
            GenerateNextSpawn();
            SpawnPreview();
        }
    }

    public void MoveSpawnPoint(Vector3 newPosition)
    {
        targetX = newPosition.x;
    }

    void Update()
    {
        if (spawnPoint == null) return;

        float currentX = spawnPoint.position.x;
        float smoothedX = Mathf.Lerp(currentX, targetX, Time.deltaTime * smoothSpeed);
        Vector3 finalPos = new Vector3(smoothedX, spawnPoint.position.y, 0);
        spawnPoint.position = finalPos;

        if (currentFella != null) currentFella.transform.position = spawnPoint.position;

        UpdateLaser(finalPos);
    }

    public void ActivateAnvil()
    {
        if (IsAnvilMode) return;

        if (currentFella != null) Destroy(currentFella);

        IsAnvilMode = true;
        isHoldingBadBlock = false;
        currentFella = Instantiate(anvilPrefab, spawnPoint.position, Quaternion.identity);

        Rigidbody2D rb = currentFella.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;
        }

        Collider2D col = currentFella.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Vector3 targetScale = anvilPrefab.transform.localScale;

        currentFella.transform.localScale = Vector3.zero;
        StartCoroutine(AnimateSpawn(currentFella.transform, targetScale));
    }

    void UpdateLaser(Vector3 origin)
    {
        if (guideLight == null) return;

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 20f, laserMask);
        float closestDistance = 15f;

        foreach (var hit in hits)
        {
            if (hit.collider.isTrigger) continue;
            if (currentFella != null && hit.collider.gameObject == currentFella) continue;

            if (hit.distance < closestDistance) closestDistance = hit.distance;
        }

        guideLight.position = new Vector3(origin.x, origin.y - (closestDistance / 2), -2f);

        LaserEffect effect = guideLight.GetComponent<LaserEffect>();
        if (effect != null) effect.SetDistance(closestDistance);
        else guideLight.localScale = new Vector3(guideLight.localScale.x, closestDistance, 1);
    }

    private void GenerateNextSpawn()
    {
        isNextBadBlock = false;
        nextBadBlockPrefab = null;

        int maxSpawnIndex = Mathf.Min(fellaPrefabs.Length, 4);

        if (LevelManager.Instance != null)
        {
            int lvl = LevelManager.Instance.currentLevel;

            if (lvl >= LevelManager.Instance.dilutionLevel2) maxSpawnIndex = Mathf.Min(fellaPrefabs.Length, 6);
            else if (lvl >= LevelManager.Instance.dilutionLevel1) maxSpawnIndex = Mathf.Min(fellaPrefabs.Length, 5);

            if (lvl >= LevelManager.Instance.badSpawnStartLevel && badBlockPrefabs != null && badBlockPrefabs.Length > 0)
            {
                if (Random.value < LevelManager.Instance.badSpawnChance)
                {
                    isNextBadBlock = true;
                    nextBadBlockPrefab = badBlockPrefabs[Random.Range(0, badBlockPrefabs.Length)];
                }
            }
        }

        if (!isNextBadBlock)
        {
            nextFellaIndex = Random.Range(0, maxSpawnIndex);
        }
    }

    public void SpawnPreview()
    {
        if (spawnPoint == null || fellaPrefabs.Length == 0) return;

        if (IsAnvilMode && currentFella != null) return;

        IsAnvilMode = false;
        if (currentFella != null) Destroy(currentFella);

        isHoldingBadBlock = isNextBadBlock;
        currentBadBlockPrefab = nextBadBlockPrefab;
        currentFellaIndex = nextFellaIndex;
        
        GenerateNextSpawn();
        UpdateNextUI();

        if (isHoldingBadBlock && currentBadBlockPrefab != null)
        {
            currentFella = Instantiate(currentBadBlockPrefab, spawnPoint.position, Quaternion.identity);
        }
        else
        {
            currentFella = Instantiate(fellaPrefabs[currentFellaIndex], spawnPoint.position, Quaternion.identity);
        }

        Vector3 targetScale = currentFella.transform.localScale;
        currentFella.transform.localScale = Vector3.zero;
        StartCoroutine(AnimateSpawn(currentFella.transform, targetScale));

        Rigidbody2D rb = currentFella.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;
        }

        Collider2D col = currentFella.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        FellaLogic logic = currentFella.GetComponent<FellaLogic>();
        if (logic != null) logic.enabled = false;

        if (guideLight != null) guideLight.gameObject.SetActive(true);
    }

    IEnumerator AnimateSpawn(Transform target, Vector3 endScale)
    {
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * spawnAnimationSpeed;
            float scaleMultiplier = Mathf.Sin(t * Mathf.PI * 0.5f) + (Mathf.Sin(t * Mathf.PI) * 0.2f);
            if (t >= 1f) scaleMultiplier = 1f;

            if (target != null) target.localScale = endScale * scaleMultiplier;
            yield return null;
        }
        if (target != null) target.localScale = endScale;
    }

    public void DropCurrent(MergeGameController controller)
    {
        if (currentFella == null) return;

        Rigidbody2D rb = currentFella.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;
        }

        Collider2D col = currentFella.GetComponent<Collider2D>();
        if (col != null) col.enabled = true;

        if (!IsAnvilMode && !isHoldingBadBlock)
        {
            FellaLogic logic = currentFella.GetComponent<FellaLogic>();
            if (logic == null) logic = currentFella.AddComponent<FellaLogic>();
            logic.enabled = true;
            logic.Setup(currentFellaIndex, controller);
        }

        currentFella = null;
        isHoldingBadBlock = false;
        if (guideLight != null) guideLight.gameObject.SetActive(false);
    }

    private void UpdateNextUI()
    {
        if (nextPreviewImage != null)
        {
            Sprite spriteToShow = null;

            if (isNextBadBlock && nextBadBlockPrefab != null)
            {
                SpriteRenderer prefabRenderer = nextBadBlockPrefab.GetComponent<SpriteRenderer>();
                if (prefabRenderer != null) spriteToShow = prefabRenderer.sprite;
            }
            else if (fellaPrefabs.Length > nextFellaIndex)
            {
                if (activeSkinSprites != null && nextFellaIndex < activeSkinSprites.Length)
                {
                    spriteToShow = activeSkinSprites[nextFellaIndex];
                }
                else
                {
                    SpriteRenderer prefabRenderer = fellaPrefabs[nextFellaIndex].GetComponent<SpriteRenderer>();
                    if (prefabRenderer != null) spriteToShow = prefabRenderer.sprite;
                }
            }

            if (spriteToShow != null)
            {
                nextPreviewImage.sprite = spriteToShow;
                nextPreviewImage.color = Color.white;
                nextPreviewImage.preserveAspect = true;
            }
        }
    }

    public void ApplySkinToFella(GameObject fellaObj, int tierIndex)
    {
        if (activeSkinSprites != null && tierIndex < activeSkinSprites.Length)
        {
            SpriteRenderer sr = fellaObj.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = activeSkinSprites[tierIndex];
        }
    }

    public int GetPrefabCount() { return fellaPrefabs.Length; }
    public GameObject GetNextTierPrefab(int currentTier)
    {
        if (currentTier + 1 < fellaPrefabs.Length) return fellaPrefabs[currentTier + 1];
        return null;
    }
}