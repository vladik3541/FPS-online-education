using UnityEngine;
using Photon.Pun;

/// <summary>
/// Контролер бомби для гравця
/// Додайте цей компонент до префабу гравця
/// </summary>
public class PlayerBombController : MonoBehaviourPunCallbacks
{
    [Header("Key Bindings")]
    [Tooltip("Клавіша для закладання/знешкодження бомби")]
    public KeyCode interactKey = KeyCode.E;
    
    [Tooltip("Клавіша для підняття бомби з землі")]
    public KeyCode pickupKey = KeyCode.F;
    
    [Header("UI Hints (опціонально)")]
    [SerializeField] private GameObject plantHintUI;
    [SerializeField] private GameObject defuseHintUI;
    [SerializeField] private GameObject pickupHintUI;
    
    private bool isPlanting = false;
    private bool isDefusing = false;
    private BombManager bombManager;

    void Start()
    {
        bombManager = BombManager.Instance;
        
        // Ховаємо підказки
        if (plantHintUI != null) plantHintUI.SetActive(false);
        if (defuseHintUI != null) defuseHintUI.SetActive(false);
        if (pickupHintUI != null) pickupHintUI.SetActive(false);
    }

    void Update()
    {
        if (!photonView.IsMine) return;
        if (bombManager == null) return;

        UpdateHints();
        HandleBombInteraction();
    }

    private void HandleBombInteraction()
    {
        BombState currentState = bombManager.GetBombState();

        // === ЗАКЛАДАННЯ БОМБИ ===
        if (currentState == BombState.NotPlanted || currentState == BombState.Planting)
        {
            // Починаємо закладання
            if (Input.GetKeyDown(interactKey) && !isPlanting)
            {
                Debug.Log("🔴 Спроба почати закладання бомби...");
                bombManager.TryPlantBomb();
                isPlanting = true;
            }

            // Оновлюємо прогрес закладання (КОЖЕН КАДР!)
            if (isPlanting && bombManager.GetBombState() == BombState.Planting)
            {
                bombManager.UpdatePlantingProgress();
            }

            // Скасовуємо закладання
            if (Input.GetKeyUp(interactKey) && isPlanting)
            {
                Debug.Log("🟡 Скасування закладання бомби");
                bombManager.CancelPlanting();
                isPlanting = false;
            }
            
            // Скидаємо прапорець якщо закладання завершилось
            if (isPlanting && bombManager.GetBombState() == BombState.Planted)
            {
                Debug.Log("🟢 Бомбу успішно закладено!");
                isPlanting = false;
            }
        }

        // === ЗНЕШКОДЖЕННЯ БОМБИ ===
        if (currentState == BombState.Planted || currentState == BombState.Defusing)
        {
            // Починаємо знешкодження
            if (Input.GetKeyDown(interactKey) && !isDefusing)
            {
                Debug.Log("🔵 Спроба почати знешкодження бомби...");
                bombManager.TryDefuseBomb();
                isDefusing = true;
            }

            // Оновлюємо прогрес знешкодження (КОЖЕН КАДР!)
            if (isDefusing && bombManager.GetBombState() == BombState.Defusing)
            {
                bombManager.UpdateDefusingProgress();
            }

            // Скасовуємо знешкодження
            if (Input.GetKeyUp(interactKey) && isDefusing)
            {
                Debug.Log("🟡 Скасування знешкодження бомби");
                bombManager.CancelDefusing();
                isDefusing = false;
            }
            
            // Скидаємо прапорець якщо знешкодження завершилось
            if (isDefusing && bombManager.GetBombState() == BombState.Defused)
            {
                Debug.Log("🟢 Бомбу успішно знешкоджено!");
                isDefusing = false;
            }
        }

        // === ПІДНЯТТЯ БОМБИ З ЗЕМЛІ ===
        if (Input.GetKeyDown(pickupKey))
        {
            bombManager.TryPickupBomb();
        }
    }

