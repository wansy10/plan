using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 陨石生成器：在玩家前方随机生成岩石障碍，随时间加快节奏与速度。
/// </summary>
public class ObstacleSpawner : MonoBehaviour
{
    [Header("预制体")]
    public GameObject[] rockPrefabs;

    [Header("生成参数")]
    public float spawnZ = 62f;
    public Vector2 spawnInterval = new Vector2(0.78f, 1.25f);
    public float spawnIntervalEnd = 0.44f;
    public Vector2 spawnX = new Vector2(-7.2f, 7.2f);
    public Vector2 spawnY = new Vector2(-2.6f, 5.0f);
    public Vector2 scaleRange = new Vector2(0.85f, 2.05f);

    [Header("速度")]
    public float startSpeed = 15f;
    public float endSpeed = 26f;

    readonly List<GameObject> pool = new List<GameObject>();
    bool spawning;
    float timer;
    float elapsed;
    int spawnedCount;
    System.Random rng = new System.Random(20261102);

    public static ObstacleSpawner Instance { get; private set; }

    public bool Spawning { get { return spawning; } }

    /// <summary>当前障碍物飞行速度（供摄像机做加速 FOV 效果）</summary>
    public float CurrentSpeedPublic { get { return CurrentSpeed(); } }

    void Awake()
    {
        Instance = this;
        if (rockPrefabs == null || rockPrefabs.Length == 0)
            Debug.LogWarning("[ObstacleSpawner] 没有配置石头预制体！");
    }

    void Update()
    {
        if (!spawning) return;
        elapsed += Time.deltaTime;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            SpawnOne();
            timer = NextInterval();
        }
    }

    float NextInterval()
    {
        float t = Mathf.Clamp01(elapsed / 60f);
        float baseInterval = Mathf.Lerp(spawnInterval.x, spawnIntervalEnd, t);
        return baseInterval * (0.75f + 0.5f * (float)rng.NextDouble());
    }

    float CurrentSpeed()
    {
        float t = Mathf.Clamp01(elapsed / 60f);
        return Mathf.Lerp(startSpeed, endSpeed, t);
    }

    public void Begin()
    {
        elapsed = 0f;
        spawnedCount = 0;
        spawning = true;
        timer = 0.9f;   // 开局给一点反应时间
        rng = new System.Random(System.Environment.TickCount ^ GetInstanceID());
    }

    public void StopSpawn()
    {
        spawning = false;
    }

    void SpawnOne()
    {
        if (rockPrefabs == null || rockPrefabs.Length == 0) return;

        GameObject prefab = rockPrefabs[rng.Next(rockPrefabs.Length)];
        GameObject go = GetFromPool(prefab);

        float scale = Mathf.Lerp(scaleRange.x, scaleRange.y, (float)rng.NextDouble());
        // 偶尔来一个大家伙，制造压迫感
        if (rng.NextDouble() < 0.12) scale *= 1.3f;

        float px = Mathf.Lerp(spawnX.x, spawnX.y, (float)rng.NextDouble());
        float py = Mathf.Lerp(spawnY.x, spawnY.y, (float)rng.NextDouble());

        // 开局前三颗不堵在出生点正面
        int guard = 0;
        while (spawnedCount < 3 && guard < 40)
        {
            float dx = px - 0f;
            float dy = py - 0.7f;
            if (dx * dx + dy * dy > 16f) break;
            px = Mathf.Lerp(spawnX.x, spawnX.y, (float)rng.NextDouble());
            py = Mathf.Lerp(spawnY.x, spawnY.y, (float)rng.NextDouble());
            guard++;
        }
        spawnedCount++;

        go.transform.position = new Vector3(
            px,
            py,
            spawnZ + Mathf.Lerp(0f, 12f, (float)rng.NextDouble()));

        go.transform.rotation = Random.rotationUniform;
        go.transform.localScale = Vector3.one * scale;
        go.SetActive(true);

        Obstacle ob = go.GetComponent<Obstacle>();
        if (ob != null)
        {
            ob.speed = CurrentSpeed();
            ob.spin = new Vector3(
                Mathf.Lerp(-25f, 25f, (float)rng.NextDouble()),
                Mathf.Lerp(-25f, 25f, (float)rng.NextDouble()),
                Mathf.Lerp(-25f, 25f, (float)rng.NextDouble()));
            
        }
    }

    GameObject GetFromPool(GameObject prefab)
    {
        for (int i = pool.Count - 1; i >= 0; i--)
        {
            if (pool[i] != null && !pool[i].activeSelf && pool[i].name.StartsWith(prefab.name))
            {
                return pool[i];
            }
        }

        GameObject go = Instantiate(prefab, transform);
        go.name = prefab.name;
        Obstacle ob = go.GetComponent<Obstacle>();
        if (ob == null) ob = go.AddComponent<Obstacle>();
        ob.OnRecycled = HandleRecycled;
        pool.Add(go);
        return go;
    }

    void HandleRecycled(Obstacle ob)
    {
        if (ob != null) ob.gameObject.SetActive(false);
    }

    /// <summary>清空场上所有障碍物（回池）</summary>
    public void ClearAll()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] != null && pool[i].activeSelf)
            {
                Obstacle ob = pool[i].GetComponent<Obstacle>();
                if (ob != null) ob.gameObject.SetActive(false);
            }
        }
    }
}
