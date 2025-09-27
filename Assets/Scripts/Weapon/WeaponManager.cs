using M_project.Scripts.Weapon;
using Photon.Pun;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [SerializeField] private Weapon[] weapons;
    [SerializeField] private FirstPersonController firstPersonController;
    
    private Weapon _currentWeapon;
    private PhotonView _photonView;
    public Weapon[] Weapons => weapons;

    public bool CanFire { get; set; } = true;

    private void Start()
    {
        _photonView = GetComponent<PhotonView>();
        foreach (var weapon in weapons)
        {
            weapon.gameObject.SetActive(false);
        }
        if (_photonView.IsMine)
        {
            _currentWeapon = weapons[0];
        }
        
        SwitchWeapon(0);
    }
    void Update()
    {
        if(!_photonView.IsMine) return;
        
        if(Input.GetKey(KeyCode.Mouse0) && CanFire)
        {
            _currentWeapon.Use(() =>
            {
                firstPersonController.PlayerCamera.AddRecoil(_currentWeapon.VerticalRecoil, _currentWeapon.HorizontalRecoil);
            });
        }
        if(Input.GetKey(KeyCode.Alpha1)) SwitchWeapon(0);
        if(Input.GetKey(KeyCode.Alpha2)) SwitchWeapon(1);
        if(Input.GetKey(KeyCode.Alpha3)) SwitchWeapon(2);
        if(Input.GetKey(KeyCode.R))
        {
            _currentWeapon.Reload();
        }
    }
    private void SwitchWeapon(int index)
    {
        if (_currentWeapon != null)
        {
            _currentWeapon.gameObject.SetActive(false);
            _currentWeapon = weapons[index];
            _currentWeapon.gameObject.SetActive(true);
        }
    }
}
