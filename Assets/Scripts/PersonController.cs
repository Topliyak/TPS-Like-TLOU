using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(PersonScript))]
[RequireComponent(typeof(WeaponManager))]
public class PersonController : MonoBehaviour
{
	[Header("Debug")]
	[SerializeField] bool _DebugAim;
	[SerializeField] Transform _Cube;
	[SerializeField] Transform _Cube1;
	[SerializeField] Transform _Cube2;

	[Header("Editor")]
	[SerializeField] bool _DrawCheckerBox;
	[SerializeField] bool _DrawRadiusWhereAvailableHelper;
	[SerializeField] bool _DrawRadiusWhereAvailableTakeble;
	[SerializeField] bool _DrawMinMaxYPosForHelper;

	private PersonScript _Person;
	private WeaponManager _WeaponManager;
	private CapsuleCollider _CapsuleCollider;
	private Comands _ControlData;
	private Transform _Camera;
	private Transform _CameraFollowPoint;
	private Transform _PointDuplicateCameraRotation;
	private Transform _ClimbPoint;
	private const string _InteractiveCheckerLayerName = "InteractiveChecker";
	private GameObject _ObjectAvailableForInteract;
	private Helper _HelperAvailableForClimb;
	private Vector3 _ClimbPosition;
	private Quaternion _ClimbRotation;
	private int _CurrentWeaponIndex = 0;
	private FewModesButton _CrouchButton;

	[Header("Camera")]
	[SerializeField] [Range(0, 1)] float _CameraFollowPointHeight;
	[SerializeField] CinemachineFreeLook _DefaultCinemachine;
	[SerializeField] CinemachineFreeLook _AimCinemachine;

	[Header("Aim")]
	[SerializeField] LayerMask _AimLayerMask;
	[SerializeField] GameObject _AimTargetUI;
	[SerializeField] float _AimPointPositionLerp;

	[Header("Interactive Objects")]
	[SerializeField] Vector3 _InteractiveCheckerSize;
	[SerializeField] Transform _TakeObjectSprite;
	[SerializeField] Vector3 _TakeObjectSpriteOffset;
	[SerializeField] LayerMask _InteractObstaclesLayer;
	[SerializeField] float _MaxDistanceToTakeble;
	[SerializeField] float _MaxDistanceToHelper;

	[Header("Climb")]
	[SerializeField] float _MinHelperLocalYPos;
	[SerializeField] float _MaxHelperLocalYPos;

	[Header("Other")]
	[SerializeField] float _LowCrouchHoldTime;

	private void Start()
	{
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
		if (_CameraFollowPoint == null) SetCameraFollowPoint();
		_CapsuleCollider = GetComponent<CapsuleCollider>();
		_Person = GetComponent<PersonScript>();
		_WeaponManager = GetComponent<WeaponManager>();
		_ControlData = new Comands();
		_Camera = Camera.main == null ? null : Camera.main.transform;
		AddTakebleObjectsChecker();
		AddPointDuplicateCameraRotation();
		AddClimbPoint();
		if (_TakeObjectSprite == null) throw new MissingReferenceException("Take Icon is null");
		_CrouchButton = new FewModesButton(KeyCode.C, _LowCrouchHoldTime);
	}

	private void Update()
	{
		UpdateCameraFollowPoint();
		_PointDuplicateCameraRotation.rotation = Quaternion.Euler(Vector3.up * _Camera.rotation.eulerAngles.y);
		UpdateComands();

		_ControlData.ClimbPointDetected = _HelperAvailableForClimb != null;
		_ControlData.ClimbPosition = _ClimbPosition;
		_ControlData.ClimbRotation = _ClimbRotation;
		_Cube.position = _ClimbPosition; // Debug

		if (_ControlData.Aim) SetAimPoint();
		_Person.SetComands(_ControlData);

		if (_Person.Aim) _AimCinemachine.Priority = _DefaultCinemachine.Priority + 1;
		else _AimCinemachine.Priority = _DefaultCinemachine.Priority - 1;

		_AimTargetUI.SetActive(_Person.Aim);
		ProcessInteract();
		UpdateWeapon();
		SyncronizateCinemachine();

		if (Input.GetKeyDown(KeyCode.Backslash))
			Debug.Break();
	}

