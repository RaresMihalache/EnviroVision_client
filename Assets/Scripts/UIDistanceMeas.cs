using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIDistanceMeas : MonoBehaviour
{
    public int rayLength = 10;
    public float delay = 0.1f;

    public Material tMat;
    public GameObject pointer;

    public TextMeshProUGUI UIDistanceText;

    public List<Image> panels; // List to store references to the panels (UI lines)

    public float distance2Val = 0f;

    public float maxDistance = 4f;

    public float lineDistance = 0.085f;




    // Update is called once per frame
    void Update()
    {
        RaycastHit hit; // did the ray make contact with an object ?

        if (OVRInput.Get(OVRInput.Button.One))
        {
            if(Physics.Raycast(transform.position, transform.forward, out hit, rayLength * 10))
            {

                GameObject myLine = new GameObject();
                myLine.transform.position = transform.position;
                myLine.AddComponent<LineRenderer>();

                LineRenderer lr = myLine.GetComponent<LineRenderer>();
                lr.material = tMat;

                lr.startWidth = 0.01f;
                lr.endWidth = 0.01f;
                lr.SetPosition(0, transform.position);
                lr.SetPosition(1, hit.point);
                GameObject.Destroy(myLine, delay);

                pointer.transform.position = hit.point;

                float distance = Vector3.Distance(transform.position, hit.point) / 2;
                int lines = Mathf.RoundToInt(distance / lineDistance);
                for(int i = 0; i < panels.Count; i++)
                {
                    if(i < lines)
                    {
                        panels[i].color = Color.blue;
                    }
                    else
                    {
                        panels[i].color = Color.gray;
                    }
                }

                UIDistanceText.text = distance.ToString();

            }
        }
    }
}
