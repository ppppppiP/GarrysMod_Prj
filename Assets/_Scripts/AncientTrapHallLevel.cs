using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

public sealed class AncientTrapHallLevel : MonoBehaviour
{
    public enum GameState { Countdown, Playing, Paused, Revive, Results }

    private const string TotalCoinsKey = "GarrysMod.TotalCoins";
    private const string BestTimeKey = "GarrysMod.AncientTrapHall.BestTime";
    private const string MagnetInventoryKey = "GarrysMod.Inventory.Magnet";
    private const string DoubleInventoryKey = "GarrysMod.Inventory.DoubleCoins";
    private const string ShieldInventoryKey = "GarrysMod.Inventory.Shield";

    [Header("Редактируемые ссылки сцены")]
    [InspectorName("Игрок — готовый префаб")] public PlayerController player;
    [InspectorName("Точка старта и воскрешения")] public Transform respawnPoint;
    [InspectorName("Ловушки в сцене")] public AncientTrapCycle[] traps;
    [InspectorName("Точки появления предметов")] public Transform[] pickupPoints;
    [InspectorName("Префаб монеты")] public AncientHallPickup coinPrefab;
    [InspectorName("Префаб магнита")] public AncientHallPickup magnetPrefab;
    [InspectorName("Префаб удвоения монет")] public AncientHallPickup doublePrefab;
    [InspectorName("Префаб щита")] public AncientHallPickup shieldPrefab;

    [Header("Баланс")]
    [InspectorName("Максимум здоровья"), Range(1, 6)] public int maximumHealth = 3;
    [InspectorName("Неуязвимость после урона"), Min(0f)] public float hitInvulnerability = 2f;
    [InspectorName("Неуязвимость после щита"), Min(0f)] public float shieldBreakInvulnerability = 3f;
    [InspectorName("Неуязвимость после воскрешения"), Min(0f)] public float reviveInvulnerability = 3f;
    [InspectorName("Интервал появления монет"), Min(0.5f)] public float coinSpawnInterval = 4f;
    [InspectorName("Интервал появления бонусов"), Min(5f)] public float bonusSpawnInterval = 28f;
    [InspectorName("Длительность магнита"), Min(1f)] public float magnetDuration = 10f;
    [InspectorName("Длительность удвоения"), Min(1f)] public float doubleDuration = 12f;
    [InspectorName("Длительность щита"), Min(1f)] public float shieldDuration = 14f;

    [Header("Рост сложности")]
    [InspectorName("Время до предельной сложности"), Tooltip("По ТЗ — около 25 минут."), Min(60f)] public float maximumDifficultyTime = 1500f;
    [InspectorName("Начальная скорость ловушек"), Range(0.2f, 3f)] public float startingTrapSpeed = 0.8f;
    [InspectorName("Предельная скорость ловушек"), Range(1f, 8f)] public float maximumTrapSpeed = 4.5f;
    [InspectorName("Ловушек на старте"), Min(1)] public int startingTrapCount = 2;
    [InspectorName("Максимум активных ловушек"), Min(1)] public int maximumActiveTraps = 12;
    [InspectorName("Смена набора ловушек (сек.)"), Tooltip("Даёт увидеть все типы ловушек уже в начале игры."), Min(2f)] public float trapRotationInterval = 5f;

    private readonly List<AncientHallPickup> activePickups = new List<AncientHallPickup>();
    private Renderer[] playerRenderers;
    private GameState state;
    private int health;
    private int runCoins;
    private int paidRevives;
    private bool adReviveUsed;
    private float activeTime;
    private float countdownRemaining;
    private float invulnerabilityRemaining;
    private float magnetRemaining;
    private float doubleRemaining;
    private float shieldRemaining;
    private float coinSpawnRemaining;
    private float bonusSpawnRemaining;
    private float difficultyUpdateRemaining;
    private bool playerVisible = true;

