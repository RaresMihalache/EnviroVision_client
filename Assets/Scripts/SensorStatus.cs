using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SensorStatus : MonoBehaviour
{
    private bool topSensorsActive = true;
    private bool bottomSensorsActive = true;

    public void setTopSensorsStatus(bool isActive)
    {
        this.topSensorsActive = isActive;
    }

    public void setBottomSensorsStatus(bool isActive)
    {
        this.bottomSensorsActive = isActive;
    }

    public bool getTopSensorsStatus()
    {
        return this.topSensorsActive;
    }

    public bool getBottomSensorsStatus()
    {
        return this.bottomSensorsActive;
    }

    public void UpdateSensorStatus(string[] values)
    {
        var sensorsLevel = values[0]; // top | bottom
        var sensorsStatus = values[1]; // show | hide

        if(sensorsStatus == "show")
        {
            if (sensorsLevel == "lower")
                bottomSensorsActive = true;
            else
                topSensorsActive = true;
        }
        else
        {
            if (sensorsLevel == "lower")
                bottomSensorsActive = false;
            else
                topSensorsActive = false;
        }
    }
}
