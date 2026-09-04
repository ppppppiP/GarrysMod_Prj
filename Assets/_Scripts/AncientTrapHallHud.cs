using TMPro;
using UnityEngine;

public sealed class AncientTrapHallHud : MonoBehaviour
{
    [InspectorName("Уровень")] public AncientTrapHallLevel level;
    [Header("Верхняя панель")]
    public TMP_Text healthText;
    public TMP_Text timerText;
    public TMP_Text coinsText;
    public TMP_Text magnetText;
    public TMP_Text doubleText;
    public TMP_Text shieldText;
    [Header("Окна")]
    public GameObject countdownPanel;
    public TMP_Text countdownText;
    public GameObject pausePanel;
    public GameObject revivePanel;
    public TMP_Text reviveText;
    public GameObject resultsPanel;
    public TMP_Text resultsText;

    [Header("Производительность")]
    [InspectorName("Частота обновления HUD"), Range(5f, 30f)] public float refreshRate = 10f;

    private float nextRefreshTime;
    private int lastHealth = int.MinValue;
    private int lastMaximumHealth = int.MinValue;
    private int lastSeconds = int.MinValue;
    private int lastCoins = int.MinValue;
    private int lastMagnet = int.MinValue;
    private int lastDouble = int.MinValue;
    private int lastShield = int.MinValue;
    private int lastMagnetInventory = int.MinValue;
    private int lastDoubleInventory = int.MinValue;
    private int lastShieldInventory = int.MinValue;
    private int lastReviveCost = int.MinValue;
    private AncientTrapHallLevel.GameState lastState = (AncientTrapHallLevel.GameState)(-1);

    private void Awake() { if (level == null) level = FindFirstObjectByType<AncientTrapHallLevel>(); }
    private void Update()
    {
        if (level == null) return;
        if (Time.unscaledTime < nextRefreshTime) return;
        nextRefreshTime = Time.unscaledTime + 1f / Mathf.Max(1f, refreshRate);

        if (healthText != null && (lastHealth != level.Health || lastMaximumHealth != level.MaximumHealth))
        {
            lastHealth = level.Health;
            lastMaximumHealth = level.MaximumHealth;
            healthText.text = new string('♥', lastHealth) + new string('♡', lastMaximumHealth - lastHealth);
        }
        if (timerText != null && lastSeconds != level.ActiveSeconds)
        {
            lastSeconds = level.ActiveSeconds;
            timerText.SetText("{0} СЕК", lastSeconds);
        }
        if (coinsText != null && lastCoins != level.RunCoins)
        {
            lastCoins = level.RunCoins;
            coinsText.SetText("МОНЕТЫ  {0}", lastCoins);
        }

        int magnetSeconds = Mathf.CeilToInt(level.MagnetSeconds);
        int doubleSeconds = Mathf.CeilToInt(level.DoubleSeconds);
        int shieldSeconds = Mathf.CeilToInt(level.ShieldSeconds);
        int magnetInventory = level.MagnetInventory;
        int doubleInventory = level.DoubleInventory;
        int shieldInventory = level.ShieldInventory;
        if (magnetText != null && (lastMagnet != magnetSeconds || lastMagnetInventory != magnetInventory))
        {
            lastMagnet = magnetSeconds; lastMagnetInventory = magnetInventory;
            magnetText.SetText(magnetSeconds > 0 ? "МАГНИТ\n{0}с" : "МАГНИТ\n×{0}", magnetSeconds > 0 ? magnetSeconds : magnetInventory);
        }
        if (doubleText != null && (lastDouble != doubleSeconds || lastDoubleInventory != doubleInventory))
        {
            lastDouble = doubleSeconds; lastDoubleInventory = doubleInventory;
            doubleText.SetText(doubleSeconds > 0 ? "×2\n{0}с" : "×2\n×{0}", doubleSeconds > 0 ? doubleSeconds : doubleInventory);
        }
        if (shieldText != null && (lastShield != shieldSeconds || lastShieldInventory != shieldInventory))
        {
            lastShield = shieldSeconds; lastShieldInventory = shieldInventory;
            shieldText.SetText(shieldSeconds > 0 ? "ЩИТ\n{0}с" : "ЩИТ\n×{0}", shieldSeconds > 0 ? shieldSeconds : shieldInventory);
        }

        bool countdown = level.State == AncientTrapHallLevel.GameState.Countdown;
        if (lastState != level.State)
        {
            lastState = level.State;
            if (countdownPanel != null) countdownPanel.SetActive(countdown);
            if (pausePanel != null) pausePanel.SetActive(lastState == AncientTrapHallLevel.GameState.Paused);
            if (revivePanel != null) revivePanel.SetActive(lastState == AncientTrapHallLevel.GameState.Revive);
            if (resultsPanel != null) resultsPanel.SetActive(lastState == AncientTrapHallLevel.GameState.Results);
        }
        if (countdownText != null && countdown) countdownText.SetText("{0}", level.CountdownNumber);
        if (reviveText != null && lastState == AncientTrapHallLevel.GameState.Revive && lastReviveCost != level.ReviveCost)
        {
            lastReviveCost = level.ReviveCost;
            reviveText.SetText("ВОСКРЕШЕНИЕ\nРеклама или {0} монет", lastReviveCost);
        }
        if (resultsText != null && lastState == AncientTrapHallLevel.GameState.Results)
            resultsText.SetText("РЕЗУЛЬТАТ\n{0} сек  •  {1} монет", lastSeconds, lastCoins);
    }

    public void Pause() => level.TogglePause();
    public void Magnet() => level.ActivateMagnet();
    public void DoubleCoins() => level.ActivateDoubleCoins();
    public void Shield() => level.ActivateShield();
    public void AdRevive() => level.RequestAdRevive();
    public void PaidRevive() => level.RequestPaidRevive();
    public void Finish() => level.FinishRun();
    public void Restart() => level.RestartRun();
    public void Exit() => level.ReturnToCamp();
}
