using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Meta.WitAi;
using System;

public class ActivateVoice : MonoBehaviour
{

    [SerializeField]
    private Wit wit;
    // Start is called before the first frame update
    void Start()
    {
        wit = GetComponent<Wit>();

        if (wit != null)
        {
            wit.Activate();
        }
    }

    // Update is called once per frame
    void Update()
    {
        wit = GetComponent<Wit>();

        if (wit != null)
        {
            wit.Activate();
        }

    }
}
