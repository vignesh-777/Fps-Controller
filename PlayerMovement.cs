
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    //Component    
    Rigidbody rb;


    [Header("Movement")]
    public float moveSpeed;
    public float walkSpeed;
    public float sprintSpeed;
    public float groundDrag;
    float horizontalInput, verticalInput;
    Vector3 moveDirection;


    [Header("Jump")]
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    bool readyToJump;

    [Header("Crouching")]
    public float crouchSpeed;
    public float crouchYScale;
    private float startYScale;

    [Header("Keybinds")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode SprintKey = KeyCode.LeftShift;
    public KeyCode crouchKey = KeyCode.C;

    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle;
    private RaycastHit slopeHit;
    private bool exitingSlope;

    [Space]
    public Transform orientation;

    enum MoveState{
        waking,
        sprinting,
        crouching,
        air
    }
    MoveState state ;
    #region  Built_Function
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        readyToJump = true;

        startYScale = transform.localScale.y;
    }

    private void Update()
    {
        // ground check
        grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, whatIsGround);

        

        // handle drag
        if (grounded)
            rb.drag = groundDrag;
        else
            rb.drag = 0;

        //Start crouch
        if(Input.GetKeyDown(crouchKey)){
            transform.localScale = new Vector3(transform.localScale.x,crouchYScale,transform.localScale.z);            
            //player in air for few sec
            rb.AddForce(Vector3.down * 5f,ForceMode.Impulse);
        }
        
        if(Input.GetKeyUp(crouchKey)){
            transform.localScale = new Vector3(transform.localScale.x,startYScale,transform.localScale.z);
        }

        MyInput();
        SpeedControl();
        StateHandler();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }
    #endregion
    
    #region StateHandler
    private void StateHandler(){


        //Mode - Crouching
        if(Input.GetKey(crouchKey)){
            state = MoveState.crouching;
            moveSpeed = crouchSpeed;
        }
        //Mode - Walking
        else if(grounded){
            state = MoveState.waking;
            moveSpeed = walkSpeed;
        }

        //Mode - Sprinting
        else if(grounded && Input.GetKey(SprintKey)){
            state = MoveState.sprinting;
            moveSpeed = sprintSpeed;
        }

        //Mode - air
        else{
            state = MoveState.air;
        }
    }
    #endregion
    private void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // when to jump
        if(Input.GetKey(jumpKey) && readyToJump && grounded)
        {
            readyToJump = false;
            Jump();

            Invoke(nameof(ResetJump), jumpCooldown);
        }

    }

    #region Movement
    private void MovePlayer()
    {
        // calculate movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;


        //on Slope
        if(OnSlope()){
            rb.AddForce(GetSlopeMoveDirection() * walkSpeed * 20f ,ForceMode.Force);
            if(rb.velocity.y > 0){
                rb.AddForce(Vector2.down * 5f, ForceMode.Impulse);
            }
        }


        // on ground
        if(grounded){            
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
        }


        // in air
        else if(!grounded){
            rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
        rb.useGravity = !OnSlope();

            
    }

    private void SpeedControl()
    {
        if(OnSlope() && !exitingSlope){
            if(rb.velocity.magnitude > walkSpeed){
                rb.velocity = rb.velocity.normalized * walkSpeed;
            }

        }else{
            Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

            // limit velocity if needed
            if(flatVel.magnitude > moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * moveSpeed;
                rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
            }

        }
        
        
    }
    #endregion
    #region Jump_System
    private void Jump()
    {
        // reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);        
        exitingSlope = true;

        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }
    private void ResetJump()
    {
        readyToJump = true;
        exitingSlope = false;
    }
    #endregion

    private bool OnSlope(){
        if(Physics.Raycast(transform.position,Vector3.down,out slopeHit,playerHeight * 0.5f + 0.3f)){
            float angle = Vector3.Angle(Vector3.up,slopeHit.normal);
            return angle>maxSlopeAngle && angle!=0;
        }
        return false;
    }
    private Vector3 GetSlopeMoveDirection(){
        return Vector3.ProjectOnPlane(moveDirection,slopeHit.normal).normalized;
    }
}