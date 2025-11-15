using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class BombUI : MonoBehaviour
{
    [Header("Bomb UI Elements")]
    [SerializeField] private GameObject bombTimerPanel;
    [SerializeField] private TextMeshProUGUI bombTimerText;
    [SerializeField] private Image bombTimerFill;
    [SerializeField] private TextMeshProUGUI bombStatusText;
    
    [Header("Progress Bars")]
    [SerializeField] private GameObject plantProgressBar;
    [SerializeField] private Image plantProgressFill;
    [SerializeField] private TextMeshProUGUI plantProgressText;
    
    [SerializeField] private GameObject defuseProgressBar;
    [SerializeField] private Image defuseProgressFill;
    [SerializeField] private TextMeshProUGUI defuseProgressText;
    
    [Header("Bomb Icon")]
    [SerializeField] private GameObject bombCarrierIcon;
    [SerializeField] private TextMeshProUGUI bombCarrierText;
    
    [Header("Notifications")]
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private float notificationDuration = 3f;
    
    private BombManager bombManager;
    private float notificationTimer = 0f;

    void Start()
    {
        bombManager = BombManager.Instance;
        
        if (bombTimerPanel != null)
            bombTimerPanel.SetActive(false);
        
        if (plantProgressBar != null)
            plantProgressBar.SetActive(false);
        
        if (defuseProgressBar != null)
            defuseProgressBar.SetActive(false);
        
        if (bombCarrierIcon != null)
            bombCarrierIcon.SetActive(false);
    }

    void Update()
    {
        if (bombManager == null) return;

        UpdateBombTimer();
        UpdateProgressBars();
        UpdateBombCarrierIcon();
        UpdateNotification();
    }

    private void UpdateBombTimer()
    {
        BombState state = bombManager.GetBombState();
        
        if (state == BombState.Planted)
        {
            if (bombTimerPanel != null)
            {
                bombTimerPanel.SetActive(true);
                
                double timeRemaining = bombManager.GetBombTimeRemaining();
                
                if (bombTimerText != null)
                {
                    bombTimerText.text = $"{timeRemaining:F1}s";
                    
                    // Міняємо колір залежно від часу
                    if (timeRemaining <= 10)
                        bombTimerText.color = Color.red;
                    else if (timeRemaining <= 20)
                        bombTimerText.color = Color.yellow;
                    else
                        bombTimerText.color = Color.white;
                }
                
                if (bombTimerFill != null)
                {
                    float fillAmount = (float)(timeRemaining / bombManager.explosionTime);
                    bombTimerFill.fillAmount = fillAmount;
                    
                    // Міняємо колір прогрес бару
                    if (timeRemaining <= 10)
                        bombTimerFill.color = Color.red;
                    else if (timeRemaining <= 20)
                        bombTimerFill.color = Color.yellow;
                    else
                        bombTimerFill.color = Color.green;
                }
            }
        }
        else
        {
            if (bombTimerPanel != null)
                bombTimerPanel.SetActive(false);
        }
        
        // Оновлюємо статус бомби
        if (bombStatusText != null)
        {
            bombStatusText.text = state switch
            {
                BombState.NotPlanted => "",
                BombState.Planting => "ЗАКЛАДАЮТЬ БОМБУ...",
                BombState.Planted => "БОМБА ЗАКЛАДЕНА!",
                BombState.Defusing => "ЗНЕШКОДЖУЮТЬ БОМБУ...",
                BombState.Defused => "БОМБУ ЗНЕШКОДЖЕНО",
                BombState.Exploded => "ВИБУХ!",
                _ => ""
            };
            
            bombStatusText.color = state switch
            {
                BombState.Planting => Color.yellow,
                BombState.Planted => Color.red,
                BombState.Defusing => Color.cyan,
                BombState.Defused => Color.green,
                BombState.Exploded => Color.red,
                _ => Color.white
            };
        }
    }

    private void UpdateProgressBars()
    {
        BombState state = bombManager.GetBombState();
        
        // Прогрес закладання
        if (state == BombState.Planting)
        {
            if (plantProgressBar != null)
            {
                plantProgressBar.SetActive(true);
                
                if (plantProgressFill != null)
                {
                    plantProgressFill.fillAmount = bombManager.GetPlantProgress();
                }
                
                if (plantProgressText != null)
                {
                    plantProgressText.text = "Закладаю бомбу... (Утримуй E)";
                }
            }
        }
        else
        {
            if (plantProgressBar != null)
                plantProgressBar.SetActive(false);
        }
        
        // Прогрес знешкодження
        if (state == BombState.Defusing)
        {
            if (defuseProgressBar != null)
            {
                defuseProgressBar.SetActive(true);
                
                if (defuseProgressFill != null)
                {
                    defuseProgressFill.fillAmount = bombManager.GetDefuseProgress();
                }
                
                if (defuseProgressText != null)
                {
                    defuseProgressText.text = "Знешкоджую бомбу... (Утримуй E)";
                }
            }
        }
        else
        {
            if (defuseProgressBar != null)
                defuseProgressBar.SetActive(false);
        }
    }

    private void UpdateBombCarrierIcon()
    {
        if (bombCarrierIcon == null) return;
        
        // Показуємо іконку якщо локальний гравець має бомбу
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("hasBomb"))
        {
            bool hasBomb = (bool)PhotonNetwork.LocalPlayer.CustomProperties["hasBomb"];
            bombCarrierIcon.SetActive(hasBomb);
            
            if (hasBomb && bombCarrierText != null)
            {
                bombCarrierText.text = "У ТЕБЕ БОМБА!\nНатисни E щоб закласти";
            }
        }
        else
        {
            bombCarrierIcon.SetActive(false);
        }
    }

    private void UpdateNotification()
    {
        if (notificationTimer > 0)
        {
            notificationTimer -= Time.deltaTime;
            
            if (notificationTimer <= 0 && notificationText != null)
            {
                notificationText.text = "";
            }
        }
    }

    public void ShowNotification(string message)
    {
        if (notificationText != null)
        {
            notificationText.text = message;
            notificationTimer = notificationDuration;
        }
    }

    // Викликається з BombManager через RPC або події
    public void OnBombPlanted()
    {
        ShowNotification("БОМБУ ЗАКЛАДЕНО!");
    }

    public void OnBombDefused()
    {
        ShowNotification("БОМБУ ЗНЕШКОДЖЕНО!");
    }

    public void OnBombExploded()
    {
        ShowNotification("ВИБУХ!");
    }

    public void OnPlayerEnteredBombSite(string siteName)
    {
        ShowNotification($"Увійшов на точку {siteName}");
    }

    public void OnPlayerLeftBombSite(string siteName)
    {
        ShowNotification($"Покинув точку {siteName}");
    }
}