
using M_project.Scripts.Weapon;
using Photon.Pun;
using UnityEngine;

public class WeaponEffects : MonoBehaviour
{
    [SerializeField] private Weapon weapon;
    [SerializeField] private ParticleSystem muzzleFlashPrefab;
    [SerializeField] private Transform muzzleFlashTransform;

    private PhotonView _photonView;
    private ParticleSystem muzzleFlashEffect;

    private void Start()
    {
        _photonView = GetComponent<PhotonView>();

        // Створюємо muzzle flash у всіх (але він запускається тільки через RPC)
        muzzleFlashEffect = Instantiate(muzzleFlashPrefab, muzzleFlashTransform);

        if (_photonView.IsMine)
        {
            weapon.OnShoot += OnShoot;
        }
    }

    private void OnShoot()
    {
        // Тільки власник зброї каже всім "показати ефект"
        _photonView.RPC(nameof(RPC_PlayMuzzleFlash), RpcTarget.All);
    }

    [PunRPC]
    private void RPC_PlayMuzzleFlash()
    {
        if (muzzleFlashEffect != null)
            muzzleFlashEffect.Emit(1);
    }

    private void OnDestroy()
    {
        if (_photonView != null && _photonView.IsMine)
        {
            weapon.OnShoot -= OnShoot;
        }
    }
}
