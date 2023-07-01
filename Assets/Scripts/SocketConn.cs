using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp;
using System;
using System.Linq;
using TMPro;


public class SocketConn : MonoBehaviour
{

    private WebSocket ws;
    private float distance;
    private bool messageReceived = false;

    private bool sensor1Obj = false;
    private bool sensor2Obj = false;
    private bool sensor3Obj = false;
    private bool sensor4Obj = false;

    private Queue<float> sensor1Queue = new Queue<float>();
    private Queue<float> sensor2Queue = new Queue<float>();
    private Queue<float> sensor3Queue = new Queue<float>();
    private Queue<float> sensor4Queue = new Queue<float>();

    private float sensor1PrevAvg = 0f;
    private float sensor1CurrAvg = 0f;
    private float sensor2PrevAvg = 0f;
    private float sensor2CurrAvg = 0f;
    private float sensor3PrevAvg = 0f;
    private float sensor3CurrAvg = 0f;
    private float sensor4PrevAvg = 0f;
    private float sensor4CurrAvg = 0f;

    private List<GameObject> sensorCubes = new List<GameObject>();

    [Flags]
    public enum MyEnum : byte
    {
        Case_1 = 0b0000,
        Case_2 = 0b0001,
        Case_3 = 0b0010,
        Case_4 = 0b0011,
        Case_5 = 0b0100,
        Case_6 = 0b0101,
        Case_7 = 0b0110,
        Case_8 = 0b0111,
        Case_9 = 0b1000,
        Case_10 = 0b1001,
        Case_11 = 0b1010,
        Case_12 = 0b1011,
    }

    public MyEnum myValue;

    public struct MergeCubesStruct
    {
        public bool backward;
        public bool forward;
    }

    MergeCubesStruct[] mergeSensorCubesArray = new MergeCubesStruct[4];


    public TextMeshProUGUI UIDistanceCase;
    public TextMeshProUGUI upperDistanceTriangularD;
    public TextMeshProUGUI upperDistanceTriangularH;
    public TextMeshProUGUI lowerDistanceTriangularD;
    public TextMeshProUGUI lowerDistanceTriangularH;
    public SensorStatus sensorStatusScript;
    public CameraStatus cameraStatusScript;
    public TriangularModeStatus triangularModeStatusScript;


    void DestroyPreviousStepCubes()
    {
        try
        {
            for (int i = sensorCubes.Count - 1; i >= 0; i--)
            {
                Destroy(sensorCubes[i]);
                sensorCubes.RemoveAt(i);
            }
        }
        catch(Exception ex)
        {
            Debug.Log("Objects already removed for mode 1!");
        }

        if (triangularModeStatusScript.getTriangularModeActive() == false) // mode 1 active (normal)
        {
            GameObject[] cubesToRemoveFromMode2 = GameObject.FindGameObjectsWithTag("triangularCube");

            foreach (GameObject obj in cubesToRemoveFromMode2)
                Destroy(obj);
        }
        else
            return;
    }

    void CreateSensorCube(Vector3 cubePosition, Vector3 cubeScale, Quaternion cubeRotation, Color color, string tag)
    {
        GameObject newCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        newCube.transform.position = cubePosition;
        newCube.transform.localScale = cubeScale;
        newCube.transform.rotation = cubeRotation;
        newCube.GetComponent<Renderer>().material.color = color;
        newCube.tag = tag;

        sensorCubes.Add(newCube);
    }

    void  CreateNewCubeTriangularMode(float d, float h, string tag, string level)
    {

        GameObject newCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Vector3 cameraPos = Camera.main.transform.position;
        Vector3 cubePosition;
        if (level == "upper")
        {
            this.upperDistanceTriangularD.text = "upper distance Triangular D: " + d.ToString();
            this.upperDistanceTriangularH.text = "upper distance Triangular H: " + h.ToString();

            cubePosition = cameraPos + Camera.main.transform.forward * h / 100f + Camera.main.transform.right * (d - 10) / 100f;
            newCube.GetComponent<Renderer>().material.color = Color.blue;

        }
        else
        {
            this.lowerDistanceTriangularD.text = "lower distance Triangular D: " + d.ToString();
            this.lowerDistanceTriangularH.text = "lower distance Triangular H: " + h.ToString();

            cubePosition = cameraPos + Camera.main.transform.forward * h / 100f + Camera.main.transform.right * (d - 10) / 100f + new Vector3(0f, -0.4f, 0f);
            newCube.GetComponent<Renderer>().material.color = Color.green;

        }
        //Vector3 cubePosition = (level == "upper") ? new Vector3(cameraPos.x + d / 100, cameraPos.y, cameraPos.z + h / 100) : new Vector3(cameraPos.x + d / 100, cameraPos.y - 0.3f, cameraPos.z + h / 100);
        newCube.transform.position = cubePosition;
        newCube.transform.localScale = new Vector3(0.04705968f, 0.03725558f, 0.00196082f);
        newCube.transform.rotation = Camera.main.transform.rotation;

        //newCube.GetComponent<Renderer>().material.color = Color.black;
        newCube.tag = tag;

        

        
    }