	public void OnInteractiveObjectDetected(GameObject interactiveObject)
	{
		Firearm firearm = interactiveObject.GetComponent<Firearm>();
		Helper helper = interactiveObject.GetComponent<Helper>();

		if (firearm != null)
			OnFirearmDetected(firearm);
		else if (helper != null)
			OnHelperDetected(helper);
	}

	public void OnInteractiveObjectLost(GameObject interactiveObject)
	{
		Helper helperComponent = interactiveObject.GetComponent<Helper>();

		if (helperComponent == _HelperAvailableForClimb)
		{
			_HelperAvailableForClimb = null;
		}

		if (_ObjectAvailableForInteract == interactiveObject)
		{
			_ObjectAvailableForInteract = null;
			_TakeObjectSprite.gameObject.SetActive(false);
		}
	}

	private void OnHelperDetected(Helper helper)
	{
		bool success = TryPoseClimbPoint(helper);

		if (!success)
		{
			if (helper == _HelperAvailableForClimb)
			{
				_HelperAvailableForClimb = null;
			}

			DeposeClimbPoint();
			return;
		}

		Vector3 origin = transform.TransformPoint(_CapsuleCollider.center);
		Ray ray = new Ray(origin, _ClimbPoint.position - origin);
		RaycastHit hit;
		float distanceToClimbPoint = Vector3.Distance(origin, _ClimbPoint.position);
		Vector3 localPos = transform.InverseTransformPoint(_ClimbPoint.position);
		Color rayColor;

		if (distanceToClimbPoint > _MaxDistanceToHelper
			|| localPos.z <= 0 || localPos.y < _MinHelperLocalYPos || localPos.y > _MaxHelperLocalYPos
			|| Physics.Raycast(ray, out hit, distanceToClimbPoint, _InteractObstaclesLayer) && hit.transform != helper.transform)
		{
			if (helper == _HelperAvailableForClimb)
			{
				_HelperAvailableForClimb = null;
			}

			DeposeClimbPoint();
			rayColor = Color.red;
			return;
		}
		else
		{
			rayColor = Color.green;
		}

		Debug.DrawRay(ray.origin, ray.direction.normalized * distanceToClimbPoint, rayColor);

		if (helper != _HelperAvailableForClimb)
		{
			Vector3 capuleCenter = transform.TransformPoint(_CapsuleCollider.center);
			float distanceToCurrentClimbPoint = Vector3.Distance(capuleCenter, _ClimbPosition);

			if (distanceToClimbPoint < distanceToCurrentClimbPoint)
			{
				_HelperAvailableForClimb = helper;
				_ClimbPosition = _ClimbPoint.position;
				_ClimbRotation = _ClimbPoint.rotation;
				DeposeClimbPoint();
			}
		}
		else
		{
			_ClimbPosition = _ClimbPoint.position;
			_ClimbRotation = _ClimbPoint.rotation;
		}
	}

	private void DeposeClimbPoint()
	{
		_ClimbPoint.SetParent(transform);
		_ClimbPoint.localPosition = Vector3.zero;
		_ClimbPoint.localRotation = Quaternion.Euler(Vector3.zero);
	}

