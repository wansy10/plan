using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 障碍物（陨石 / 岩石）：向玩家方向飞驰，掠过玩家后判定为“顺利通过”。
/// </summary>
public class Obstacle : MonoBehaviour
{
    /// <summary>场上所有激活中的障碍物（供玩家做碰撞检测）</summary>
    public static readonly List<Obstacle> Active = new List<Obstacle>();

    [Tooltip("基础碰撞半径（会乘以 localScale）")]
    public float radius = 1.3f;

    [Tooltip("向 -Z 方向的飞行速度")]
    public float speed = 22f;

    [Tooltip("自转速度")]
    public Vector3 spin = new Vector3(18f, 12f, 8f);

    const float PassZ = -5.5f;
    const float KillZ = -22f;

    bool passed;
    bool recycled;

    Material[] mats;
    Color[] baseColors;

    const float FadeStartZ = -1.5f;   // 从这里开始淡出
    const float FadeEndZ = -7.0f;     // 到这里完全透明（镜头前）

    public System.Action<Obstacle> OnRecycled;

    void Awake()
    {
        Renderer[] rs = GetComponentsInChildren<Renderer>(true);
        int total = 0;
        for (int i = 0; i < rs.Length; i++) total += rs[i].materials.Length;

        mats = new Material[total];
        baseColors = new Color[total];
        int k = 0;
        for (int i = 0; i < rs.Length; i++)
        {
            Material[] list = rs[i].materials;
            for (int j = 0; j < list.Length; j++)
            {
                mats[k] = list[j];
                baseColors[k] = list[j].color;
                k++;
            }
        }
    }

    void OnEnable()
    {
        passed = false;
        recycled = false;
        if (!Active.Contains(this)) Active.Add(this);
    }

    void OnDisable()
    {
        if (Active.Contains(this)) Active.Remove(this);
    }

    void Update()
    {
        if (recycled) return;

        Vector3 p = transform.position;
        p.z -= speed * Time.deltaTime;
        transform.position = p;

        transform.Rotate(spin * Time.deltaTime, Space.Self);

        if (!passed && p.z < PassZ)
        {
            passed = true;
            if (GameManager.Instance != null) GameManager.Instance.OnObstaclePassed();
        }

        UpdateFade(p.z);

        if (p.z < KillZ) Recycle();
    }

    void UpdateFade(float z)
    {
        if (mats == null) return;
        float a = Mathf.InverseLerp(FadeEndZ, FadeStartZ, z);
        for (int i = 0; i < mats.Length; i++)
        {
            if (mats[i] == null) continue;
            Color c = baseColors[i];
            c.a = a;
            mats[i].color = c;
        }
    }

    void Recycle()
    {
        if (recycled) return;
        recycled = true;
        if (OnRecycled != null) OnRecycled(this);
    }
}
