using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏总控：状态机 / 计时 / 音效 / 界面切换
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum GameState { Menu, Playing, Won, Lost }

    public static GameManager Instance { get; private set; }

    [Header("规则")]
    [Tooltip("通关所需坚持的秒数")]
    public float roundDuration = 60f;

    [Header("场景引用")]
    public PlayerController player;
    public ObstacleSpawner spawner;
    public CameraFollow cameraRig;

    [Header("音效")]
    public AudioSource flySource;      // 飞行时循环
    public AudioSource sfxSource;      // 一次性音效
    public AudioClip flyClip;          // 飞行
    public AudioClip crashClip;        // 撞到障碍物
    public AudioClip passClip;         // 顺利通过 / 通关

    [Header("界面 - 开始菜单")]
    public GameObject menuPanel;

    [Header("界面 - 游戏内")]
    public GameObject hudPanel;
    public Text timerText;
    public Text hintText;
    public Image progressFill;
    public Text passedText;

    [Header("界面 - 结算")]
    public GameObject resultPanel;
    public Text resultTitle;
    public Text resultSub;
    public Text resultPassed;

    public GameState State { get; private set; } = GameState.Menu;

    float timeLeft;
    int passedCount;
    float engineTarget;   // 引擎音量目标
    float shakeTime;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        GoToMenu();
    }

    void Update()
    {
        // 引擎音量平滑
        if (flySource != null)
        {
            flySource.volume = Mathf.MoveTowards(flySource.volume, engineTarget, Time.unscaledDeltaTime * 0.8f);
            if (engineTarget <= 0f && flySource.volume <= 0.001f && flySource.isPlaying) flySource.Stop();
        }

        if (State == GameState.Playing)
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                Win();
            }
            RefreshHud();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (State == GameState.Playing) GoToMenu();
        }
    }

    // ---------------- 状态切换 ----------------

    public void GoToMenu()
    {
        State = GameState.Menu;
        timeLeft = roundDuration;
        passedCount = 0;
        engineTarget = 0f;

        if (spawner != null)
        {
            spawner.StopSpawn();
            spawner.ClearAll();
        }
        if (player != null) player.ResetToIdle();

        if (menuPanel != null) menuPanel.SetActive(true);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        if (cameraRig != null) cameraRig.SetShake(0f);
    }

    public void StartGame()
    {
        State = GameState.Playing;
        timeLeft = roundDuration;
        passedCount = 0;
        engineTarget = 1f;

        if (spawner != null)
        {
            spawner.ClearAll();
            spawner.Begin();
        }
        if (player != null) player.ResetToStart();
        if (cameraRig != null) cameraRig.SetShake(0f);

        if (menuPanel != null) menuPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);

        RefreshHud();

        if (flySource != null && flyClip != null)
        {
            flySource.clip = flyClip;
            flySource.loop = true;
            if (!flySource.isPlaying) flySource.Play();
        }
        PlaySfx(passClip, 0.35f); // 起飞提示音
    }

    void Win()
    {
        State = GameState.Won;
        engineTarget = 0f;
        if (spawner != null) spawner.StopSpawn();
        if (player != null) player.SetFrozen(true);
        PlaySfx(passClip, 1f);

        ShowResult("通 关 成 功",
                   "你在陨石带里坚持了整整 60 秒",
                   new Color(0.42f, 1f, 0.78f));
    }

    public void Lose()
    {
        if (State != GameState.Playing) return;
        State = GameState.Lost;
        engineTarget = 0f;
        if (spawner != null) spawner.StopSpawn();
        if (player != null) player.PlayCrash();
        if (cameraRig != null) cameraRig.SetShake(0.55f);
        PlaySfx(crashClip, 1f);

        float survived = roundDuration - timeLeft;
        ShowResult("撞 毁 了",
                   string.Format("坚持了 {0:F1} 秒，离通关只差一点点", survived),
                   new Color(1f, 0.45f, 0.36f));
    }

    void ShowResult(string title, string sub, Color titleColor)
    {
        if (menuPanel != null) menuPanel.SetActive(false);
        if (hudPanel != null) hudPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(true);

        if (resultTitle != null)
        {
            resultTitle.text = title;
            resultTitle.color = titleColor;
        }
        if (resultSub != null) resultSub.text = sub;
        if (resultPassed != null) resultPassed.text = "避开陨石  " + passedCount + "  颗";
    }

    // ---------------- 游戏事件 ----------------

    public void OnObstaclePassed()
    {
        if (State != GameState.Playing) return;
        passedCount++;
        PlaySfx(passClip, 0.55f);
        RefreshHud();
    }

    public void OnPlayerHit()
    {
        Lose();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void PlaySfx(AudioClip clip, float volume)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip, volume);
    }

    void RefreshHud()
    {
        if (timerText != null)
        {
            int secs = Mathf.CeilToInt(timeLeft);
            timerText.text = secs.ToString("00");
            timerText.color = Color.Lerp(new Color(1f, 0.42f, 0.34f), new Color(0.90f, 0.97f, 1f), Mathf.Clamp01(timeLeft / 20f));
        }
        if (progressFill != null)
        {
            float p = 1f - Mathf.Clamp01(timeLeft / roundDuration);
            progressFill.fillAmount = p;
        }
        if (passedText != null)
        {
            passedText.text = "已避开陨石  " + passedCount + "  颗";
        }
    }
}
