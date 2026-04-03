using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class MainCharMovement : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public float MaxSpeed = 3f;
    public float dashSpeed = 8f;
    public float dashDuration = 0.3f;
    public float dashCooldown = 0.3f;

    private bool isMovingUp;
    private bool isMovingLeft;
    private bool isMovingDown;
    private bool isMovingRight;
    private bool isDashing;
    private bool canDash = true;
    private int activateRSprites; //Alternative version of sprites to be used while moving other direction. Example: Sword always stays on right hand.
    
    
    private Vector2 direction;
    private Rigidbody2D rBody;
    private Animator animator;
    
    public bool isWalking;
    void Start()
    {
        rBody = GetComponent<Rigidbody2D>();   
        animator = GetComponent<Animator>();
    }
    void FixedUpdate()
    {
        
        calcDirection();
        if (isDashing) return;

        rBody.linearVelocity = direction * MaxSpeed;
        isWalking = isMovingUp || isMovingLeft || isMovingDown || isMovingRight;
        animator.SetBool("isWalking",isWalking);
        animator.SetFloat("activateRSprites",activateRSprites);
    }
    void calcDirection()
    {
        direction = Vector2.zero;
        if (isMovingUp)
        {
            direction += new Vector2(-2,2);
        }
        if (isMovingLeft){
            direction += new Vector2(-2,0);
        }
        if (isMovingDown){
            direction += new Vector2(2,-2);
        }
        if (isMovingRight){
            direction += new Vector2(2,0);
        }
        direction = direction.normalized;
        if (direction.x < 0)
        {
            activateRSprites = 1;
        }
        else if (direction.x > 0)
        {
            activateRSprites = 0;
        }
    }

    private IEnumerator RunDash()
    {
        canDash = false;
        isDashing = true;
        animator.SetTrigger("dashTrigger");

        rBody.linearVelocity = direction * dashSpeed;
        yield return new WaitForSeconds(dashDuration);
        isDashing = false;
        yield return new WaitForSeconds(dashCooldown);
        
        animator.ResetTrigger("dashTrigger");
        canDash = true;
    }

    // Update is called once per frame
    public void MoveUp(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            isMovingUp = true;
        }
        else if (ctx.canceled)
        {
            isMovingUp = false;
        }
    }
    public void MoveLeft(InputAction.CallbackContext ctx)
    {
        
        if (ctx.performed)
        {
            isMovingLeft = true;
        }
        else if (ctx.canceled)
        {
            isMovingLeft = false;
        }
        
    }
    public void MoveDown(InputAction.CallbackContext ctx)
    {
        
        if (ctx.performed)
        {
            isMovingDown = true;
        }
        else if (ctx.canceled)
        {
            isMovingDown = false;
        }
    }
    public void MoveRight(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            isMovingRight = true;
        }
        else if (ctx.canceled)
        {
            isMovingRight = false;
        }
    }
    public void Dash(InputAction.CallbackContext ctx)
    {
        if (ctx.performed)
        {
            if (canDash) StartCoroutine(RunDash());
        }
    }
}
