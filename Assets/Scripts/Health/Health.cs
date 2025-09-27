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

    // Викликає той, хто стріляє
    public void TakeDamage(float damage, int attackerId)
    {
        // ⚡ Важливо: поширюємо всім
        photonView.RPC(nameof(RPC_TakeDamage), photonView.Owner, damage, attackerId);
    }

    [PunRPC]
    public void RPC_TakeDamage(float damage, int attackerId)
    {
        health -= damage;
        if (health < 0) health = 0;

        UpdateUI();
        OnUpdateHealth?.Invoke();

        if (health <= 0)
        {
            OnDie?.Invoke();
            Die(attackerId);
        }
    }

    protected virtual void Die(int killerActorId)
    {
        if (killerActorId != -1 && PhotonNetwork.IsMasterClient)
        {
            RoundManager.Instance.RewardForKill(killerActorId, "rifle"); // тип зброї, щоб різна винагорода
        }
        gameObject.SetActive(false);
    }

    private void UpdateUI()
    {
        if (healthSlider != null)
            healthSlider.value = health;

        if (healthText != null)
            healthText.text = health.ToString("0");
    }

    // Синхронізація на випадок лагів
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