	private bool TryPoseClimbPoint(Helper helper)
	{
		_ClimbPoint.SetParent(helper.transform);
		_ClimbPoint.position = transform.position;
		_ClimbPoint.localPosition = new Vector3(Mathf.Clamp(_ClimbPoint.localPosition.x, helper.Min.x, helper.Max.x),
												Mathf.Clamp(_ClimbPoint.localPosition.y, helper.Min.y, helper.Max.y),
												Mathf.Clamp(_ClimbPoint.localPosition.z, helper.Min.z, helper.Max.z));

		bool inZoneA = _ClimbPoint.localPosition.z >= helper.Max.z;
		bool inZoneB = _ClimbPoint.localPosition.x <= helper.Min.x;
		bool inZoneC = _ClimbPoint.localPosition.z <= helper.Min.z;
		bool inZoneD = _ClimbPoint.localPosition.x >= helper.Max.x;

		Vector3 localPosInZoneA = new Vector3(_ClimbPoint.localPosition.x, helper.Max.y, helper.Max.z);
		Vector3 localPosInZoneB = new Vector3(helper.Min.x, helper.Max.y, _ClimbPoint.localPosition.z);
		Vector3 localPosInZoneC = new Vector3(_ClimbPoint.localPosition.x, helper.Max.y, helper.Min.z);
		Vector3 localPosInZoneD = new Vector3(helper.Max.x, helper.Max.y, _ClimbPoint.localPosition.z);

		float localAngleInZoneA = 180;
		float localAngleInZoneB = 90;
		float localAngleInZoneC = 0;
		float localAngleInZoneD = -90;

		if (!inZoneA && !inZoneB && !inZoneC && !inZoneD)
		{
			return false;
		}
		else
		{
			float[] straight1 = FindStraight(new Vector2(helper.Min.x, helper.Min.z), new Vector2(helper.Max.x, helper.Max.z));
			float[] straight2 = FindStraight(new Vector2(helper.Max.x, helper.Min.z), new Vector2(helper.Min.x, helper.Max.z));

			float k1 = straight1[0];
			float b1 = straight1[1];

			float k2 = straight2[0];
			float b2 = straight2[1];

			if (_ClimbPoint.localPosition.z >= k1 * _ClimbPoint.localPosition.x + b1)
			{
				if (_ClimbPoint.localPosition.z >= k2 * _ClimbPoint.localPosition.x + b2)
				{
					// Zone A
					if (!helper.forward) return false;
					_ClimbPoint.localPosition = localPosInZoneA;
					_ClimbPoint.localRotation = Quaternion.Euler(0, localAngleInZoneA, 0);
				}
				else
				{
					// Zone B
					if (!helper.left) return false;
					_ClimbPoint.localPosition = localPosInZoneB;
					_ClimbPoint.localRotation = Quaternion.Euler(0, localAngleInZoneB, 0);
				}
			}
			else
			{
				if (_ClimbPoint.localPosition.z >= k2 * _ClimbPoint.localPosition.x + b2)
				{
					// Zone D
					if (!helper.right) return false;
					_ClimbPoint.localPosition = localPosInZoneD;
					_ClimbPoint.localRotation = Quaternion.Euler(0, localAngleInZoneD, 0);
				}
				else
				{
					// Zone C
					if (!helper.back) return false;
					_ClimbPoint.localPosition = localPosInZoneC;
					_ClimbPoint.localRotation = Quaternion.Euler(0, localAngleInZoneC, 0);
				}
			}
		}
		
		return true;
	}

	private float[] FindStraight(Vector2 a1, Vector2 a2)
	{
		// y1 = k * x1 + b
		// y2 = k * x2 + b
		// b = y1 - k * x1
		// y2 = k * x2 + y1 - k * x1
		// y2 - y1 = k * (x2 - x1)
		// k = (y2 - y1) / (x2 - x1)

		float k = (a2.y - a1.y) / (a2.x - a1.x);
		float b = a1.y - k * a1.x;

		return new float[2] { k, b, };
	}

	private void OnFirearmDetected(Firearm firearm)
	{
		Vector3 origin = transform.TransformPoint(_CapsuleCollider.center);
		Ray ray = new Ray(origin, firearm.transform.position - origin);
		float rayDistance = Vector3.Distance(origin, firearm.transform.position);
		Color rayColor = Color.green;

		if (_Person.Aim
			|| _PointDuplicateCameraRotation.InverseTransformPoint(firearm.transform.position).z < 0.1f
			|| Physics.Raycast(ray, rayDistance, _InteractObstaclesLayer))
		{
			OnInteractiveObjectLost(firearm.gameObject);
			rayColor = Color.red;
			return;
		}

		Debug.DrawRay(ray.origin, ray.direction.normalized * rayDistance, rayColor);

		_ObjectAvailableForInteract = firearm.gameObject;
		_TakeObjectSprite.gameObject.SetActive(true);
		_TakeObjectSprite.position = _ObjectAvailableForInteract.transform.position + _TakeObjectSpriteOffset;
	}

