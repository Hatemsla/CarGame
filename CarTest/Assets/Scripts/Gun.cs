using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Gun : MonoBehaviour
{
    [HideInInspector] public abstract float FireRate { get; }
    [HideInInspector] public abstract float Range { get; }
    [HideInInspector] public abstract WaitForSeconds LaserDuration { get; }
    [HideInInspector] public abstract Transform ShootPoint { get; set; }
    [HideInInspector] public abstract LineRenderer Laser { get; set; }
    [HideInInspector] public abstract float NextFire { get; set; }


    //public abstract void Shoot();
    public abstract IEnumerator ShotEffect();
    protected abstract IEnumerator SlowCar(Rigidbody car);
}
