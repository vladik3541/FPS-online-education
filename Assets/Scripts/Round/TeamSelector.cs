using Photon.Pun;
using ExitGames.Client.Photon;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TeamSelector : MonoBehaviour
{
    [SerializeField] private RoundManager roundManager;
    private void Start()
    {
        PhotonNetwork.IsMessageQueueRunning = false;
    }

    public void SelectTeam(int teamIndex) // 0 = Red, 1 = Blue
    {
        Hashtable props = new Hashtable();
        props["team"] = teamIndex;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        PhotonNetwork.IsMessageQueueRunning = true;
        gameObject.SetActive(false);
        roundManager.Initialize();
    }
    
}
