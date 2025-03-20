using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private GameObject monsterPrefab; // The monster prefab to spawn
    [SerializeField] private float spawnInterval = 5f;  // Time interval between spawns
    [SerializeField] private int maxMonstersPerSpawn = 3; // Number of monsters to spawn each time
    [SerializeField] private float spawnHeightOffset = 1f; // Offset for spawn height (if needed)

    private void Start()
    {
        // Start the spawn loop
        InvokeRepeating("SpawnMonsters", 0f, spawnInterval);
    }

    private void SpawnMonsters()
    {
        // Spawn monsters periodically
        for (int i = 0; i < maxMonstersPerSpawn; i++)
        {
            Vector3 spawnPosition = GetRandomPositionFromSpawner();
            GameObject monster = Instantiate(monsterPrefab, spawnPosition, Quaternion.identity);

            // Add any initialization code for the monster here (like setting its state)
            monster.SetActive(true);  // Ensure it’s active immediately (no delay)
        }
    }

    private Vector3 GetRandomPositionFromSpawner()
    {
        // Generate a random offset around the spawner's position
        float x = Random.Range(-1f, 1f); // Random within a small range around the spawner
        float z = Random.Range(-1f, 1f); // Random within a small range around the spawner

        // Use the spawner's y-position with an optional height offset
        float y = transform.position.y + spawnHeightOffset;

        return new Vector3(transform.position.x + x, y, transform.position.z + z);
    }
}