    public GameState State => state;
    public bool SimulationRunning => state == GameState.Playing;
    public int Health => health;
    public int MaximumHealth => maximumHealth;
    public int RunCoins => runCoins;
    public int TotalCoins => GetTotalCoins();
    public int ActiveSeconds => Mathf.FloorToInt(activeTime);
    public int BestSeconds => Mathf.FloorToInt(PlayerPrefs.GetFloat(BestTimeKey, 0f));
    public int CountdownNumber => Mathf.Clamp(Mathf.CeilToInt(countdownRemaining), 1, 3);
    public float MagnetSeconds => magnetRemaining;
    public float DoubleSeconds => doubleRemaining;
    public float ShieldSeconds => shieldRemaining;
    public int MagnetInventory => PlayerPrefs.GetInt(MagnetInventoryKey, 1);
    public int DoubleInventory => PlayerPrefs.GetInt(DoubleInventoryKey, 1);
    public int ShieldInventory => PlayerPrefs.GetInt(ShieldInventoryKey, 1);
    public int ReviveCost => 50 * (1 << Mathf.Min(paidRevives, 20));
    public bool CanAdRevive => !adReviveUsed;

    private void Awake()
    {
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Application.targetFrameRate = -1;
        QualitySettings.vSyncCount = 0;
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (traps == null || traps.Length == 0) traps = FindObjectsByType<AncientTrapCycle>(FindObjectsSortMode.None);
        playerRenderers = player != null ? player.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
        foreach (AncientTrapCycle trap in traps) if (trap != null) trap.Bind(this);
        BeginRun();
    }

    private void Update()
    {
        if (state == GameState.Countdown)
        {
            countdownRemaining -= Time.unscaledDeltaTime;
            if (countdownRemaining <= 0f) ResumePlaying();
            return;
        }
        if (state != GameState.Playing) return;

        float delta = Time.deltaTime;
        activeTime += delta;
        invulnerabilityRemaining = Mathf.Max(0f, invulnerabilityRemaining - delta);
        magnetRemaining = Mathf.Max(0f, magnetRemaining - delta);
        doubleRemaining = Mathf.Max(0f, doubleRemaining - delta);
        shieldRemaining = Mathf.Max(0f, shieldRemaining - delta);
        UpdateBlink();
        difficultyUpdateRemaining -= delta;
        if (difficultyUpdateRemaining <= 0f)
        {
            difficultyUpdateRemaining = 0.1f;
            UpdateDifficulty();
        }
        UpdatePickups(delta);
    }

    private void BeginRun()
    {
        Time.timeScale = 1f;
        health = maximumHealth;
        runCoins = 0;
        activeTime = 0f;
        paidRevives = 0;
        adReviveUsed = false;
        magnetRemaining = doubleRemaining = shieldRemaining = invulnerabilityRemaining = 0f;
        coinSpawnRemaining = 1.5f;
        bonusSpawnRemaining = bonusSpawnInterval;
        difficultyUpdateRemaining = 0f;
        ResetPlayer();
        foreach (AncientTrapCycle trap in traps) if (trap != null) trap.ResetCycle();
        StartCountdown();
    }

    private void UpdateDifficulty()
    {
        float difficulty = Mathf.Clamp01(activeTime / Mathf.Max(60f, maximumDifficultyTime));
        difficulty = difficulty * difficulty * (3f - 2f * difficulty);
        float speed = Mathf.Lerp(startingTrapSpeed, maximumTrapSpeed, difficulty);
        int activeCount = Mathf.Clamp(Mathf.FloorToInt(Mathf.Lerp(startingTrapCount, maximumActiveTraps, difficulty)), 1, traps.Length);
        int rotationOffset = Mathf.FloorToInt(activeTime / Mathf.Max(2f, trapRotationInterval)) % traps.Length;
        for (int i = 0; i < traps.Length; i++)
        {
            int wrappedIndex = (i - rotationOffset + traps.Length) % traps.Length;
            if (traps[i] != null) traps[i].SetRuntimeState(wrappedIndex < activeCount, speed);
        }
    }

