using Photon.Pun;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponView : MonoBehaviour
{
    public event Action<WeaponData> OnSelect;
    [SerializeField] private WeaponData weaponDataCT;
    [SerializeField] private WeaponData weaponDataT;
    [SerializeField] private TextMeshProUGUI name;
    [SerializeField] private TextMeshProUGUI price;
    [SerializeField] private Image image;
    private string _team;
    public void Initialize()
    {
        ApplySettings();
    }
    public void ApplySettings()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("team", out object teamValue))
        {
            string team = teamValue.ToString();
            _team = team;

            if (team == "CT")
            {
                name.text = weaponDataCT.name;
                price.text = weaponDataCT.price.ToString();
                image.sprite = weaponDataCT.gunImage;
            }
            else if (team == "T")
            {
                if (weaponDataT = null) gameObject.SetActive(false);
                else
                {
                    name.text = weaponDataT.name;
                    price.text = weaponDataT.price.ToString();
                    image.sprite = weaponDataT.gunImage;
                }
            }
        }
        else
        {
            Debug.LogWarning("” локального гравц€ не встановлена команда!");
        }
    }
    public void Select()
    {
        OnSelect?.Invoke(_team == "CT"? weaponDataCT : weaponDataT);
    }
}
