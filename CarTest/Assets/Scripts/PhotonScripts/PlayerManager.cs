using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using PhotonScripts;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class PlayerManager : MonoBehaviour
{
    private PhotonView _photonView;
    private RaceManager _raceManager;
    public GameObject playerCar;
    public float nextFire;
    public float fireRate;

    private void Start()
    {
        _photonView = GetComponent<PhotonView>();
        _raceManager = FindObjectOfType<RaceManager>();

        if (_photonView.IsMine)
        {
            CreateController();
            nextFire = playerCar.GetComponentInChildren<PlayerGun>().NextFire;
            fireRate = playerCar.GetComponentInChildren<PlayerGun>().FireRate;
            _raceManager.reloadBar.maxValue = fireRate;
            _raceManager.reloadBar.value = nextFire;
        }
    }

    private void CreateController()
    {
        int i = Array.IndexOf(PhotonNetwork.PlayerList, PhotonNetwork.LocalPlayer);
        playerCar = PhotonNetwork.Instantiate(Path.Combine("PhotonPrefabs", "PlayerCar"),
            _raceManager.startPositions[i].position, Quaternion.identity);

        if (playerCar != null)
        {
            playerCar.GetComponentInChildren<Camera>().enabled = true;
        }
    }
}
