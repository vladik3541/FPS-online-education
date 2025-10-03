using System;
using System.Collections;
using Photon.Pun;
using UnityEngine;

namespace M_project.Scripts.Weapon
{
    public class Weapon : MonoBehaviour
    {
        public event Action OnShoot;
        public event Action<bool> OnReloading;
        public event Action<int, int> OnChangeAmmo;

        private PhotonView ownerView;
        
        [SerializeField] protected float damage;
        [SerializeField] protected LayerMask layerEnemy;
        [SerializeField] protected Transform spawnPoint;
        [SerializeField] protected Transform cameraLook;
        [SerializeField] private float fireSpeed;

        [SerializeField,Min(0)] private float verticalRecoil, horizontalRecoil;

        [SerializeField] protected int totalAmmo, maxMagazineCapacity;
        [SerializeField] protected float timeReload;

        protected int _currentAmmo;
        private float _lastShootTime;
        private bool _isReload;
        public float VerticalRecoil => verticalRecoil;
        public float HorizontalRecoil => horizontalRecoil;

        private void OnEnable()
        {
            OnChangeAmmo?.Invoke(_currentAmmo, totalAmmo);
        }

        private void Start()
        {
            _currentAmmo = maxMagazineCapacity;
            ownerView = GetComponent<PhotonView>();
        }
        public void Use(Action OnShot)
        {
            if(Time.time > _lastShootTime && _currentAmmo > 0 && !_isReload)
            {
                OnShoot?.Invoke();
                Shoot();
                OnChangeAmmo?.Invoke(_currentAmmo, totalAmmo);
                CheckReload();
                
                _lastShootTime = Time.time + fireSpeed;
                OnShot?.Invoke();
            }
        }

        protected virtual void Shoot()
        {
            Vector3 direction = cameraLook.forward;
            RaycastHit hit;
            if (Physics.Raycast(cameraLook.position, direction, out hit, float.MaxValue))
            {
                if (hit.collider.TryGetComponent(out Health health))
                {
                    health.TakeDamage(damage, ownerView.OwnerActorNr,"rifle");
                }
            }
            _currentAmmo--;
        }
        public void Reload()
        {
            if (totalAmmo >= 0 && _currentAmmo != maxMagazineCapacity)
            {
                StartCoroutine(StartReload());
            }
        }
        private void CheckReload()
        {
            if (_currentAmmo <= 0 && totalAmmo >= 0)
            {
                StartCoroutine(StartReload());
            }
        }
        IEnumerator StartReload()
        {
            _isReload = true;
            OnReloading?.Invoke(_isReload);
            yield return new WaitForSeconds(timeReload);
            _isReload = false;
            OnReloading?.Invoke(_isReload);
            int ammoNeeded = maxMagazineCapacity - _currentAmmo;

            if (totalAmmo < ammoNeeded)
            {
                _currentAmmo += totalAmmo;
                totalAmmo = 0;
            }
            else
            {
                _currentAmmo = maxMagazineCapacity;
                totalAmmo -= ammoNeeded;
            }
            OnChangeAmmo?.Invoke(_currentAmmo, totalAmmo);
        }
    }
}