    private void UpdatePickups(float delta)
    {
        coinSpawnRemaining -= delta;
        bonusSpawnRemaining -= delta;
        if (coinSpawnRemaining <= 0f)
        {
            SpawnPickup(coinPrefab);
            coinSpawnRemaining = coinSpawnInterval;
        }
        if (bonusSpawnRemaining <= 0f)
        {
            AncientHallPickup choice = Random.value < 0.34f ? magnetPrefab : Random.value < 0.5f ? doublePrefab : shieldPrefab;
            SpawnPickup(choice);
            bonusSpawnRemaining = bonusSpawnInterval;
        }
        if (magnetRemaining <= 0f || player == null) return;
        for (int i = activePickups.Count - 1; i >= 0; i--)
        {
            AncientHallPickup pickup = activePickups[i];
            if (pickup == null) { activePickups.RemoveAt(i); continue; }
            if (pickup.Kind == AncientHallPickup.PickupKind.Coin)
                pickup.transform.position = Vector3.MoveTowards(pickup.transform.position, player.transform.position + Vector3.up, 9f * delta);
        }
    }

    private void SpawnPickup(AncientHallPickup prefab)
    {
        if (prefab == null || pickupPoints == null || pickupPoints.Length == 0) return;
        for (int i = activePickups.Count - 1; i >= 0; i--)
            if (activePickups[i] == null) activePickups.RemoveAt(i);
        Transform point = pickupPoints[Random.Range(0, pickupPoints.Length)];
        AncientHallPickup pickup = Instantiate(prefab, point.position, Quaternion.identity, transform);
        pickup.Bind(this);
        activePickups.Add(pickup);
    }

    public void Collect(AncientHallPickup pickup)
    {
        if (pickup == null || state != GameState.Playing) return;
        if (pickup.Kind == AncientHallPickup.PickupKind.Coin)
        {
            int amount = doubleRemaining > 0f ? 2 : 1;
            runCoins += amount;
            SetTotalCoins(GetTotalCoins() + amount);
        }
        else if (pickup.Kind == AncientHallPickup.PickupKind.Magnet) magnetRemaining += magnetDuration;
        else if (pickup.Kind == AncientHallPickup.PickupKind.DoubleCoins) doubleRemaining += doubleDuration;
        else if (pickup.Kind == AncientHallPickup.PickupKind.Shield) shieldRemaining += shieldDuration;
        activePickups.Remove(pickup);
        Destroy(pickup.gameObject);
    }

    public void TakeDamage()
    {
        if (state != GameState.Playing || invulnerabilityRemaining > 0f) return;
        if (shieldRemaining > 0f)
        {
            shieldRemaining = 0f;
            invulnerabilityRemaining = shieldBreakInvulnerability;
            return;
        }
        health--;
        invulnerabilityRemaining = hitInvulnerability;
        if (health <= 0) EnterRevive();
    }

    private void UpdateBlink()
    {
        bool visible = invulnerabilityRemaining <= 0f || ((int)(Time.unscaledTime * 10f) & 1) == 0;
        SetPlayerVisible(visible);
    }

    private void EnterRevive()
    {
        state = GameState.Revive;
        Time.timeScale = 0f;
        SetPlayerEnabled(false);
        SetPlayerVisible(true);
        magnetRemaining = doubleRemaining = shieldRemaining = 0f;
    }

    private void Revive(bool byAd)
    {
        if (byAd) adReviveUsed = true; else paidRevives++;
        health = maximumHealth;
        invulnerabilityRemaining = reviveInvulnerability;
        ResetPlayer();
        StartCountdown();
    }

    private void ResetPlayer()
    {
        if (player == null) return;
        CharacterController controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.transform.SetPositionAndRotation(respawnPoint != null ? respawnPoint.position : Vector3.up * 1.2f, Quaternion.identity);
        if (controller != null) controller.enabled = true;
        player.ResetMotion();
        SetPlayerVisible(true);
    }

