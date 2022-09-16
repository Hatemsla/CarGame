using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class Laser : MonoBehaviour
{
    public int LaserID;

    private void Start()
    {
        LaserID = GetComponentInParent<PhotonView>().ViewID;
    }
}
