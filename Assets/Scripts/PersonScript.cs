using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
	public bool Climb { get; private set; }

	private Vector3 _GroundNormal;
	private bool _GoBack;
	private CapsuleCollider _Collider;
	private Rigidbody _Rigidbody;
	private Animator _Animator;
	private bool _UseAnyGun;
	private bool _UseKnife;
	private bool _UsePistol;
	private bool _UseMachineGun;
	private Firearm _CurrentWeapon;
	private Transform _LeftHandIK;
	private Transform _RightHandIK;
	private Transform _RightHand;
	private float _LeftHandIKWeight;
	private float _TopRigWeight;
	private Vector3 _Move;
	private float _Forward;
	private float _Jump;
	private float _IsGroundedRayDistance;
	private CapsuleColliderProfile _CurrentCapsuleProfile;
	private Transform _ClimbRef;
	private bool _ShouldClose;
	private bool _ShouldRotate;
	private Vector3 _PointCloseTo;
	private Quaternion _PointRotateTo;
	private float _CoeffCloseAndRotateBy;
	const string _TurnOffAlwaysGroundedMethodName = "TurnOffAlwaysGrounded";

	[Header("Debug")]
	[SerializeField] bool _AlwaysGrounded;
	[SerializeField] bool _DebugClimb;
	[SerializeField] Climber.ClimbType _DebuggedClimbType;
	[SerializeField] Transform _Cube;

	[Header("IsGrounded check")]
	[SerializeField] float _IsGroundedDefaultRayDistance;
	[SerializeField] LayerMask _IsGroundedLayerMask;

	[Header("CapsuleCollider Profiles")]
	[SerializeField] float _DelayToTurnOffAlwaysGroundedAfterLowCrouch;
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

	[Header("Climb")]
	[SerializeField] Climber _Climber;

	[Header("Weapon")]
	public Firearm Weapon;
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
		_Climber.Initialize();
		Climb = false;
		InitBones();
		InitClimbRef();
	}

	private void Update()
	{
		if (Weapon != _CurrentWeapon) OnWeaponChanged();
	}

	private void FixedUpdate()
	{
		if (_ShouldClose) CloseToPoint();
		if (_ShouldRotate) RotateToPoint();
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
		ProcessCrouch(comands.Crouch);
		ProcessLowCrouch(comands.LowCrouch);
		ProcessClimb(comands.Jump, comands.ClimbPointDetected, comands.ClimbPosition, comands.ClimbRotation);
		UpdateCapsuleCollider();

		if (!Climb && !Crouch && !LowCrouch)
		{
			if (IsGrounded) ProcessJump(comands.Jump);
			else ProcessInAir();
		}
		else
		{
			_Animator.applyRootMotion = true;
		}

		if (IsGrounded && !Climb) ApplyExtraTurn(comands.TurnAngle);
		RotateNormalUpwards(LowCrouch ? _GroundNormal : Vector3.up);

		if (Aim && comands.Shoot && Weapon != null) Weapon.Shoot(AimPoint);
		UpdateAnimator(comands.Move);
		UpdateAnimatorLayersWeight();
		UpdateIKReferenceTransform();
	}

	private void CloseToPoint()
	{
		transform.position = Vector3.Lerp(transform.position, _PointCloseTo, _CoeffCloseAndRotateBy * Time.deltaTime);
	}

	private void RotateToPoint()
	{
		transform.rotation = Quaternion.Lerp(transform.rotation, _PointRotateTo, _CoeffCloseAndRotateBy * Time.deltaTime);
	}

	private void OnClimbed()
	{
		Climb = false;
		_Climber.FinishingClimb = false;
		_Collider.enabled = true;
		_Rigidbody.useGravity = true;
		_Rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
		_ClimbRef.SetParent(transform);
		_ClimbRef.localPosition = Vector3.zero;
	}

	private void ProcessClimb(bool jump, bool climbPointDetected, Vector3 climbPosition, Quaternion climbRotation)
	{
		if (Climb && !_Climber.FinishingClimb)
		{
			// Debug
			if (_DebugClimb)
			{
				_Climber.Initialize();
				_Climber.CurrentClimbType = _DebuggedClimbType;
			}

			//Vector3 newPos = _ClimbRef.position + _ClimbRef.TransformVector(_Climber.Offset);
			//transform.position = Vector3.Lerp(transform.position, newPos, _Climber.OffsetLerp * Time.deltaTime);
			//transform.rotation = Quaternion.Lerp(transform.rotation, _ClimbRef.rotation, _Climber.OffsetLerp * Time.deltaTime);
			_Climber.DistanceToClimbPoint = Vector3.Distance(transform.position, _PointCloseTo);

			if (!_DebugClimb && _Climber.DistanceToClimbPoint < _Climber.MinDistanceToClimbRef && 
				Quaternion.Angle(transform.rotation, _ClimbRef.rotation) < _Climber.MinDistanceToClimbRef)
			{
				_Climber.FinishingClimb = true;
				_ShouldClose = _ShouldRotate = false;
			}
			else
			{
				_Climber.FinishingClimb = false;
			}
		}
		else if (!Climb && climbPointDetected && IsGrounded && jump)
		{
			Climb = true;
			_Climber.FinishingClimb = false;
			_Collider.enabled = false;
			_Rigidbody.useGravity = false;
			_Rigidbody.constraints = RigidbodyConstraints.None;
			_ClimbRef.SetParent(null);
			_ClimbRef.position = climbPosition;
			_ClimbRef.rotation = Quaternion.Euler(Vector3.up * climbRotation.eulerAngles.y);
			_Rigidbody.velocity = Vector3.zero;
			FindBestClimbType();
			print(_Climber.CurrentClimbType);
			_PointCloseTo = _ClimbRef.position + _ClimbRef.TransformVector(_Climber.Offset);
			_PointRotateTo = _ClimbRef.rotation;
			_CoeffCloseAndRotateBy = _Climber.OffsetLerp;
			_ShouldClose = _ShouldRotate = true;
		}
	}

	private void FindBestClimbType()
	{
		Climber.ClimbType bestClimbType = Climber.ClimbType.ClimbLow;
		float minMagnitude = (_ClimbRef.position + _Climber.ClimbTypeOffsetPairs[Climber.ClimbType.ClimbLow] - transform.position).magnitude;

		foreach (var climbType in _Climber.ClimbTypeOffsetPairs.Keys)
		{
			float currentMagnitude = (_ClimbRef.position + _Climber.ClimbTypeOffsetPairs[climbType] - transform.position).magnitude;

			if (currentMagnitude < minMagnitude)
			{
				minMagnitude = currentMagnitude;
				bestClimbType = climbType;
			}
		}

		foreach (var climbType in _Climber.ClimbTypeStatusPairs.Keys.ToArray())
			_Climber.ClimbTypeStatusPairs[climbType] = climbType == bestClimbType;

		_Climber.CurrentClimbType = bestClimbType;
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
		Aim = IsGrounded && !Climb && aim && Weapon != null ? true : false;
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
			_IsGroundedRayDistance = 0;
			_Rigidbody.AddForce(Vector3.up * _JumpForce, ForceMode.Impulse);
		}
	}

	private void ProcessCrouch(bool crouch)
	{
		if (IsGrounded && crouch)
		{
			Crouch = true;
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
			if (!LowCrouch)
			{
				_AlwaysGrounded = true;
				Invoke(_TurnOffAlwaysGroundedMethodName, _DelayToTurnOffAlwaysGroundedAfterLowCrouch);
			}

			LowCrouch = true;
			Crouch = false;
		}
		else
		{
			LowCrouch = false;
		}
	}

	private void RotateNormalUpwards(Vector3 normal)
	{
		Vector3 yz = Quaternion.LookRotation(transform.forward, normal).eulerAngles;
		float x = Quaternion.LookRotation(transform.right, normal).eulerAngles.z;
		transform.rotation = Quaternion.Euler(x, yz.y, yz.z);
	}

	private void TurnOffAlwaysGrounded() => _AlwaysGrounded = false;

	private void UpdateCurrentCapsuleProfile()
	{
		if (Crouch) _CurrentCapsuleProfile = _CrouchCapsule;
		else if (LowCrouch) _CurrentCapsuleProfile = _LowCrouchCapsule;
		else _CurrentCapsuleProfile = _StandCapsule;
	}

	private void UpdateCapsuleCollider()
	{
		UpdateCurrentCapsuleProfile();
		_Collider.center = Vector3.Lerp(_Collider.center, _CurrentCapsuleProfile.Center, _CapsuleCenterLerp * Time.deltaTime);
		_Collider.radius = Mathf.Lerp(_Collider.radius, _CurrentCapsuleProfile.Radius, _CapsuleRadiusLerp * Time.deltaTime);
		_Collider.height = Mathf.Lerp(_Collider.height, _CurrentCapsuleProfile.Height, _CapsuleHeightLerp * Time.deltaTime);
		_Collider.direction = (int)_CurrentCapsuleProfile.Direction;
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
		_Animator.SetFloat(AnimatorParameters.Horizontal, _Move.x);
		_Animator.SetFloat(AnimatorParameters.Vertical, _Move.z);
		_Animator.SetFloat(AnimatorParameters.Forward, _Forward);
		_Animator.SetFloat(AnimatorParameters.Jump, _Jump);
		_Animator.SetFloat(AnimatorParameters.distanceToClimbPoint, _Climber.DistanceToClimbPoint);
		_Animator.SetBool(AnimatorParameters.IsGrounded, IsGrounded);
		_Animator.SetBool(AnimatorParameters.GoBack, _GoBack);
		_Animator.SetBool(AnimatorParameters.Crouch, Crouch);
		_Animator.SetBool(AnimatorParameters.LowCrouch, LowCrouch);
		_Animator.SetBool(AnimatorParameters.UseAnyGun, _UseAnyGun);
		_Animator.SetBool(AnimatorParameters.UseKnife, _UseKnife);
		_Animator.SetBool(AnimatorParameters.UsePistol, _UsePistol);
		_Animator.SetBool(AnimatorParameters.useMachineGun, _UseMachineGun);
		_Animator.SetBool(AnimatorParameters.climb, Climb);
		_Animator.SetBool(AnimatorParameters.climbLow, Climb && _Climber.ClimbTypeStatusPairs[Climber.ClimbType.ClimbLow]);
		_Animator.SetBool(AnimatorParameters.climbLowMiddle, Climb && _Climber.ClimbTypeStatusPairs[Climber.ClimbType.ClimbLowMiddle]);
		_Animator.SetBool(AnimatorParameters.climbMiddle, Climb && _Climber.ClimbTypeStatusPairs[Climber.ClimbType.ClimbMiddle]);
		_Animator.SetBool(AnimatorParameters.climbHigh, Climb && _Climber.ClimbTypeStatusPairs[Climber.ClimbType.ClimbHigh]);
		_Animator.SetBool(AnimatorParameters.finishClimb, _Climber.FinishingClimb);
	}

	private void CheckIsGrounded()
	{
		RaycastHit hit;
		Vector3 origin = transform.TransformPoint(_Collider.center);
		//float rayDistance = origin.y - _Collider.bounds.min.y + _IsGroundedRayDistance;
		float rayDistance = _Collider.height / 2 + _IsGroundedRayDistance;
		Color rayColor;

		//if (Physics.SphereCast(origin, _Collider.radius * 0.9f, Vector3.down, out hit, rayDistance, _IsGroundedLayerMask))
		if (Physics.SphereCast(origin, _Collider.radius * 0.9f, -transform.up, out hit, rayDistance, _IsGroundedLayerMask))
		{
			IsGrounded = true;
			_Animator.applyRootMotion = true;
			_GroundNormal = hit.normal;
			rayColor = Color.green;
		}
		else
		{
			IsGrounded = false;
			_Animator.applyRootMotion = false;
			_GroundNormal = Vector3.up;
			rayColor = Color.red;
		}

		//Debug.DrawRay(origin, Vector3.down * rayDistance, rayColor);
		Debug.DrawRay(origin, -transform.up * rayDistance, rayColor);

		if (_AlwaysGrounded == true)
		{
			IsGrounded = true;
			_Animator.applyRootMotion = true;
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

	private void InitClimbRef()
	{
		_ClimbRef = new GameObject("ClimbRef").transform;
		_ClimbRef.SetParent(transform);
	}
}
