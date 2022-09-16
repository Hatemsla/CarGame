using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DetailChangeColor : MonoBehaviour
{
    public GameObject playerCar;
    public GameObject ColorPicker;
    public GameObject GlassSlider;
    public List<Detail> details;

    private void Start()
    {
        details = playerCar.GetComponentsInChildren<Detail>().ToList();
        details[0].StartColorChange();

        FindObjectOfType<DBManager>().detailChangeColor = GetComponent<DetailChangeColor>();
    }

    /// <summary>
    /// Изменение детали в меню
    /// </summary>
    /// <param name="value">Значение детали</param>
    public void ChangedDetail(int value)
    {
        foreach (Detail detail in details)
        {
            if (detail.detailValue != 3)
            {
                detail.StopColorChange();
            }
        }
        if (value == 3)
        {
            ActiveGlassSlider();
        }
        else
        {
            ActiveColorPicker();
            foreach (Detail detail in details)
            {
                if (detail.detailValue != 3)
                {
                    detail.StopColorChange();
                }
            }
            foreach (Detail detail in details)
            {
                if (detail.detailValue == value && detail.detailValue != 3)
                {
                    detail.StartColorChange();
                }
            }
        }
    }

    private void ActiveColorPicker()
    {
        ColorPicker.SetActive(true);
        GlassSlider.SetActive(false);
    }

    private void ActiveGlassSlider()
    {
        ColorPicker.SetActive(false);
        GlassSlider.SetActive(true);
    }
}
