using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GhostTrail : MonoBehaviour
{
    #region 组件引用与配置
    /// <summary>
    /// 用于获取角色位置和朝向的移动脚本。
    /// </summary>
    private PlayerController move;

    /// <summary>
    /// 用于获取角色当前动画、贴图等信息的脚本。
    /// </summary>
    private AnimatonScript anim;

    /// <summary>
    /// 本身对象的 SpriteRenderer 组件引用。
    /// （虽然当前脚本中未直接使用到 sr 的绘制功能，但可视情况保留）
    /// </summary>
    private SpriteRenderer sr;

    /// <summary>
    /// 存放所有“残影”子对象的父对象。
    /// </summary>
    public Transform ghostsParent;

    /// <summary>
    /// 残影生成时的颜色。
    /// </summary>
    public Color trailColor;

    /// <summary>
    /// 残影逐渐消失时过渡到的颜色。
    /// </summary>
    public Color fadeColor;

    /// <summary>
    /// 残影出现的时间间隔。
    /// </summary>
    public float ghostInterval;

    /// <summary>
    /// 残影淡出的时间。
    /// </summary>
    public float fadeTime;
    #endregion

    private void Start()
    {
        // 查找场景中的 AnimatonScript 脚本，并赋值给 anim
        anim = FindObjectOfType<AnimatonScript>();
        
        // 查找场景中的 Movement 脚本，用于获取角色位置、朝向等信息
        move = FindObjectOfType<PlayerController>();
        
        // 获取当前物体上的 SpriteRenderer 组件（可根据需求使用）
        sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// 创建残影序列：依次为每个子对象（残影）设置位置、翻转、贴图以及颜色等效果。
    /// </summary>
    public void ShowGhost()
    {
        // 使用 DOTween 创建一个顺序动画序列
        Sequence s = DOTween.Sequence();

        // 遍历残影父对象中的所有子对象
        for (int i = 0; i < ghostsParent.childCount; i++)
        {
            // 当前残影子对象的引用
            Transform currentGhost = ghostsParent.GetChild(i);
            
            // 1. 先将残影子对象的位置设为玩家当前位置
            s.AppendCallback(() => currentGhost.position = move.transform.position);
            
            // 2. 设置残影的翻转状态，与角色当前朝向一致
            s.AppendCallback(() => currentGhost.GetComponent<SpriteRenderer>().flipX = anim.sr.flipX);
            
            // 3. 设置残影的 Sprite 图像，与角色当前图像一致
            s.AppendCallback(() => currentGhost.GetComponent<SpriteRenderer>().sprite = anim.sr.sprite);
            
            // 4. 立即将残影颜色切换到 trailColor（duration = 0 表示瞬间切换）
            s.Append(currentGhost.GetComponent<SpriteRenderer>().material.DOColor(trailColor, 0));
            
            // 5. 开始淡出逻辑（使用 FadeSprite 方法来处理）
            s.AppendCallback(() => FadeSprite(currentGhost));
            
            // 6. 在下一个残影出现前，等待一个 ghostInterval 间隔
            s.AppendInterval(ghostInterval);
        }
    }

    /// <summary>
    /// 将传入的残影对象颜色从当前颜色渐变到 fadeColor，实现淡出的效果。
    /// </summary>
    /// <param name="current">当前残影 Transform</param>
    public void FadeSprite(Transform current)
    {
        // 停止对该残影材质颜色的所有 Tweener，防止冲突
        current.GetComponent<SpriteRenderer>().material.DOKill();
        
        // 将颜色在 fadeTime 秒内从 trailColor 过渡到 fadeColor
        current.GetComponent<SpriteRenderer>().material.DOColor(fadeColor, fadeTime);
    }
}