	private void ProcessInteract()
	{
		if (!_ControlData.Interact || _ObjectAvailableForInteract == null) return;
		
		if (_Person.Aim)
		{
			OnInteractiveObjectLost(_ObjectAvailableForInteract);
			return;
		}

		Firearm weaponComponent = _ObjectAvailableForInteract.GetComponent<Firearm>();
		if (weaponComponent != null)
		{
			_WeaponManager.AddWeapon(weaponComponent, true);
		}

		OnInteractiveObjectLost(_ObjectAvailableForInteract);
	}

	private void UpdateWeapon()
	{
		_CurrentWeaponIndex = (_CurrentWeaponIndex + _ControlData.WeaponIndexDelta) % WeaponManager.WeaponCellsNumber;
		if (_CurrentWeaponIndex < 0) _CurrentWeaponIndex = WeaponManager.WeaponCellsNumber + _CurrentWeaponIndex;

		if (_Person.Weapon != _WeaponManager.Weapons[_CurrentWeaponIndex])
			_Person.Weapon = _WeaponManager.PutOutWeaponFromHolder(_CurrentWeaponIndex);
	}

	private void SetAimPoint()
	{
		RaycastHit hit;
		Ray ray = new Ray(_Camera.position, _Camera.forward);
		Vector3 newAimPoint;

		if (Physics.Raycast(ray, out hit, 1000, _AimLayerMask))
			newAimPoint = hit.point;
		else
			newAimPoint = _Camera.position + _Camera.forward * 100;

		if (transform.InverseTransformPoint(newAimPoint).z <= 0)
			newAimPoint = transform.position + (transform.forward + transform.up) * 100;

		_Person.AimPoint = Vector3.Lerp(_Person.AimPoint, newAimPoint, _AimPointPositionLerp * Time.deltaTime);
	}

	private void SyncronizateCinemachine()
	{
		if (_Person.Aim)
		{
			_DefaultCinemachine.m_XAxis.Value = _AimCinemachine.m_XAxis.Value;
			_DefaultCinemachine.m_YAxis.Value = _AimCinemachine.m_YAxis.Value;
		}
		else
		{
			_AimCinemachine.m_XAxis.Value = _DefaultCinemachine.m_XAxis.Value;
			_AimCinemachine.m_YAxis.Value = _DefaultCinemachine.m_YAxis.Value;
		}
	}

	private void UpdateComands()
	{
		float h = Input.GetAxis("Horizontal");
		float v = Input.GetAxis("Vertical");
		float mouseWheel = Input.GetAxis("Mouse ScrollWheel");

		if (mouseWheel > 0) _ControlData.WeaponIndexDelta = 1;
		else if (mouseWheel < 0) _ControlData.WeaponIndexDelta = -1;
		else if (mouseWheel == 0) _ControlData.WeaponIndexDelta = 0;

		_CrouchButton.Update();

		if (_CrouchButton.GetModeStatus(0))
		{
			_ControlData.Crouch = !_ControlData.Crouch;
			_ControlData.LowCrouch = false;
		}
		
		if (_CrouchButton.GetModeStatus(1))
		{
			_ControlData.LowCrouch = !_ControlData.LowCrouch;
			_ControlData.Crouch = false;
		}

		if (Input.GetKeyDown(KeyCode.CapsLock))
			_ControlData.WalkSlow = !_ControlData.WalkSlow;

		_ControlData.Move = new Vector3(h, 0, v);
		_ControlData.Sprint = Input.GetKey(KeyCode.LeftShift);
		_ControlData.Jump = Input.GetKeyDown(KeyCode.Space);
		_ControlData.Interact = Input.GetKeyDown(KeyCode.E);
		_ControlData.Aim = Input.GetKey(KeyCode.Mouse1) || _DebugAim;
		_ControlData.Shoot = Input.GetKey(KeyCode.Mouse0);
		_ControlData.ReloadGun = Input.GetKeyDown(KeyCode.R);

		if (h != 0 || v != 0 || _Person.Aim && _ControlData.Aim)
		{
			_ControlData.ExtraAngle = Quaternion.Euler(0, _Camera.eulerAngles.y - transform.eulerAngles.y, 0);
		}
		else
		{
			_ControlData.ExtraAngle = Quaternion.Euler(Vector3.zero);
		}
	}

