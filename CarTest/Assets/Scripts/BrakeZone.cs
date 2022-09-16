using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrakeZone : MonoBehaviour
{
    public float maxSpeed;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            var enemy = other.gameObject.GetComponentInParent<CarEngine>();
            if (enemy.currentSpeed > maxSpeed)
            {
                enemy.isBraking = true;
            }
            else
            {
                enemy.isBraking = false;
            }
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            var enemy = other.gameObject.GetComponentInParent<CarEngine>();
            if (enemy.currentSpeed < 20)
            {
                enemy.isBraking = false;
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            other.gameObject.GetComponentInParent<CarEngine>().isBraking = false;
        }
    }
}
