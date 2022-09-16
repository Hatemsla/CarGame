using HSVPicker;
using HSVPickerExamples;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Detail : MonoBehaviour
{
    public int detailValue;
    private Renderer _windowRenderer;
    private ColorPicker _picker;
    private ColorPickerTester _pickerTester;

    private void Start()
    {
        if (SceneManager.GetActiveScene().buildIndex != 1)
        {
            if (detailValue != 3)
            {
                _picker = GetComponent<ColorPickerTester>().picker;
                _pickerTester = GetComponent<ColorPickerTester>();
            }
            else
            {
                _windowRenderer = GetComponent<Renderer>();
            }
        }
    }

    /// <summary>
    /// Активация изменения цвета выбранной детали
    /// </summary>
    public void StartColorChange()
    {
        gameObject.GetComponent<ColorPickerTester>().enabled = true;
        _picker.onValueChanged.AddListener(ChangeColor);
    }

    /// <summary>
    /// Остановка изменения цвета выбранной детали
    /// </summary>
    public void StopColorChange()
    {
        gameObject.GetComponent<ColorPickerTester>().enabled = false;
        _picker.onValueChanged.RemoveAllListeners();
    }
    
    /// <summary>
    /// Изменение гладкости стекла
    /// </summary>
    /// <param name="newSmooth"></param>
    public void AdjustSmoothness(float newSmooth)
    {
        _windowRenderer.material.SetFloat("_Glossiness", newSmooth);
    }

    /// <summary>
    /// Изменение цвета детали
    /// </summary>
    /// <param name="color"></param>
    private void ChangeColor(Color color)
    {
        _pickerTester.renderer.material.color = color;
        _pickerTester.Color = color;
    }
}
