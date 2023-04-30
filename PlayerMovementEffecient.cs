using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovementEffecient : MonoBehaviour
{
    #region variables
    //movement vectors 
[SerializeField][Header("Input Vectors")]
   public Vector3 MoveInput3D;
   public Vector2 MoveInput;
//movement-centered
[SerializeField] [Header("Speed Settings")]
    public float rotSpeed;
    public float gravity;
    public float Speed;
    public float JumpSpeed;

[SerializeField] [Header("Jump/Grounded")]
    public bool isGrounded;
    public float sphereRadius;
    public LayerMask mask;
    public Transform checker;
    public Transform checker2;

[SerializeField][Header("External Scripts")]

    private float placeHolder;

//Components
    CharacterController Controller;
    Animator Animator;
    PlayerInput playerInput;
    RaycastHit h;


[SerializeField][Header("StrafeNum")]
    public int move;
    


[SerializeField][Header("Movement Booleans")]
    public bool isMoving; public bool isRunning; public bool isJumpPress; public bool isShifting; public bool fixedAngle; public bool isJumping;

//animation
    int joggingHash, runningHash, jumpingHash, StrafeHash, ShiftDirX, fixedAngle2, groundHash;


    private float time = 0f;



[SerializeField][Header("Foot Settings")]
    public bool enableInverseKinematics;
    public bool enableFootKinematics;
    public bool showDebugger;
    public Animator animator;

    private Vector3 leftFootIK, rightFootIK, rightFootPos, leftFootPos;

    private Quaternion leftFootIKRotation, rightFootIKRotation;
    private RaycastHit footHit;

    [Range(0, 3)] public float ActiveDistance;
    public Transform Foot1;
    public Transform Foot2;

    [Range(0, 3)]public float pelvisSpeed;
    [Range(0, 3)]public float footSpeed;
    [Range(0, 1)]public float footIncrement;
    [Range(0, 1)]public float allowedOffSet;


#endregion
    
    #region InputAwake
    void Awake(){
        Controller = GetComponent<CharacterController>();
        Animator = GetComponent<Animator>();
        playerInput = new PlayerInput();


        joggingHash = Animator.StringToHash("isJogging");
        runningHash = Animator.StringToHash("isRunning");
        jumpingHash = Animator.StringToHash("isJumping");
        StrafeHash = Animator.StringToHash("isShifting");
        ShiftDirX = Animator.GetInteger("ShiftDirX");
        fixedAngle2 = Animator.StringToHash("fixedAngle");
        groundHash = Animator.StringToHash("isGrounded");

        playerInput.Controls.Moves.started += MovementInput;
        playerInput.Controls.Moves.canceled += MovementInput;
        playerInput.Controls.Moves.performed += MovementInput;
        playerInput.Controls.Run.started += RunInput;
        playerInput.Controls.Run.canceled += RunInput;
        playerInput.Controls.Jump.started += JumpInput;
        playerInput.Controls.Jump.canceled += JumpInput;
        playerInput.Controls.Crouching.started += ShiftPut;
        playerInput.Controls.Crouching.canceled += ShiftPut;
        playerInput.Controls.Strafe.started += Strafe;
        playerInput.Controls.Strafe.canceled += Strafe;


    }

    #endregion

    #region LifeCycles
    void Start(){
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update(){
        if(isGrounded){
        GetMove();}
        Rotation();
        Animation();
        
        VertMove();
        Controller.Move(MoveInput3D * Time.deltaTime);



    }
    #endregion
 
    #region Movement
    public void GetMove(){
        MoveInput3D = new Vector3(MoveInput.x, 0, MoveInput.y);
        float speed = isRunning? Speed * 2 : Speed;
        MoveInput3D = CameraMove(MoveInput3D) * speed;
    }

    void Animation(){
        if(!isShifting){
            rotSpeed = 15f;
            Animator.SetBool(StrafeHash, false);
            Animator.SetInteger("ShiftDirX", 0);
            if(isMoving && !isRunning){
                Animator.SetBool(joggingHash, true);
                Animator.SetBool(runningHash, false);
            } else if(isMoving && isRunning){
                Animator.SetBool(runningHash, true);
                Animator.SetBool(joggingHash, true);
            } else if(!isMoving){
                Animator.SetBool(joggingHash, false);
                Animator.SetBool(runningHash, false);
            }
            
        } else{
            Animator.SetBool(StrafeHash, true);
            if(isMoving && !fixedAngle){
                Animator.SetBool(StrafeHash, true);
                Animator.SetBool(joggingHash, true);

            } else if(!isMoving){
                Animator.SetBool(StrafeHash, false);
                Animator.SetBool(joggingHash, false);
            }
            
            MoveInput3D = (MoveInput3D/3); rotSpeed = 5f;
            Animator.SetBool(StrafeHash, true);


        }
    }
    Vector3 CameraMove(Vector3 move){
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        forward.y = MoveInput3D.y; right.y =MoveInput3D.y;
        forward = forward.normalized; right = right.normalized;

        Vector3 CameraMove = move.z * forward + move.x * right;
        return CameraMove;
    }
//Rotation/Player orientation
    void Rotation(){
        Vector3 LookAt = new Vector3(MoveInput3D.x, 0f, MoveInput3D.z);
        

        Quaternion currentAngle = transform.rotation;
        
        if(isMoving){
            Quaternion MoveAngle = Quaternion.LookRotation(LookAt);
            transform.rotation = Quaternion.Slerp(currentAngle, MoveAngle, rotSpeed * Time.deltaTime);
        }
    }

    void VertMove(){
       isGrounded = Physics.CheckSphere(checker.position, sphereRadius, mask) || Physics.CheckSphere(checker2.position, 0.7f, mask);  
        if(isGrounded){
            MoveInput3D.y = -9.8f;
            Animator.SetBool(groundHash, true);
            enableInverseKinematics =  true;
        } else if(!isGrounded && !isJumping){
            enableInverseKinematics = false;
            MoveInput3D.y -= gravity;
            if(MoveInput3D.y <= -30f){MoveInput3D.y = -30f;}
            time += Time.deltaTime;
            if(time >= 0.1f){
                Animator.SetBool(groundHash, false);
            }
            
            
        }
    }

    void OnDrawGizmos(){

            Gizmos.DrawWireSphere(Foot1.position, 0.1f);
            Gizmos.DrawWireSphere(Foot2.position, 0.1f);

    }


#endregion

    #region IK


    void OnAnimatorIK(int layerIndex){
        Animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
        Animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1f);

        Animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
        Animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1f);

        MoveFeet(AvatarIKGoal.RightFoot, Foot1, ref rightFootPos);
        MoveFeet(AvatarIKGoal.LeftFoot, Foot2, ref leftFootPos);
        
    }

    void MoveFeet(AvatarIKGoal foot, Transform FootSphere, ref Vector3 footPosition){
        Vector3 FootIKPosition = Animator.GetIKPosition(foot);
        RaycastHit ray;
        if(showDebugger){
            Debug.DrawLine(FootSphere.position, FootSphere.position + Vector3.down * (ActiveDistance), Color.yellow);
        }
print(Physics.Raycast(FootSphere.position, Vector3.down, out ray, ActiveDistance, mask));
        if(Physics.Raycast(FootSphere.position, Vector3.down, out ray, ActiveDistance, mask)){
            
            Vector3 contactPoint = ray.point;
            if(Mathf.Abs(contactPoint.y - FootIKPosition.y) < allowedOffSet){
                return;
            } else{
                FootSphere.position = new Vector3(FootSphere.position.x, contactPoint.y, FootSphere.position.z);

                FootIKPosition = FootSphere.position;
                
            }
        }
        Animator.SetIKPosition(foot, FootIKPosition);
    }



    #endregion

    #region InputCallBacks
    void MovementInput(InputAction.CallbackContext Input){
        MoveInput = Input.ReadValue<Vector2>();
        isMoving = (MoveInput != Vector2.zero);
    }

    void JumpInput(InputAction.CallbackContext Input){
        isJumpPress = Input.ReadValueAsButton();
    }

    void RunInput(InputAction.CallbackContext Input){
        isRunning = Input.ReadValueAsButton();
    }
    void ShiftPut(InputAction.CallbackContext Input){
        isShifting = Input.ReadValueAsButton();
    }
    void Strafe(InputAction.CallbackContext Input){
        fixedAngle = Input.ReadValueAsButton();
    }
    void OnEnable(){
        playerInput.Controls.Enable();
    }
    void onDisable(){
        playerInput.Controls.Disable();
    }
    #endregion
}


//add in strafing and crouching, as well as a jump that allows for all types(strafe jump to side, crouch jump forward and backflip.)