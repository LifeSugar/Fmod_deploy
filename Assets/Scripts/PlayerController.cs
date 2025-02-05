using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using DG.Tweening;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class PlayerController : MonoBehaviour
{
    #region 组件引用
    /// <summary>
    /// 用于碰撞检测的组件
    /// </summary>
    private Collision coll;

    /// <summary>
    /// Rigidbody2D 组件，用于物理移动
    /// </summary>
    [HideInInspector]
    public Rigidbody2D rb;

    /// <summary>
    /// 角色动画控制脚本
    /// </summary>
    private AnimatonScript anim;

    /// <summary>
    /// 胶囊碰撞体组件
    /// </summary>
    private CapsuleCollider2D caps;

    /// <summary>
    /// 存储角色数据信息（例如生命值等）
    /// </summary>
    private PlayerData _playerData;
    #endregion

    #region 移动与状态标记
    // 标记角色是否正处于跳跃上升阶段
    public bool isJumpingUp;

    // 攻击冷却期间禁止攻击的标记
    private bool attackBaned;

    // 标记相机是否正在震动
    private bool cameraShaking;

    // 标记角色是否死亡
    private bool isDead;

    // 标记角色是否触地
    private bool groundTouch;

    // 标记角色是否已经进行了冲刺
    public bool hasDashed;
    public Color fadedColor;

    // 角色朝向（1代表右，-1代表左）
    public int side = 1;
    #endregion

    #region 音效

    //冲刺音效
    [SerializeField] private EventReference dashSound;
    
    //脚步音效实例
    private EventInstance playerStepsInstance;

    #endregion

    #region 移动属性
    [Space]
    [Header("Properties")]
    public float speed = 10;         // 移动速度
    public float jumpForce = 50;     // 跳跃力度
    public float slideSpeed = 5;     // 墙壁滑行速度
    public float wallJumpLerp = 10;  // 墙跳时速度插值系数
    public float dashSpeed = 20;     // 冲刺速度

    // 各种状态标记
    public bool canMove;     // 是否允许移动
    public bool wallGrab;    // 是否抓住墙壁（原本用于抓墙，现可留作其他用途）
    public bool wallJumped;  // 是否进行了墙跳
    public bool wallSlide;   // 是否正在墙上滑行
    public bool isDashing;   // 是否正在冲刺
    public bool canAttack;   // 是否允许攻击
    #endregion

    #region 粒子效果与其他调试属性
    [Space]
    [Header("Polish")]
    public ParticleSystem dashParticle;     // 冲刺粒子效果
    public ParticleSystem jumpParticle;     // 跳跃粒子效果
    public ParticleSystem wallJumpParticle; // 墙跳粒子效果
    public ParticleSystem slideParticle;    // 墙滑粒子效果

    [Space]
    public float invincibleTime = 0.9f;  // 受伤后无敌时间
    #endregion

    #region 单例

    public static PlayerController instance {get; private set;}

    private void Awake()
    {
        if (instance != null)
        {
            Debug.Log("there is already an instance of PlayerController");
        }
        
        instance = this;
    }

    #endregion

    #region Unity内置方法
    private void Start()
    {
        // 获取各个组件
        coll = GetComponent<Collision>();
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<AnimatonScript>();
        caps = GetComponent<CapsuleCollider2D>();
        _playerData = GetComponentInChildren<PlayerData>();

        // 初始化时关闭冲刺粒子效果
        dashParticle.Stop();
        
        //根据脚本音效创建实例
        playerStepsInstance = AudioManager.instance.CreatEventInstance(FmodEvents.instance.playerSteps);
    }

    private void Update()
    {
        // 读取玩家输入
        float x = Input.GetAxis("Horizontal");  // 水平方向输入 (-1：左，1：右)
        float y = Input.GetAxis("Vertical");      // 垂直方向输入
        Vector2 dir = new Vector2(x, y);          // 组成移动方向向量

        // 判断是否正在进行上升跳跃
        isJumpingUp = (Input.GetButton("Jump") && rb.velocity.y > 0);

        // 当角色不处于受伤状态且未死亡时，处理移动与其他逻辑
        if (!_playerData.isHurting && !isDead)
        {
            cameraShaking = false;

            // 【新增】按下 Fire3 键冲刺（Dash）
            if (Input.GetButtonDown("Fire3") && canMove && !isDashing && !hasDashed)
            {
                Dash();
            }

            // 非冲刺状态下执行行走逻辑
            if (!isDashing)
            {
                Walk(dir);
            }

            // 更新动画参数
            anim.SetHorizontalMovement(x, y, rb.velocity.y);

            // 以下代码原本用于处理抓墙操作，
            // 这里保留但不影响冲刺功能（如果需要可调整或移除）
            if (Input.GetButtonUp("Fire3") || !coll.onWall || !canMove)
            {
                wallGrab = false;
                wallSlide = false;
            }

            // 角色在地面时重置墙跳状态，并启用 BetterJumping 控制
            if (coll.onGround && !isDashing)
            {
                wallJumped = false;
                GetComponent<BetterJumping>().enabled = true;
            }

            // 处理墙上状态（非地面）
            if (coll.onWall && !coll.onGround)
            {
                hasDashed = false;
                if (x != 0 && !wallGrab)
                {
                    if (!isJumpingUp)
                    {
                        wallSlide = true;
                        WallSlide();
                    }
                }
            }

            // 若不在墙上或处于地面，则停止墙滑
            if (!coll.onWall || coll.onGround)
            {
                wallSlide = false;
            }

            // 处理跳跃输入（非冲刺、非攻击、非反弹状态下）
            if (Input.GetButtonDown("Jump") && !isDashing && !anim.isAttackingDown && !GetComponent<BetterJumping>().isBouncing)
            {
                anim.SetTrigger("jump");

                if (coll.onGround)
                    Jump(Vector2.up, false);
                else if (coll.onWall && !coll.onGround)
                    WallJump();
            }

            // 触地检测，首次触地时触发相关逻辑
            if (coll.onGround && !groundTouch)
            {
                GroundTouch();
                groundTouch = true;
            }
            if (!coll.onGround && groundTouch)
            {
                groundTouch = false;
            }

            // 更新墙滑粒子效果
            WallParticle(y);

            // 如果角色处于抓墙、墙滑状态或无法移动，则不更新角色翻转
            if (wallGrab || wallSlide || !canMove)
                return;

            // 根据水平输入更新角色朝向和碰撞体偏移
            if (x > 0 && !wallSlide)
            {
                side = 1;
                anim.Flip(side);
                caps.offset = new Vector2(0.15f, -0.05f);
            }
            else if (x < 0 && !wallSlide)
            {
                side = -1;
                anim.Flip(side);
                caps.offset = new Vector2(-0.18f, -0.05f);
            }

            // 攻击时降低移动速度
            if ((anim.isAttacking1 && coll.onGround) || anim.isAttacking2 || anim.isAttacking3)
                speed = 2f;
            else
                speed = 10f;
            
            UpdateAudio();
        }

        // 角色受伤逻辑
        if (_playerData.isHurting)
        {
            if (!cameraShaking)
            {
                cameraShaking = true;
            }
            if (_playerData.playerHP > 0)
                StartCoroutine(Hurting());
            else
                StartCoroutine(Death());
        }

        if (hasDashed)
            this.GetComponentInChildren<SpriteRenderer>().color = fadedColor;
        else
            this.GetComponentInChildren<SpriteRenderer>().color = Color.white;

    }
    #endregion

    #region 移动方法
    /// <summary>
    /// 角色触地处理：重置冲刺状态、播放触地粒子效果，并结束反弹状态。
    /// </summary>
    void GroundTouch()
    {
        hasDashed = false;
        jumpParticle.Play();
        StartCoroutine(EndBounce());
    }

    /// <summary>
    /// 处理墙跳逻辑：根据墙壁位置调整朝向，并执行跳跃。
    /// </summary>
    private void WallJump()
    {
        // 根据角色当前朝向和墙壁所在侧，必要时翻转角色
        if ((side == 1 && coll.onRightWall) || (side == -1 && !coll.onRightWall))
        {
            side *= -1;
            anim.Flip(side);
        }

        // 临时禁用移动，避免控制冲突
        StopCoroutine(DisableMovement(0));
        StartCoroutine(DisableMovement(0.1f));

        // 计算墙跳方向（综合向上和侧面）
        Vector2 wallDir = coll.onRightWall ? Vector2.left : Vector2.right;
        Jump((Vector2.up / 1.5f + wallDir / 1.5f), true);

        wallJumped = true;
    }

    /// <summary>
    /// 处理墙壁滑行：在墙上施加一个固定的向下速度。
    /// </summary>
    private void WallSlide()
    {
        // 若角色当前墙侧与朝向不符，翻转动画
        if (coll.wallSide != side)
            anim.Flip(side * -1);

        if (!canMove)
            return;

        rb.velocity = new Vector2(rb.velocity.x, -slideSpeed);
    }

    /// <summary>
    /// 角色行走逻辑，根据输入设置水平速度。
    /// </summary>
    /// <param name="dir">输入的移动方向</param>
    private void Walk(Vector2 dir)
    {
        if (!canMove)
            return;

        if (wallGrab)
            return;

        if (!wallJumped)
            rb.velocity = new Vector2(dir.x * speed, rb.velocity.y);
        else
            // 使用插值使墙跳后的移动更加平滑
            rb.velocity = Vector2.Lerp(rb.velocity, new Vector2(dir.x * speed, rb.velocity.y), wallJumpLerp * Time.deltaTime);
    }

    /// <summary>
    /// 角色跳跃逻辑，处理普通跳跃与墙跳。
    /// </summary>
    /// <param name="dir">跳跃方向</param>
    /// <param name="wall">是否为墙跳</param>
    private void Jump(Vector2 dir, bool wall)
    {
        // 根据墙侧设置粒子效果的显示方向
        slideParticle.transform.parent.localScale = new Vector3(ParticleSide(), 1, 1);
        ParticleSystem particle = wall ? wallJumpParticle : jumpParticle;

        // 重置垂直速度后施加跳跃力
        rb.velocity = new Vector2(rb.velocity.x, 0);
        rb.velocity += dir * jumpForce;

        particle.Play();
    }
    #endregion

    #region 辅助方法和协程
    /// <summary>
    /// 暂时禁用移动，用于墙跳等短暂无法控制的状态。
    /// </summary>
    /// <param name="time">禁用时间</param>
    IEnumerator DisableMovement(float time)
    {
        canMove = false;
        yield return new WaitForSeconds(time);
        canMove = true;
    }

    /// <summary>
    /// 用于 DOTween 的回调函数，调整 Rigidbody 的拖拽系数。
    /// </summary>
    /// <param name="x">拖拽值</param>
    void RigidbodyDrag(float x)
    {
        rb.drag = x;
    }

    /// <summary>
    /// 更新墙滑粒子效果，根据角色状态和输入调整粒子颜色和方向。
    /// </summary>
    /// <param name="vertical">垂直输入值</param>
    void WallParticle(float vertical)
    {
        var main = slideParticle.main;

        if (wallSlide || (wallGrab && vertical < 0))
        {
            slideParticle.transform.parent.localScale = new Vector3(ParticleSide(), 1, 1);
            main.startColor = Color.white;
        }
        else
        {
            main.startColor = Color.clear;
        }
    }

    /// <summary>
    /// 根据角色所处的墙面，确定粒子效果的方向（缩放）。
    /// </summary>
    /// <returns>在右墙返回1，否则返回-1</returns>
    int ParticleSide()
    {
        return coll.onRightWall ? 1 : -1;
    }

    /// <summary>
    /// 处理角色冲刺逻辑：设置速度、播放动画和粒子效果。
    /// </summary>
    private void Dash()
    {
        // 以下注释代码可用于开启相机震动和涟漪效果：
        // Camera.main.transform.DOComplete();
        // Camera.main.transform.DOShakePosition(0.2f, 0.5f, 14, 90, false, true);
        // FindObjectOfType<RippleEffect>().Emit(Camera.main.WorldToViewportPoint(transform.position));

        hasDashed = true;
        anim.SetTrigger("dash");

        // 设置冲刺速度（根据角色朝向）
        rb.velocity = Vector2.right * side * dashSpeed;
        StartCoroutine(DashWait());
        AudioManager.instance.PlayOneShot(dashSound, this.transform.position);
    }

    /// <summary>
    /// 冲刺协程：处理冲刺期间状态、调整拖拽、重置重力等。
    /// </summary>
    IEnumerator DashWait()
    {
        // 显示残影效果
        FindObjectOfType<GhostTrail>().ShowGhost();
        StartCoroutine(GroundDash());

        // 使用 DOTween 渐变调整拖拽值
        DOVirtual.Float(14, 0, 0.8f, RigidbodyDrag);

        dashParticle.Play();
        rb.gravityScale = 0;
        GetComponent<BetterJumping>().enabled = false;
        wallJumped = true;
        isDashing = true;

        yield return new WaitForSeconds(0.3f);

        dashParticle.Stop();
        rb.gravityScale = 3;
        GetComponent<BetterJumping>().enabled = true;
        wallJumped = false;
        isDashing = false;
    }

    /// <summary>
    /// 短暂延时后检测若角色处于地面则重置冲刺状态。
    /// </summary>
    IEnumerator GroundDash()
    {
        yield return new WaitForSeconds(0.15f);
        if (coll.onGround)
            hasDashed = false;
    }

    /// <summary>
    /// 结束反弹状态的协程，延时后关闭反弹标记。
    /// </summary>
    IEnumerator EndBounce()
    {
        yield return new WaitForSeconds(0.2f);
        GetComponent<BetterJumping>().isBouncing = false;
    }

    /// <summary>
    /// 攻击延迟协程（用于解除攻击禁止状态）。
    /// </summary>
    IEnumerator AttackDelay()
    {
        yield return new WaitForSeconds(0.2f);
        attackBaned = false;
    }

    /// <summary>
    /// 受伤逻辑：应用轻微击退效果并进入无敌时间。
    /// </summary>
    IEnumerator Hurting()
    {
        rb.gravityScale = 3;
        rb.velocity = new Vector2(_playerData.dir.normalized.x * 0.5f, -2f);
        yield return new WaitForSeconds(invincibleTime);
        _playerData.isHurting = false;
    }

    /// <summary>
    /// 角色死亡逻辑：停止物理运动并触发死亡动画。
    /// </summary>
    IEnumerator Death()
    {
        isDead = true;
        rb.gravityScale = 0;
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(invincibleTime);
        anim.SetTrigger("Death");
    }
    #endregion

    #region 音效控制

    private void UpdateAudio()
    {
        if (rb.velocity.x != 0 && coll.onGround)
        {
            PLAYBACK_STATE playbackState;
            playerStepsInstance.getPlaybackState(out playbackState);
            if (playbackState.Equals(PLAYBACK_STATE.STOPPED))
            {
                playerStepsInstance.start();
            }
        }
        else
        {
            playerStepsInstance.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }

    #endregion
}
