using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    private RoomManager roomManager;

    [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField]
    private float spawnInterval = 5f;

    [SerializeField]
    private float SetspawnChance = 0.5f;
    private float spawnChance;
    private float spawnTimer;

    private bool forceNextSpawn;


    private void Start()
    {
        spawnTimer = spawnInterval;
        spawnChance = SetspawnChance;
    }


    private void Update()
    {
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            CheckSpawn();

            spawnTimer = spawnInterval;
        }
    }


    private void CheckSpawn()
    {
        if (forceNextSpawn)
        {
            forceNextSpawn = false;

            SpawnEnemy();

            return;
        }

        float randomValue = Random.value;

        if (randomValue <= spawnChance)
        {
            SpawnEnemy();
        }
        else
        {
            spawnChance += 0.1f;
            Debug.Log("Enemy ไม่เกิดในรอบนี้");
        }
    }


    private void SpawnEnemy()
    {
        Room room = roomManager.GetRandomNormalRoom();

        spawnChance = SetspawnChance;

        if (room == null)
        {
            Debug.Log("ไม่สามารถ Spawn Enemy ได้ เพราะไม่มีห้อง Normal");

            return;
        }


        Debug.Log("Enemy จะเกิดที่ " + room.gameObject.name);

        if (room.IsPlayerInside())
        {
            room.StartCursedImmediately();
        }
        else
        {
            room.SetWarning();
        }
    }

    public void ForceNextSpawn()
    {
        forceNextSpawn = true;
        Debug.Log("การทำความสะอาดสำเร็จ ? Enemy รอบถัดไปจะเกิดแน่นอน");
    }
}