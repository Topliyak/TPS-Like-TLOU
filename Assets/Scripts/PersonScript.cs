using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PersonScript : MonoBehaviour
{
	[HideInInspector] public Vector3 AimPoint;

	public bool IsGrounded { get; private set; }
	public bool Crouch { get; private set; }
	public bool LowCrouch { get; private set; }
	public bool Aim { get; private set; }

	private Vector3 _GroundNormal;
	private bool _GoBack;
	private CapsuleCollider _Collider;
	private Rigidbody _Rigidbody;
	private Animator _Animator;
	private bool _UseAnyGun;
	private bool _UseKnife;
	private bool _UsePistol;
	private bool _UseMachineGun;
	private const string _HorizontalAnimatorParameter = "horizontal";
	private const string _VerticalAnimatorParameter = "vertical";
	private const string _ForwardAnimatorParameter = "forward";
	private const string _GoBackAnimatorParameter = "goBack";
	private const string _IsGroundedAnimatorParameter = "isGrounded";
	private const string _JumpAnimatorParameter = "jump";
	private const string _CrouchAnimatorParameter = "crouch";
	private const string _LowCrouchAnimatorParameter = "lowCrouch";
	private const string _UseAnyGunAnimatorParameter = "useAnyGun";
	private const string _UseKnifeAnimatorParameter = "useKnife";
	private const string _UsePistolAnimatorParameter = "usePistol";
	private const string _useMachineGunAnimatorParameter = "useMachineGun";
	private FirearmScript _CurrentWeapon;
	private Transform _LeftHandIK;
	private Transform _RightHandIK;
	private Transform _RightHand;
	private float _LeftHandIKWeight;
	private float _TopRigWeight;
	private Vector3 _Move;
	private float _Forward;
	private float _Jump;
	private float _IsGroundedRayDistance;
	private CapsuleColliderProfile _CurrentCapsuleProfule;

	[Header("IsGrounded check")]
	[SerializeField] float _IsGroundedDefaultRayDistance;
	[SerializeField] LayerMask _IsGroundedLayerMask;

	[Header("CapsuleCollider Profiles")]
	[SerializeField] float _CapsuleRadiusLerp;
	[SerializeField] float _CapsuleHeightLerp;
	[SerializeField] float _CapsuleCenterLerp;
	[SerializeField] CapsuleColliderProfile _StandCapsule;
	[SerializeField] CapsuleColliderProfile _CrouchCapsule;
	[SerializeField] CapsuleColliderProfile _LowCrouchCapsule;

	[Header("Move")]
	[SerializeField] float _TurnSpeed;
	[SerializeField] float _Acceleration;
	[SerializeField] float _Decceleration;
	[SerializeField] float _JumpForce;

	[Header("Weapon")]
	public FirearmScript Weapon;
	[SerializeField] Vector3 _WeaponLocalPositionInHand;
	[SerializeField] Vector3 _WeaponLocalRotationInHand;

	[Header("IK")]
	[SerializeField] float _TopRigWeightLerp;

	[Header("AimLookAtWeight")]
	[SerializeField] [Range(0, 1)] float _AimWeight;
	[SerializeField] [Range(0, 1)] float _AimBodyWeight;
	[SerializeField] [Range(0, 1)] float _AimHeadWeight;

	private void Start()
	{
		_Animator = GetComponent<Animator>();
		_Rigidbody = GetComponent<Rigidbody>();
		_Collider = GetComponent<CapsuleCollider>();
		InitBones();
	}

	private void Update()
	{
		if (Weapon != _CurrentWeapon) OnWeaponChanged();
	}

	private void OnAnimatorIK(int layerIndex)
	{
		SetLookAtWeight();
		_Animator.SetLookAtPosition(AimPoint);
		PoseLeftHandOnWeapon();
		TurnRightHandToTarget();
	}

	public void SetComands(Comands comands)
	{
		CheckIsGrounded();
		ProcessAim(comands.Aim);
		ProcessVelocity(comands.Move, comands.Sprint, comands.WalkSlow);
		_CurrentCapsuleProfule = _StandCapsule;
		ProcessCrouch(comands.Crouch);
		ProcessLowCrouch(comands.LowCrouch);
		UpdateCapsuleCollider();

		if (IsGrounded) ProcessJump(comands.Jump);
		else ProcessInAir();


		if (IsGrounded) ApplyExtraTurn(comands.TurnAngle);

		if (Aim && comands.Shoot && Weapon != null) Weapon.Shoot(AimPoint);
		UpdateAnimator(comands.Move);
		UpdateAnimatorLayersWeight();
		UpdateIKReferenceTransform();
	}

	private void SetLookAtWeight()
	{
		if (Aim) _Animator.SetLookAtWeight(_AimWeight, _AimBodyWeight, _AimHeadWeight);
		else _Animator.SetLookAtWeight(0);
	}

	private void UpdateIKReferenceTransform()
	{
		if (Weapon != null)
		{
			bool weaponForLeftHand = Weapon.Type == WeaponType.Pistol || Weapon.Type == WeaponType.MachineGun;

			if (weaponForLeftHand)
			{
				_LeftHandIK.position = Weapon.LeftHand.position;
				_LeftHandIK.rotation = Weapon.LeftHand.rotation;
			}

			Vector3 rh_RotEuler = Quaternion.LookRotation(AimPoint - _RightHand.position).eulerAngles;
			_RightHandIK.rotation = Quaternion.Euler(rh_RotEuler.x, rh_RotEuler.y, -90);
		}
	}

	private void TurnRightHandToTarget()
	{
		if (Weapon != null && Aim)
		{
			_Animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
			_Animator.SetIKRotation(AvatarIKGoal.RightHand, _RightHandIK.rotation);
		}
		else
		{
			_Animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 0);
		}
	}

	private void PoseLeftHandOnWeapon()
	{
		if (Weapon != null)
		{
			bool holdWeaponWithLeftHand = Weapon.Type == WeaponType.Pistol || Weapon.Type == WeaponType.MachineGun;

			_LeftHandIKWeight = Aim && holdWeaponWithLeftHand ? 1 : 0;
			_Animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, _LeftHandIKWeight);
			_Animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, _LeftHandIKWeight);

			if (holdWeaponWithLeftHand)
			{
				_Animator.SetIKPosition(AvatarIKGoal.LeftHand, _LeftHandIK.position);
				_Animator.SetIKRotation(AvatarIKGoal.LeftHand, _LeftHandIK.rotation);
			}
		}
	}

	private void UpdateAnimatorLayersWeight()
	{
		_TopRigWeight = Mathf.Lerp(_TopRigWeight, Aim ? 1 : 0, _TopRigWeightLerp * Time.deltaTime);
		_Animator.SetLayerWeight(1, _TopRigWeight);
	}

	private void OnWeaponChanged()
	{
		_CurrentWeapon = Weapon;
		UpdateWeaponFlags();

		if (Weapon != null)
		{
			Weapon.transform.SetParent(_RightHand);
			Weapon.transform.localPosition = Vector3.zero;
			Weapon.transform.localPosition = Weapon.transform.localPosition + _WeaponLocalPositionInHand;
			Weapon.transform.localRotation = Quaternion.Euler(_WeaponLocalRotationInHand);
		}
	}

	private void UpdateWeaponFlags()
	{
		_UseAnyGun = Weapon != null;
		_UseKnife = Weapon != null && Weapon.Type == WeaponType.Knife;
		_UsePistol = Weapon != null && Weapon.Type == WeaponType.Pistol;
		_UseMachineGun = Weapon != null && Weapon.Type == WeaponType.MachineGun;
	}

	private void ProcessAim(bool aim)
	{
		Aim = IsGrounded && aim && Weapon != null ? true : false;
	}

	private void ProcessInAir()
	{
		_IsGroundedRayDistance = _Rigidbody.velocity.y <= 0 ? _IsGroundedDefaultRayDistance : 0;
		_Jump = _Rigidbody.velocity.y;
	}

	private void ProcessJump(bool jump)
	{
		if (jump)
		{
			IsGrounded = false;
			_Animator.applyRootMotion = false;
			_IsGroundedRayDistance = 0;
			_Rigidbody.AddForce(Vector3.up * _JumpForce, ForceMode.Impulse);
		}
	}

	private void ProcessCrouch(bool crouch)
	{
		if (IsGrounded && crouch)
		{
			Crouch = true;
			_CurrentCapsuleProfule = _CrouchCapsule;
		}
		else
		{
			Crouch = false;
		}
	}

	private void ProcessLowCrouch(bool lowCrouch)
	{
		if (IsGrounded && lowCrouch)
		{
			LowCrouch = true;
			Crouch = false;
			_CurrentCapsuleProfule = _LowCrouchCapsule;
		}
		else
		{
			LowCrouch = false;
		}
	}

	private void UpdateCapsuleCollider()
	{
		_Collider.center = Vector3.Lerp(_Collider.center, _CurrentCapsuleProfule.Center, _CapsuleCenterLerp * Time.deltaTime);
		_Collider.radius = Mathf.Lerp(_Collider.radius, _CurrentCapsuleProfule.Radius, _CapsuleRadiusLerp * Time.deltaTime);
		_Collider.height = Mathf.Lerp(_Collider.height, _CurrentCapsuleProfule.Height, _CapsuleHeightLerp * Time.deltaTime);
		_Collider.direction = (int)_CurrentCapsuleProfule.Direction;
	}

	private void ProcessVelocity(Vector3 move, bool sprint, bool walkSlow)
	{
		float multyplyer = 1;

		if (Aim) multyplyer = 0.5f;
		else if (sprint) multyplyer = 2;
		else if (walkSlow) multyplyer = 0.5f;

		move *= multyplyer;

		move = transform.TransformDirection(move);
		move = Vector3.ProjectOnPlane(move, _GroundNormal);
		move = transform.InverseTransformDirection(move);

		float xAcceleration = DefineAccelerationForAxis(_Move.x, move.x);
		float zAcceleration = DefineAccelerationForAxis(_Move.z, move.z);

		float currentX = Mathf.Lerp(_Move.x, move.x, xAcceleration * Time.deltaTime);
		float currentZ = Mathf.Lerp(_Move.z, move.z, zAcceleration * Time.deltaTime);

		_Move.Set(currentX, move.y, currentZ);

		_GoBack = _Move.z < 0;

		float targetForward = _Move.z >= 0 ? _Move.magnitude : -_Move.magnitude;
		float forwardAcceleration = DefineAccelerationForAxis(_Forward, targetForward);
		_Forward = Mathf.Lerp(_Forward, targetForward, forwardAcceleration);
	}

	private float DefineAccelerationForAxis(float origin, float target)
	{
		float acceleration;

		if (origin * target >= 0)
			if (Mathf.Abs(target) > Mathf.Abs(origin))
				acceleration = _Acceleration;
			else
				acceleration = _Decceleration;
		else
			acceleration = _Decceleration;

		return acceleration;
	}

	private void ApplyExtraTurn(float turnAngle)
	{
		transform.Rotate(0, turnAngle * _TurnSpeed * Time.deltaTime, 0);
	}

	private void UpdateAnimator(Vector3 move)
	{
		_Animator.SetFloat(_HorizontalAnimatorParameter, _Move.x);
		_Animator.SetFloat(_VerticalAnimatorParameter, _Move.z);
		_Animator.SetFloat(_ForwardAnimatorParameter, _Forward);
		_Animator.SetFloat(_JumpAnimatorParameter, _Jump);
		_Animator.SetBool(_IsGroundedAnimatorParameter, IsGrounded);
		_Animator.SetBool(_GoBackAnimatorParameter, _GoBack);
		_Animator.SetBool(_CrouchAnimatorParameter, Crouch);
		_Animator.SetBool(_LowCrouchAnimatorParameter, LowCrouch);
		_Animator.SetBool(_UseAnyGunAnimatorParameter, _UseAnyGun);
		_Animator.SetBool(_UseKnifeAnimatorParameter, _UseKnife);
		_Animator.SetBool(_UsePistolAnimatorParameter, _UsePistol);
		_Animator.SetBool(_useMachineGunAnimatorParameter, _UseMachineGun);
	}

	private void CheckIsGrounded()
	{
		RaycastHit hit;
		Vector3 origin = transform.position + Vector3.up * _Collider.height / 2;
		float rayDistance = _Collider.height / 2 + _IsGroundedRayDistance;

		Debug.DrawRay(origin, Vector3.down * rayDistance, Color.green);

		if (Physics.SphereCast(origin, _Collider.radius, Vector3.down, out hit, rayDistance, _IsGroundedLayerMask))
		{
			IsGrounded = true;
			_Animator.applyRootMotion = true;
			_GroundNormal = hit.normal;
		}
		else
		{
			IsGrounded = false;
			_Animator.applyRootMotion = false;
			_GroundNormal = Vector3.up;
		}
	}

	private void InitBones()
	{
		_RightHand = _Animator.GetBoneTransform(HumanBodyBones.RightHand);
		_LeftHandIK = new GameObject("LeftHandIK").transform;
		_RightHandIK = new GameObject("RightHandIK").transform;
		_LeftHandIK.SetParent(transform);
		_RightHandIK.SetParent(transform);
	}
}

[System.Serializable]
class CapsuleColliderProfile
{
	public enum CapsuleColliderDirection: byte { X_Axis, Y_Axis, Z_Axis, };
	
	public Vector3 Center;
	public float Radius;
	public float Height;
	public CapsuleColliderDirection Direction;
}
