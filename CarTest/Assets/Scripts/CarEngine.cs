using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarEngine : MonoBehaviour
{
    public Transform path;
    public float maxSteerAngle = 45f;
    public float turnSpeed = 5f;
    public float maxMotorTorque = 250f;
    public float maxBrakeTorque = 30000f;
    public float currentSpeed;
    public float maxSpeed = 150f;
    public float avoidSpeed = 30f;
    public WheelCollider[] FrontWheelsCol;
    public WheelCollider[] RearWheelsCol;
    public Transform[] FrontWheelTrans;
    public Transform[] RearWheelTrans;
    public Vector3 COM;
    public bool isBraking = false;
    public int totalLaps = 5;
    public float respawnWait = 5;
    public float respawnCounter = 0f;
    public float wayDistance;

    [Header("Sensors")]
    public float sensorLength = 10f;
    public Vector3 frontSensorPosition = new Vector3(0, 0.4f, 2.2f);
    public float frontSideSensorPosition = 1.1f;
    public float frontSensorAngle = 30f;
    public float sideSensorLength = 3f;

    public List<Transform> nodes;
    public int currentNode;
    public int passedNode;
    private bool _avoiding;
    private float _targetSteerAngle;
    private bool _reversing;
    private float _reversCounter;
    private float _waitToReverse = 2.0f;
    private float _reversFor = 1.5f;
    [HideInInspector] public int currentLap = 1;
    [HideInInspector] public float time = 0f;
    private bool _isTime;
    private bool _isFirstRound = true;
    [HideInInspector] public bool isKnocked = false;
    private Rigidbody _rb;
    private readonly LayerMask _transparentLayer = 7;
    private Vector3 _targetNode;


    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.centerOfMass = COM;
        Transform[] pathTransforms = path.GetComponentsInChildren<Transform>();
        nodes = new List<Transform>();

        for (int i = 0; i < pathTransforms.Length; i++) // добавление чекпоинтов в список
        {
            if (pathTransforms[i] != path.transform)
            {
                nodes.Add(pathTransforms[i]);
            }
        }
    }

    void FixedUpdate()
    {
        Sensors();
        ApplySteer();
        Drive();
        CheckWaypointDistance();
        UpdateAllWheelPose();
        Braking();
        LerpToSteerAngle();
        Respawn();

        if (_isTime) // если проехан старт
        {
            time += Time.deltaTime;
        }
    }

    /// <summary>
    /// Респанв бота на текущем чекпоинте
    /// </summary>
    void Respawn()
    {
        if (_rb.velocity.magnitude < 2 && !isKnocked)
        {
            respawnCounter += Time.deltaTime;
            if (respawnCounter >= respawnWait)
            {
                if (currentNode == 0)
                {
                    transform.position = nodes[nodes.Count - 1].position;
                    _targetNode = new Vector3(nodes[0].position.x, transform.position.y, nodes[0].position.z) - transform.position;
                    transform.rotation = Quaternion.LookRotation(_targetNode, Vector3.up);
                }
                else
                {
                    transform.position = nodes[currentNode - 1].position;
                    _targetNode = new Vector3(nodes[currentNode].position.x, transform.position.y, nodes[currentNode].position.z) - transform.position;
                    transform.rotation = Quaternion.LookRotation(_targetNode, Vector3.up);
                }
                StartCoroutine(Transparent());
                respawnCounter = 0;
                _reversCounter = 0;
                _reversing = false;
            }
        }
    }

    /// <summary>
    /// Счетчик кругов
    /// </summary>
    public void LapCount()
    {
        if (!_isFirstRound)
        {
            if (currentLap == totalLaps)
            {
                Debug.Log("Вы победили!");
                currentLap = 1;
            }
            currentLap++;
        }
        time = 0f;
        _isTime = true;
        _isFirstRound = false;
    }

    /// <summary>
    /// Плавный поворот
    /// </summary>
    private void LerpToSteerAngle()
    {
        FrontWheelsCol[0].steerAngle = Mathf.Lerp(FrontWheelsCol[0].steerAngle, _targetSteerAngle, Time.deltaTime * turnSpeed);
        FrontWheelsCol[1].steerAngle = Mathf.Lerp(FrontWheelsCol[1].steerAngle, _targetSteerAngle, Time.deltaTime * turnSpeed);
    }

    /// <summary>
    /// Управление сенсорами
    /// </summary>
    private void Sensors()
    {
        RaycastHit hit;
        Vector3 sensorFrontStartPos = transform.position;
        sensorFrontStartPos += transform.forward * frontSensorPosition.z;
        sensorFrontStartPos += transform.up * frontSensorPosition.y;
        float avoidMultiplier = 0;
        _avoiding = false;

        //braking sensor
        if (Physics.Raycast(sensorFrontStartPos, transform.forward, out hit, sensorLength))
        {
            if (!hit.transform.CompareTag("Terrain") && hit.transform.gameObject.layer != _transparentLayer)
            {
                Debug.DrawLine(sensorFrontStartPos, hit.point, Color.red);
                _avoiding = true;
                if (hit.distance < 4)
                {
                    foreach (WheelCollider wheel in FrontWheelsCol)
                    {
                        wheel.motorTorque = 0;
                    }
                    foreach (WheelCollider wheel in RearWheelsCol)
                    {
                        wheel.motorTorque = 0;
                    }
                }
                else
                {
                    foreach (WheelCollider wheel in FrontWheelsCol)
                    {
                        wheel.motorTorque = maxMotorTorque * Time.deltaTime * 10;
                    }
                    foreach (WheelCollider wheel in RearWheelsCol)
                    {
                        wheel.motorTorque = maxMotorTorque * Time.deltaTime * 10;
                    }
                }
            }
        }

        //front right sensor
        sensorFrontStartPos += transform.right * frontSideSensorPosition;
        if (Physics.Raycast(sensorFrontStartPos, transform.forward, out hit, sensorLength))
        {
            if (!hit.transform.CompareTag("Terrain") && hit.transform.gameObject.layer != _transparentLayer)
            {
                Debug.DrawLine(sensorFrontStartPos, hit.point);
                _avoiding = true;
                avoidMultiplier -= 1f;
            }
        }
        //front angle right sensor
        else if (Physics.Raycast(sensorFrontStartPos, Quaternion.AngleAxis(frontSensorAngle, transform.up) * transform.forward, out hit, sensorLength))
        {
            if (!hit.transform.CompareTag("Terrain") && hit.transform.gameObject.layer != _transparentLayer)
            {
                Debug.DrawLine(sensorFrontStartPos, hit.point);
                _avoiding = true;
                avoidMultiplier -= 0.5f;
            }
        }

        //side right sensor
        if (Physics.Raycast(sensorFrontStartPos, transform.right, out hit, sideSensorLength))
        {
            if (!hit.transform.CompareTag("Terrain") && hit.transform.gameObject.layer != _transparentLayer)
            {
                Debug.DrawLine(sensorFrontStartPos, hit.point);
                _avoiding = true;
                avoidMultiplier -= 0.5f;
            }
        }
        sensorFrontStartPos -= transform.right * frontSideSensorPosition * 2;

        //front left sensor
        if (Physics.Raycast(sensorFrontStartPos, transform.forward, out hit, sensorLength))
        {
            if (!hit.transform.CompareTag("Terrain") && hit.transform.gameObject.layer != _transparentLayer)
            {
                Debug.DrawLine(sensorFrontStartPos, hit.point);
                _avoiding = true;
                avoidMultiplier += 1f;
            }
        }
        //front angle left sensor
        else if (Physics.Raycast(sensorFrontStartPos, Quaternion.AngleAxis(-frontSensorAngle, transform.up) * transform.forward, out hit, sensorLength))
        {
            if (!hit.transform.CompareTag("Terrain") && hit.transform.gameObject.layer != _transparentLayer)
            {
                Debug.DrawLine(sensorFrontStartPos, hit.point);
                _avoiding = true;
                avoidMultiplier += 0.5f;
            }
        }

        //side left sensor
        if (Physics.Raycast(sensorFrontStartPos, -transform.right, out hit, sideSensorLength))
        {
            if (!hit.transform.CompareTag("Terrain") && hit.transform.gameObject.layer != _transparentLayer)
            {
                Debug.DrawLine(sensorFrontStartPos, hit.point);
                _avoiding = true;
                avoidMultiplier += 0.5f;
            }
        }

        //front senser
        if (avoidMultiplier == 0)
        {
            if (Physics.Raycast(sensorFrontStartPos, transform.forward, out hit, sensorLength))
            {
                if (!hit.transform.CompareTag("Terrain") && hit.transform.gameObject.layer != _transparentLayer)
                {
                    Debug.DrawLine(sensorFrontStartPos, hit.point);
                    _avoiding = true;
                    if (hit.normal.x < 0)
                    {
                        avoidMultiplier = -1;
                    }
                    else
                    {
                        avoidMultiplier = 1;
                    }
                }
            }
        }

        if (_rb.velocity.magnitude < 2 && !_reversing && !isKnocked)
        {
            _reversCounter += Time.deltaTime;
            if (_reversCounter >= _waitToReverse)
            {
                _reversCounter = 0;
                _reversing = true;
                isBraking = false;
            }
        }

        if (_reversing)
        {
            avoidMultiplier *= -1;
            _reversCounter += Time.deltaTime;
            if (_reversCounter >= _reversFor)
            {
                _reversCounter = 0;
                _reversing = false;
            }
        }

        if (_avoiding)
        {
            _targetSteerAngle = avoidSpeed * avoidMultiplier;
        }
    }

    /// <summary>
    /// Торможение
    /// </summary>
    void Braking()
    {
        if (isBraking)
        {
            foreach (WheelCollider wheel in FrontWheelsCol)
            {
                wheel.brakeTorque = maxBrakeTorque;
            }
            foreach (WheelCollider wheel in RearWheelsCol)
            {
                wheel.brakeTorque = maxBrakeTorque;
            }
        }
        else
        {
            foreach (WheelCollider wheel in FrontWheelsCol)
            {
                wheel.brakeTorque = 0;
            }
            foreach (WheelCollider wheel in RearWheelsCol)
            {
                wheel.brakeTorque = 0;
            }
        }
    }

    /// <summary>
    /// Обновление вращения всех колес
    /// </summary>
    void UpdateAllWheelPose()
    {
        UpdateWheelPose(FrontWheelsCol[0], FrontWheelTrans[0]);
        UpdateWheelPose(FrontWheelsCol[1], FrontWheelTrans[1]);
        UpdateWheelPose(RearWheelsCol[0], RearWheelTrans[0]);
        UpdateWheelPose(RearWheelsCol[1], RearWheelTrans[1]);
    }

    /// <summary>
    /// Вращение колеса
    /// </summary>
    /// <param name="wheelCollider"></param>
    /// <param name="wheelTransform"></param>
    void UpdateWheelPose(WheelCollider wheelCollider, Transform wheelTransform)
    {
        Vector3 pos = wheelTransform.position;
        Quaternion rot = wheelTransform.rotation;
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }

    /// <summary>
    /// Дистанция до следущего чекпоинта
    /// </summary>
    public void CheckWaypointDistance()
    {
        wayDistance = Vector3.Distance(transform.position, nodes[currentNode].position);
        if (Vector3.Distance(transform.position, nodes[currentNode].position) < 20f)
        {
            if (currentNode == nodes.Count - 1)
            {
                currentNode = 0;
            }
            else
            {
                currentNode++;
            }
        }
    }
    
    /// <summary>
    /// Движение бота
    /// </summary>
    void Drive()
    {
        currentSpeed = 2 * Mathf.PI * FrontWheelsCol[0].radius * FrontWheelsCol[0].rpm * 60 / 1000;

        if (currentSpeed < maxSpeed && !isBraking)
        {
            if (!_reversing)
            {
                foreach (WheelCollider wheel in FrontWheelsCol)
                {
                    wheel.motorTorque = maxMotorTorque * Time.deltaTime * 10;
                }
                foreach (WheelCollider wheel in RearWheelsCol)
                {
                    wheel.motorTorque = maxMotorTorque * Time.deltaTime * 10;
                }
            }
            else
            {
                foreach (WheelCollider wheel in FrontWheelsCol)
                {
                    wheel.motorTorque = -maxMotorTorque * Time.deltaTime * 10;
                }
                foreach (WheelCollider wheel in RearWheelsCol)
                {
                    wheel.motorTorque = -maxMotorTorque * Time.deltaTime * 10;
                }
            }
        }
        else
        {
            foreach (WheelCollider wheel in FrontWheelsCol)
            {
                wheel.motorTorque = 0;
            }
            foreach (WheelCollider wheel in RearWheelsCol)
            {
                wheel.motorTorque = 0;
            }
        }
    }

    /// <summary>
    /// Поворот бота
    /// </summary>
    void ApplySteer()
    {
        if (_avoiding) return;
        Vector3 relativeVector = transform.InverseTransformPoint(nodes[currentNode].position);
        float newSteer = relativeVector.x / relativeVector.magnitude * maxSteerAngle;
        _targetSteerAngle = newSteer;
    }

    /// <summary>
    /// Прозрачность бота
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
            if (rend.gameObject.name == "Line")
                continue;
            rend.material.shader = standartShader;
            Color color = rend.material.color;
            color.a = 1;
            rend.material.color = color;
        }
    }
}