using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckpointTrigger : MonoBehaviour
{
    public int checkpointID;
    private void Start()
    {
        checkpointID = int.Parse(gameObject.name);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            var player = other.GetComponentInParent<CarController>();
            if (player.currentNode == checkpointID)
                player.CheckWaypoint(); // расчет дистанции до следующего чекпоинта
        }
        if (other.gameObject.CompareTag("Enemy"))
        {
            var enemy = other.GetComponentInParent<CarEngine>();
            enemy.passedNode++;
        }
    }
}
