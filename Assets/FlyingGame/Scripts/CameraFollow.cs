using UnityEngine;

/// <summary>
/// 跟随玩家的摄像机，附带轻微延迟、加速感 FOV 与撞击震屏。
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0f, 2.35f, -9.2f);
    public float smooth = 4.5f;

    [Header("FOV")]
    public Camera cam;
    public float baseFov = 64f;
    public float maxFov = 76f;

    [Header("震屏")]
    public float shakeStrength;

    Vector3 shakeSeed;
    float shakeTime;

    void Reset()
    {
        cam = GetComponent<Camera>();
    }

    void Awake()
    {
        if (cam == null) cam = GetComponent<Camera>();
        shakeSeed = new Vector3(Random.value * 100f, Random.value * 100f, Random.value * 100f);
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(target.position.x * 0.55f, offset.y + target.position.y * 0.35f, offset.z);
        transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-smooth * Time.deltaTime));

        Vector3 look = new Vector3(target.position.x * 0.28f, target.position.y * 0.55f + 0.5f, target.position.z + 14f);
        Quaternion rot = Quaternion.LookRotation(look - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, 1f - Mathf.Exp(-6f * Time.deltaTime));

        if (cam != null)
        {
            float sp = 15f;
            if (ObstacleSpawner.Instance != null) sp = ObstacleSpawner.Instance.CurrentSpeedPublic;
            float t = Mathf.InverseLerp(15f, 26f, sp);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, Mathf.Lerp(baseFov, maxFov, t), Time.deltaTime * 1.5f);
        }

        if (shakeTime > 0f)
        {
            shakeTime -= Time.deltaTime;
            float amp = shakeStrength * Mathf.Clamp01(shakeTime / 0.6f);
            float t = Time.time * 46f;
            Vector3 s = new Vector3(
                Mathf.PerlinNoise(shakeSeed.x, t) - 0.5f,
                Mathf.PerlinNoise(shakeSeed.y, t) - 0.5f,
                0f) * amp * 2.2f;
            transform.position += s;
        }
    }

    public void SetShake(float strength)
    {
        shakeStrength = strength;
        shakeTime = strength > 0f ? 0.6f : 0f;
    }
}