    private void UpdateHints()
    {
        if (!photonView.IsMine) return;

        // Перевіряємо чи є бомба у гравця
        bool hasBomb = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("hasBomb") 
            && (bool)PhotonNetwork.LocalPlayer.CustomProperties["hasBomb"];

        // Перевіряємо команду
        bool isTerrorist = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("team") 
            && (int)PhotonNetwork.LocalPlayer.CustomProperties["team"] == 0;
        
        bool isCT = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("team") 
            && (int)PhotonNetwork.LocalPlayer.CustomProperties["team"] == 1;

        BombState state = bombManager.GetBombState();

        // Підказка для закладання
        if (plantHintUI != null)
        {
            bool canPlant = isTerrorist && hasBomb && state == BombState.NotPlanted && IsInBombSite();
            plantHintUI.SetActive(canPlant);
        }

        // Підказка для знешкодження
        if (defuseHintUI != null)
        {
            bool canDefuse = isCT && state == BombState.Planted && IsNearBomb();
            defuseHintUI.SetActive(canDefuse);
        }

        // Підказка для підняття бомби
        if (pickupHintUI != null)
        {
            bool canPickup = isTerrorist && !hasBomb && state == BombState.NotPlanted && IsNearBomb();
            pickupHintUI.SetActive(canPickup);
        }
    }

    private bool IsInBombSite()
    {
        // Перевіряємо чи гравець в зоні bomb site
        BombSite[] bombSites = FindObjectsOfType<BombSite>();
        foreach (BombSite site in bombSites)
        {
            float distance = Vector3.Distance(transform.position, site.transform.position);
            if (distance <= bombManager.bombSiteRadius)
            {
                return true;
            }
        }
        return false;
    }

    private bool IsNearBomb()
    {
        // Перевіряємо чи гравець біля бомби
        Vector3 bombPosition = bombManager.GetBombPosition();
        float distance = Vector3.Distance(transform.position, bombPosition);
        return distance <= 2f;
    }

    /// <summary>
    /// Викликається коли гравець помирає
    /// Додайте цей виклик до вашої системи здоров'я
    /// </summary>
    public void OnDeath()
    {
        if (!photonView.IsMine) return;

        // Перевіряємо чи у гравця була бомба
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("hasBomb"))
        {
            bool hasBomb = (bool)PhotonNetwork.LocalPlayer.CustomProperties["hasBomb"];
            if (hasBomb && bombManager != null)
            {
                bombManager.OnBombCarrierDeath(transform.position);
            }
        }

        // Скидаємо стани
        isPlanting = false;
        isDefusing = false;
    }

    /// <summary>
    /// Викликається коли гравець отримує урон під час закладання/знешкодження
    /// </summary>
    public void OnTakeDamage()
    {
        if (!photonView.IsMine) return;

        // Скасовуємо закладання якщо отримали урон
        if (isPlanting)
        {
            bombManager.CancelPlanting();
            isPlanting = false;
        }

        // Скасовуємо знешкодження якщо отримали урон
        if (isDefusing)
        {
            bombManager.CancelDefusing();
            isDefusing = false;
        }
    }

    // === DEBUG ===
    
    void OnGUI()
    {
        if (!photonView.IsMine) return;
        if (!Debug.isDebugBuild) return;

        GUILayout.BeginArea(new Rect(10, 300, 300, 200));
        GUILayout.Label("=== BOMB DEBUG ===");
        
        bool hasBomb = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("hasBomb") 
            && (bool)PhotonNetwork.LocalPlayer.CustomProperties["hasBomb"];
        
        GUILayout.Label($"Has Bomb: {hasBomb}");
        GUILayout.Label($"Is Planting: {isPlanting}");
        GUILayout.Label($"Is Defusing: {isDefusing}");
        GUILayout.Label($"Bomb State: {bombManager.GetBombState()}");
        GUILayout.Label($"In Bomb Site: {IsInBombSite()}");
        GUILayout.Label($"Near Bomb: {IsNearBomb()}");
        
        if (bombManager.GetBombState() == BombState.Planted)
        {
            GUILayout.Label($"Time to Explode: {bombManager.GetBombTimeRemaining():F1}s");
        }
        
        GUILayout.EndArea();
    }
}