using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckNode : MonoBehaviour
{
    public int passedNode;
    public float wayDistance;
    private CarEngine _carE;
    private CarController _carC;
    private bool _controller = true;
    [HideInInspector] public float time;

    private void Start()
    {
        if (TryGetComponent(out _carE)) // определение на каком объекте весит CheckNode
        {
            _controller = true;
            passedNode = _carE.passedNode;
            wayDistance = _carE.wayDistance;
            time = _carE.time;
        }
        if (TryGetComponent(out _carC))
        {
            _controller = false;
            passedNode = _carC.passedNode;
            wayDistance = _carC.wayDistance;
            time = _carC.time;
        }
    }

    private void Update()
    {
        if (_controller)
        {
            passedNode = _carE.passedNode;
            wayDistance = _carE.wayDistance;
            time = _carE.time;
        }
        if (!_controller)
        {
            passedNode = _carC.passedNode;
            wayDistance = _carC.wayDistance;
            time = _carC.time;
        }
    }
}

