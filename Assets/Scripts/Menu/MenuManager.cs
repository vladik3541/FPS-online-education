using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public static MenuManager instance;
    [SerializeField] private Menu[] menus;

    private void Awake()
    {
        instance = this;
    }

    public void OpenMenu(string menuName)
    {
        foreach (Menu menu in menus)
        {
            if (menu.menuName == menuName)
            {
                menu.OpenMenu();
            }
            else if (menu.isOpen)
            {
                menu.OpenMenu();
            }
            else
            {
                menu.CloseMenu();
            }
        }
    }
    public void CloseMenu(string menuName)
    {
        foreach (Menu menu in menus)
        {
            if (menu.menuName == menuName)
            {
                menu.CloseMenu();
            }
            else if (!menu.isOpen)
            {
                menu.CloseMenu();
            }
            else
            {
                menu.OpenMenu();
            }
        }
    }
    public void OpenMenu(Menu menu)
    {
        menu.OpenMenu();
    }
    public void CloseMenu(Menu menu)
    {
        menu.CloseMenu();
    }
}
