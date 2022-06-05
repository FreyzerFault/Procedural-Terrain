using System;
using System.Collections;
using System.Collections.Generic;
using TINManager;
using UnityEngine;
using UnityEngine.UI;

public class TINControlUI : MonoBehaviour
{
    private TINVisualizer tinVisualizer;
    
    public Slider errorToleranceSlider;
    public Text errorToleranceLabel;

    public Button BuildAnimationButton;
    
    
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
        
        OnErrorToleranceSliderChange();
    }

    private void InitializeParams()
    {
        OnErrorToleranceSliderChange();
    }

    public void OnErrorToleranceSliderChange()
    {
        tinVisualizer.errorTolerance = errorToleranceSlider.value;
        errorToleranceLabel.text = "Error Tolerance: " +  errorToleranceSlider.value.ToString("G3");
    }

    public void ResetSeed()
    {
        tinVisualizer.ResetRandomSeed();
        tinVisualizer.ResetTIN();
        
        if (tinVisualizer.animationRunning)
            BuildingAnimation();
    }

    public void BuildingAnimation()
    {
        tinVisualizer.BuildingAnimation();

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
}
