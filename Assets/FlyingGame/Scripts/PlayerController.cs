using UnityEngine;

/// <summary>
/// 飞行器操控：WASD / 方向键 移动，带侧倾与俯仰姿态，圆形碰撞检测。
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("移动范围")]
    public float minX = -5.2f;
    public float maxX = 5.2f;
    public float minY = -1.6f;
    public float maxY = 3.6f;

    [Header("手感")]
    public float moveSpeed = 12f;        // 单位/秒
    public float followSharpness = 14f;  // 越大越跟手
    public float bankAngle = 26f;        // 左右侧倾
    public float pitchAngle = 14f;       // 上下俯仰
    public float idleBob = 0.12f;

    [Header("碰撞")]
    public float hitRadius = 0.78f;

    [Header("子物体")]
    public Transform modelRoot;
    [Tooltip("菜单展示时的模型缩放")] public float menuScale = 1f;
    [Tooltip("游戏进行时的模型缩放")] public float playScale = 1f;
    public GameObject engineGlow;
    public TrailRenderer trail;

    Vector3 homePos;
    Vector2 offset;
    float roll;
    float pitch;
    float bobPhase;
    bool frozen;
    bool crashed;

    public bool Crashed { get { return crashed; } }

    void Awake()
    {
        homePos = transform.position;
        bobPhase = Random.Range(0f, 6f);
    }

    void Update()
    {
        bool playable = GameManager.Instance != null && GameManager.Instance.State == GameManager.GameState.Playing;

        if (!playable || frozen || crashed)
        {
            UpdateIdleMotion();
            UpdateVisuals(0f, 0f);
            return;
        }

        float h = 0f, v = 0f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;

        h = Mathf.Clamp(h, -1f, 1f);
        v = Mathf.Clamp(v, -1f, 1f);

        offset += new Vector2(h, v) * moveSpeed * Time.deltaTime;
        offset.x = Mathf.Clamp(offset.x, minX, maxX);
        offset.y = Mathf.Clamp(offset.y, minY, maxY);

        bobPhase += Time.deltaTime * 3.1f;
        Vector3 desired = homePos + new Vector3(offset.x, offset.y + Mathf.Sin(bobPhase) * idleBob * 0.5f, 0f);
        float k = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desired, k);
        transform.position = new Vector3(transform.position.x, transform.position.y, homePos.z);

        UpdateVisuals(h, v);
        CheckCollisions();
    }

    void UpdateIdleMotion()
    {
        bobPhase += Time.deltaTime * 1.4f;
        offset = Vector2.MoveTowards(offset, Vector2.zero, Time.deltaTime * 2.5f);
        Vector3 desired = homePos + new Vector3(offset.x, offset.y + Mathf.Sin(bobPhase) * idleBob, 0f);
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-3f * Time.deltaTime));
    }

    void UpdateVisuals(float h, float v)
    {
        roll = Mathf.Lerp(roll, -h * bankAngle, Time.deltaTime * 6f);
        pitch = Mathf.Lerp(pitch, v * pitchAngle * 0.55f, Time.deltaTime * 6f);
        if (modelRoot != null)
        {
            bool menu = GameManager.Instance == null || GameManager.Instance.State != GameManager.GameState.Playing;
            float yaw = menu ? -9f + Mathf.Sin(bobPhase * 0.42f) * 7f : 0f;
            float tilt = menu ? 19f : 0f;
            modelRoot.localRotation = Quaternion.Euler(-pitch + tilt, yaw, roll);
            float s = menu ? menuScale : playScale;
            if (!crashed && Mathf.Abs(modelRoot.localScale.x - s) > 0.01f)
            {
                float nv = Mathf.MoveTowards(modelRoot.localScale.x, s, Time.deltaTime * 1.6f);
                modelRoot.localScale = new Vector3(nv, nv, nv);
            }
        }
    }

    void CheckCollisions()
    {
        if (crashed) return;
        var list = Obstacle.Active;
        Vector3 p = transform.position;
        for (int i = 0; i < list.Count; i++)
        {
            Obstacle o = list[i];
            if (o == null) continue;
            Vector3 d = o.transform.position - p;
            float r = o.radius * o.transform.localScale.x + hitRadius;
            if (d.sqrMagnitude <= r * r)
            {
                PlayCrash();
                if (GameManager.Instance != null) GameManager.Instance.OnPlayerHit();
                return;
            }
        }
    }

    public void PlayCrash()
    {
        if (crashed) return;
        crashed = true;
        if (engineGlow != null) engineGlow.SetActive(false);
        if (trail != null) trail.emitting = false;
        if (modelRoot != null)
        {
            StopAllCoroutines();
            StartCoroutine(CrashRoutine());
        }
    }

    System.Collections.IEnumerator CrashRoutine()
    {
        float t = 0f;
        while (t < 0.6f)
        {
            t += Time.deltaTime;
            modelRoot.localRotation = Quaternion.Euler(
                modelRoot.localRotation.eulerAngles.x,
                modelRoot.localRotation.eulerAngles.y + 720f * Time.deltaTime,
                modelRoot.localRotation.eulerAngles.z + 400f * Time.deltaTime);
            float s = Mathf.Lerp(1f, 0.25f, t / 0.6f);
            modelRoot.localScale = new Vector3(s, s, s);
            yield return null;
        }
        modelRoot.gameObject.SetActive(false);
    }

    public void SetFrozen(bool value)
    {
        frozen = value;
    }

    public void ResetToStart()
    {
        StopAllCoroutines();
        crashed = false;
        frozen = false;
        offset = Vector2.zero;
        transform.position = homePos;
        roll = 0f; pitch = 0f;
        if (modelRoot != null)
        {
            modelRoot.gameObject.SetActive(true);
            modelRoot.localRotation = Quaternion.identity;
            modelRoot.localScale = Vector3.one;
        }
        if (engineGlow != null) engineGlow.SetActive(true);
        if (trail != null)
        {
            trail.Clear();
            trail.emitting = true;
        }
    }

    public void ResetToIdle()
    {
        StopAllCoroutines();
        crashed = false;
        frozen = false;
        offset = Vector2.zero;
        transform.position = homePos;
        roll = 0f; pitch = 0f;
        if (modelRoot != null)
        {
            modelRoot.gameObject.SetActive(true);
            modelRoot.localRotation = Quaternion.identity;
            modelRoot.localScale = Vector3.one;
        }
        if (engineGlow != null) engineGlow.SetActive(true);
        if (trail != null)
        {
            trail.Clear();
            trail.emitting = true;
        }
    }
}
