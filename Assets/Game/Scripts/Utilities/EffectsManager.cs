using UnityEngine;

public class EffectsManager : MonoBehaviour
{
    [Header("Particle Prefabs")]
    [Tooltip("Drag your Particle System Prefab here")]
    public GameObject mergeParticlePrefab;

    public void PlayMergeEffect(Vector3 position)
    {
        if (mergeParticlePrefab != null)
        {
            GameObject p = Instantiate(mergeParticlePrefab, position, Quaternion.identity);

            ParticleSystem ps = p.GetComponent<ParticleSystem>();
            if (ps != null) ps.Play();

            Destroy(p, 2.0f);
        }
    }
}