using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangularModeStatus : MonoBehaviour
{
    private bool triangularModeActive = false;

    public void setTriangularModeActive(bool isActive)
    {
        this.triangularModeActive = isActive;
    }

    public bool getTriangularModeActive()
    {
        return this.triangularModeActive;
    }

    public void updateTriangularStatus(string[] values)
    {
        var mode = values[0]; // show | hide
        var op = values[1]; // normal | triangular

        if(mode == "show")
        {
            if (op == "first")
                triangularModeActive = false;
            else
                triangularModeActive = true;
        }
        else
        {
            if (op == "first")
                triangularModeActive = true;
            else
                triangularModeActive = false;
        }
    }
}
