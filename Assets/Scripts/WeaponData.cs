using M_project.Scripts.Weapon;
using UnityEngine;

public enum WeaponType
{
    main, pistol, knife, granad, bomb
}
[CreateAssetMenu(menuName = "Weapon/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [field: SerializeField] public Weapon NameID { get; private set; }
    [field: SerializeField] public WeaponType Type { get; private set; }
    [field: SerializeField] public Weapon HolderPrefabs { get; private set; }
    [field: SerializeField] public GameObject DropPrefabs { get; private set; }
    [field: SerializeField] public Sprite Icon { get; private set; }
    [field: SerializeField] public bool CanDrop { get; private set; }
}
