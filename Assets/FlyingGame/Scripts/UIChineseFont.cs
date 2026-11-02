using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 兜底：若有 UI 文本缺少字体（例如跨机器打开场景导致丢失），运行时补上中文字体。
/// </summary>
[DefaultExecutionOrder(-500)]
public class UIChineseFont : MonoBehaviour
{
    public int fontSize = 64;

    static Font cachedFont;

    void Awake()
    {
        Apply();
    }

    public void Apply()
    {
        Text[] texts = GetComponentsInChildren<Text>(true);
        bool need = false;
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].font == null) { need = true; break; }
        }
        if (!need) return;

        Font f = GetChineseFont();
        if (f == null) return;

        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].font == null) texts[i].font = f;
        }
    }

    public static Font GetChineseFont()
    {
        if (cachedFont != null) return cachedFont;

        string[] names = new string[]
        {
            "Microsoft YaHei UI",
            "Microsoft YaHei",
            "微软雅黑",
            "SimHei",
            "Noto Sans SC",
            "PingFang SC",
            "Arial Unicode MS"
        };

        try
        {
            cachedFont = Font.CreateDynamicFontFromOSFont(names, 64);
        }
        catch
        {
            cachedFont = null;
        }

        if (cachedFont == null)
        {
            try { cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch { }
        }
        if (cachedFont == null)
        {
            try { cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
            catch { }
        }
        return cachedFont;
    }
}
