using System.Net.Mime;
using TINManager;
using UnityEngine;
using UnityEngine.UI;

public class TINControlUI : MonoBehaviour
{
    private TINVisualizer tinVisualizer;

    public Slider errorToleranceSlider;
    public Text errorToleranceLabel;
    public Slider maxPointsPerCycleSlider;
    public Text maxPointsPerCycleLabel;
    
    public Slider progressBarSlider;
    public Text progressBarLabel;

    public Text time;
    public Text iterations;
    public Text triangles;
    public Text vertices;

    public Button BuildAnimationButton;

    public LineRenderer lineDisplay;

    private ColorBlock defaultColorBlock;
    private ColorBlock stopColorBlock;

    void Awake()
    {
        tinVisualizer = GetComponent<TINVisualizer>();

        defaultColorBlock = BuildAnimationButton.colors;
        stopColorBlock = defaultColorBlock;
        stopColorBlock.normalColor = Color.red;
        stopColorBlock.selectedColor = Color.red;
        stopColorBlock.highlightedColor = Color.red;
        stopColorBlock.pressedColor = Color.red;

        InitializeParams();
    }

    private void Update()
    {
        if (tinVisualizer.animationRunning)
        {
            BuildAnimationButton.GetComponentInChildren<Text>().text = "STOP";
            BuildAnimationButton.GetComponentInChildren<Text>().color = Color.white;
            BuildAnimationButton.colors = stopColorBlock;
        }
        else
        {
            BuildAnimationButton.GetComponentInChildren<Text>().text = "Build TIN";
            BuildAnimationButton.GetComponentInChildren<Text>().color = Color.black;
            BuildAnimationButton.colors = defaultColorBlock;
        }
    }

    private void InitializeParams()
    {
        OnErrorToleranceSliderChange();
        OnPointsPerCicleSliderChange();
        time.text = "00:00";
        iterations.text = "0 iterations";
        vertices.text = "4 vertices";
        triangles.text = "2 triangles";
    }

    public void OnErrorToleranceSliderChange()
    {
        tinVisualizer.errorTolerance = errorToleranceSlider.value;
        tinVisualizer.ResetTIN();
        errorToleranceLabel.text = "Error Tolerance: " + errorToleranceSlider.value.ToString("G3");
    }

    public void OnPointsPerCicleSliderChange()
    {
        tinVisualizer.maxPointsPerCycle = (int) maxPointsPerCycleSlider.value;
        tinVisualizer.ResetTIN();
        maxPointsPerCycleLabel.text = "Max Points Per Cycle: " + tinVisualizer.maxPointsPerCycle;
    }

    public void ResetSeed()
    {
        tinVisualizer.ResetRandomSeed();
        tinVisualizer.ResetTIN();
    }

    public void ToggleBuildingAnimation()
    {
        tinVisualizer.BuildingAnimation();
    }

    public void RunOneIteration()
    {
        tinVisualizer.AddNextPoint();
    }

    /// <summary>
    /// Modifica la linea de la vuelta ciclista para visualizarla de perfil.
    /// Como esta en un plano siempre podemos rotar ese plano y dejarlo en Z = 0
    /// </summary>
    public void UpdateLine(Vector3[] points)
    {
        if (points.Length == 0)
        {
            lineDisplay.positionCount = 0;
            return;
        }
        // Para verla de perfil hay que hacer una Rotacion Inversa en el eje Y para poner todos los puntos en Z = 0
        Vector3 dir = (points[1] - points[0]).normalized;
        float angle = Mathf.Asin(dir.z);
        if (dir.x <= 0)
            angle = -angle + Mathf.PI;
        Quaternion rotation = Quaternion.Euler(0, angle * Mathf.Rad2Deg, 0);

        // Calculamos la longitud que ocupa si contar la altura
        Vector3 initToEnd = points[points.Length - 1] - points[0];
        initToEnd.y = 0;
        float width = initToEnd.magnitude;

        Vector3 orig = points[0];

        for (int i = 0; i < points.Length; i++)
        {
            // Lo devolvermos primero a su origen, lo rotamos para dejarlo en Z = 0 y lo movemos la mitad de su anchura para centrarlo
            points[i] = rotation * (points[i] - orig) - Vector3.right * width / 2;

            // Lo colocamos en panel donde lo visualizamos
            points[i] = lineDisplay.GetComponent<RectTransform>().TransformPoint(points[i]);
        }

        lineDisplay.positionCount = points.Length;
        lineDisplay.SetPositions(points);
    }
    
    
    public void UpdateProgressBar(float progress)
    {
        progressBarSlider.value = progress;
        progressBarLabel.text = progress.ToString("P");
    }
}