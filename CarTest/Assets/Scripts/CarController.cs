using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using System.Linq;
using ExitGames.Client.Photon;
using Newtonsoft.Json.Serialization;
using Photon.Pun;
using Photon.Realtime;
using PhotonScripts;

public class CarController : MonoBehaviourPun
{
    private Rigidbody _rigidbody;

    private float _accel;
    private float _brake;
    private float _steering;

    [Header("Vehicle Settings")]
    public float EginePower = 250f;
    public float BrakeForce = 30000f;
    public float SteerAngle = 35f;

    [Header("Wheel Colliders")]
    public WheelCollider[] FrontWheelsCol;
    public WheelCollider[] RearWheelsCol;

    [Header("Wheel Transforms")]
    public Transform[] FrontWheelTrans;
    public Transform[] RearWheelTrans;
    public Vector3 COM; // center of mass
    [Header("Other Settings")]
    public Transform path;
    public List<Transform> nodes;
    public int currentNode = 0;

    [HideInInspector] public int passedNode = 0;
    [HideInInspector] public float wayDistance;
    [HideInInspector] public float time = 0f;

    public int currentLap = 1;
    public float nextFire;
    public int racePosition;
    private float _respawnCounter = 0;
    private float _respawnWait = 2f;
    private bool _isTime;
    private bool _isFirstRound = true;
    private bool _isRacePlaces;
    private bool _isSetPosition;
    private Vector3 _targetNode;
    private RaceManager _raceManager;
    public PhotonView photonView;
    private Rigidbody _rb;
    private DBManager _dbManager;
    private RoundTrigger _roundTrigger;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        photonView = GetComponent<PhotonView>();
        _rigidbody = GetComponent<Rigidbody>();
        _roundTrigger = FindObjectOfType<RoundTrigger>();
        _dbManager = FindObjectOfType<DBManager>();
        _dbManager.details = gameObject.GetComponentsInChildren<Detail>().ToList();

        _raceManager = FindObjectOfType<RaceManager>();
        _raceManager.players.Add(this);

        _rb.centerOfMass = COM;

        if (!photonView.IsMine)
        {
            Destroy(GetComponentInChildren<Camera>().gameObject);
            _raceManager.carObjects.Add(gameObject.GetComponent<CheckNode>()); // добавление другогих игрковов в локальный список игроков

            LoadCar();
        }

