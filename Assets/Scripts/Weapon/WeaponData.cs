using UnityEngine;
public enum WeaponType
{
    main, pistol, knife, granad, bomb
}
[CreateAssetMenu(menuName = "Weapon/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public int price;
    public string name;
    public Sprite gunImage;
    public GameObject drop;
    public GameObject holder;
}
