using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using FMODUnity;

public class Star : MonoBehaviour
{
    public bool selfDestroy = false;
    [SerializeField] 
    // private EventReference collectSound;
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null && PlayerController.instance.hasDashed)
        {
            AudioManager.instance.PlayOneShot(FmodEvents.instance.starCollected, this.transform.position);
            PlayerController.instance.hasDashed = false;
            if (selfDestroy)
                Destroy(this.gameObject);
        }
    }
}
