using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaChangeTrigger : MonoBehaviour
{
    [SerializeField] private string parameterName;
    [SerializeField] private float parameterValue;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null)
        {
            AudioManager.instance.SetAmbientParameter(parameterName, parameterValue);
        }
    }
    
}
