using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;
using System;

public enum RoundState
{
    WaitingForPlayers,
    BuyTime,
    Playing,
    RoundEnd
}

public class RoundManager : MonoBehaviourPunCallbacks
{
    public static RoundManager Instance;

    [Header("Round Manager")] 
    [SerializeField] private PlayerSpawnManager playerSpawnManager;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private BombManager bombManager; // ✅ НОВИЙ КОМПОНЕНТ
    
    [Header("Settings")]
    public float timeWaitingForPlayers = 5f;
    public int roundsToWin = 10;
    public float buyTimeDuration = 15f;
    public float roundTimeDuration = 120f;
    public float endRoundDelay = 5f;
    
    private double roundStartTime;
    private RoundState currentState;
    
    public void Initialize()
    {
        PhotonNetwork.IsMessageQueueRunning = true;
        playerSpawnManager.Initialize(this);
        
        if (Instance == null) 
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
        
        // ✅ Ініціалізуємо BombManager
        if (bombManager != null)
        {
            bombManager.Initialize();
        }
        
        StartRound();
    }

    private void StartRound()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom.CustomProperties != null)
        {
            Hashtable props = PhotonNetwork.CurrentRoom.CustomProperties;

            // Стан раунду
            if (props.ContainsKey("state"))
            {
                currentState = (RoundState)(int)props["state"];
            }

            // Час старту раунду
            if (props.ContainsKey("startTime"))
            {
                roundStartTime = (double)props["startTime"];
            }

            // Рахунок
            if (props.ContainsKey("tWins") && props.ContainsKey("ctWins"))
            {
                scoreManager.RedWins = (int)props["tWins"];
                scoreManager.BlueWins = (int)props["ctWins"];
                UpdateScoreUI_RPC(scoreManager.RedWins, scoreManager.BlueWins);
            }
        }
        
        // Ініціалізуємо дані для всіх гравців, що вже в кімнаті
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.ContainsKey("team"))
            {
                int teamIndex = (int)player.CustomProperties["team"];
                playerSpawnManager.PlayerTeams[player.ActorNumber] = teamIndex;
                playerSpawnManager.PlayerAlive[player.ActorNumber] = true;
            }
        }

        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(SetRoundState), RpcTarget.All, RoundState.WaitingForPlayers, PhotonNetwork.Time);
        }
    }

    private void Update()
    {
        uiManager.UpdateTimer(GetRemainingTime()); 
        playerSpawnManager.Update();
        if (!PhotonNetwork.IsMasterClient) return;

        switch (currentState)
        {
            case RoundState.WaitingForPlayers:
                if (GetRemainingTime() <= 0)
                    photonView.RPC(nameof(SetRoundState), RpcTarget.All, RoundState.BuyTime, PhotonNetwork.Time);
                break;
            case RoundState.BuyTime:
                if (GetRemainingTime() <= 0)
                    photonView.RPC(nameof(SetRoundState), RpcTarget.All, RoundState.Playing, PhotonNetwork.Time);
                break;
            case RoundState.Playing:
                if (PhotonNetwork.IsMasterClient)
                {
                    // ✅ ЗМІНЕНО: Перевіряємо команди тільки якщо бомба не закладена
                    if (bombManager.GetBombState() != BombState.Planted && 
                        bombManager.GetBombState() != BombState.Defusing)
                    {
                        playerSpawnManager.CheckTeamsAlive();
                    }
                }
                
                // ✅ ЗМІНЕНО: CT виграють якщо час вийшов і бомбу не закладено
                if (GetRemainingTime() <= 0)
                {
                    if (bombManager.GetBombState() == BombState.NotPlanted || 
                        bombManager.GetBombState() == BombState.Planting)
                    {
                        EndRound(1); // CT виграють по часу
                    }
                }
                break;
        }
    }
    
    [PunRPC]
    private void SetRoundState(RoundState newState, double startTime)
    {
        currentState = newState;
        roundStartTime = startTime;

        Debug.Log($"Змінюю стан раунду на: {newState}");

        if (newState == RoundState.WaitingForPlayers)
        {
            playerSpawnManager.ReviveAllPlayers();
            
            // ✅ Скидаємо бомбу
            if (bombManager != null)
            {
                bombManager.ResetBomb();
            }
            
            if (!playerSpawnManager.LocalPlayerSpawned && 
                PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("team") &&
                PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("hasPlayed"))
            {
                playerSpawnManager.SpawnPlayer(PhotonNetwork.LocalPlayer);
            }
        }
        else if (newState == RoundState.BuyTime)
        {
            playerSpawnManager.ReviveDeadPlayersAndLockMovement();
            
            // ✅ Скидаємо і присвоюємо бомбу
            if (bombManager != null)
            {
                bombManager.ResetBomb();
            }

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player == PhotonNetwork.LocalPlayer)
                {
                    if (!playerSpawnManager.LocalPlayerSpawned && player.CustomProperties.ContainsKey("team"))
                    {
                        playerSpawnManager.SpawnPlayer(player);
                        LockMovementForLocalPlayer();
                    }
                }
            }
        }
        else if (newState == RoundState.Playing)
        {
            UnlockMovementForAll();
        }

        SetRoomProperties(currentState, startTime, scoreManager.RedWins, scoreManager.BlueWins);
    }
    
    private void LockMovementForLocalPlayer()
    {
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject playerObj in allPlayers)
        {
            PhotonView pv = playerObj.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                var controller = playerObj.GetComponent<FirstPersonController>();
                if (controller != null)
                {
                    controller.CanMove = false;
                    playerObj.GetComponent<WeaponManager>().CanFire = false;
                    Debug.Log($"Заблокував рух для локального гравця {playerObj.name}");
                }
            }
        }
    }
    
    private void UnlockMovementForAll()
    {
        Debug.Log("Розблоковую рух для всіх гравців");
        
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject playerObj in allPlayers)
        {
            playerObj.SetActive(true);
            var controller = playerObj.GetComponent<FirstPersonController>();
            if (controller != null)
            {
                controller.CanMove = true;
                playerObj.GetComponent<WeaponManager>().CanFire = true;
                Debug.Log($"Розблокував рух для {playerObj.name}");
            }
        }
    }
    
    public double GetRemainingTime()
    {
        double elapsed = PhotonNetwork.Time - roundStartTime;
        double duration = currentState switch
        {
            RoundState.WaitingForPlayers => timeWaitingForPlayers,
            RoundState.BuyTime => buyTimeDuration,
            RoundState.Playing => roundTimeDuration,
            _ => 0
        };
        return Math.Max(0, duration - elapsed);
    }

    public void EndRound(int winningTeam)
    {
        scoreManager.AddWin(winningTeam);

        GiveEndRoundMoney(winningTeam);
        SetRoomProperties(currentState, PhotonNetwork.Time, scoreManager.RedWins, scoreManager.BlueWins);
        photonView.RPC(nameof(UpdateScoreUI_RPC), RpcTarget.All, scoreManager.RedWins, scoreManager.BlueWins);
        photonView.RPC(nameof(SetRoundState), RpcTarget.All, RoundState.RoundEnd, PhotonNetwork.Time);

        if (PhotonNetwork.IsMasterClient)
            Invoke(nameof(StartNextRound), endRoundDelay);
    }
    
    private void GiveEndRoundMoney(int winningTeam)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        foreach (var player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey("team")) continue;

            int team = (int)player.CustomProperties["team"];
            int currentMoney = player.CustomProperties.ContainsKey("money") ? (int)player.CustomProperties["money"] : 0;

            int reward = 0;

            if (team == winningTeam) reward = 3250;
            else if (winningTeam == -1) reward = 1500;
            else reward = 1400;

            int newMoney = Mathf.Clamp(currentMoney + reward, 0, 16000);
            Hashtable props = new Hashtable { { "money", newMoney } };
            player.SetCustomProperties(props);
            
            Debug.Log($"Гравець {player.NickName} отримав {reward}$. Всього: {newMoney}$");
        }
    }
    
    public void RewardForKill(int actorId, string weaponType)
    {
        int reward = weaponType switch
        {
            "knife" => 1500,
            "rifle" => 300,
            "awp"   => 100,
            "smg"   => 600,
            _       => 300
        };

        Player killer = PhotonNetwork.CurrentRoom.GetPlayer(actorId);
        if (killer != null)
        {
            int currentMoney = killer.CustomProperties.ContainsKey("money") ? (int)killer.CustomProperties["money"] : 0;
            int newMoney = Mathf.Clamp(currentMoney + reward, 0, 16000);

            Hashtable props = new Hashtable { { "money", newMoney } };
            killer.SetCustomProperties(props);
        }
    }
    
    private void StartNextRound()
    {
        if (scoreManager.RedWins >= roundsToWin || scoreManager.BlueWins >= roundsToWin)
        {
            Debug.Log("Game Over!");
            return;
        }
        playerSpawnManager.LocalPlayerSpawned = false;
        photonView.RPC(nameof(SetRoundState), RpcTarget.All, RoundState.BuyTime, PhotonNetwork.Time);
    }

    [PunRPC]
    private void UpdateScoreUI_RPC(int tWins, int ctWins)
    {
        uiManager.UpdateScore(tWins, ctWins);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey("team"))
        {
            int teamIndex = (int)changedProps["team"];
            playerSpawnManager.PlayerTeams[targetPlayer.ActorNumber] = teamIndex;
            playerSpawnManager.PlayerAlive[targetPlayer.ActorNumber] = true;

            if (targetPlayer == PhotonNetwork.LocalPlayer)
            {
                if (currentState == RoundState.WaitingForPlayers)
                {
                    playerSpawnManager.SpawnPlayer(targetPlayer);
                    Hashtable props = new Hashtable { { "hasPlayed", true } };
                    targetPlayer.SetCustomProperties(props);
                }
                else if (currentState == RoundState.BuyTime)
                {
                    playerSpawnManager.SpawnPlayer(targetPlayer);
                    LockMovementForLocalPlayer();
                    Hashtable props = new Hashtable { { "hasPlayed", true } };
                    targetPlayer.SetCustomProperties(props);
                }
                else
                {
                    Debug.Log("Ти приєднався під час гри — чекатимеш до наступного раунду");
                    playerSpawnManager.LocalPlayerSpawned = false;
                }
            }
        }
    }

    public void TryJoinTeam(int requestedTeam)
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("team"))
        {
            Debug.Log("Ти вже в команді!");
            return;
        }

        int tCount = 0, ctCount = 0;
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey("team"))
            {
                int team = (int)p.CustomProperties["team"];
                if (team == 0) tCount++;
                else if (team == 1) ctCount++;
            }
        }

        int finalTeam = requestedTeam;

        if (requestedTeam == 0)
        {
            if (tCount > ctCount)
            {
                finalTeam = 1;
                Debug.Log("Команда T переповнена, ти потрапив в CT");
            }
        }
        else if (requestedTeam == 1)
        {
            if (ctCount > tCount)
            {
                finalTeam = 0;
                Debug.Log("Команда CT переповнена, ти потрапив в T");
            }
        }

        Hashtable props = new Hashtable { { "team", finalTeam } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        Debug.Log($"Приєднався до команди {(finalTeam == 0 ? "T (Red)" : "CT (Blue)")}");
        
        if (uiManager != null)
        {
            uiManager.HideTeamSelectionUI();
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"Гравець {newPlayer.NickName} приєднався до лобі");
        
        if (newPlayer == PhotonNetwork.LocalPlayer && !newPlayer.CustomProperties.ContainsKey("team"))
        {
            if (uiManager != null)
            {
                uiManager.ShowTeamSelectionUI();
            }
        }
    }
    
    public void GetTeamCounts(out int tCount, out int ctCount)
    {
        tCount = 0;
        ctCount = 0;
        
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey("team"))
            {
                int team = (int)p.CustomProperties["team"];
                if (team == 0) tCount++;
                else if (team == 1) ctCount++;
            }
        }
    }
    
    private void SetRoomProperties(RoundState state, double startTime, int tWins, int ctWins)
    {
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["state"] = (int)state;
        props["startTime"] = startTime;
        props["tWins"] = tWins;
        props["ctWins"] = ctWins;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }
    
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("state"))
        {
            currentState = (RoundState)(int)PhotonNetwork.CurrentRoom.CustomProperties["state"];
            roundStartTime = (double)PhotonNetwork.CurrentRoom.CustomProperties["startTime"];
        }

        if (propertiesThatChanged.ContainsKey("tWins"))
        {
            scoreManager.RedWins = (int)PhotonNetwork.CurrentRoom.CustomProperties["tWins"];
            scoreManager.BlueWins = (int)PhotonNetwork.CurrentRoom.CustomProperties["ctWins"];
            UpdateScoreUI_RPC(scoreManager.RedWins, scoreManager.BlueWins);
        }
    }
    
    [PunRPC]
    private void OnPlayerDeathNotification(int actorNumber)
    {
        Debug.Log($"Отримано повідомлення про смерть гравця {actorNumber}");
    }
}