using Photon.Pun;
using ExitGames.Client.Photon;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TeamSelector : MonoBehaviour
{
    [SerializeField] private RoundManager roundManager;
    [Header("UI Elements")]
    [SerializeField] private Button teamTButton;
    [SerializeField] private Button teamCTButton;
    [SerializeField] private TextMeshProUGUI tCountText;
    [SerializeField] private TextMeshProUGUI ctCountText;
    [SerializeField] private TextMeshProUGUI tWarningText;
    [SerializeField] private TextMeshProUGUI ctWarningText;
    private void Start()
    {
        // Підписуємось на кнопки
        teamTButton.onClick.AddListener(() => OnTeamButtonClicked(0));
        teamCTButton.onClick.AddListener(() => OnTeamButtonClicked(1));
        PhotonNetwork.IsMessageQueueRunning = false;
    }

    private void Update()
    {

        UpdateTeamCountsDisplay();
        
    }
    public void Show()
    {
        gameObject.SetActive(true);
        UpdateTeamCountsDisplay();
        
        // Блокуємо курсор
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        
        // Розблоковуємо курсор (для FPS)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnTeamButtonClicked(int teamIndex)
    {
        Hashtable props = new Hashtable();
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        roundManager.Initialize();
        roundManager.TryJoinTeam(teamIndex);
        Hide();
        PhotonNetwork.IsMessageQueueRunning = true;
    }

    private void UpdateTeamCountsDisplay()
    {
        if (roundManager == null) return;

        roundManager.GetTeamCounts(out int tCount, out int ctCount);

        // Оновлюємо текст
        tCountText.text = $"Гравців: {tCount}";
        ctCountText.text = $"Гравців: {ctCount}";

        // Показуємо попередження якщо команда переповнена
        if (tCount > ctCount)
        {
            tWarningText.gameObject.SetActive(true);
            tWarningText.text = "⚠️ Команда повна, ти потрапиш в CT";
            ctWarningText.gameObject.SetActive(false);
        }
        else if (ctCount > tCount)
        {
            ctWarningText.gameObject.SetActive(true);
            ctWarningText.text = "⚠️ Команда повна, ти потрапиш в T";
            tWarningText.gameObject.SetActive(false);
        }
        else
        {
            // Команди збалансовані
            tWarningText.gameObject.SetActive(false);
            ctWarningText.gameObject.SetActive(false);
        }
    }
    
}
