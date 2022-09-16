using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class RaceManager : MonoBehaviour
{
    public Text racePostotionsText;
    public Text lapText;
    public Text racePlacesText;
    public Text raceNameText;
    public Text racePlacesTimeText;
    public GameObject racePlacesPanel;
    public Slider reloadBar;
    public int carCount;
    public int totalRounds = 5;
    public int i = 0;
    public List<CheckNode> carObjects;
    public List<CarController> players;
    public Transform[] startPositions;
    

    private void Start()
    {
        Physics.IgnoreLayerCollision(3, 7); // игнорирование коллизий мемжду обычными и прочзачными игроками
        Physics.IgnoreLayerCollision(7, 7);
        carObjects = FindObjectsOfType<CheckNode>().ToList();
    }

    private void Update()
    {
        racePostotionsText.text = "";
        racePlacesText.text = "";
        racePlacesTimeText.text = "";
        raceNameText.text = "";

        carCount = carObjects.Count;
        carObjects = carObjects.OrderByDescending(x => x.passedNode).ThenBy(x => x.wayDistance).ToList(); // сортировка списка игроков по тому, кто больше проехал
    }
}
