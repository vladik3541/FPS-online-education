using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;
using System;

public enum BombState
{
    NotPlanted,
    Planting,
    Planted,
    Defusing,
    Defused,
    Exploded
}

public class BombManager : MonoBehaviourPunCallbacks
{
    public static BombManager Instance;

    [Header("Bomb Settings")]
    public float plantDuration = 3f;
    public float explosionTime = 45f;
    public float defuseDuration = 10f;
    public float defuseDurationWithKit = 5f;
    
    [Header("Bomb Site")]
    public Transform[] bombSites;
    public float bombSiteRadius = 5f;
    
    [Header("Components")]
    [SerializeField] private BombUI bombUI;
    [SerializeField] private BombAudioManager audioManager;
    [SerializeField] private BombRewardSystem rewardSystem;
    
    private BombState currentBombState = BombState.NotPlanted;
    private double bombPlantedTime;
    private double bombActionStartTime;
    private int bombCarrierActorId = -1;
    private Vector3 bombPosition;
    private int plantingSiteIndex = -1;
    
    private GameObject bombVisual;
    private int currentPlantingPlayer = -1;
    private int currentDefusingPlayer = -1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Initialize()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            AssignBombCarrier();
        }
    }

    void Update()
    {
        if (currentBombState == BombState.Planted)
        {
            double timeLeft = GetBombTimeRemaining();
            
            if (PhotonNetwork.IsMasterClient && timeLeft <= 0)
            {
                ExplodeBomb();
            }
        }
    }

    private void AssignBombCarrier()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (player.CustomProperties.ContainsKey("team"))
            {
                int team = (int)player.CustomProperties["team"];
                if (team == 0)
                {
                    bombCarrierActorId = player.ActorNumber;
                    
                    Hashtable props = new Hashtable { { "hasBomb", true } };
                    player.SetCustomProperties(props);
                    
                    photonView.RPC(nameof(UpdateBombCarrier_RPC), RpcTarget.All, player.ActorNumber);
                    
                    // ✅ Нагорода за носіння бомби
                    if (rewardSystem != null)
                    {
                        rewardSystem.OnBombCarrierAssigned(player.ActorNumber);
                    }
                    
                    Debug.Log($"Бомба призначена гравцю {player.NickName}");
                    return;
                }
            }
        }
    }

    [PunRPC]
    private void UpdateBombCarrier_RPC(int actorId)
    {
        bombCarrierActorId = actorId;
    }

    public void TryPlantBomb()
    {
        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("hasBomb")) return;
        if (!(bool)PhotonNetwork.LocalPlayer.CustomProperties["hasBomb"]) return;
        if (currentBombState != BombState.NotPlanted) return;

        int siteIndex = GetNearestBombSite();
        if (siteIndex == -1)
        {
            Debug.Log("Ти не в зоні закладання бомби!");
            return;
        }

        photonView.RPC(nameof(StartPlanting_RPC), RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, siteIndex, PhotonNetwork.Time);
    }

    [PunRPC]
    private void StartPlanting_RPC(int actorId, int siteIndex, double startTime)
    {
        currentBombState = BombState.Planting;
        currentPlantingPlayer = actorId;
        plantingSiteIndex = siteIndex;
        bombActionStartTime = startTime;
        
        Debug.Log($"Гравець {actorId} починає закладати бомбу на точці {(char)('A' + siteIndex)}");
        
        if (actorId == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            LockLocalPlayerMovement(true);
        }
    }

    public void CancelPlanting()
    {
        if (currentBombState != BombState.Planting) return;
        if (currentPlantingPlayer != PhotonNetwork.LocalPlayer.ActorNumber) return;

        photonView.RPC(nameof(CancelPlanting_RPC), RpcTarget.All);
    }

    [PunRPC]
    private void CancelPlanting_RPC()
    {
        if (currentPlantingPlayer == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            LockLocalPlayerMovement(false);
        }
        
        currentBombState = BombState.NotPlanted;
        currentPlantingPlayer = -1;
        Debug.Log("Закладання бомби скасовано");
    }

    public void UpdatePlantingProgress()
    {
        if (currentBombState != BombState.Planting) return;
        if (currentPlantingPlayer != PhotonNetwork.LocalPlayer.ActorNumber) return;

        double elapsed = PhotonNetwork.Time - bombActionStartTime;
        
        if (elapsed >= plantDuration)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(PlantBomb_RPC), RpcTarget.All, bombSites[plantingSiteIndex].position, PhotonNetwork.Time, currentPlantingPlayer);
            }
        }
    }

    [PunRPC]
    private void PlantBomb_RPC(Vector3 position, double plantTime, int planterActorId)
    {
        currentBombState = BombState.Planted;
        bombPosition = position;
        bombPlantedTime = plantTime;
        
        LockLocalPlayerMovement(false);
        
        CreateBombVisual(position);
        
        Player carrier = PhotonNetwork.CurrentRoom.GetPlayer(bombCarrierActorId);
        if (carrier != null)
        {
            Hashtable props = new Hashtable { { "hasBomb", false } };
            carrier.SetCustomProperties(props);
        }
        
        // ✅ UI оновлення
        if (bombUI != null)
        {
            bombUI.OnBombPlanted();
        }
        
        // ✅ Звук
        if (audioManager != null)
        {
            audioManager.OnBombPlantedSound();
        }
        
        // ✅ Нагорода
        if (PhotonNetwork.IsMasterClient && rewardSystem != null)
        {
            rewardSystem.OnBombPlanted(planterActorId);
        }
        
        currentPlantingPlayer = -1;
        Debug.Log($"Бомбу закладено! Вибух через {explosionTime} секунд");
    }

    public void TryDefuseBomb()
    {
        if (currentBombState != BombState.Planted) return;
        
        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("team")) return;
        int team = (int)PhotonNetwork.LocalPlayer.CustomProperties["team"];
        if (team != 1) return;
        
        GameObject localPlayer = GetLocalPlayerObject();
        if (localPlayer == null) return;
        
        float distance = Vector3.Distance(localPlayer.transform.position, bombPosition);
        if (distance > 2f)
        {
            Debug.Log("Ти занадто далеко від бомби!");
            return;
        }
        
        bool hasDefuseKit = PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("hasDefuseKit") 
            && (bool)PhotonNetwork.LocalPlayer.CustomProperties["hasDefuseKit"];
        
        photonView.RPC(nameof(StartDefusing_RPC), RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber, hasDefuseKit, PhotonNetwork.Time);
    }

    [PunRPC]
    private void StartDefusing_RPC(int actorId, bool hasKit, double startTime)
    {
        currentBombState = BombState.Defusing;
        currentDefusingPlayer = actorId;
        bombActionStartTime = startTime;
        
        float duration = hasKit ? defuseDurationWithKit : defuseDuration;
        Debug.Log($"Гравець {actorId} починає знешкоджувати бомбу ({duration}s)");
        
        if (actorId == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            LockLocalPlayerMovement(true);
        }
    }

    public void CancelDefusing()
    {
        if (currentBombState != BombState.Defusing) return;
        if (currentDefusingPlayer != PhotonNetwork.LocalPlayer.ActorNumber) return;

        photonView.RPC(nameof(CancelDefusing_RPC), RpcTarget.All);
    }

    [PunRPC]
    private void CancelDefusing_RPC()
    {
        if (currentDefusingPlayer == PhotonNetwork.LocalPlayer.ActorNumber)
        {
            LockLocalPlayerMovement(false);
        }
        
        currentBombState = BombState.Planted;
        currentDefusingPlayer = -1;
        Debug.Log("Знешкодження бомби скасовано");
    }

    public void UpdateDefusingProgress()
    {
        if (currentBombState != BombState.Defusing) return;
        if (currentDefusingPlayer != PhotonNetwork.LocalPlayer.ActorNumber) return;

        Player defuser = PhotonNetwork.LocalPlayer;
        bool hasKit = defuser.CustomProperties.ContainsKey("hasDefuseKit") 
            && (bool)defuser.CustomProperties["hasDefuseKit"];
        
        float duration = hasKit ? defuseDurationWithKit : defuseDuration;
        double elapsed = PhotonNetwork.Time - bombActionStartTime;
        
        if (elapsed >= duration)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC(nameof(DefuseBomb_RPC), RpcTarget.All, currentDefusingPlayer);
            }
        }
    }

    [PunRPC]
    private void DefuseBomb_RPC(int defuserActorId)
    {
        currentBombState = BombState.Defused;
        
        LockLocalPlayerMovement(false);
        
        if (bombVisual != null)
        {
            Destroy(bombVisual);
        }
        
        // ✅ UI оновлення
        if (bombUI != null)
        {
            bombUI.OnBombDefused();
        }
        
        // ✅ Звук
        if (audioManager != null)
        {
            audioManager.OnBombDefusedSound();
        }
        
        // ✅ Нагорода
        if (PhotonNetwork.IsMasterClient && rewardSystem != null)
        {
            rewardSystem.OnBombDefused(defuserActorId);
        }
        
        currentDefusingPlayer = -1;
        Debug.Log("Бомбу знешкоджено! CT виграли раунд");
        
        if (PhotonNetwork.IsMasterClient)
        {
            RoundManager.Instance.EndRound(1);
        }
    }

    private void ExplodeBomb()
    {
        photonView.RPC(nameof(ExplodeBomb_RPC), RpcTarget.All);
    }

    [PunRPC]
    private void ExplodeBomb_RPC()
    {
        currentBombState = BombState.Exploded;
        
        if (bombVisual != null)
        {
            Destroy(bombVisual);
        }
        
        // ✅ UI оновлення
        if (bombUI != null)
        {
            bombUI.OnBombExploded();
        }
        
        // ✅ Звук
        if (audioManager != null)
        {
            audioManager.OnBombExplodedSound();
        }
        
        // ✅ Нагорода
        if (PhotonNetwork.IsMasterClient && rewardSystem != null)
        {
            rewardSystem.OnBombExploded();
        }
        
        Debug.Log("БОМБА ВИБУХНУЛА! T виграли раунд");
        
        if (PhotonNetwork.IsMasterClient)
        {
            RoundManager.Instance.EndRound(0);
        }
    }

    public void ResetBomb()
    {
        currentBombState = BombState.NotPlanted;
        bombCarrierActorId = -1;
        currentPlantingPlayer = -1;
        currentDefusingPlayer = -1;
        
        if (bombVisual != null)
        {
            Destroy(bombVisual);
        }
        
        // ✅ Скидаємо нагороди
        if (rewardSystem != null)
        {
            rewardSystem.ResetRound();
        }
        
        if (PhotonNetwork.IsMasterClient)
        {
            AssignBombCarrier();
        }
    }

    private int GetNearestBombSite()
    {
        GameObject localPlayer = GetLocalPlayerObject();
        if (localPlayer == null) return -1;

        for (int i = 0; i < bombSites.Length; i++)
        {
            float distance = Vector3.Distance(localPlayer.transform.position, bombSites[i].position);
            if (distance <= bombSiteRadius)
            {
                return i;
            }
        }
        return -1;
    }

    private GameObject GetLocalPlayerObject()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject player in players)
        {
            PhotonView pv = player.GetComponent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                return player;
            }
        }
        return null;
    }

    private void LockLocalPlayerMovement(bool locked)
    {
        GameObject localPlayer = GetLocalPlayerObject();
        if (localPlayer != null)
        {
            var controller = localPlayer.GetComponent<FirstPersonController>();
            if (controller != null)
            {
                controller.CanMove = !locked;
            }
            
            var weaponManager = localPlayer.GetComponent<WeaponManager>();
            if (weaponManager != null)
            {
                weaponManager.CanFire = !locked;
            }
        }
    }

    private void CreateBombVisual(Vector3 position)
    {
        bombVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bombVisual.transform.position = position;
        bombVisual.transform.localScale = new Vector3(0.5f, 0.3f, 0.5f);
        bombVisual.GetComponent<Renderer>().material.color = Color.red;
        
        Destroy(bombVisual.GetComponent<Collider>());
    }

    public void OnBombCarrierDeath(Vector3 deathPosition)
    {
        if (currentBombState != BombState.NotPlanted) return;
        
        bombPosition = deathPosition;
        CreateBombVisual(deathPosition);
        
        Debug.Log("Гравець з бомбою загинув! Бомба на землі");
    }

    public void TryPickupBomb()
    {
        if (currentBombState != BombState.NotPlanted) return;
        
        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("team")) return;
        int team = (int)PhotonNetwork.LocalPlayer.CustomProperties["team"];
        if (team != 0) return;
        
        GameObject localPlayer = GetLocalPlayerObject();
        if (localPlayer == null) return;
        
        float distance = Vector3.Distance(localPlayer.transform.position, bombPosition);
        if (distance <= 2f)
        {
            photonView.RPC(nameof(PickupBomb_RPC), RpcTarget.All, PhotonNetwork.LocalPlayer.ActorNumber);
        }
    }

    [PunRPC]
    private void PickupBomb_RPC(int actorId)
    {
        bombCarrierActorId = actorId;
        
        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorId);
        if (player != null)
        {
            Hashtable props = new Hashtable { { "hasBomb", true } };
            player.SetCustomProperties(props);
        }
        
        if (bombVisual != null)
        {
            Destroy(bombVisual);
        }
        
        Debug.Log($"Гравець {actorId} підняв бомбу");
    }

    // === ПУБЛІЧНІ МЕТОДИ ДЛЯ UI ===
    
    public float GetPlantProgress()
    {
        if (currentBombState != BombState.Planting) return 0f;
        double elapsed = PhotonNetwork.Time - bombActionStartTime;
        return Mathf.Clamp01((float)(elapsed / plantDuration));
    }

    public float GetDefuseProgress()
    {
        if (currentBombState != BombState.Defusing) return 0f;
        
        Player defuser = PhotonNetwork.CurrentRoom.GetPlayer(currentDefusingPlayer);
        if (defuser == null) return 0f;
        
        bool hasKit = defuser.CustomProperties.ContainsKey("hasDefuseKit") 
            && (bool)defuser.CustomProperties["hasDefuseKit"];
        
        float duration = hasKit ? defuseDurationWithKit : defuseDuration;
        double elapsed = PhotonNetwork.Time - bombActionStartTime;
        return Mathf.Clamp01((float)(elapsed / duration));
    }

    public double GetBombTimeRemaining()
    {
        if (currentBombState != BombState.Planted) return 0;
        double elapsed = PhotonNetwork.Time - bombPlantedTime;
        return Math.Max(0, explosionTime - elapsed);
    }

    public BombState GetBombState()
    {
        return currentBombState;
    }

    public Vector3 GetBombPosition()
    {
        return bombPosition;
    }

    public int GetBombCarrierId()
    {
        return bombCarrierActorId;
    }

    public bool HasBomb(int actorId)
    {
        return actorId == bombCarrierActorId;
    }
}