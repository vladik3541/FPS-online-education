using System;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using ExitGames.Client.Photon;

public class MoneyManager : MonoBehaviourPunCallbacks
{
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private int startingMoney = 800;

    private const string MoneyKey = "money";

    public int Money
    {
        get
        {
            if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue(MoneyKey, out var value))
                return (int)value;

            return startingMoney; // дефолт
        }
    }

    private void Start()
    {
        if (photonView.IsMine)
        {
            // Ініціалізуємо гроші тільки для себе
            if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(MoneyKey))
                SetMoney(startingMoney);
            
            UpdateMoneyUI(Money);
        }
        else
        {
            moneyText.gameObject.SetActive(false);
        }
    }

    public void AddMoney(int amount)
    {
        int currentMoney = Money;
        int newMoney = Mathf.Clamp(currentMoney + amount, 0, 16000);
        SetMoney(newMoney);
    }

    public void SpendMoney(int amount)
    {
        AddMoney(-amount);
    }

    public void SetMoney(int amount)
    {
        Hashtable props = new Hashtable { { MoneyKey, amount } };
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    private void UpdateMoneyUI(int amount)
    {
        if (moneyText != null)
            moneyText.text = "$ " + amount.ToString();
    }

    // !!! Оновлюємо тільки через callback
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (targetPlayer.IsLocal && changedProps.ContainsKey(MoneyKey))
        {
            UpdateMoneyUI((int)changedProps[MoneyKey]);
        }
    }
}