using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class Launcher : MonoBehaviourPunCallbacks
{
    public static Launcher instance;
    public Text remainingTimeText;

    [SerializeField] private RoomTimer _roomTimer;
    [SerializeField] private InputField _roomInputField;
    [SerializeField] private Text _roomNameText;
    [SerializeField] private Text _currentPlayersCountText;
    [SerializeField] private Transform _roomList;
    [SerializeField] private GameObject _roomButtonPrefab;
    [SerializeField] private Transform _playerList;
    [SerializeField] private GameObject _playerNamePrefab;
    [SerializeField] private GameObject _startGameButton;
    private DBManager _dbManager;
    private Color _startGameButtonColor;
    private bool _isNotLeave = true;

    void Start()
    {
        _dbManager = FindObjectOfType<DBManager>();
        _startGameButtonColor = _startGameButton.GetComponent<Image>().material.color;

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
            if (PhotonNetwork.PlayerList.Length >= 1)
            {
                _startGameButton.GetComponent<Button>().enabled = true;
                _startGameButtonColor = new Color(1f, 1f, 1f);
            }
            else
            {
                _startGameButton.GetComponent<Button>().enabled = false;
                _startGameButtonColor = new Color(0.7f, 0.7f, 0.7f);
            }

            Hashtable ht = PhotonNetwork.CurrentRoom.CustomProperties;
            ht.Remove("timer");
            ht.Add("timer", _roomTimer.timeRemaining);
            PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
        }

        if (_roomTimer.timeRemaining <= 110 && _isNotLeave)
        {
            LeaveRoom();
            _isNotLeave = !_isNotLeave;
        }
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    public override void OnJoinedLobby()
    {
        MenuManager.instance.OpenMenu("Title");
        PhotonNetwork.NickName = "Player " + Random.Range(0, 1000);
        _dbManager.playerName = PhotonNetwork.NickName;
        _dbManager.playerId = int.Parse(_dbManager.playerName.Substring(7));
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
            PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
        }
        else
        {
            _roomTimer.timeRemaining = float.Parse(PhotonNetwork.CurrentRoom.CustomProperties["timer"].ToString());
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
        Debug.Log($"The {newPlayer.NickName} is connected");

        remainingTimeText.text = PhotonNetwork.CurrentRoom.CustomProperties["timer"].ToString();
    }

    public void StartGame()
    {
        PhotonNetwork.LoadLevel(1);
    }
}
