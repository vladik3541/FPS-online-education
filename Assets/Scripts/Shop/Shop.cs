using Photon.Pun;
using UnityEngine;

public class Shop : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject shopUI;
    [SerializeField] private WeaponView[] weaponView;
    void Awake()
    {
        Initialize();
    }
    private void Update()
    {
        if (PhotonNetwork.LocalPlayer == null) return;
        if (Input.GetKeyDown(KeyCode.B))
        {
            shopUI.SetActive(!shopUI.activeSelf);
        }
    }
    private void Initialize()
    {
        shopUI.SetActive(true);
        foreach (var weapon in weaponView)
        {
            weapon.Initialize();
            weapon.OnSelect += Buy;
        }
        shopUI.SetActive(false);
    }
    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changeProps)
    {
        if (targetPlayer == PhotonNetwork.LocalPlayer)
        {
            Initialize();
        }
    }
    public void Buy(WeaponData weaponData)
    {

    }
}