	private void SetCameraFollowPoint()
	{
		_CameraFollowPoint = new GameObject("CameraFollowPoint").transform;
		_CameraFollowPoint.SetParent(transform);
		_DefaultCinemachine.Follow = _DefaultCinemachine.LookAt = _CameraFollowPoint;
		_AimCinemachine.Follow = _AimCinemachine.LookAt = _CameraFollowPoint;
	}

	private void AddTakebleObjectsChecker()
	{
		GameObject takebleChecker = new GameObject("Interactive Checker");
		takebleChecker.layer = LayerMask.NameToLayer(_InteractiveCheckerLayerName);
		takebleChecker.transform.SetParent(transform);
		takebleChecker.transform.localPosition = Vector3.zero;
		takebleChecker.transform.localRotation = Quaternion.Euler(Vector3.zero);
		takebleChecker.transform.localScale = Vector3.one;

		BoxCollider takebleCheckerBox = takebleChecker.AddComponent<BoxCollider>();
		takebleCheckerBox.center = Vector3.up * _InteractiveCheckerSize.y / 2;
		takebleCheckerBox.size = _InteractiveCheckerSize;
		takebleCheckerBox.isTrigger = true;

		Rigidbody takebleCheckerRigidbody = takebleChecker.AddComponent<Rigidbody>();
		takebleCheckerRigidbody.isKinematic = true;
		takebleCheckerRigidbody.useGravity = false;

		TakebleObjectsChecker takebleCheckerComponent = takebleChecker.AddComponent<TakebleObjectsChecker>();
		takebleCheckerComponent.Controller = this;
	}

	private void AddPointDuplicateCameraRotation()
	{
		_PointDuplicateCameraRotation = new GameObject("PointDuplicateCameraRotation").transform;
		_PointDuplicateCameraRotation.SetParent(transform);
		_PointDuplicateCameraRotation.localPosition = Vector3.zero;
	}

	private void AddClimbPoint()
	{
		_ClimbPoint = new GameObject("Climb Point").transform;
		_ClimbPoint.SetParent(transform);
	}

	private void OnDrawGizmosSelected()
	{
		CapsuleCollider capsuleCollider = GetComponent<CapsuleCollider>();

		// Draw TakebleChecker BoxCollider
		if (_DrawCheckerBox)
			Gizmos.DrawWireCube(transform.position + Vector3.up * _InteractiveCheckerSize.y / 2, _InteractiveCheckerSize);

		// Draw sphere where helper available
		if (_DrawRadiusWhereAvailableHelper)
			Gizmos.DrawWireSphere(transform.TransformPoint(capsuleCollider.center), _MaxDistanceToHelper);

		// Draw sphere where takeble objects available
		if (_DrawRadiusWhereAvailableTakeble)
			Gizmos.DrawWireSphere(transform.TransformPoint(capsuleCollider.center), _MaxDistanceToTakeble);

		if (_DrawMinMaxYPosForHelper)
		{
			Vector3 a = transform.TransformPoint(new Vector3(0, _MinHelperLocalYPos, 1));
			Vector3 b = transform.TransformPoint(new Vector3(0, _MaxHelperLocalYPos, 1));
			Gizmos.DrawLine(a, b);
		}
	}

	private void UpdateCameraFollowPoint()
	{
		if (_CapsuleCollider.direction == (int)CapsuleColliderProfile.CapsuleColliderDirection.X_Axis)
		{
			_CameraFollowPoint.position = transform.position + transform.up * _CapsuleCollider.radius;
		}
		else if (_CapsuleCollider.direction == (int)CapsuleColliderProfile.CapsuleColliderDirection.Y_Axis)
		{
			_CameraFollowPoint.position = transform.position + transform.up * _CapsuleCollider.height * _CameraFollowPointHeight;
		}
		else if (_CapsuleCollider.direction == (int)CapsuleColliderProfile.CapsuleColliderDirection.Z_Axis)
		{
			_CameraFollowPoint.position = transform.position + 
											transform.forward * _CapsuleCollider.height * _CameraFollowPointHeight + 
											transform.up * _CapsuleCollider.radius;
		}
	}
}