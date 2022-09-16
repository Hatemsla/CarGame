using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Menu : MonoBehaviour
{
    public string menuName;
    
    /// <summary>
    /// Активация меню
    /// </summary>
    public void Open()
    {
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// Отключние меню
    /// </summary>
    public void Close()
    {
        gameObject.SetActive(false);
    }
}
