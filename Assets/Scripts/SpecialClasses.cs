using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
class CapsuleColliderProfile
{
	public enum CapsuleColliderDirection : byte { X_Axis, Y_Axis, Z_Axis, };

	public Vector3 Center;
	public float Radius;
	public float Height;
	public CapsuleColliderDirection Direction;
}

class AnimatorParameters
{
	public const string Horizontal = "horizontal";
	public const string Vertical = "vertical";
	public const string Forward = "forward";
	public const string GoBack = "goBack";
	public const string IsGrounded = "isGrounded";
	public const string Jump = "jump";
	public const string Crouch = "crouch";
	public const string LowCrouch = "lowCrouch";
	public const string UseAnyGun = "useAnyGun";
	public const string UseKnife = "useKnife";
	public const string UsePistol = "usePistol";
	public const string useMachineGun = "useMachineGun";
	public const string climb = "climb";
	public const string climbLow = "climbLow";
	public const string climbLowMiddle = "climbMiddleLow";
	public const string climbMiddle = "climbMiddle";
	public const string climbHigh = "climbHigh";
	public const string distanceToClimbPoint = "distanceToClimbPoint";
	public const string finishClimb = "finishingClimb";
}

public class Comands
{
	public Vector3 Move;
	public Vector3 ClimbPosition;
	public Quaternion ClimbRotation;
	public Quaternion ExtraAngle;
	public int WeaponIndexDelta;
	public bool Crouch, LowCrouch, ClimbPointDetected, Jump, WalkSlow, Sprint, Interact, Aim, Shoot, ReloadGun;
}
