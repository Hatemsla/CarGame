using System.Collections;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using PhotonScripts;
using UnityEngine;

public class PlayerGun : Gun
{
    public override float FireRate => 3f;
    public override float Range => 100f;
    public override WaitForSeconds LaserDuration => new WaitForSeconds(0.05f);

    public override Transform ShootPoint { get; set; }
    public override LineRenderer Laser { get; set; }
    public override float NextFire { get; set; }
    private LayerMask _mask = -1;
    private PhotonView _photonView;
    private CarController _carController;
    private Laser _laserInfo;
    private float[] _position;
    private float[] _direction;

    void Start()
    {
        Laser = GetComponentInChildren<LineRenderer>();
        ShootPoint = transform.GetChild(0).transform;
        NextFire = FireRate;

        _photonView = GetComponentInParent<PhotonView>();
        _carController = GetComponentInParent<CarController>();
        _laserInfo = GetComponentInChildren<Laser>();
    }

    void Update()
    {
        if (NextFire <= FireRate)
            NextFire += Time.deltaTime;

        if (_photonView.IsMine && Input.GetKeyDown(KeyCode.Q) && NextFire >= FireRate)
        {
            _position = new[] { ShootPoint.position.x, ShootPoint.position.y, ShootPoint.position.z }; // сохраненеие позиций лазера
            _direction = new[] { ShootPoint.forward.x, ShootPoint.forward.y, ShootPoint.forward.z };
            Shoot(_position, _direction, Range, _mask);
        }
    }

    /// <summary>
    /// Стрельба игрока с отрисовкой лазера
    /// </summary>
    /// <param name="position">Позиция лазера</param>
    /// <param name="direction">Направлнеие лазера</param>
    /// <param name="range">Дистанция лазера</param>
    /// <param name="mask">Маска лазера</param>
    public void Shoot(float[] position, float[] direction, float range, int mask)
    {
        NextFire = 0;
        RaycastHit hit;

        Vector3 _position = new Vector3(position[0], position[1], position[2]);
        Vector3 _direction = new Vector3(direction[0], direction[1], direction[2]);

        if (Physics.Raycast(_position, _direction, out hit, range, mask, QueryTriggerInteraction.Ignore)) // если есть поподание
        {
            StartCoroutine(ShotEffect()); // эффект лазера
            Laser.startColor = Color.red;
            Laser.endColor = Color.red;
            Laser.SetPosition(0, _position); // начальная точка лазера
            Laser.SetPosition(1, _position + _direction * hit.distance); // конечная точка
            if (hit.transform.CompareTag("Wall"))
            {
                _carController.DestroyWall(hit.transform.GetComponent<Wall>().WallID); // уничтожение стены
            }

            if (hit.transform.gameObject.layer == 3) // если игрок не прозрачный
            {
                var car = hit.transform.GetComponentInParent<Rigidbody>();
                StartCoroutine(SlowCar(car)); // замедление машины
            }
        }
        else
        {
            StartCoroutine(ShotEffect());
            Laser.startColor = Color.red;
            Laser.endColor = Color.red;
            Laser.SetPosition(0, _position); // начальная точка лазера
            Laser.SetPosition(1, _position + _direction * range); // конечная точка
        }

        object[] data = { _position[0], _position[1], _position[2], _direction[0], _direction[1], _direction[2], range, mask, _laserInfo.LaserID };

        PhotonNetwork.RaiseEvent(PhotonEvents.PLAYER_SHOOT_EVENT, data, RaiseEventOptions.Default, SendOptions.SendUnreliable); // отправка данных об лазере другим игрокам
    }

    /// <summary>
    /// Эффект лазера
    /// </summary>
    /// <returns></returns>
    public override IEnumerator ShotEffect()
    {
        Laser.enabled = true;
        yield return LaserDuration;
        Laser.enabled = false;
    }

    /// <summary>
    /// Замедление машины
    /// </summary>
    /// <param name="car"></param>
    /// <returns></returns>
    protected override IEnumerator SlowCar(Rigidbody car)
    {
        car.drag = 1f;
        if (car.gameObject.GetComponent<CarEngine>())
            car.gameObject.GetComponent<CarEngine>().isKnocked = true;

        yield return new WaitForSeconds(2f);

        if (car.gameObject.GetComponent<CarEngine>())
            car.gameObject.GetComponent<CarEngine>().isKnocked = false;
        car.drag = 0;
    }
}
