using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    private RoomManager roomManager;

    [SerializeField]
    private GameObject enemyPrefab;

    [SerializeField]
    private float SetSpawnInterval;

    [SerializeField]
    private float SetspawnChance;
    private float spawnChance;
    private float spawnTimer;

    [SerializeField]
    private float ReduceTime = 2.0f;
    private void Start()
    {
        spawnTimer = SetSpawnInterval;
        spawnChance = SetspawnChance;
    }


    private void Update()
    {
        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)
        {
            CheckSpawn();

            spawnTimer = SetSpawnInterval;
        }
    }


    private void CheckSpawn()
    {

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

    public void ReduceTimeNextSpawn()
    {
        spawnTimer = Mathf.Max(0f, spawnTimer - ReduceTime);
        Debug.Log("Spawn Interval ปัจจุบัน: " + spawnTimer + " วินาที");

    }
}