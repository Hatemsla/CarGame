using System.Collections;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using PhotonScripts;
using UnityEngine;

public class AIGun : Gun
{
    public override float FireRate => 4f;
    public override float Range => 50f;
    public override WaitForSeconds LaserDuration => new WaitForSeconds(0.05f);

    public override Transform ShootPoint { get; set; }
    public override LineRenderer Laser { get; set; }
    public override float NextFire { get; set; }
    private RaycastHit _hit;
    private LayerMask _mask = -1;
    private PhotonView _photonView;
    private Laser _laserInfo;
    private float[] _position;
    private float[] _direction;

    void Start()
    {
        Laser = GetComponentInChildren<LineRenderer>();
        ShootPoint = transform.GetChild(0).transform;
        NextFire = FireRate;

        _photonView = GetComponentInParent<PhotonView>();
        _laserInfo = GetComponentInChildren<Laser>();
    }
    void Update()
    {
        if (NextFire <= FireRate)
            NextFire += Time.deltaTime;
        if (Physics.Raycast(ShootPoint.position, ShootPoint.forward, out _hit, Range) && NextFire >= FireRate)
        {
            _position = new[]{ShootPoint.position.x, ShootPoint.position.y, ShootPoint.position.z};
            _direction = new[]{ShootPoint.forward.x, ShootPoint.forward.y, ShootPoint.forward.z};
            if (_hit.transform.CompareTag("Wall"))
            {
                Shoot(_position, _direction, Range, _mask);
            }
            else if ((_hit.transform.CompareTag("Enemy") || _hit.transform.CompareTag("Player"))
                        && _hit.distance > 15f && _hit.transform.gameObject.layer == 3)
            {
                Shoot(_position, _direction, Range, _mask);
            }
        }
    }
    public void Shoot(float[] position, float[] direction, float range, int mask)
    {
        NextFire = 0;
        RaycastHit hit;
        
        Vector3 _postion = new Vector3(position[0], position[1], position[2]);
        Vector3 _direction = new Vector3(direction[0], direction[1], direction[2]);
        
        if (Physics.Raycast(_postion, _direction, out hit, range, mask, QueryTriggerInteraction.Ignore))
        {
            StartCoroutine(ShotEffect());
            Laser.startColor = Color.red;
            Laser.endColor = Color.red;
            Laser.SetPosition(0, ShootPoint.position);
            Laser.SetPosition(1, _postion + _direction * hit.distance);
            if (hit.transform.CompareTag("Wall"))
            {
                DestroyWall(hit.transform.GetComponent<Wall>().WallID);
            }
            if (hit.transform.CompareTag("Player") || hit.transform.CompareTag("Enemy"))
            {
                var car = hit.transform.GetComponentInParent<Rigidbody>();
                if(car.drag < 1)
                    StartCoroutine(SlowCar(car));
            }
        }
        else
        {
            StartCoroutine(ShotEffect());
            Laser.startColor = Color.red;
            Laser.endColor = Color.red;
            Laser.SetPosition(0, _postion);
            Laser.SetPosition(1, _postion + _direction * range);
        }
    }
    
    /// <summary>
    /// Уничтожение стены
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

        yield return new WaitForSeconds(1f);

        if (car.gameObject.GetComponent<CarEngine>())
            car.gameObject.GetComponent<CarEngine>().isKnocked = false;
        car.drag = 0;
    }
}
