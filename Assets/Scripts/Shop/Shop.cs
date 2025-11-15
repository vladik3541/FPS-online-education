using Photon.Pun;
using ExitGames.Client.Photon;
using UnityEngine;

public class Shop : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject chooseTeamUI;
    [SerializeField] private GameObject shopUI;
    [SerializeField] private WeaponView[] weaponView;

    private bool isShopUnlocked = true;
    private RoundState currentState;

    void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        if (PhotonNetwork.LocalPlayer == null) return;
        // Перевірка натискання клавіші B для відкриття/закриття магазину
        if (Input.GetKeyDown(KeyCode.B) && isShopUnlocked)
        {
            ToggleShop();
        }
    }

    private void Initialize()
    {
        shopUI.SetActive(false);

        foreach (var weapon in weaponView)
        {
            weapon.Initialize();
            weapon.OnSelect += Buy;
        }
    }

    private void ToggleShop()
    {
        bool newShopState = !shopUI.activeSelf;
        shopUI.SetActive(newShopState);
        SetCursorState(newShopState);
    }

    private void SetCursorState(bool showCursor)
    {
        Cursor.visible = showCursor;
        Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changeProps)
    {
        if (targetPlayer == PhotonNetwork.LocalPlayer)
        {
            Initialize();
        }
    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (propertiesThatChanged.ContainsKey("state"))
        {
            currentState = (RoundState)(int)PhotonNetwork.CurrentRoom.CustomProperties["state"];
            HandleStateChange();
        }
    }

    private void HandleStateChange()
    {
        switch (currentState)
        {
            case RoundState.WaitingForPlayers:
            case RoundState.BuyTime:
                UnlockShop();
                break;

            case RoundState.Playing:
            case RoundState.RoundEnd:
                LockShop();
                break;
        }
    }

    private void UnlockShop()
    {
        isShopUnlocked = true;
    }

    private void LockShop()
    {
        isShopUnlocked = false;

        // Якщо магазин відкритий, закрити його
        if (shopUI.activeSelf)
        {
            shopUI.SetActive(false);
            SetCursorState(false);
        }
    }

    public void Buy(WeaponData weaponData)
    {
        // Логіка покупки зброї
    }
}