    private void StartCountdown()
    {
        state = GameState.Countdown;
        countdownRemaining = 3f;
        Time.timeScale = 0f;
        SetPlayerEnabled(false);
    }

    private void ResumePlaying()
    {
        Time.timeScale = 1f;
        state = GameState.Playing;
        SetPlayerEnabled(true);
    }

    private void SetPlayerEnabled(bool value) { if (player != null) player.enabled = value; }
    private void SetPlayerVisible(bool value)
    {
        if (playerVisible == value) return;
        playerVisible = value;
        foreach (Renderer target in playerRenderers) if (target != null) target.enabled = value;
    }

    public void TogglePause()
    {
        if (state == GameState.Playing)
        {
            state = GameState.Paused;
            Time.timeScale = 0f;
            SetPlayerEnabled(false);
        }
        else if (state == GameState.Paused) StartCountdown();
    }

    public void ActivateMagnet() { ActivateInventory(MagnetInventoryKey, AncientHallPickup.PickupKind.Magnet); }
    public void ActivateDoubleCoins() { ActivateInventory(DoubleInventoryKey, AncientHallPickup.PickupKind.DoubleCoins); }
    public void ActivateShield() { ActivateInventory(ShieldInventoryKey, AncientHallPickup.PickupKind.Shield); }

    private void ActivateInventory(string key, AncientHallPickup.PickupKind kind)
    {
        if (state != GameState.Playing) return;
        int amount = PlayerPrefs.GetInt(key, 1);
        if (amount <= 0) return;
        PlayerPrefs.SetInt(key, amount - 1);
        if (kind == AncientHallPickup.PickupKind.Magnet) magnetRemaining += magnetDuration;
        else if (kind == AncientHallPickup.PickupKind.DoubleCoins) doubleRemaining += doubleDuration;
        else shieldRemaining += shieldDuration;
    }

    public void RequestAdRevive() { if (state == GameState.Revive && !adReviveUsed) Revive(true); }
    public void RequestPaidRevive()
    {
        if (state != GameState.Revive || GetTotalCoins() < ReviveCost) return;
        SetTotalCoins(GetTotalCoins() - ReviveCost);
        Revive(false);
    }

    public void FinishRun()
    {
        if (state != GameState.Revive) return;
        state = GameState.Results;
        PlayerPrefs.SetFloat(BestTimeKey, Mathf.Max(PlayerPrefs.GetFloat(BestTimeKey, 0f), activeTime));
        SaveSharedWallet();
        Debug.Log("AncientTrapHallAttempt: activeSeconds=" + ActiveSeconds + ", coins=" + runCoins);
    }

    public void RestartRun() { if (state == GameState.Results) BeginRun(); }
    public void ReturnToCamp() { Time.timeScale = 1f; SceneManager.LoadScene("00 - MENU"); }

    private void OnApplicationFocus(bool focus) { if (!focus && state == GameState.Playing) TogglePause(); }
    private void OnApplicationPause(bool paused) { if (paused) { SaveSharedWallet(); if (state == GameState.Playing) TogglePause(); } }
    private void OnDestroy() { Time.timeScale = 1f; SaveSharedWallet(); }

    private static int GetTotalCoins()
    {
        if (YandexGame.Instance != null && YandexGame.savesData != null) return YandexGame.savesData.money;
        return PlayerPrefs.GetInt(TotalCoinsKey, YandexGame.savesData != null ? YandexGame.savesData.money : 0);
    }

    private static void SetTotalCoins(int value)
    {
        value = Mathf.Max(0, value);
        PlayerPrefs.SetInt(TotalCoinsKey, value);
        if (YandexGame.savesData != null) YandexGame.savesData.money = value;
    }

    private static void SaveSharedWallet()
    {
        PlayerPrefs.Save();
        if (YandexGame.Instance != null) YandexGame.SaveProgress();
    }
}
