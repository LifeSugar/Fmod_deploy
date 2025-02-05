using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public int maxHP = 5;
    public int playerHP = 5;

    public bool isHurting = false;

    public Vector2 dir;

    private PlayerController move;

    private void Start()
    {
        move = GetComponent<PlayerController>();
        playerHP = maxHP;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        
        if (other.tag == "DamageTrigger" && !isHurting && !move.isDashing)
        {
            
            Debug.Log("gethurt");
            dir = new Vector2((transform.position.x - other.transform.position.x),0);
            isHurting = true;
            playerHP -= 1;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H) && playerHP < maxHP)
        {
            playerHP += 1;
        }
    }
}