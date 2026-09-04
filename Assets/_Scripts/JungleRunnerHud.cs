using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class JungleRunnerHud : MonoBehaviour
{
    public JungleRunnerLevel level;
    public TMP_Text timerText;
    public TMP_Text coinsText;
    public TMP_Text countdownText;
    public TMP_Text reviveDetailsText;
    public TMP_Text reviveCostText;
    public TMP_Text resultsText;
    public TMP_Text magnetText;
    public TMP_Text doubleText;
    public TMP_Text shieldText;
    public GameObject pausePanel;
    public GameObject revivePanel;
    public GameObject resultsPanel;
    public Button pauseButton;
    public Button jumpButton;
    public Button magnetButton;
    public Button doubleButton;
    public Button shieldButton;
    public Button continueButton;
    public Button adReviveButton;
    public Button paidReviveButton;
    public Button finishButton;
    public Button restartButton;
    public Button pauseCampButton;
    public Button resultsCampButton;

    private float nextRefresh;

    private void Awake()
    {
        if (level == null) level = FindFirstObjectByType<JungleRunnerLevel>();
        Bind(pauseButton, () => level.TogglePause());
        Bind(jumpButton, () => level.RequestJump());
        Bind(magnetButton, () => level.ActivateMagnet());
        Bind(doubleButton, () => level.ActivateDoubleCoins());
        Bind(shieldButton, () => level.ActivateShield());
        Bind(continueButton, () => level.TogglePause());
        Bind(adReviveButton, () => level.RequestAdRevive());
        Bind(paidReviveButton, () => level.RequestPaidRevive());
        Bind(finishButton, () => level.RequestFinish());
        Bind(restartButton, () => level.RequestRestart());
        Bind(pauseCampButton, () => level.ReturnToCamp());
        Bind(resultsCampButton, () => level.ReturnToCamp());
        Refresh();
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null) button.onClick.AddListener(action);
    }

    private void Update()
    {
        if (level == null || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.1f;
        Refresh();
    }

    private void Refresh()
    {
        if (level == null) return;
        if (timerText != null) timerText.SetText("{0:00}:{1:00}", level.ActiveSeconds / 60, level.ActiveSeconds % 60);
        if (coinsText != null) coinsText.SetText("● {0}", level.TotalCoins);
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(level.IsCountingDown);
            countdownText.SetText("{0}", level.CountdownNumber);
        }
        if (pausePanel != null) pausePanel.SetActive(level.IsPaused);
        if (revivePanel != null) revivePanel.SetActive(level.IsWaitingForRevive);
        if (resultsPanel != null) resultsPanel.SetActive(level.IsShowingResults);
        if (reviveDetailsText != null) reviveDetailsText.SetText("Время {0:00}:{1:00}   Монеты {2}", level.ActiveSeconds / 60, level.ActiveSeconds % 60, level.RunCoins);
        if (reviveCostText != null) reviveCostText.SetText("ВОСКРЕСНУТЬ ЗА {0} ●", level.ReviveCost);
        if (resultsText != null) resultsText.SetText("Время {0:00}:{1:00}\nРекорд {2:00}:{3:00}\nМонеты {4}", level.ActiveSeconds / 60, level.ActiveSeconds % 60, level.BestSeconds / 60, level.BestSeconds % 60, level.RunCoins);
        if (adReviveButton != null) adReviveButton.gameObject.SetActive(level.CanUseAdRevive);
        bool gameplayControls = !level.IsIntroPlaying;
        if (jumpButton != null) jumpButton.interactable = gameplayControls;
        if (magnetButton != null) magnetButton.interactable = gameplayControls;
        if (doubleButton != null) doubleButton.interactable = gameplayControls;
        if (shieldButton != null) shieldButton.interactable = gameplayControls;
        if (magnetText != null) magnetText.SetText(level.MagnetSeconds > 0 ? "МАГНИТ\n{0}с" : "МАГНИТ\n×{1}", Mathf.CeilToInt(level.MagnetSeconds), level.MagnetInventory);
        if (doubleText != null) doubleText.SetText(level.DoubleSeconds > 0 ? "×2\n{0}с" : "×2\n×{1}", Mathf.CeilToInt(level.DoubleSeconds), level.DoubleInventory);
        if (shieldText != null) shieldText.SetText(level.ShieldSeconds > 0 ? "ЩИТ\n{0}с" : "ЩИТ\n×{1}", Mathf.CeilToInt(level.ShieldSeconds), level.ShieldInventory);
    }
}
