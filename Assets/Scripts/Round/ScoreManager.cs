using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField]private UIManager _uiManager;
    
    private int redWins;
    private int blueWins;

    public void Initialize(UIManager uiManager)
    {
        _uiManager = uiManager;
    }
    public int RedWins
    {
        get { return redWins; }
        set { redWins = value; }
    }

    public int BlueWins
    {
        get { return blueWins; }
        set { blueWins = value; }
    }
    
    public void AddWin(int team)
    {
        if (team == 0) redWins++;
        else blueWins++;
        _uiManager.UpdateScore(redWins, blueWins);
    }
    
}
