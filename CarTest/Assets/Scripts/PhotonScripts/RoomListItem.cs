using System.Collections;
using System.Collections.Generic;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class RoomListItem : MonoBehaviour
{
    [SerializeField] private Text _roomName;

    public RoomInfo info;
    public void SetUp(RoomInfo roomInfo)
    {
        info = roomInfo;
        _roomName.text = info.Name;
    }

    public void OnClick()
    {
        Launcher.instance.JoinRoom(info);
    }
}