    void CreateSensorCube(Vector3 cubePosition, Vector3 cubeScale, Quaternion cubeRotation, float distance, string tag)
    {
        GameObject newCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        newCube.transform.position = cubePosition;
        newCube.transform.localScale = cubeScale;
        newCube.transform.rotation = cubeRotation;
        if (distance < 50f)
            newCube.GetComponent<Renderer>().material.color = Color.red;
        else
            newCube.GetComponent<Renderer>().material.color = Color.green;
        newCube.tag = tag;

        sensorCubes.Add(newCube);
    }

    bool canConstructTriangle(float a, float b, float c)
    {
        if (a + b > c && b + c > a && a + c > b)
            return true;
        return false;
    }

    void CreateSensorCubes()
    {
        DestroyPreviousStepCubes();

        //Debug.Log(triangularModeStatusScript.getTriangularModeActive());

        if (triangularModeStatusScript.getTriangularModeActive()) // mode 2 activated (triangular mode)
        {
            Debug.Log("triangular mode activated");
            float baseLineUpper = 20f;
            float baseLineLower = 48f;
            // Cos theorem
            if(canConstructTriangle(sensor1CurrAvg, sensor3CurrAvg, 20f) && (sensor1Obj || sensor3Obj) && sensorStatusScript.getTopSensorsStatus())
            {
                Debug.Log("Can construct upper sensors cube!");
                float cosine = (sensor1CurrAvg * sensor1CurrAvg + baseLineUpper * baseLineUpper - sensor3CurrAvg * sensor3CurrAvg) / (2 * sensor1CurrAvg * baseLineUpper);
                float angleInRadians = Mathf.Acos(cosine);

                float height = sensor1CurrAvg * Mathf.Sin(angleInRadians); // distance z-axis
                float distance = height / Mathf.Tan(angleInRadians); // distance x-axis

                CreateNewCubeTriangularMode(distance, height, "triangularCube", "upper");

            }

            if(canConstructTriangle(sensor2CurrAvg, sensor4CurrAvg, 48f) && (sensor2Obj || sensor4Obj) && sensorStatusScript.getBottomSensorsStatus())
            {
                Debug.Log("Can construct lower sensors cube!");
                float cosine = (sensor2CurrAvg * sensor2CurrAvg + baseLineLower * baseLineLower - sensor4CurrAvg * sensor4CurrAvg) / (2 * sensor2CurrAvg * baseLineLower);
                float angleInRadians = Mathf.Acos(cosine);

                float height = sensor4CurrAvg * Mathf.Sin(angleInRadians); // distance z-axis
                float distance = height / Mathf.Tan(angleInRadians); // distance x-axis

                CreateNewCubeTriangularMode(distance, height, "triangularCube", "lower");
            }
        }

        else {

            this.lowerDistanceTriangularD.text = "";
            this.lowerDistanceTriangularH.text = "";
            this.upperDistanceTriangularD.text = "";
            this.upperDistanceTriangularH.text = "";

            float posFactorXUL = sensor1CurrAvg * -0.005f;
            float posFactorXDL = sensor4CurrAvg * -0.005f;
            float posFactorXUR = sensor3CurrAvg * 0.005f;
            float posFactorXDR = sensor2CurrAvg * 0.005f;

            float minDistance = 20f;
            float maxDistance = 500f;

            float minPosYUp = 1.155f; // for 25 cm
            float maxPosYUp = 4.4f; // for 500 cm
            float minPosYDown = 0.84f; // for 25 cm
            float maxPosYDown = 1.00f; // for 500 cm

            float posYUpPerCm = 0.006831f;
            float posYDownPerCm = 0.000336f;

            Vector3 cubeMinScale = new Vector3(0.1699968f, 0.1345808f, 0.0070832f); // for 25 cm
            Vector3 cubeMaxScale = new Vector3(2.57473f, 2.038328f, 0.1072804f); // for 500 cm
            Vector3 scalePerCm = new Vector3(0.005062f, 0.004007f, 0.000210f); // for each cm from 25 to 500

            Vector3 cubePositionUL = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * sensor1CurrAvg / 50f + new Vector3(posFactorXUL, minPosYUp + (sensor1CurrAvg - minDistance) * posYUpPerCm, 0f);
            Vector3 cubePositionUR = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * sensor3CurrAvg / 50f + new Vector3(posFactorXUR, minPosYUp + (sensor3CurrAvg - minDistance) * posYUpPerCm, 0f);
            Vector3 cubePositionDL = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * sensor4CurrAvg / 50f + new Vector3(posFactorXDL, minPosYDown + (sensor4CurrAvg - minDistance) * posYDownPerCm, 0f);
            Vector3 cubePositionDR = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * sensor2CurrAvg / 50f + new Vector3(posFactorXDR, minPosYDown + (sensor2CurrAvg - minDistance) * posYDownPerCm, 0f);

            Vector3 cubeScaleUL = cubeMinScale + (sensor1CurrAvg - minDistance) * scalePerCm;
            Vector3 cubeScaleUR = cubeMinScale + (sensor3CurrAvg - minDistance) * scalePerCm;
            Vector3 cubeScaleDL = cubeMinScale + (sensor4CurrAvg - minDistance) * scalePerCm;
            Vector3 cubeScaleDR = cubeMinScale + (sensor2CurrAvg - minDistance) * scalePerCm;


            Quaternion cubeRotation = Camera.main.transform.rotation;

            if (sensor1CurrAvg == 0 || sensor2CurrAvg == 0 || sensor3CurrAvg == 0 || sensor4CurrAvg == 0)
                return;

            switch (myValue)
            {
                case MyEnum.Case_1:
                    {
                        if (sensorStatusScript.getTopSensorsStatus())
                        {
                            CreateSensorCube(cubePositionUL, cubeScaleUL, cubeRotation, sensor1CurrAvg, "obstacleUL");
                            CreateSensorCube(cubePositionUR, cubeScaleUR, cubeRotation, sensor3CurrAvg, "obstacleUR");
                        }
                        if (sensorStatusScript.getBottomSensorsStatus())
                        {
                            CreateSensorCube(cubePositionDL, cubeScaleDL, cubeRotation, sensor4CurrAvg, "obstacleDL");
                            CreateSensorCube(cubePositionDR, cubeScaleDR, cubeRotation, sensor2CurrAvg, "obstacleDR");
                        }
                    }
                    break;
                case MyEnum.Case_2:
                    {
                        float leftAvg = (sensor1CurrAvg + sensor4CurrAvg) / 2f;
                        float posFactorXL = leftAvg * -0.005f;

                        Vector3 cubeScaleL = cubeMinScale + (leftAvg - minDistance) * scalePerCm;
                        cubeScaleL.y *= 2f;

                        Vector3 cubePositionL = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * leftAvg / 50f + new Vector3(posFactorXL, minPosYDown + (leftAvg - minDistance) * posYDownPerCm, 0f);

                        if (sensorStatusScript.getTopSensorsStatus() && sensorStatusScript.getBottomSensorsStatus())
                        {
                            CreateSensorCube(cubePositionL, cubeScaleL, cubeRotation, leftAvg, "obstacleL");
                            CreateSensorCube(cubePositionUR, cubeScaleUR, cubeRotation, sensor3CurrAvg, "obstacleUR");
                            CreateSensorCube(cubePositionDR, cubeScaleDR, cubeRotation, sensor2CurrAvg, "obstacleDR");
                        }
                        else
                        {
                            if (sensorStatusScript.getTopSensorsStatus())
                            {
                                CreateSensorCube(cubePositionUR, cubeScaleUR, cubeRotation, sensor3CurrAvg, "obstacleUR");
                                CreateSensorCube(cubePositionUL, cubeScaleUL, cubeRotation, sensor1CurrAvg, "obstacleUL");
                            }
                            else if (sensorStatusScript.getBottomSensorsStatus())
                            {
                                CreateSensorCube(cubePositionDR, cubeScaleDR, cubeRotation, sensor2CurrAvg, "obstacleDR");
                                CreateSensorCube(cubePositionDL, cubeScaleDL, cubeRotation, sensor4CurrAvg, "obstacleDL");
                            }
                        }
                    }
                    break;
                case MyEnum.Case_3:
                    {
                        float rightAvg = (sensor3CurrAvg + sensor2CurrAvg) / 2f;
                        float posFactorXR = rightAvg * 0.005f;

                        Vector3 cubeScaleR = cubeMinScale + (rightAvg - minDistance) * scalePerCm;
                        cubeScaleR.y *= 2f;

                        Vector3 cubePositionR = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * rightAvg / 50f + new Vector3(posFactorXR, minPosYDown + (rightAvg - minDistance) * posYDownPerCm, 0f);


                        if (sensorStatusScript.getTopSensorsStatus() && sensorStatusScript.getBottomSensorsStatus())
                        {
                            CreateSensorCube(cubePositionR, cubeScaleR, cubeRotation, rightAvg, "obstacleR");
                            CreateSensorCube(cubePositionUL, cubeScaleUL, cubeRotation, sensor1CurrAvg, "obstacleUL");
                            CreateSensorCube(cubePositionDL, cubeScaleDL, cubeRotation, sensor4CurrAvg, "obstacleDL");
                        }
                        else
                        {
                            if (sensorStatusScript.getTopSensorsStatus())
                            {
                                CreateSensorCube(cubePositionUR, cubeScaleUR, cubeRotation, sensor3CurrAvg, "obstacleUR");
                                CreateSensorCube(cubePositionUL, cubeScaleUL, cubeRotation, sensor1CurrAvg, "obstacleUL");
                            }
                            else if (sensorStatusScript.getBottomSensorsStatus())
                            {
                                CreateSensorCube(cubePositionDR, cubeScaleDR, cubeRotation, sensor2CurrAvg, "obstacleDR");
                                CreateSensorCube(cubePositionDL, cubeScaleDL, cubeRotation, sensor4CurrAvg, "obstacleDL");
                            }
                        }
                    }
                    break;
                case MyEnum.Case_4:
                    {
                        float upAvg = (sensor1CurrAvg + sensor3CurrAvg) / 2f;

                        Vector3 cubeScaleU = cubeMinScale + (upAvg - minDistance) * scalePerCm;
                        cubeScaleU.x *= 2f;

                        Vector3 cubePositionU = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * upAvg / 50f + new Vector3(0f, minPosYUp + (upAvg - minDistance) * posYUpPerCm, 0f);

                        if (sensorStatusScript.getTopSensorsStatus() && sensorStatusScript.getBottomSensorsStatus())
                        {
                            CreateSensorCube(cubePositionU, cubeScaleU, cubeRotation, upAvg, "obstacleU");
                            CreateSensorCube(cubePositionDL, cubeScaleDL, cubeRotation, sensor4CurrAvg, "obstacleDL");
                            CreateSensorCube(cubePositionDR, cubeScaleDR, cubeRotation, sensor2CurrAvg, "obstacleDR");
                        }
                        else
                        {
                            if (sensorStatusScript.getTopSensorsStatus())
                            {
                                CreateSensorCube(cubePositionU, cubeScaleU, cubeRotation, upAvg, "obstacleU");
                            }
                            else if (sensorStatusScript.getBottomSensorsStatus())
                            {
                                CreateSensorCube(cubePositionDL, cubeScaleDL, cubeRotation, sensor4CurrAvg, "obstacleDL");
                                CreateSensorCube(cubePositionDR, cubeScaleDR, cubeRotation, sensor2CurrAvg, "obstacleDR");
                            }
                        }
                    }
                    break;
                case MyEnum.Case_5:
                    {
                        float downAvg = (sensor2CurrAvg + sensor4CurrAvg) / 2f;

                        Vector3 cubeScaleD = cubeMinScale + (downAvg - minDistance) * scalePerCm;
                        cubeScaleD.x *= 2f;

                        Vector3 cubePositionD = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * downAvg / 50f + new Vector3(0f, minPosYDown + (downAvg - minDistance) * posYDownPerCm, 0f);

                        if (sensorStatusScript.getTopSensorsStatus() && sensorStatusScript.getBottomSensorsStatus())
                        {
                            CreateSensorCube(cubePositionD, cubeScaleD, cubeRotation, downAvg, "obstacleD");
                            CreateSensorCube(cubePositionUL, cubeScaleUL, cubeRotation, sensor1CurrAvg, "obstacleUL");
                            CreateSensorCube(cubePositionUR, cubeScaleUR, cubeRotation, sensor3CurrAvg, "obstacleUR");
                        }
                        else
                        {
                            if (sensorStatusScript.getTopSensorsStatus())
                            {
                                CreateSensorCube(cubePositionUL, cubeScaleUL, cubeRotation, sensor1CurrAvg, "obstacleUL");
                                CreateSensorCube(cubePositionUR, cubeScaleUR, cubeRotation, sensor3CurrAvg, "obstacleUR");
                            }
                            else if (sensorStatusScript.getBottomSensorsStatus())
                            {
                                CreateSensorCube(cubePositionD, cubeScaleD, cubeRotation, downAvg, "obstacleD");
                            }
                        }
                    }
                    break;
                case MyEnum.Case_6:
                    {
                        float upAvg = (sensor1CurrAvg + sensor3CurrAvg) / 2f;
                        float leftAvg = (sensor1CurrAvg + sensor4CurrAvg) / 2f;

                        Vector3 cubeScaleU = cubeMinScale + (upAvg - minDistance) * scalePerCm;
                        cubeScaleU.x *= 2f;
                        Vector3 cubeScaleL = cubeMinScale + (leftAvg - minDistance) * scalePerCm;
                        cubeScaleL.y *= 2f;

                        float posFactorXL = leftAvg * -0.005f;

                        Vector3 cubePositionU = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * upAvg / 50f + new Vector3(0f, minPosYUp + (upAvg - minDistance) * posYUpPerCm, 0f);
                        Vector3 cubePositionL = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * leftAvg / 50f + new Vector3(posFactorXL, minPosYDown + (leftAvg - minDistance) * posYDownPerCm, 0f);


                        if (sensorStatusScript.getTopSensorsStatus() && sensorStatusScript.getBottomSensorsStatus())
                        {
                            CreateSensorCube(cubePositionU, cubeScaleU, cubeRotation, upAvg, "obstacleU");
                            CreateSensorCube(cubePositionL, cubeScaleL, cubeRotation, leftAvg, "obstacleL");
                            CreateSensorCube(cubePositionDR, cubeScaleDR, cubeRotation, sensor2CurrAvg, "obstacleDR");
                        }
                        else
                        {
                            if (sensorStatusScript.getTopSensorsStatus())
                            {
                                CreateSensorCube(cubePositionU, cubeScaleU, cubeRotation, upAvg, "obstacleU");
                            }
                            else if (sensorStatusScript.getBottomSensorsStatus())
                            {
                                CreateSensorCube(cubePositionDL, cubeScaleDL, cubeRotation, sensor4CurrAvg, "obstacleDL");
                                CreateSensorCube(cubePositionDR, cubeScaleDR, cubeRotation, sensor2CurrAvg, "obstacleDR");
                            }
                        }
                    }
                    break;
                case MyEnum.Case_7:
                    {
                        float upAvg = (sensor1CurrAvg + sensor3CurrAvg) / 2f;
                        float rightAvg = (sensor2CurrAvg + sensor3CurrAvg) / 2f;

                        Vector3 cubeScaleU = cubeMinScale + (upAvg - minDistance) * scalePerCm;
                        cubeScaleU.x *= 2f;
                        Vector3 cubeScaleR = cubeMinScale + (rightAvg - minDistance) * scalePerCm;
                        cubeScaleR.y *= 2f;

                        float posFactorXR = rightAvg * 0.005f;

                        Vector3 cubePositionU = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * upAvg / 50f + new Vector3(0f, minPosYUp + (upAvg - minDistance) * posYUpPerCm, 0f);
                        Vector3 cubePositionR = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * rightAvg / 50f + new Vector3(posFactorXR, minPosYDown + (rightAvg - minDistance) * posYDownPerCm, 0f);

                        if (sensorStatusScript.getTopSensorsStatus() && sensorStatusScript.getBottomSensorsStatus())
                        {
                            CreateSensorCube(cubePositionU, cubeScaleU, cubeRotation, upAvg, "obstacleU");
                            CreateSensorCube(cubePositionR, cubeScaleR, cubeRotation, rightAvg, "obstacleR");
                            CreateSensorCube(cubePositionDL, cubeScaleDL, cubeRotation, sensor4CurrAvg, "obstacleDL");
                        }
                        else
                        {
                            if (sensorStatusScript.getTopSensorsStatus())
                            {
                                CreateSensorCube(cubePositionU, cubeScaleU, cubeRotation, upAvg, "obstacleU");
                            }
                            else if (sensorStatusScript.getBottomSensorsStatus())
                            {
                                CreateSensorCube(cubePositionDL, cubeScaleDL, cubeRotation, sensor4CurrAvg, "obstacleDL");
                                CreateSensorCube(cubePositionDR, cubeScaleDR, cubeRotation, sensor2CurrAvg, "obstacleDR");
                            }
                        }
                    }
                    break;
                case MyEnum.Case_8:
                    {
                        float downAvg = (sensor2CurrAvg + sensor4CurrAvg) / 2f;
                        float rightAvg = (sensor2CurrAvg + sensor3CurrAvg) / 2f;

                        float posFactorXR = rightAvg * 0.005f;

                        Vector3 cubeScaleD = cubeMinScale + (downAvg - minDistance) * scalePerCm;
                        cubeScaleD.x *= 2f;
                        Vector3 cubeScaleR = cubeMinScale + (rightAvg - minDistance) * scalePerCm;
                        cubeScaleR.y *= 2f;

                        Vector3 cubePositionD = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * downAvg / 50f + new Vector3(0f, minPosYDown + (downAvg - minDistance) * posYDownPerCm, 0f);
                        Vector3 cubePositionR = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * rightAvg / 50f + new Vector3(posFactorXR, minPosYDown + (rightAvg - minDistance) * posYDownPerCm, 0f);

                        if (sensorStatusScript.getTopSensorsStatus() && sensorStatusScript.getBottomSensorsStatus())
                        {
                            CreateSensorCube(cubePositionD, cubeScaleD, cubeRotation, downAvg, "obstacleD");
                            CreateSensorCube(cubePositionR, cubeScaleR, cubeRotation, rightAvg, "obstacleR");
                            CreateSensorCube(cubePositionUL, cubeScaleUL, cubeRotation, sensor1CurrAvg, "obstacleUL");
                        }
                        else
                        {
                            if (sensorStatusScript.getTopSensorsStatus())
                            {
                                CreateSensorCube(cubePositionUL, cubeScaleUL, cubeRotation, sensor1CurrAvg, "obstacleUL");
                                CreateSensorCube(cubePositionUR, cubeScaleUR, cubeRotation, sensor3CurrAvg, "obstacleUR");
                            }
                            else if (sensorStatusScript.getBottomSensorsStatus())
                            {
                                CreateSensorCube(cubePositionD, cubeScaleD, cubeRotation, downAvg, "obstacleD");

                            }
                        }
                    }
                    break;
                case MyEnum.Case_9:
                    {
                        float downAvg = (sensor2CurrAvg + sensor4CurrAvg) / 2f;
                        float leftAvg = (sensor1CurrAvg + sensor4CurrAvg) / 2f;


                        float posFactorXL = leftAvg * -0.005f;

                        Vector3 cubeScaleD = cubeMinScale + (downAvg - minDistance) * scalePerCm;
                        cubeScaleD.x *= 2f;
                        Vector3 cubeScaleL = cubeMinScale + (leftAvg - minDistance) * scalePerCm;
                        cubeScaleL.y *= 2f;

                        Vector3 cubePositionD = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * downAvg / 50f + new Vector3(0f, minPosYDown + (downAvg - minDistance) * posYDownPerCm, 0f);
                        Vector3 cubePositionL = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * leftAvg / 50f + new Vector3(posFactorXL, minPosYDown + (leftAvg - minDistance) * posYDownPerCm, 0f);


                        if (sensorStatusScript.getTopSensorsStatus() && sensorStatusScript.getBottomSensorsStatus())
                        {
                            CreateSensorCube(cubePositionD, cubeScaleD, cubeRotation, downAvg, "obstacleD");
                            CreateSensorCube(cubePositionL, cubeScaleL, cubeRotation, leftAvg, "obstacleL");
                            CreateSensorCube(cubePositionUR, cubeScaleUR, cubeRotation, sensor3CurrAvg, "obstacleUR");
                        }
                        else
                        {
                            if (sensorStatusScript.getTopSensorsStatus())
                            {
                                CreateSensorCube(cubePositionUL, cubeScaleUL, cubeRotation, sensor1CurrAvg, "obstacleUL");
                                CreateSensorCube(cubePositionUR, cubeScaleUR, cubeRotation, sensor3CurrAvg, "obstacleUR");
                            }
                            else if (sensorStatusScript.getBottomSensorsStatus())
                            {
                                CreateSensorCube(cubePositionD, cubeScaleD, cubeRotation, downAvg, "obstacleD");

                            }
                        }
                    }
                    break;
                case MyEnum.Case_10:
                    {
                        float rightAvg = (sensor2CurrAvg + sensor3CurrAvg) / 2f;
                        float leftAvg = (sensor1CurrAvg + sensor4CurrAvg) / 2f;
                        float posFactorXR = rightAvg * 0.005f;
                        float posFactorXL = leftAvg * -0.005f;

                        Vector3 cubeScaleR = cubeMinScale + (rightAvg - minDistance) * scalePerCm;
                        cubeScaleR.y *= 2f;
                        Vector3 cubeScaleL = cubeMinScale + (leftAvg - minDistance) * scalePerCm;
                        cubeScaleL.y *= 2f;

                        Vector3 cubePositionR = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * rightAvg / 50f + new Vector3(posFactorXR, minPosYDown + (rightAvg - minDistance) * posYDownPerCm, 0f);
                        Vector3 cubePositionL = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * leftAvg / 50f + new Vector3(posFactorXL, minPosYDown + (leftAvg - minDistance) * posYDownPerCm, 0f);

                        if (sensorStatusScript.getTopSensorsStatus() && sensorStatusScript.getBottomSensorsStatus())
                        {
                            CreateSensorCube(cubePositionR, cubeScaleR, cubeRotation, rightAvg, "obstacleR");
                            CreateSensorCube(cubePositionL, cubeScaleL, cubeRotation, leftAvg, "obstacleL");
                        }
                        else
                        {
                            if (sensorStatusScript.getTopSensorsStatus())
                            {
                                CreateSensorCube(cubePositionUL, cubeScaleUL, cubeRotation, sensor1CurrAvg, "obstacleUL");
                                CreateSensorCube(cubePositionUR, cubeScaleUR, cubeRotation, sensor3CurrAvg, "obstacleUR");
                            }
                            else if (sensorStatusScript.getBottomSensorsStatus())
                            {
                                CreateSensorCube(cubePositionDL, cubeScaleDL, cubeRotation, sensor4CurrAvg, "obstacleDL");
                                CreateSensorCube(cubePositionDR, cubeScaleDR, cubeRotation, sensor2CurrAvg, "obstacleDR");
                            }
                        }
                    }
                    break;
                case MyEnum.Case_11:
                    {
                        float upAvg = (sensor1CurrAvg + sensor3CurrAvg) / 2f;
                        float downAvg = (sensor2CurrAvg + sensor4CurrAvg) / 2f;

                        Vector3 cubeScaleD = cubeMinScale + (downAvg - minDistance) * scalePerCm;
                        cubeScaleD.x *= 2f;
                        Vector3 cubeScaleU = cubeMinScale + (upAvg - minDistance) * scalePerCm;
                        cubeScaleU.x *= 2f;

                        Vector3 cubePositionD = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * downAvg / 50f + new Vector3(0f, minPosYDown + (downAvg - minDistance) * posYDownPerCm, 0f);
                        Vector3 cubePositionU = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * upAvg / 50f + new Vector3(0f, minPosYUp + (upAvg - minDistance) * posYUpPerCm, 0f);

                        if (sensorStatusScript.getTopSensorsStatus() && sensorStatusScript.getBottomSensorsStatus())
                        {
                            CreateSensorCube(cubePositionU, cubeScaleU, cubeRotation, upAvg, "obstacleU");
                            CreateSensorCube(cubePositionD, cubeScaleD, cubeRotation, downAvg, "obstacleD");
                        }
                        else
                        {
                            if (sensorStatusScript.getTopSensorsStatus())
                            {
                                CreateSensorCube(cubePositionU, cubeScaleU, cubeRotation, upAvg, "obstacleU");
                            }
                            else if (sensorStatusScript.getBottomSensorsStatus())
                            {
                                CreateSensorCube(cubePositionD, cubeScaleD, cubeRotation, downAvg, "obstacleD");
                            }
                        }
                    }
                    break;
                case MyEnum.Case_12:
                    {
                        float allAvg = (sensor1CurrAvg + sensor2CurrAvg + sensor3CurrAvg + sensor4CurrAvg) / 4f;

                        Vector3 cubePositionA = Camera.main.transform.position + new Vector3(0f, -1f, 0f) + Camera.main.transform.forward * allAvg / 50f + new Vector3(0f, minPosYDown + (allAvg - minDistance) * posYDownPerCm, 0f);

                        Vector3 cubeScaleA = cubeMinScale + (allAvg - minDistance) * scalePerCm;
                        cubeScaleA.x *= 2f;
                        cubeScaleA.y *= 2f;

                        if (sensorStatusScript.getTopSensorsStatus() && sensorStatusScript.getBottomSensorsStatus())
                            CreateSensorCube(cubePositionA, cubeScaleA, cubeRotation, allAvg, "obstacleA");
                    }
                    break;
                default:
                    break;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        ws = new WebSocket("ws://192.168.1.8/ws");

        ws.OnOpen += OnOpen;
        ws.OnMessage += OnMessage;
        ws.OnError += OnError;

        ws.Connect();
    }

    private void OnDestroy()
    {
        ws.Close();
    }

    private void OnOpen(object sender, EventArgs e)
    {
        Debug.Log("Connected");
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        Debug.Log("WebSocket error: " + e.Message);
    }

    private void OnMessage(object sender, MessageEventArgs e)
    {
        try
        {
            //Debug.Log("Received message -> " + e.Data);
            string[] parts = e.Data.Split(":");
            if (parts.Length == 2)
            {
                string sensorName = parts[0].Trim();
                int sensorId = int.Parse(sensorName.Replace("Sensor ", ""));
                distance = float.Parse(parts[1].Trim());
                messageReceived = true;

                switch (sensorId)
                {
                    case 1:
                        UpdateSensorValue(sensor1Queue, ref sensor1PrevAvg, ref sensor1CurrAvg, distance, sensorId);
                        if (Math.Abs(sensor1CurrAvg - sensor1PrevAvg) >= 3)
                        {
                            //Debug.Log(sensorName + " current: " + sensor1CurrAvg + ", previous: " + sensor1PrevAvg);
                            sensor1Obj = true;
                        }
                        break;
                    case 2:
                        UpdateSensorValue(sensor2Queue, ref sensor2PrevAvg, ref sensor2CurrAvg, distance, sensorId);
                        if (Math.Abs(sensor2CurrAvg - sensor2PrevAvg) >= 3)
                        {
                            //Debug.Log(sensorName + " current: " + sensor1CurrAvg + ", previous: " + sensor1PrevAvg);
                            sensor2Obj = true;
                        }
                        break;
                    case 3:
                        UpdateSensorValue(sensor3Queue, ref sensor3PrevAvg, ref sensor3CurrAvg, distance, sensorId);
                        if (Math.Abs(sensor3CurrAvg - sensor3PrevAvg) >= 3)
                        {
                            //Debug.Log(sensorName + " current: " + sensor1CurrAvg + ", previous: " + sensor1PrevAvg);
                            sensor3Obj = true;
                        }
                        break;
                    case 4:
                        UpdateSensorValue(sensor4Queue, ref sensor4PrevAvg, ref sensor4CurrAvg, distance, sensorId);
                        if (Math.Abs(sensor4CurrAvg - sensor4PrevAvg) >= 3)
                        {
                            //Debug.Log(sensorName + " current: " + sensor1CurrAvg + ", previous: " + sensor1PrevAvg);
                            sensor4Obj = true;
                        }
                        break;
                    default:
                        //Debug.Log("Invalid Sensor ID: " + sensorId);
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.Log(ex.Message);
        }
    }

    private void UpdateSensorValue(Queue<float> sensorQueue, ref float prevAvg, ref float currentAvg, float distance, int sensorId)
    {
        if (sensorQueue.Count == 2)
        {
            sensorQueue.Dequeue();
            sensorQueue.Enqueue(distance);
            if (currentAvg == 0)
            {
                prevAvg = sensorQueue.ElementAt(0);
            }
            else
            {
                prevAvg = currentAvg;
            }

            currentAvg = sensorQueue.ElementAt(1);
        }
        else
        {
            sensorQueue.Enqueue(distance);
        }
    }

    // Update is called once per frame
    void Update()
    {

        if (messageReceived)
        {
            if (!triangularModeStatusScript.getTriangularModeActive())
            {
                if (sensor1Obj)
                {
                    FindParticularCase();
                    //Debug.Log("sensor1: " + sensor1CurrAvg);
                }
                else if (sensor2Obj)
                {
                    FindParticularCase();
                    //Debug.Log("sensor2: " + sensor2CurrAvg);
                }
                else if (sensor3Obj)
                {
                    FindParticularCase();
                    //Debug.Log("sensor3: " + sensor3CurrAvg);

                }
                else if (sensor4Obj)
                {
                    FindParticularCase();
                    //Debug.Log("sensor4: " + sensor4CurrAvg);
                }

            
            }
            else
            {
                CreateSensorCubes();
                UIDistanceCase.text = "Triangular Mode";
                Debug.Log("wtfffasdsadas");
            }

            messageReceived = false;
            sensor1Obj = false;
            sensor2Obj = false;
            sensor3Obj = false;
            sensor4Obj = false;

        }

    }

    void FindParticularCase()
    {
        if (Math.Abs(sensor1CurrAvg - sensor3CurrAvg) <= 10)
        {
            mergeSensorCubesArray[0].forward = true;
            mergeSensorCubesArray[1].backward = true;
        }
        if (Math.Abs(sensor3CurrAvg - sensor2CurrAvg) <= 10)
        {
            mergeSensorCubesArray[1].forward = true;
            mergeSensorCubesArray[2].backward = true;
        }
        if (Math.Abs(sensor2CurrAvg - sensor4CurrAvg) <= 10)
        {
            mergeSensorCubesArray[2].forward = true;
            mergeSensorCubesArray[3].backward = true;
        }
        if (Math.Abs(sensor4CurrAvg - sensor1CurrAvg) <= 10)
        {
            mergeSensorCubesArray[3].forward = true;
            mergeSensorCubesArray[0].backward = true;
        }

        if (mergeSensorCubesArray[0].forward && mergeSensorCubesArray[1].forward && mergeSensorCubesArray[2].forward)
        {
            // obstacleA
            myValue = MyEnum.Case_12;
        }
        else if (mergeSensorCubesArray[0].forward && mergeSensorCubesArray[2].forward)
        {
            // obstacleU, obstacleD
            myValue = MyEnum.Case_11;
        }
        else if (mergeSensorCubesArray[1].forward && mergeSensorCubesArray[3].forward)
        {
            // obstacleL, obstacleR
            myValue = MyEnum.Case_10;
        }
        else if (mergeSensorCubesArray[2].forward && mergeSensorCubesArray[3].forward)
        {
            // obstacleL, obstacleD, obstacleUR
            myValue = MyEnum.Case_9;
        }
        else if (mergeSensorCubesArray[1].forward && mergeSensorCubesArray[2].forward)
        {
            // obstacleD, obstacleR, obstacleUL
            myValue = MyEnum.Case_8;
        }
        else if (mergeSensorCubesArray[0].forward && mergeSensorCubesArray[1].forward)
        {
            // obstacleR, obstacleU, obstacleDL
            myValue = MyEnum.Case_7;
        }
        else if (mergeSensorCubesArray[0].forward && mergeSensorCubesArray[3].forward)
        {
            // obstacleU, obstacleL, obstacleDR
            myValue = MyEnum.Case_6;
        }
        else if (mergeSensorCubesArray[2].forward)
        {
            // obstacleD, obstacleUL, obstacleUR
            myValue = MyEnum.Case_5;
        }
        else if (mergeSensorCubesArray[0].forward)
        {
            // obstacleU, obstacleDL, obstableDR
            myValue = MyEnum.Case_4;
        }
        else if (mergeSensorCubesArray[1].forward)
        {
            // obstacleR, obstacleUL, obstacleDL
            myValue = MyEnum.Case_3;
        }
        else if (mergeSensorCubesArray[3].forward)
        {
            // obstacleL, obstacleUR, obstacleDR
            myValue = MyEnum.Case_2;
        }
        else
        {
            // obstacleUL, obstacleUR, obstacleDL, obstacleDR
            myValue = MyEnum.Case_1;
        }

        UIDistanceCase.text = myValue.ToString();
        Debug.Log("myValue: " + myValue);
        CreateSensorCubes();

        mergeSensorCubesArray[0].forward = mergeSensorCubesArray[0].backward
            = mergeSensorCubesArray[1].forward = mergeSensorCubesArray[1].backward
            = mergeSensorCubesArray[2].forward = mergeSensorCubesArray[2].backward
            = mergeSensorCubesArray[3].forward = mergeSensorCubesArray[3].backward
            = false;
    }
}
