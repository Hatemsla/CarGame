using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

public class MenuManager : MonoBehaviourPunCallbacks
{
    public static MenuManager instance;

    [SerializeField] private List<Menu> _menus;
    public MouseDetect _mouseDetect;

    private void Start()
    {
        _mouseDetect = FindObjectOfType<MouseDetect>();
    }

    /// <summary>
    /// Открытие выбранного меню
    /// </summary>
    /// <param name="menuName"></param>
    public void OpenMenu(string menuName)
    {
        foreach (Menu menu in _menus)
        {
            if (menu.menuName == menuName)
            {
                menu.Open();
                var mouseDetectEnabled = menu.menuName == "Room" ? _mouseDetect.enabled = true : _mouseDetect.enabled = false; // определение открыто ли окно Room и включение и отключение камеры
            }
            else
            {
                menu.Close();
            }
        }
    }

    private void Awake()
    {
        instance = this;
    }
}
