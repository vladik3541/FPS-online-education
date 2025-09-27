using System;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PlayerSpawnManager : MonoBehaviourPunCallbacks
{
    public static PlayerSpawnManager Instance;
    
    [Header("Prefabs & Spawns")]
    public string prefabRed = "PhotonPrefabs/PlayerRed";
    public string prefabBlue = "PhotonPrefabs/PlayerBlue";
    public Transform spawnRed;
    public Transform spawnBlue;

    private Dictionary<int, int> playerTeams = new Dictionary<int, int>();
    private Dictionary<int, bool> playerAlive = new Dictionary<int, bool>();
    private Dictionary<int, GameObject> playerObjects = new Dictionary<int, GameObject>(); // Кеш об'єктів гравців
    
    private bool localPlayerSpawned = false;
    private RoundManager _roundManager;
    public Dictionary<int, int> PlayerTeams => playerTeams;
    public Dictionary<int, bool> PlayerAlive => playerAlive;
    public Dictionary<int, GameObject> PlayerObjects => playerObjects;

    public bool LocalPlayerSpawned
    {
        get => localPlayerSpawned;
        set {localPlayerSpawned = value;}
    }

    public void Initialize(RoundManager roundManager)
    {
        Instance = this;
        _roundManager = roundManager;
    }

    public void Update()
    {
        RefreshPlayerCache(); // Оновлюємо кеш гравців
    }

    public void SpawnPlayer(Player player)
    {
        if (!player.CustomProperties.ContainsKey("team"))
        {
            Debug.LogWarning($"Гравцю {player.NickName} не призначена команда!");
            return;
        }
        
        int teamIndex = (int)player.CustomProperties["team"];
        Transform spawnPoint = (teamIndex == 0) ? spawnRed : spawnBlue; // Виправлено: 0 = Red, 1 = Blue
        string prefabName = (teamIndex == 0) ? prefabRed : prefabBlue;   // Виправлено: 0 = Red, 1 = Blue

        Debug.Log($"Спавню гравця {player.NickName}, команда {teamIndex}, префаб {prefabName}");

        if (player == PhotonNetwork.LocalPlayer)
        {
            GameObject existingPlayer = FindLocalPlayer();

            if (existingPlayer == null)
            {
                GameObject playerObj = PhotonNetwork.Instantiate(prefabName, spawnPoint.position, spawnPoint.rotation);
                localPlayerSpawned = true;
                playerObjects[player.ActorNumber] = playerObj; // Додаємо в кеш
                Debug.Log($"Створив нового локального гравця: {playerObj.name}");
            }
            else
            {
                // Переносимо існуючого гравця
                CharacterController cc = existingPlayer.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;

                existingPlayer.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

                if (cc != null) cc.enabled = true;
                
                playerObjects[player.ActorNumber] = existingPlayer; // Оновлюємо кеш
                Debug.Log($"Переніс існуючого локального гравця: {existingPlayer.name}");
            }
        }

        // Оновлюємо дані
        playerTeams[player.ActorNumber] = teamIndex;
        playerAlive[player.ActorNumber] = true;
    }
    private GameObject FindLocalPlayer()
    {
        PhotonView[] allViews = FindObjectsOfType<PhotonView>();
        foreach (PhotonView view in allViews)
        {
            if (view.IsMine && view.CompareTag("Player"))
            {
                Debug.Log($"Знайшов локального гравця: {view.gameObject.name}");
                return view.gameObject;
            }
        }
        return null;
    }
    public void ReviveDeadPlayersAndLockMovement()
    {
        Debug.Log("Починаю відродження мертвих гравців та блокування руху");

        // Спочатку відроджуємо всіх мертвих гравців
        foreach (var kvp in playerTeams)
        {
            int actorNumber = kvp.Key;
            int teamIndex = kvp.Value;

            Debug.Log($"Обробляю гравця {actorNumber}, команда {teamIndex}");

            // Перевіряємо чи гравець мертвий
            bool isDead = !playerAlive.ContainsKey(actorNumber) || !playerAlive[actorNumber];
            GameObject playerObj = FindPlayerObject(actorNumber);

            if (isDead || playerObj == null)
            {
                // Гравець мертвий або його об'єкт відсутній - потрібно відродити
                playerAlive[actorNumber] = true;
                Debug.Log($"Відроджую гравця {actorNumber}");

                // Знаходимо гравця в кімнаті
                Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
                if (player != null)
                {
                    // Якщо це локальний гравець - створюємо його об'єкт
                    if (player == PhotonNetwork.LocalPlayer)
                    {
                        // Видаляємо старий об'єкт якщо він існує
                        if (playerObj != null)
                        {
                            PhotonNetwork.Destroy(playerObj);
                            playerObjects.Remove(actorNumber);
                        }

                        // Створюємо новий об'єкт
                        SpawnPlayer(player);
                    }
                    else
                    {
                        // Для інших гравців - надсилаємо RPC щоб вони самі себе відродили
                        photonView.RPC("RespawnPlayer", player, actorNumber);
                    }
                }
            }
            else if (playerObj != null)
            {
                // Гравець живий - просто переносимо на старт
                Debug.Log($"Переношу живого гравця {actorNumber} на старт");
                MovePlayerToSpawn(playerObj, teamIndex);
            }

            // Після спавну всіх гравців - блокуємо рух
            Invoke("LockAllPlayersMovement", 0.1f); // Невелика затримка щоб об'єкти встигли створитись
        }
    }
    // Оновлює кеш об'єктів гравців
    private void RefreshPlayerCache()
    {
        // Очищаємо старі записи
        var keysToRemove = new List<int>();
        foreach (var kvp in playerObjects)
        {
            if (kvp.Value == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        foreach (int key in keysToRemove)
        {
            playerObjects.Remove(key);
        }

        // Шукаємо всіх гравців на сцені
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject playerObj in allPlayers)
        {
            PhotonView pv = playerObj.GetComponent<PhotonView>();
            if (pv != null && pv.Owner != null)
            {
                int actorNumber = pv.Owner.ActorNumber;
                if (!playerObjects.ContainsKey(actorNumber))
                {
                    playerObjects[actorNumber] = playerObj;
                    Debug.Log($"Знайшов та закешував гравця {actorNumber}: {playerObj.name}");
                }
            }
        }
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // Очищаємо дані про гравця, що вийшов
        if (playerTeams.ContainsKey(otherPlayer.ActorNumber))
            playerTeams.Remove(otherPlayer.ActorNumber);
        if (playerAlive.ContainsKey(otherPlayer.ActorNumber))
            playerAlive.Remove(otherPlayer.ActorNumber);
        if (playerObjects.ContainsKey(otherPlayer.ActorNumber))
            playerObjects.Remove(otherPlayer.ActorNumber);
    }
    [PunRPC]
    private void RespawnPlayer(int actorNumber)
    {
        // Цей RPC викликається для конкретного гравця, щоб він відродив себе
        if (PhotonNetwork.LocalPlayer.ActorNumber == actorNumber)
        {
            Debug.Log($"Отримав команду на відродження себе (актор {actorNumber})");
        
            // Видаляємо свій старий об'єкт якщо він існує
            GameObject oldObj = FindPlayerObject(actorNumber);
            if (oldObj != null)
            {
                PhotonNetwork.Destroy(oldObj);
                playerObjects.Remove(actorNumber);
            }
        
            // Створюємо новий об'єкт
            SpawnPlayer(PhotonNetwork.LocalPlayer);
        }
    }
    private void LockAllPlayersMovement()
    {
        Debug.Log("Блокую рух для всіх гравців");
    
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject playerObj in allPlayers)
        {
            var controller = playerObj.GetComponent<FirstPersonController>();
            if (controller != null)
            {
                controller.CanMove = false;
                playerObj.GetComponent<WeaponManager>().CanFire = false;
                Debug.Log($"Заблокував рух для {playerObj.name}");
            }
        }
    }
    private void MovePlayerToSpawn(GameObject playerObj, int teamIndex)
    {
        Vector3 spawnPos = (teamIndex == 0) ? spawnRed.position : spawnBlue.position;
        Quaternion spawnRot = (teamIndex == 0) ? spawnRed.rotation : spawnBlue.rotation;
    
        CharacterController cc = playerObj.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
    
        playerObj.transform.SetPositionAndRotation(spawnPos, spawnRot);
    
        if (cc != null) cc.enabled = true;
    }
    private GameObject FindPlayerObject(int actorNumber)
    {
        if (playerObjects.TryGetValue(actorNumber, out var cached) && cached != null)
            return cached;

        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject playerObj in allPlayers)
        {
            PhotonView pv = playerObj.GetComponent<PhotonView>();
            if (pv != null && pv.Owner != null && pv.Owner.ActorNumber == actorNumber)
            {
                playerObjects[actorNumber] = playerObj;
                return playerObj;
            }
        }

        return null; // без Warning
    }
    public void PlayerDied(int actorNumber)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Відмічаємо гравця як мертвого
        playerAlive[actorNumber] = false;
        Debug.Log($"Гравець {actorNumber} помер!");

        // НЕ видаляємо з кешу playerObjects тут, бо об'єкт може ще існувати
        // Він буде очищений автоматично в RefreshPlayerCache() або при респавні
    
        // Повідомляємо всіх гравців про смерть (для UI, статистики тощо)
        photonView.RPC("OnPlayerDeathNotification", RpcTarget.Others, actorNumber);

        // Перевіряємо чи залишились живі гравці в командах
        CheckTeamsAlive();
    }
    public void ReviveAllPlayers()
    {
        foreach (var id in new List<int>(playerAlive.Keys))
        {
            playerAlive[id] = true;
        }
        Debug.Log("Відродив всіх гравців");
    }
    public void CheckTeamsAlive()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int terroristsAlive = 0;
        int counterAlive = 0;

        foreach (var kvp in playerAlive)
        {
            int actorNumber = kvp.Key;
            bool alive = kvp.Value;

            if (!alive) continue;

            Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorNumber);
            if (player == null) continue;

            if (player.CustomProperties.ContainsKey("team"))
            {
                int team = (int)player.CustomProperties["team"];

                if (team == 0) terroristsAlive++;
                else if (team == 1) counterAlive++;
            }
        }

        if (terroristsAlive == 0 && counterAlive > 0)
        {
            _roundManager.EndRound(1); // CounterTerrorists win
        }
        else if (counterAlive == 0 && terroristsAlive > 0)
        {
            _roundManager.EndRound(0); // Terrorists win
        }
    }
}
