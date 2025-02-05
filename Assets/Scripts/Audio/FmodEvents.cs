using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

/// <summary>
/// 另一种音效触发方式
/// </summary>
public class FmodEvents : MonoBehaviour
{
    public static FmodEvents instance;

    [field: Header("Ambience")] 
    [field: SerializeField] public EventReference ambientWind {get; private set;}
    
    [field: Header("EnvironmentSFX")]
    [field: SerializeField] public EventReference starCollected { get; private set; }
    [field: SerializeField] public EventReference unknowstuff { get; private set; }
    
    [field: Header("PlayerSFX")]
    [field: SerializeField] public EventReference playerSteps { get; private set; }

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("More than one instance of FmodEventManager found!");
        }
        instance = this;
    }
    
    
}
