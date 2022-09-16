using System.Collections.Generic;
using System.IO;
using Photon.Pun;
using UnityEngine;

public class RoundTrigger : MonoBehaviour
{
    public GameObject wallPrefab;
    public Transform wallSpawns;
    public RaceManager raceManager;
    public List<GameObject> walls;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            var car = other.gameObject.GetComponentInParent<CarController>();
            if (car.currentNode == 1)
                car.LapCount();
        }
        if (other.gameObject.CompareTag("Enemy"))
        {
            var enemy = other.gameObject.GetComponentInParent<CarEngine>();
            if (enemy.currentNode == 1)
                enemy.LapCount();
        }

        if (raceManager.carObjects.IndexOf(other.GetComponentInParent<CheckNode>()) == 0) // если машина первая в гонке
        {
            FindObjectOfType<CarController>().SpawnWall(); // спавн стен на трассе
            // other.GetComponentInParent<CarController>().SpawnWall();
        }
    }
}