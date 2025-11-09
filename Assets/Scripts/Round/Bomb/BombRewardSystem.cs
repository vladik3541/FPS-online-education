using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

/// <summary>
/// Система нагород за дії з бомбою
/// Додайте цей компонент до BombManager або окремого GameObject
/// </summary>
public class BombRewardSystem : MonoBehaviourPunCallbacks
{
    [Header("Monetary Rewards")]
    [SerializeField] private int bombPlantReward = 300;      // За закладання бомби
    [SerializeField] private int bombDefuseReward = 300;     // За знешкодження бомби
    [SerializeField] private int bombExplodeBonus = 500;     // Бонус T якщо бомба вибухла
    [SerializeField] private int bombCarrierBonus = 100;     // Бонус носію бомби на початку раунду
    
    [Header("Experience/Stats")]
    [SerializeField] private int plantXP = 50;
    [SerializeField] private int defuseXP = 100;
    [SerializeField] private int explodeXP = 75;
    
    private BombManager bombManager;
    private int lastBombPlanter = -1;      // Хто заклав бомбу
    private int lastBombDefuser = -1;      // Хто знешкодив бомбу

    void Start()
    {
        bombManager = BombManager.Instance;
    }

    /// <summary>
    /// Викликається коли гравець заклав бомбу
    /// </summary>
    public void OnBombPlanted(int planterActorId)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        lastBombPlanter = planterActorId;
        GiveReward(planterActorId, bombPlantReward, plantXP);
        
        // Збільшуємо статистику
        IncrementPlayerStat(planterActorId, "bombsPlanted");
        
