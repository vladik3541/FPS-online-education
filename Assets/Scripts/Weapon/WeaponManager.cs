using M_project.Scripts.Weapon;
using Photon.Pun;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private Weapon mainWeapon;
    [SerializeField] private Weapon pistolWeapon;
    [SerializeField] private Weapon knife;
    [SerializeField] private Weapon granade;
    [SerializeField] private Weapon bomb;
    [SerializeField] private FirstPersonController firstPersonController;
    
    private Weapon _currentWeapon;
    private PhotonView _photonView;

    public bool CanFire { get; set; } = true;

    private void Start()
    {
        _photonView = GetComponent<PhotonView>();

        // Початкова зброя
        if (_photonView.IsMine)
        {
            EquipWeapon(WeaponType.main);
        }
        else
        {
            DisableAllWeapons();
        }
    }

    private void Update()
    {
        if(!_photonView.IsMine) return;

        if(Input.GetKey(KeyCode.Mouse0) && CanFire)
        {
            _currentWeapon.Use(() =>
            {
                firstPersonController.PlayerCamera.AddRecoil(_currentWeapon.VerticalRecoil, _currentWeapon.HorizontalRecoil);
            });
        }

        if (Input.GetKeyDown(KeyCode.Alpha1)) SwitchWeapon(WeaponType.main);
        if (Input.GetKeyDown(KeyCode.Alpha2)) SwitchWeapon(WeaponType.pistol);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SwitchWeapon(WeaponType.knife);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SwitchWeapon(WeaponType.granad);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SwitchWeapon(WeaponType.bomb);

        if (Input.GetKeyDown(KeyCode.R))
        {
            _currentWeapon.Reload();
        }
    }

    private void SwitchWeapon(WeaponType type)
    {
        _photonView.RPC(nameof(RPC_SwitchWeapon), RpcTarget.AllBuffered, (int)type);
    }

    [PunRPC]
    private void RPC_SwitchWeapon(int weaponTypeInt)
    {
        WeaponType type = (WeaponType)weaponTypeInt;
        EquipWeapon(type);
    }

    private void EquipWeapon(WeaponType type)
    {
        DisableAllWeapons();

        switch (type)
        {
            case WeaponType.main:
                _currentWeapon = mainWeapon;
                break;
            case WeaponType.pistol:
                _currentWeapon = pistolWeapon;
                break;
            case WeaponType.knife:
                _currentWeapon = knife;
                break;
            case WeaponType.granad:
                _currentWeapon = granade;
                break;
            case WeaponType.bomb:
                _currentWeapon = bomb;
                break;
        }

        if (_currentWeapon != null)
            _currentWeapon.gameObject.SetActive(true);
    }

    private void DisableAllWeapons()
    {
        if (mainWeapon) mainWeapon.gameObject.SetActive(false);
        if (pistolWeapon) pistolWeapon.gameObject.SetActive(false);
        if (knife) knife.gameObject.SetActive(false);
        if (granade) granade.gameObject.SetActive(false);
        if (bomb) bomb.gameObject.SetActive(false);
    }
}
