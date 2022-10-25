using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;
using System;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher instance;
    public Text remainingTimeText;

    [SerializeField] private RoomTimer _roomTimer;
    [SerializeField] private InputField _roomInputField;
    [SerializeField] private InputField _authLoginInputField;
    [SerializeField] private InputField _authPasswordInputField;
    [SerializeField] private InputField _regLoginInputField;
    [SerializeField] private InputField _regPasswordInputField;
    [SerializeField] private Text _roomNameText;
    [SerializeField] private Text _currentPlayersCountText;
    [SerializeField] private GameObject _loginAlert;
    [SerializeField] private GameObject _regAlert;
    [SerializeField] private Transform _roomList;
    [SerializeField] private GameObject _roomButtonPrefab;
    [SerializeField] private Transform _playerList;
    [SerializeField] private GameObject _playerNamePrefab;
    [SerializeField] private GameObject _startGameButton;

    private DBManager _dbManager;
    private int _minPlayersCount = 2;
    private bool _isNotLoad = true;
    private bool _isNotLeave = true;
    private bool _isInAcc;
    private bool _isNotRoomReady = true;
    private bool _isChangeButtonColor;

    private void Start()
    {
        _dbManager = FindObjectOfType<DBManager>();

        instance = this;
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.EnableCloseConnection = true;
        MenuManager.instance.OpenMenu("Loading");
    }

    private void Update()
    {
        _currentPlayersCountText.text = PhotonNetwork.PlayerList.Length.ToString();


        if (PhotonNetwork.IsMasterClient && PhotonNetwork.IsConnectedAndReady)
        {
            if (PhotonNetwork.PlayerList.Length == 3) //комната не загружается сама
            {
                if (_isNotLoad)
                {
                    _isNotLeave = false;
                    PhotonNetwork.LoadLevel(1);
                }
            }
            else if (_roomTimer.timeRemaining <= 0 && PhotonNetwork.PlayerList.Length >= _minPlayersCount)
            {
                if (_isNotLoad)
                {
                    _isNotLeave = false;
                    PhotonNetwork.LoadLevel(1);
                }
            }
            else if (_isNotLeave && _roomTimer.timeRemaining <= 0 && PhotonNetwork.PlayerList.Length < _minPlayersCount)
            {
                LeaveRoom();
                _isNotLeave = !_isNotLeave;
            }

            Hashtable ht = PhotonNetwork.CurrentRoom.CustomProperties;
            ht["timer"] = _roomTimer.timeRemaining;
            PhotonNetwork.LocalPlayer.SetCustomProperties(ht);
        }
    }

    public void Login()
    {
        if (_dbManager.CheckPlayerExisting(_authLoginInputField.text, _authPasswordInputField.text))
        {
            _dbManager.playerLogin = _authLoginInputField.text;
            _dbManager.playerPassword = _authPasswordInputField.text;
            _isInAcc = true;
            PhotonNetwork.NickName = _dbManager.playerLogin;
            MenuManager.instance.OpenMenu("Title");
        }
        else
        {
            _loginAlert.SetActive(true);
        }
    }

    public void Registration()
    {
        if (!_dbManager.CheckPlayerExisting(_regLoginInputField.text, _regPasswordInputField.text))
        {
            _dbManager.Registrartion(_regLoginInputField.text, _regPasswordInputField.text);
            _dbManager.playerLogin = _regLoginInputField.text;
            _dbManager.playerPassword = _regPasswordInputField.text;
            _isInAcc = true;
            PhotonNetwork.NickName = _dbManager.playerLogin;
            MenuManager.instance.OpenMenu("Title");
        }
        else
        {
            _regAlert.SetActive(true);
        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby()
    {
        if (_isInAcc)
            MenuManager.instance.OpenMenu("Title");
        else
            MenuManager.instance.OpenMenu("Auth");

        PhotonNetwork.NickName = _dbManager.playerLogin;
    }

    public void CreateRoom()
    {
        if (string.IsNullOrEmpty(_roomInputField.text))
            return;

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.MaxPlayers = 10;

        PhotonNetwork.CreateRoom(_roomInputField.text, roomOptions);
        MenuManager.instance.OpenMenu("Loading");
    }

    public override void OnJoinedRoom()
    {
        _roomNameText.text = "Комната: " + PhotonNetwork.CurrentRoom.Name;
        MenuManager.instance.OpenMenu("Room");

        Player[] players = PhotonNetwork.PlayerList;

        for (int i = 0; i < _playerList.childCount; i++)
        {
            Destroy(_playerList.GetChild(i).gameObject);
        }

        for (int i = 0; i < players.Length; i++)
        {
            Instantiate(_playerNamePrefab, _playerList).GetComponent<PlayerListItem>().SetUp(players[i]);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable ht = new Hashtable();
            ht.Add("timer", _roomTimer.timeRemaining);
            PhotonNetwork.LocalPlayer.SetCustomProperties(ht);
        }
        else
        {
            //_roomTimer.timeRemaining = float.Parse(PhotonNetwork.CurrentRoom.CustomProperties["timer"].ToString());
            foreach (var player in PhotonNetwork.PlayerList)
            {
                if (player.IsMasterClient)
                {
                    _roomTimer.timeRemaining = Convert.ToSingle(player.CustomProperties["timer"]);
                }
            }
        }

        _startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        _startGameButton.SetActive(PhotonNetwork.IsMasterClient);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
        MenuManager.instance.OpenMenu("Loading");
    }

    public override void OnLeftRoom()
    {
        MenuManager.instance.OpenMenu("Title");
        _roomTimer.timeRemaining = 120;
    }

    public void JoinRoom(RoomInfo roomInfo)
    {
        PhotonNetwork.JoinRoom(roomInfo.Name);
        MenuManager.instance.OpenMenu("Loading");
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        for (int i = 0; i < _roomList.childCount; i++)
        {
            Destroy(_roomList.GetChild(i).gameObject);
        }
        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].RemovedFromList)
                continue;

            Instantiate(_roomButtonPrefab, _roomList).GetComponent<RoomListItem>().SetUp(roomList[i]);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Instantiate(_playerNamePrefab, _playerList).GetComponent<PlayerListItem>().SetUp(newPlayer);
    }

    public void StartGame()
    {
        PhotonNetwork.LoadLevel(1);
    }
}
