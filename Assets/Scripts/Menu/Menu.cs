using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    public string menuName;
    public bool isOpen;
    public void OpenMenu()
    {
        gameObject.SetActive(true);
        isOpen = true;
    }

    public void CloseMenu()
    {
        gameObject.SetActive(false);
        isOpen = false;
    }
}
