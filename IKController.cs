using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class IKController : MonoBehaviour
{
#region variables
    [SerializeField][Header("Foot Settings")]
    public bool enableInverseKinematics;
    public bool enableFootKinematics;
    public Animator animator;
    public LayerMask layerMask;

    private Vector3 rightFootPosition, leftFootPosition, leftFootIK, rightFootIK;

    private Quaternion leftFootIKRotation, rightFootIKRotation;

    private float pelvisPositionY, rightFootY, leftFootY;

    private float footRayCastHeight = 1.14f; 
    private float rayCastDistance = 1.5f;
    
    private float pelvisSpeed = 0.28f;
    private float footSpeed = 0.5f;

#endregion


    private void FixedUpdate(){
        if(!enableInverseKinematics){return;}
        

        AdjustFootTarget(ref rightFootPosition, HumanBodyBones.RightFoot);
        AdjustFootTarget(ref leftFootPosition, HumanBodyBones.LeftFoot);

        FeetPositionFinder(rightFootPosition, ref rightFootIK, ref rightFootIKRotation);
        FeetPositionFinder(leftFootPosition, ref leftFootIK, ref leftFootIKRotation);

    }

    private void OnAnimatorIK(int layerIndex){
        if(!enableInverseKinematics){return;}
        MovePelvisHeight();
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
        if(enableFootKinematics){
            animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1 );
            animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1 );
        }

        MoveFeetToPosition(AvatarIKGoal.RightFoot, rightFootPosition, rightFootIKRotation, ref rightFootY);
        MoveFeetToPosition(AvatarIKGoal.LeftFoot, leftFootPosition, leftFootIKRotation, ref leftFootY);
    }

    void MoveFeetToPosition(AvatarIKGoal foot, Vector3 IKPosition, Quaternion IKFootRotation, ref float leftFootY){
        Vector3 targetPosition = animator.GetIKPosition(foot);
        if(IKPosition != Vector3.zero){
            targetPosition = transform.InverseTransformPoint(targetPosition);
            IKPosition = transform.InverseTransformPoint(IKPosition);

            float Y = Mathf.Lerp(leftFootY, IKPosition.y, footSpeed);
            targetPosition.y += Y;

            leftFootY = Y;

            targetPosition = transform.TransformPoint(targetPosition);

            animator.SetIKRotation(foot, IKFootRotation);

        }
        animator.SetIKPosition(foot, targetPosition);

    }

    private void MovePelvisHeight(){

        if(rightFootIK == Vector3.zero || leftFootIK == Vector3.zero || pelvisPositionY == 0){
            pelvisPositionY = animator.bodyPosition.y;
            return;
        }
        float leftOffset = leftFootIK.y - transform.position.y;
        float rightOffset = rightFootIK.y - transform.position.y;

        float totalOffset = (leftOffset < rightOffset)? leftOffset : rightOffset;

        Vector3 newPelvisPosition = animator.bodyPosition + Vector3.up * totalOffset;
        newPelvisPosition.y = Mathf.Lerp(pelvisPositionY, newPelvisPosition.y, pelvisSpeed);
        animator.bodyPosition = newPelvisPosition;
        pelvisPositionY = animator.bodyPosition.y;

    }

    private void FeetPositionFinder(Vector3 fromFootPosition, ref Vector3 footIK, ref Quaternion feetIKRotation ){

        RaycastHit footHit;
        if(Physics.Raycast(fromFootPosition, Vector3.down, out footHit, rayCastDistance + footRayCastHeight, layerMask)){
            footIK = fromFootPosition;
            footIK.y = footHit.point.y + pelvisPositionY;
            feetIKRotation = Quaternion.FromToRotation(Vector3.up, footHit.normal) * transform.rotation;

            return;
        }

        footIK = Vector3.zero;

    }


    private void AdjustFootTarget(ref Vector3 feetPosition, HumanBodyBones foot){

        feetPosition = animator.GetBoneTransform(foot).position;
        feetPosition.y = transform.position.y + footRayCastHeight;

    }

}
