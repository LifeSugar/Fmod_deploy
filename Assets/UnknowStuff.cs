using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

[RequireComponent(typeof(StudioEventEmitter))]


public class UnknowStuff : MonoBehaviour
{
    private StudioEventEmitter emitter;

    private void Start()
    {
        emitter = AudioManager.instance.InitializeEventEmitters(FmodEvents.instance.unknowstuff,
            this.gameObject);
        emitter.Play();
        
    }
}
