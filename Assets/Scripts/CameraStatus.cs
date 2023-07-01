using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraStatus : MonoBehaviour
{
    private bool cameraActive = true;
    public Material skyboxMaterial;
    public Material groundMaterial;

    public void setCameraStatus(bool isActive)
    {
        this.cameraActive = isActive;
    }

    public bool getCameraStatus()
    {
        return this.cameraActive;
    }


    public void Start()
    {
        this.skyboxMaterial = RenderSettings.skybox;    
    }

    public void UpdateCameraStatus(string[] values)
    {
        var cameraStatus = values[0]; // show | hide
        if (cameraStatus == "show")
        {
            this.cameraActive = false;
            RenderSettings.skybox = null;
            RenderSettings.customReflection = null;

            Color groundColor = groundMaterial.color;
            groundColor.a = 0f;
            groundMaterial.color = groundColor;
        }
        else
        {
            this.cameraActive = true;
            RenderSettings.skybox = skyboxMaterial;
            RenderSettings.customReflection = null;

            Color groundColor = groundMaterial.color;
            groundColor.a = 1f;
            groundMaterial.color = groundColor;
        }
    }
}
