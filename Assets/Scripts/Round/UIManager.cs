using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [Header("Team Selection")]
    [SerializeField] private TeamSelector teamSelectionUI;
    public void UpdateTimer(double time)
    {
        int minutes = Mathf.FloorToInt((float)time / 60);
        int seconds = Mathf.FloorToInt((float)time % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    public void UpdateScore(int red, int blue)
    {
        scoreText.text = $"T: {red} | CT: {blue}";
    }

    public void OnRoundStateChanged(RoundState state)
    {
        if (state == RoundState.WaitingForPlayers)
            Debug.Log("UI: Очікуємо гравців");
    }
    // ✅ НОВИЙ МЕТОД: Показати UI вибору команди
    public void ShowTeamSelectionUI()
    {
        if (teamSelectionUI != null)
        {
            teamSelectionUI.Show();
        }
    }

    // ✅ НОВИЙ МЕТОД: Сховати UI вибору команди
    public void HideTeamSelectionUI()
    {
        if (teamSelectionUI != null)
        {
            teamSelectionUI.Hide();
        }
    }
}
