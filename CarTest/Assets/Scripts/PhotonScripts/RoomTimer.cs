using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomTimer : MonoBehaviour
{
    public float timeRemaining = 120;

    private void Update()
    {
        if (timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            Timer(timeRemaining);
        }
        else
        {
            timeRemaining = 0;
        }
    }

    private void Timer(float time)
    {
        time += 1;
        float minutes = Mathf.FloorToInt(time / 60);
        float seconds = Mathf.FloorToInt(time % 60);
        Launcher.instance.remainingTimeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
