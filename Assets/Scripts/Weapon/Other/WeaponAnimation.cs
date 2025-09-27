using System;
using M_project.Scripts.Weapon;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Serialization;

public class WeaponAnimation : MonoBehaviour
{
    [SerializeField] private Weapon weapon;
    [SerializeField] private Animator animator;
    private PhotonView _photonView;
    void Start()
    {
        _photonView = GetComponent<PhotonView>();
        weapon.OnShoot += ShootAnim;
        weapon.OnReloading += ReloadAnim;
    }

    private void Update()
    {
        if(!_photonView.IsMine)return;
        if(FirstPersonController.inputHandler.GetMovementInput() != Vector2.zero)
        {
            animator.SetBool("Walk", true);
        }
        else
        {
            animator.SetBool("Walk", false);
        }
    }

    private void ShootAnim()
    {
        animator.SetTrigger("Shoot");
    }

    private void ReloadAnim(bool isReloading)
    {
        if(isReloading)
            animator.SetBool("Reload", true);
        else
            animator.SetBool("Reload", false);
    }
    void OnDestroy()
    {
        weapon.OnShoot -= ShootAnim;
        weapon.OnReloading -= ReloadAnim;
    }
}
