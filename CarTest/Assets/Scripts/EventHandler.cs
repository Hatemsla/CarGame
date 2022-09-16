using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EventHandler : MonoBehaviour
{
    public static UnityEvent CarMoveEvent;
    public static UnityEvent BoxMoveEvent;
    public static UnityEvent RespawnCarEvent;
    UnityEvent carChangeEvent;

    private void Start()
    {
        if (CarMoveEvent == null)
            CarMoveEvent = new UnityEvent();

        if (RespawnCarEvent == null)
            RespawnCarEvent = new UnityEvent();

        if (BoxMoveEvent == null)
            BoxMoveEvent = new UnityEvent();
    }
}
