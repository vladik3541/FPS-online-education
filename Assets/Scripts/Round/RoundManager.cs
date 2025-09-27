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
        playerSpawnManager.Update();// Оновлюємо кеш гравців
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
                    playerSpawnManager.CheckTeamsAlive();
                }
                if (GetRemainingTime() <= 0)
                    EndRound(-1);
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
            if (!playerSpawnManager.LocalPlayerSpawned && PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("team"))
            {
                playerSpawnManager.SpawnPlayer(PhotonNetwork.LocalPlayer);
            }
        }
        else if (newState == RoundState.BuyTime)
        {
            playerSpawnManager.ReviveDeadPlayersAndLockMovement();

            // Спавнимо нових гравців, які під’єднались під час Playing
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                if (player == PhotonNetwork.LocalPlayer)
                {
                    if (!playerSpawnManager.LocalPlayerSpawned && player.CustomProperties.ContainsKey("team"))
                    {
                        playerSpawnManager.SpawnPlayer(player);
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

        GiveEndRoundMoney(winningTeam); // reward end round
        //Зберігаємо рахунок у властивостях кімнати
        SetRoomProperties(currentState, PhotonNetwork.Time, scoreManager.RedWins, scoreManager.BlueWins);

        //Оновлюємо UI для всіх
        photonView.RPC(nameof(UpdateScoreUI_RPC), RpcTarget.All, scoreManager.RedWins, scoreManager.BlueWins);

        // Переводимо у стан "кінець раунду"
        photonView.RPC(nameof(SetRoundState), RpcTarget.All, RoundState.RoundEnd, PhotonNetwork.Time);

        if (PhotonNetwork.IsMasterClient)
            Invoke(nameof(StartNextRound), endRoundDelay);
    }
    private void GiveEndRoundMoney(int winningTeam)
    {
        foreach (var player in PhotonNetwork.PlayerList)
        {
            int team = (int)player.CustomProperties["team"];
            int currentMoney = player.CustomProperties.ContainsKey("money") ? (int)player.CustomProperties["money"] : 0;

            int reward = 0;

            if (team == winningTeam) reward = 3250;       // перемога
            else if (winningTeam == -1) reward = 1500;    // нічия (час закінчився)
            else reward = 1400;                           // поразка

            int newMoney = Mathf.Clamp(currentMoney + reward, 0, 16000);
            Hashtable props = new Hashtable { { "money", newMoney } };
            player.SetCustomProperties(props);
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
                // Спавнимо тільки якщо можна
                if (currentState == RoundState.WaitingForPlayers || currentState == RoundState.BuyTime)
                {
                    playerSpawnManager.SpawnPlayer(targetPlayer);
                }
                else
                {
                    Debug.Log("Ти приєднався під час гри — чекатимеш до наступного раунду");
                    playerSpawnManager.LocalPlayerSpawned = false; // він ще не має гравця
                }
            }
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // Визначаємо команду
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

        int teamIndex = (tCount <= ctCount) ? 0 : 1;
        Hashtable props = new Hashtable { { "team", teamIndex } };
        newPlayer.SetCustomProperties(props);

        Debug.Log($"Гравець {newPlayer.NickName} приєднався до команди {teamIndex}");

        // Якщо це не майстер, він сам собі вирішить коли спавнитись у OnPlayerPropertiesUpdate
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
        // Цей RPC можна використовувати для оновлення UI, показу повідомлень про смерть тощо
        Debug.Log($"Отримано повідомлення про смерть гравця {actorNumber}");
    
        // Тут можна додати логіку для оновлення UI смерті
    }
}