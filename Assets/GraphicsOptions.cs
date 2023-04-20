using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphicsOptions : MonoBehaviour
{
    public ResolutionScale resolutionScale;
    public FullScreenMode isFullscreened = FullScreenMode.FullScreenWindow;
    [SerializeField]
    private TMPro.TMP_Dropdown resoDropdown;
    [SerializeField]
    private TMPro.TMP_Dropdown qraphicsDropdown;
    [SerializeField]
    private TMPro.TMP_Dropdown fullscreenDropdown;
    [SerializeField]
    private UnityEngine.UI.Toggle syncToggle;
    [SerializeField]
    private MPClient client;
    public enum ResolutionScale
    {
        r1920 = 0,
        r1280 = 1,
        r1024 = 2,
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ChangeSyncProps()
    {
        client.dynamicPropSync = syncToggle.isOn;
    }
    public void ChangeResolutionScale()
    {
        switch((ResolutionScale)resoDropdown.value)
        {
            case ResolutionScale.r1920:
            {
                resolutionScale = ResolutionScale.r1920;
                Screen.SetResolution(1920, 1080, isFullscreened);
                break;
            }
            case ResolutionScale.r1280:
            {
                resolutionScale = ResolutionScale.r1280;
                Screen.SetResolution(1280, 720, isFullscreened);
                break;
            }
            case ResolutionScale.r1024:
            {
                resolutionScale = ResolutionScale.r1024;
                Screen.SetResolution(1024, 768, isFullscreened);
                break;
            }
        }
    }
    public void ChangeResolutionScale(ResolutionScale scale)
    {
        switch(scale)
        {
            case ResolutionScale.r1920:
            {
                resolutionScale = ResolutionScale.r1920;
                Screen.SetResolution(1920, 1080, isFullscreened);
                break;
            }
            case ResolutionScale.r1280:
            {
                resolutionScale = ResolutionScale.r1280;
                Screen.SetResolution(1280, 720, isFullscreened);
                break;
            }
            case ResolutionScale.r1024:
            {
                resolutionScale = ResolutionScale.r1024;
                Screen.SetResolution(1024, 768, isFullscreened);
                break;
            }
        }
    }
    public void ChangeWindowType()
    {
        switch(fullscreenDropdown.value)
        {
            case 0:
            {
                isFullscreened = FullScreenMode.FullScreenWindow;
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
            }
            case 1:
            {
                isFullscreened = FullScreenMode.FullScreenWindow;
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                break;
            }
            case 2:
            {
                isFullscreened = FullScreenMode.Windowed;
                Screen.fullScreenMode = FullScreenMode.Windowed;
                break;
            }
        }
    }
    public void ChangeGraphicsQuality()
    {
        QualitySettings.SetQualityLevel(qraphicsDropdown.value, false);
    }
}