        if (photonView.IsMine)
        {
            _raceManager.carObjects.Add(gameObject.GetComponent<CheckNode>()); // добавление игрока в список игроков
            // _dbManager.LoadModifications();

            for (int i = 0; i < _dbManager.details.Count; i++) // применнеи цветов игрока без БД
            {
                var colors = PhotonNetwork.LocalPlayer.CustomProperties[$"{_dbManager.details[i].detailValue}"].ToString().Split();
                _dbManager.details[i].gameObject.GetComponent<Renderer>().material.color =
                        new Color(float.Parse(colors[0]),
                        float.Parse(colors[1]),
                        float.Parse(colors[2]));
                _dbManager.details[i].gameObject.GetComponent<Renderer>().material.
                        SetFloat("_Glossiness", float.Parse(colors[3]));
            }

            for (int i = 0; i < _dbManager.details.Count; i++)
            {
                var color = _dbManager.details[i].GetComponent<Renderer>().material.color;
                photonView.RPC(nameof(SetColor), RpcTarget.AllBuffered, color.r, color.g, color.b, i); // установка цветов деталей игрока и их синхронизация с другими игроками
            }

            LoadCar();
        }
    }

    private void Update()
    {
        // ShowPlaces();
        if (photonView.IsMine)
        {
            PositionControl();
            ReloadControl();
        }
    }

    private void OnEnable()
    {
        PhotonNetwork.NetworkingClient.EventReceived += NetworkingClientOnEventReceived;
    }

    private void OnDisable()
    {
        PhotonNetwork.NetworkingClient.EventReceived -= NetworkingClientOnEventReceived;
    }

    private void NetworkingClientOnEventReceived(EventData obj)
    {
        if (obj.Code == PhotonEvents.DESTROY_WALL_SEGMENT_EVENT) // синхронизация удаления фрагмента стены
        {
            object[] data = (object[])obj.CustomData;

            var walls = FindObjectsOfType<Wall>();

            foreach (var wall in walls)
            {
                if (wall.WallID == (int)data[0])
                {
                    Destroy(wall.gameObject);
                    break;
                }
            }
        }
        else if (obj.Code == PhotonEvents.PLAYER_SHOOT_EVENT) // синхронизация эффекта стрельбы
        {
            object[] data = (object[])obj.CustomData;

            float[] position = { (float)data[0], (float)data[1], (float)data[2] };
            float[] direction = { (float)data[3], (float)data[4], (float)data[5] };
            float range = (float)data[6];
            int mask = (int)data[7];
            int laserId = (int)data[8];

            RaycastHit hit;
            LineRenderer Laser = null;
            PlayerGun playerGun = null;
            Vector3 _postion = new Vector3(position[0], position[1], position[2]);
            Vector3 _direction = new Vector3(direction[0], direction[1], direction[2]);
            var lasers = FindObjectsOfType<Laser>();

            foreach (var laser in lasers)
            {
                if (laser.LaserID == laserId)
                {
                    Laser = laser.GetComponent<LineRenderer>();
                    playerGun = laser.GetComponentInParent<PlayerGun>();
                    break;
                }
            }

            if (Physics.Raycast(_postion, _direction, out hit, range, mask, QueryTriggerInteraction.Ignore))
            {
                StartCoroutine(playerGun!.ShotEffect());
                Laser.startColor = Color.red;
                Laser.endColor = Color.red;
                Laser.SetPosition(0, _postion);
                Laser.SetPosition(1, _postion + _direction * hit.distance);
            }
            else
            {
                StartCoroutine(playerGun!.ShotEffect());
                Laser.startColor = Color.red;
                Laser.endColor = Color.red;
                Laser.SetPosition(0, _postion);
                Laser.SetPosition(1, _postion + _direction * range);
            }
        }
        else if (obj.Code == PhotonEvents.WHEEL_UPDATE_EVENT) // синхронизация поворота и вращения колес
        {
            object[] data = (object[])obj.CustomData;

            Quaternion rot = new Quaternion((float)data[0], (float)data[1],
                                            (float)data[2], (float)data[3]);
            int viewId = (int)data[4];
            int i = (int)data[5];
            bool isFront = (bool)data[6];


            foreach (var playerCar in _raceManager.carObjects)
            {
                if (playerCar.GetComponent<CarController>() &&
                    playerCar.GetComponent<PhotonView>().ViewID == viewId)
                {
                    var car = playerCar.GetComponent<CarController>();
                    if (isFront)
                    {
                        car.FrontWheelTrans[i].rotation = rot;
                    }
                    else
                    {
                        car.RearWheelTrans[i].rotation = rot;
                    }
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Синхронизированное изменение цвета машины
    /// </summary>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    /// <param name="i"></param>
    [PunRPC]
    private void SetColor(float r, float g, float b, int i)
    {
        var _details = GetComponentsInChildren<Detail>();
        _details[i].GetComponent<Renderer>().material.color = new Color(r, g, b);
    }

    /// <summary>
    /// Уничтожение фрагмента стены
    /// </summary>
    /// <param name="wallId"></param>
    public void DestroyWall(int wallId)
    {
        var walls = FindObjectsOfType<Wall>();

        foreach (var wall in walls)
        {
            if (wall.WallID == wallId)
            {
                Destroy(wall.gameObject);
                break;
            }
        }

        object[] data = { wallId };

        PhotonNetwork.RaiseEvent(PhotonEvents.DESTROY_WALL_SEGMENT_EVENT, data, RaiseEventOptions.Default, SendOptions.SendUnreliable);
    }

    // Создание стен
    public void SpawnWall()
    {
        if (_roundTrigger.walls.Count > 0)
        {
            foreach (GameObject wall in _roundTrigger.walls)
            {
                Destroy(wall);
            }
            _roundTrigger.walls = new List<GameObject>();
        }

        int i = 1;
        foreach (Transform wall in _roundTrigger.wallSpawns)
        {
            int j = 0;
            var wallObj = Instantiate(_roundTrigger.wallPrefab, wall.localPosition,
                Quaternion.identity);
            wallObj.transform.rotation = wall.localRotation;
            wallObj.transform.position = wall.position;
            var walls = wallObj.GetComponentsInChildren<Wall>();
            foreach (var w in walls)
            {
                w.WallID = int.Parse($"{i}{j}");
                j++;
            }
            _roundTrigger.walls.Add(wallObj);
            i++;
        }
    }

    /// <summary>
    /// Определение текущей позиции в гонке
    /// </summary>
    private void PositionControl()
    {
        racePosition = _raceManager.carObjects.IndexOf(GetComponent<CheckNode>()) + 1;
        _raceManager.racePostotionsText.text = $"Позиция: {racePosition}/{_raceManager.carCount}";
        _raceManager.lapText.text = $"Круг: {currentLap}/{_raceManager.totalRounds}";

        // for (int i = 0; i < raceManager.carCount; i++)
        // {
        //     var ts = System.TimeSpan.FromSeconds(raceManager.carObjects[i].time);
        //     raceManager.racePlacesText.text += $"{(i + 1)}\n";
        //     raceManager.raceNameText.text += $"{raceManager.carObjects[i].name}\n";
        //     raceManager.racePlacesTimeText.text += $"{ts.Minutes}:{ts.Seconds}:{ts.Milliseconds:f2}\n";
        // }
    }

    /// <summary>
    /// Изменение цвета слайдера перезарядки
    /// </summary>
    private void ReloadControl()
    {
        nextFire = GetComponentInChildren<PlayerGun>().NextFire;
        if (nextFire >= 3)
        {
            _raceManager.reloadBar.transform.GetChild(1).GetComponentInChildren<Image>().color = Color.red;
        }
        else
        {
            _raceManager.reloadBar.transform.GetChild(1).GetComponentInChildren<Image>().color = Color.white;
        }
        _raceManager.reloadBar.value = nextFire;
    }

    private void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            Movement();
            wayDistance = Vector3.Distance(transform.position, nodes[currentNode].position);
        }
        else
        {
            wayDistance = Vector3.Distance(transform.position, nodes[currentNode].position);
        }
    }

    /// <summary>
    /// Движение игрока
    /// </summary>
    private void Movement()
    {
        _accel = Input.GetAxis("Vertical");
        _brake = Input.GetAxis("Brake");
        _steering = Input.GetAxis("Horizontal");

        CarMove();
        Respawn();

        if (_isTime)
            time += Time.deltaTime;
    }

    private void ShowPlaces()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            _isRacePlaces = !_isRacePlaces;
            for (int i = 0; i < _raceManager.carCount; i++)
            {
                var ts = TimeSpan.FromSeconds(_raceManager.carObjects[i].time);
                _raceManager.racePlacesText.text += $"{(i + 1)}\n";
                _raceManager.raceNameText.text += $"{_raceManager.carObjects[i].name}\n";
                _raceManager.racePlacesTimeText.text += $"{ts.Minutes}:{ts.Seconds}:{ts.Milliseconds:f2}\n";
            }
            if (_isRacePlaces)
                _raceManager.racePlacesPanel.SetActive(true);
            else
                _raceManager.racePlacesPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Определение клавиш и активация событий движений
    /// </summary>
    private void CarMove()
    {
        if (Input.GetKey(KeyCode.W))
        {
            EventHandler.CarMoveEvent.AddListener(MoveForward);
            EventHandler.CarMoveEvent.Invoke();
            EventHandler.CarMoveEvent.RemoveAllListeners();
        }
        if (Input.GetKey(KeyCode.A))
        {
            EventHandler.CarMoveEvent.AddListener(MoveLeft);
            EventHandler.CarMoveEvent.Invoke();
            EventHandler.CarMoveEvent.RemoveAllListeners();
        }
        if (Input.GetKey(KeyCode.D))
        {
            EventHandler.CarMoveEvent.AddListener(MoveRight);
            EventHandler.CarMoveEvent.Invoke();
            EventHandler.CarMoveEvent.RemoveAllListeners();
        }
        if (Input.GetKey(KeyCode.S))
        {
            EventHandler.CarMoveEvent.AddListener(MoveBack);
            EventHandler.CarMoveEvent.Invoke();
            EventHandler.CarMoveEvent.RemoveAllListeners();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            EventHandler.CarMoveEvent.AddListener(StopMove);
            EventHandler.CarMoveEvent.Invoke();
            EventHandler.CarMoveEvent.RemoveAllListeners();
        }
    }

    /// <summary>
    /// Респавн игрока
    /// </summary>
    private void Respawn()
    {
        if (Input.GetKey(KeyCode.R) && _rigidbody.velocity.magnitude < 2)
        {
            _respawnCounter += Time.deltaTime;
            if (_respawnCounter >= _respawnWait)
            {
                if (currentNode == 0)
                {
                    transform.position = new Vector3(nodes[nodes.Count - 1].position.x, 0.5f, nodes[nodes.Count - 1].position.z);
                    _targetNode = new Vector3(nodes[0].position.x, nodes[0].position.y, nodes[0].position.z) - transform.position;
                    transform.rotation = Quaternion.LookRotation(_targetNode, Vector3.up);
                }
                else
                {
                    transform.position = new Vector3(nodes[currentNode - 1].position.x, 0.5f, nodes[currentNode - 1].position.z);
                    _targetNode = new Vector3(nodes[currentNode].position.x, nodes[currentNode].position.y, nodes[currentNode].position.z) - transform.position;
                    transform.rotation = Quaternion.LookRotation(_targetNode, Vector3.up);
                }
                StartCoroutine(Transparent());
                _respawnCounter = 0;
            }
        }
        else
            _respawnCounter = 0;
    }

    /// <summary>
    /// Эффект прозрачности игрока
    /// </summary>
    /// <returns></returns>
    IEnumerator Transparent()
    {
        Shader transparentShader = Shader.Find("Transparent/Diffuse");
        Shader standartShader = Shader.Find("Standard");
        var renders = GetComponentsInChildren<Renderer>();

        foreach (var rend in renders)
        {
            if (rend.gameObject.name == "Line")
                continue;
            rend.material.shader = transparentShader;
            Color color = rend.material.color;
            color.a = 0.2f;
            rend.material.color = color;
        }

        foreach (Transform obj in GetComponentsInChildren<Transform>())
            obj.gameObject.layer = 7;

        transform.gameObject.layer = 7;

        yield return new WaitForSeconds(5f);

        foreach (Transform obj in GetComponentsInChildren<Transform>())
            obj.gameObject.layer = 3;

        transform.gameObject.layer = 3;

        foreach (var rend in renders)
        {
            if (rend.gameObject.GetComponent<LineRenderer>())
                continue;
            rend.material.shader = standartShader;
            Color color = rend.material.color;
            color.a = 1;
            rend.material.color = color;
        }
    }

    /// <summary>
    /// Загрузка доп данных после создания игрока
    /// </summary>
    private void LoadCar()
    {
        if (GetComponentInChildren<Camera>().enabled || !photonView.IsMine)
        {
            Scene scene = SceneManager.GetSceneAt(0);
            var sceneObjects = scene.GetRootGameObjects();
            foreach (GameObject item in sceneObjects)
            {
                if (item.name == "Checkpoints")
                {
                    path = item.GetComponent<Transform>();
                    Transform[] pathTransforms = path.GetComponentsInChildren<Transform>();
                    nodes = new List<Transform>();

                    for (int i = 0; i < pathTransforms.Length; i++)
                    {
                        if (pathTransforms[i] != path.transform)
                        {
                            nodes.Add(pathTransforms[i]);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Определенние текущего чепоинта
    /// </summary>
    public void CheckWaypoint()
    {
        if (currentNode == nodes.Count - 1)
        {
            currentNode = 0;
        }
        else
        {
            currentNode++;
        }
        passedNode++;
    }

    /// <summary>
    /// Счетчик кругов
    /// </summary>
    public void LapCount()
    {
        if (!_isFirstRound)
        {
            if (currentLap == _raceManager.totalRounds)
            {
                //raceManager.finishText.gameObject.SetActive(true);
                //raceManager.finishText.text = $"Гонка закончена. Вы {raceManager.racePosition}!";
                Debug.Log("Гонка завершена!");
                return;
            }
            currentLap++;
        }
        time = 0f;
        _isTime = true;
        _isFirstRound = false;
    }

    /// <summary>
    /// Обновление вращения колес игрока
    /// </summary>
    private void UpdateAllWheelPose()
    {
        UpdateWheelPose(FrontWheelsCol[0], FrontWheelTrans[0], 0, true);
        UpdateWheelPose(FrontWheelsCol[1], FrontWheelTrans[1], 1, true);
        UpdateWheelPose(RearWheelsCol[0], RearWheelTrans[0], 0, false);
        UpdateWheelPose(RearWheelsCol[1], RearWheelTrans[1], 1, false);
        UpdateWheelPose(RearWheelsCol[2], RearWheelTrans[2], 2, false);
        UpdateWheelPose(RearWheelsCol[3], RearWheelTrans[3], 3, false);
    }

    /// <summary>
    /// Обновление вращения колеса
    /// </summary>
    /// <param name="wheelCollider"></param>
    /// <param name="wheelTransform"></param>
    /// <param name="i"></param>
    /// <param name="isFront"></param>
    private void UpdateWheelPose(WheelCollider wheelCollider, Transform wheelTransform, int i, bool isFront)
    {
        Vector3 pos;
        Quaternion rot = wheelTransform.rotation;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.rotation = rot;

        object[] data = { rot.x, rot.y, rot.z, rot.w, photonView.ViewID, i, isFront };

        PhotonNetwork.RaiseEvent(PhotonEvents.WHEEL_UPDATE_EVENT, data, RaiseEventOptions.Default,
            SendOptions.SendUnreliable);
    }

    public void MoveForward()
    {
        foreach (WheelCollider wheel in FrontWheelsCol)
        {
            wheel.motorTorque = EginePower * _accel * Time.deltaTime * 10;
            wheel.brakeTorque = BrakeForce * _brake;
            wheel.steerAngle = SteerAngle * _steering;
        }
        foreach (WheelCollider wheel in RearWheelsCol)
        {
            wheel.motorTorque = EginePower * _accel;
            wheel.brakeTorque = BrakeForce * _brake;
        }
        UpdateAllWheelPose();
    }
    public void MoveRight()
    {
        foreach (WheelCollider wheel in FrontWheelsCol)
        {
            wheel.motorTorque = EginePower * _accel * Time.deltaTime * 10;
            wheel.brakeTorque = BrakeForce * _brake;
            wheel.steerAngle = SteerAngle * _steering;
        }
        foreach (WheelCollider wheel in RearWheelsCol)
        {
            wheel.motorTorque = EginePower * _accel * Time.deltaTime * 10;
            wheel.brakeTorque = BrakeForce * _brake;
        }
        UpdateAllWheelPose();
    }
    public void MoveLeft()
    {
        foreach (WheelCollider wheel in FrontWheelsCol)
        {
            wheel.motorTorque = EginePower * _accel;
            wheel.brakeTorque = BrakeForce * _brake;
            wheel.steerAngle = SteerAngle * _steering;
        }
        foreach (WheelCollider wheel in RearWheelsCol)
        {
            wheel.motorTorque = EginePower * _accel;
            wheel.brakeTorque = BrakeForce * _brake;
        }
        UpdateAllWheelPose();
    }
    public void MoveBack()
    {
        foreach (WheelCollider wheel in FrontWheelsCol)
        {
            wheel.motorTorque = EginePower * _accel;
            wheel.brakeTorque = BrakeForce * _brake;
            wheel.steerAngle = SteerAngle * _steering;
        }
        foreach (WheelCollider wheel in RearWheelsCol)
        {
            wheel.motorTorque = EginePower * _accel;
            wheel.brakeTorque = BrakeForce * _brake;
        }
        UpdateAllWheelPose();
    }
    public void StopMove()
    {
        foreach (WheelCollider wheel in FrontWheelsCol)
        {
            wheel.motorTorque = EginePower * _accel;
            wheel.brakeTorque = BrakeForce * _brake;
            wheel.steerAngle = SteerAngle * _steering;
        }
        foreach (WheelCollider wheel in RearWheelsCol)
        {
            wheel.motorTorque = EginePower * _accel;
            wheel.brakeTorque = BrakeForce * _brake;
        }
        UpdateAllWheelPose();
    }
}