        Debug.Log($"💰 Гравець {planterActorId} отримав {bombPlantReward}$ за закладання бомби");
    }

    /// <summary>
    /// Викликається коли гравець знешкодив бомбу
    /// </summary>
    public void OnBombDefused(int defuserActorId)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        lastBombDefuser = defuserActorId;
        GiveReward(defuserActorId, bombDefuseReward, defuseXP);
        
        // Збільшуємо статистику
        IncrementPlayerStat(defuserActorId, "bombsDefused");
        
        Debug.Log($"💰 Гравець {defuserActorId} отримав {bombDefuseReward}$ за знешкодження бомби");
    }

    /// <summary>
    /// Викликається коли бомба вибухла
    /// </summary>
    public void OnBombExploded()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        // Даємо бонус гравцю що заклав бомбу
        if (lastBombPlanter != -1)
        {
            GiveReward(lastBombPlanter, bombExplodeBonus, explodeXP);
            Debug.Log($"💰 Гравець {lastBombPlanter} отримав {bombExplodeBonus}$ за вибух бомби");
        }

        // Даємо невеликий бонус всім терористам що живі
        RewardAliveTeam(0, 200); // 0 = T команда
    }

    /// <summary>
    /// Дає стартовий бонус носію бомби
    /// </summary>
    public void OnBombCarrierAssigned(int carrierActorId)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        GiveReward(carrierActorId, bombCarrierBonus, 0);
        Debug.Log($"💰 Носій бомби {carrierActorId} отримав бонус {bombCarrierBonus}$");
    }

    /// <summary>
    /// Скидає дані перед новим раундом
    /// </summary>
    public void ResetRound()
    {
        lastBombPlanter = -1;
        lastBombDefuser = -1;
    }

    // === ПРИВАТНІ МЕТОДИ ===

    private void GiveReward(int actorId, int money, int xp)
    {
        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorId);
        if (player == null) return;

        // Даємо гроші
        int currentMoney = player.CustomProperties.ContainsKey("money") 
            ? (int)player.CustomProperties["money"] : 0;
        int newMoney = Mathf.Clamp(currentMoney + money, 0, 16000);

        // Даємо XP
        int currentXP = player.CustomProperties.ContainsKey("xp") 
            ? (int)player.CustomProperties["xp"] : 0;
        int newXP = currentXP + xp;

        Hashtable props = new Hashtable 
        { 
            { "money", newMoney },
            { "xp", newXP }
        };
        player.SetCustomProperties(props);
    }

    private void IncrementPlayerStat(int actorId, string statName)
    {
        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorId);
        if (player == null) return;

        int currentStat = player.CustomProperties.ContainsKey(statName) 
            ? (int)player.CustomProperties[statName] : 0;

        Hashtable props = new Hashtable { { statName, currentStat + 1 } };
        player.SetCustomProperties(props);
    }

    private void RewardAliveTeam(int teamIndex, int bonus)
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.ContainsKey("team")) continue;
            if (!player.CustomProperties.ContainsKey("isAlive")) continue;

            int team = (int)player.CustomProperties["team"];
            bool isAlive = (bool)player.CustomProperties["isAlive"];

            if (team == teamIndex && isAlive)
            {
                GiveReward(player.ActorNumber, bonus, 0);
            }
        }
    }

    // === ПУБЛІЧНІ МЕТОДИ ДЛЯ СТАТИСТИКИ ===

    /// <summary>
    /// Отримати кількість закладених бомб гравцем
    /// </summary>
    public int GetPlayerBombsPlanted(Player player)
    {
        return player.CustomProperties.ContainsKey("bombsPlanted") 
            ? (int)player.CustomProperties["bombsPlanted"] : 0;
    }

    /// <summary>
    /// Отримати кількість знешкоджених бомб гравцем
    /// </summary>
    public int GetPlayerBombsDefused(Player player)
    {
        return player.CustomProperties.ContainsKey("bombsDefused") 
            ? (int)player.CustomProperties["bombsDefused"] : 0;
    }

    /// <summary>
    /// Отримати XP гравця
    /// </summary>
    public int GetPlayerXP(Player player)
    {
        return player.CustomProperties.ContainsKey("xp") 
            ? (int)player.CustomProperties["xp"] : 0;
    }

    // === ДОСЯГНЕННЯ ===

    private void CheckAchievements(int actorId)
    {
        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorId);
        if (player == null) return;

        int bombsPlanted = GetPlayerBombsPlanted(player);
        int bombsDefused = GetPlayerBombsDefused(player);

        // Приклади досягнень
        if (bombsPlanted >= 10)
        {
            UnlockAchievement(actorId, "Demolition Expert");
        }

        if (bombsDefused >= 10)
        {
            UnlockAchievement(actorId, "Bomb Squad");
        }

        if (bombsPlanted >= 50)
        {
            UnlockAchievement(actorId, "Terrorist Legend");
        }

        if (bombsDefused >= 50)
        {
            UnlockAchievement(actorId, "Counter-Strike Hero");
        }
    }

    private void UnlockAchievement(int actorId, string achievementName)
    {
        Player player = PhotonNetwork.CurrentRoom.GetPlayer(actorId);
        if (player == null) return;

        // Перевіряємо чи вже є це досягнення
        string achievementKey = $"achievement_{achievementName}";
        if (player.CustomProperties.ContainsKey(achievementKey)) return;

        // Розблоковуємо
        Hashtable props = new Hashtable { { achievementKey, true } };
        player.SetCustomProperties(props);

        Debug.Log($"🏆 Гравець {player.NickName} розблокував досягнення: {achievementName}");
        
        // Тут можна показати UI повідомлення про досягнення
    }
}

// === РОЗШИРЕННЯ ДЛЯ ІНТЕГРАЦІЇ З BOMBMANAGER ===
/*
 * Додайте наступні виклики до BombManager:
 * 
 * В PlantBomb_RPC():
 *     BombRewardSystem rewards = GetComponent<BombRewardSystem>();
 *     if (rewards != null) rewards.OnBombPlanted(currentPlantingPlayer);
 * 
 * В DefuseBomb_RPC():
 *     BombRewardSystem rewards = GetComponent<BombRewardSystem>();
 *     if (rewards != null) rewards.OnBombDefused(currentDefusingPlayer);
 * 
 * В ExplodeBomb_RPC():
 *     BombRewardSystem rewards = GetComponent<BombRewardSystem>();
 *     if (rewards != null) rewards.OnBombExploded();
 * 
 * В AssignBombCarrier():
 *     BombRewardSystem rewards = GetComponent<BombRewardSystem>();
 *     if (rewards != null) rewards.OnBombCarrierAssigned(bombCarrierActorId);
 * 
 * В ResetBomb():
 *     BombRewardSystem rewards = GetComponent<BombRewardSystem>();
 *     if (rewards != null) rewards.ResetRound();
 */