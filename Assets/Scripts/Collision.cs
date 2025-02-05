using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Collision : MonoBehaviour
{
    [Header("Layers")] 
    public LayerMask groundLayer; // 用于检测地面的 LayerMask

    [Space] 
    
    public bool onGround;     // 是否在地面上
    public bool onWall;       // 是否碰到墙壁
    public bool onRightWall;  // 是否在右侧墙壁上
    public bool onLeftWall;   // 是否在左侧墙壁上
    public int wallSide;      // 记录角色面向的墙壁方向 (-1 代表右墙，1 代表左墙)

    [Space]
    [Header("Collision")]

    public float collisionRadius = 0.25f; // 碰撞检测的半径
    public Vector2 bottomOffset, rightOffset, leftOffset; // 碰撞检测的偏移量（地面、左墙、右墙）
    private Color debugCollisionColor = Color.red; // 用于 Gizmos 绘制调试颜色

    private void Update()
    {
        // 通过 Physics2D.OverlapCircle 进行碰撞检测
        // 检测地面 (bottomOffset 位置)
        onGround = Physics2D.OverlapCircle((Vector2)transform.position + bottomOffset, collisionRadius, groundLayer);
        
        // 检测是否贴着墙壁（左侧或右侧）
        onWall = Physics2D.OverlapCircle((Vector2)transform.position + rightOffset, collisionRadius, groundLayer) 
                 || Physics2D.OverlapCircle((Vector2)transform.position + leftOffset, collisionRadius, groundLayer);

        // 分别检测左右墙壁
        onRightWall = Physics2D.OverlapCircle((Vector2)transform.position + rightOffset, collisionRadius, groundLayer);
        onLeftWall = Physics2D.OverlapCircle((Vector2)transform.position + leftOffset, collisionRadius, groundLayer);

        // 角色靠在右墙时 wallSide 设为 -1，靠在左墙时设为 1
        wallSide = onRightWall ? -1 : 1;
    }
    
    void OnDrawGizmos()
    {
        // 设定 Gizmos 颜色为红色
        Gizmos.color = Color.red;

        // 定义用于绘制 Gizmos 的位置数组
        var positions = new Vector2[] { bottomOffset, rightOffset, leftOffset };

        // 绘制底部检测点（用于地面检测）
        Gizmos.DrawWireSphere((Vector2)transform.position  + bottomOffset, collisionRadius);
        // 绘制右侧检测点（用于检测右墙）
        Gizmos.DrawWireSphere((Vector2)transform.position + rightOffset, collisionRadius);
        // 绘制左侧检测点（用于检测左墙）
        Gizmos.DrawWireSphere((Vector2)transform.position + leftOffset, collisionRadius);
    }
}
