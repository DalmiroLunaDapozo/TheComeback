using UnityEngine;
using System.Collections;

public class Disintegration : MonoBehaviour
{
    public float dissolveDuration = 2f;
    private float dissolveStrength;

    private EnemyAI enemyAI;
    private bool dissolveStarted;
    private Renderer objRenderer;
    private Material monsterMaterial;

    private void Start()
    {
        dissolveStarted = false;
        enemyAI = GetComponentInParent<EnemyAI>();

        objRenderer = GetComponent<Renderer>();

        // Get the first material (assuming it's the monster texture)
        monsterMaterial = objRenderer.material;
    }

    public IEnumerator Dissolver()
    {
        float elapsedTime = 0;

        while (elapsedTime < dissolveDuration)
        {
            elapsedTime += Time.deltaTime;
            dissolveStrength = Mathf.Lerp(0, 1f, elapsedTime / dissolveDuration);

            // Apply dissolve effect
            if (monsterMaterial.HasProperty("_Dissolve"))
            {
                monsterMaterial.SetFloat("_Dissolve", dissolveStrength);
            }

            yield return null;
        }
    }

    private void StartDissolve()
    {
        StartCoroutine(Dissolver());
    }

    void Update()
    {
        if (enemyAI.isDead && !dissolveStarted)
        {
           
            dissolveStarted = true;
            Invoke(nameof(StartDissolve), 2f);
        }
    }
}
