using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Camera))]
public class MouseDetect : MonoBehaviour
{
    private Camera _camera;
    public DetailChangeColor detailChangeColor;

    private void Start()
    {
        _camera = GetComponent<Camera>();
    }

    private void Update()
    {
        if(Input.GetMouseButtonDown(0))
            GetMouseInfo();
    }

    /// <summary>
    /// Выбор детали по клику
    /// </summary>
    private void GetMouseInfo()
    {
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity) && detailChangeColor != null)
        {
            if (hit.collider.gameObject.GetComponent<Detail>())
            {
                int value = hit.collider.gameObject.GetComponent<Detail>().detailValue;
                detailChangeColor.ChangedDetail(value); // иззмененние цвета детали на стандартный при выборе
                detailChangeColor.gameObject.GetComponent<Dropdown>().value = value; // выбор детали в меню
            }
        }
    }
}
