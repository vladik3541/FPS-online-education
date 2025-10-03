using System;
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class Health : MonoBehaviourPunCallbacks, IPunObservable
{
    public event Action OnDie;
    public event Action OnUpdateHealth;

    [SerializeField] protected float maxHealth = 100f;
    [SerializeField] protected float health;

    [SerializeField] private Slider healthSlider;
    [SerializeField] private TextMeshProUGUI healthText;

    private PhotonView photonView;

    private void Start()
    {
        health = maxHealth;
        photonView = GetComponent<PhotonView>();
        if (!photonView.IsMine)
        {
            healthSlider.gameObject.SetActive(false);
            healthText.gameObject.SetActive(false);
        }
        healthSlider.maxValue = maxHealth;
        UpdateUI();
    }

    // ✅ ВИПРАВЛЕННЯ: Додано параметр weaponType
    public void TakeDamage(float damage, int attackerId, string weaponType)
    {
        photonView.RPC(nameof(RPC_TakeDamage), photonView.Owner, damage, attackerId, weaponType);
    }

    [PunRPC]
    public void RPC_TakeDamage(float damage, int attackerId, string weaponType)
    {
        health -= damage;
        if (health < 0) health = 0;

        UpdateUI();
        OnUpdateHealth?.Invoke();

        if (health <= 0)
        {
            OnDie?.Invoke();
            
            // ✅ ВИПРАВЛЕННЯ: Викликаємо Die через RPC для всіх
            photonView.RPC(nameof(RPC_Die), RpcTarget.All, attackerId, weaponType);
        }
    }

    // ✅ НОВИЙ RPC МЕТОД: Викликається для всіх гравців
    [PunRPC]
    protected void RPC_Die(int killerActorId, string weaponType)
    {
        // Викликаємо віртуальний метод для дочірніх класів
        Die(killerActorId, weaponType);
    }

    // ✅ ВИПРАВЛЕННЯ: Додано параметр weaponType
    protected virtual void Die(int killerActorId, string weaponType)
    {
        // ✅ Нараховуємо винагороду ТІЛЬКИ РАЗ (тільки на клієнті жертви або хоста)
        if (photonView.IsMine && killerActorId != -1 && RoundManager.Instance != null)
        {
            RoundManager.Instance.RewardForKill(killerActorId, weaponType);
        }

        // ✅ Деактивація виконується для ВСІХ (бо викликано через RPC)
        gameObject.SetActive(false);
    }

    private void UpdateUI()
    {
        if (healthSlider != null)
            healthSlider.value = health;

        if (healthText != null)
            healthText.text = health.ToString("0");
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(health);
        }
        else
        {
            health = (float)stream.ReceiveNext();
            UpdateUI();
        }
    }
}