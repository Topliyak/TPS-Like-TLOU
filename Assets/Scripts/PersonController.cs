using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(PersonScript))]
[RequireComponent(typeof(WeaponManager))]
public class PersonController : MonoBehaviour
{
	[Header("Debug")]
	public bool DebugAim;

	private PersonScript _Person;
	private WeaponManager _WeaponManager;
	private CapsuleCollider _CapsuleCollider;
	private Comands _ControlData;
	private Transform _Camera;
	private Transform _CameraFollowPoint;
	private Transform _PointDuplicateCameraRotation;
	private const string _InteractiveCheckerLayerName = "InteractiveChecker";
	[SerializeField] private GameObject _ObjectAvailableForInteract;
	[SerializeField] private int _CurrentWeaponIndex = 0;

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

	private void Start()
	{
		if (_CameraFollowPoint == null) SetCameraFollowPoint();
		_CapsuleCollider = GetComponent<CapsuleCollider>();
		_Person = GetComponent<PersonScript>();
		_WeaponManager = GetComponent<WeaponManager>();
		_ControlData = new Comands();
		_Camera = Camera.main == null ? null : Camera.main.transform;
		AddTakebleObjectsChecker();
		AddPointDuplicateCameraRotation();
		if (_TakeObjectSprite == null) throw new MissingReferenceException("Take Icon is null");
	}

	private void Update()
	{
		_CameraFollowPoint.localPosition = Vector3.up * _CapsuleCollider.height * _CameraFollowPointHeight;
		_PointDuplicateCameraRotation.rotation = Quaternion.Euler(Vector3.up * _Camera.rotation.eulerAngles.y);
		UpdateComands();

		if (_ControlData.Aim) SetAimPoint();
		_Person.SetComands(_ControlData);

		if (_Person.Aim) _AimCinemachine.Priority = _DefaultCinemachine.Priority + 1;
		else _AimCinemachine.Priority = _DefaultCinemachine.Priority - 1;

		_AimTargetUI.SetActive(_Person.Aim);
		_CurrentWeaponIndex = (_CurrentWeaponIndex + _ControlData.WeaponIndexDelta) % WeaponManager.WeaponCellsNumber;
		ProcessInteract();
		UpdateWeapon();
		SyncronizateCinemachine();
	}

	public void OnInteractiveObjectDetected(GameObject interactiveObject)
	{
		Vector3 origin = transform.TransformPoint(_CapsuleCollider.center);
		Ray ray = new Ray(origin, interactiveObject.transform.position - origin);
		float rayDistance = Vector3.Distance(origin, interactiveObject.transform.position);
		Debug.DrawRay(ray.origin, ray.direction.normalized * rayDistance, Color.red);

		if (_Person.Aim
			|| _PointDuplicateCameraRotation.InverseTransformPoint(interactiveObject.transform.position).z < 0.1f
			|| Physics.Raycast(ray, rayDistance, _InteractObstaclesLayer))
		{
			OnInteractiveObjectLost(interactiveObject);
			return;
		}

		_ObjectAvailableForInteract = interactiveObject;
		_TakeObjectSprite.gameObject.SetActive(true);
		_TakeObjectSprite.position = _ObjectAvailableForInteract.transform.position + _TakeObjectSpriteOffset;
	}

	public void OnInteractiveObjectLost(GameObject interactiveObject)
	{
		if (_ObjectAvailableForInteract == interactiveObject)
		{
			_ObjectAvailableForInteract = null;
			_TakeObjectSprite.gameObject.SetActive(false);
		}
	}

	private void ProcessInteract()
	{
		if (!_ControlData.Interact || _ObjectAvailableForInteract == null) return;
		
		if (_Person.Aim)
		{
			OnInteractiveObjectLost(_ObjectAvailableForInteract);
			return;
		}

		WeaponScript weaponComponent = _ObjectAvailableForInteract.GetComponent<WeaponScript>();
		if (weaponComponent != null)
		{
			_WeaponManager.AddWeapon(weaponComponent, true);
		}

		OnInteractiveObjectLost(_ObjectAvailableForInteract);
	}

	private void UpdateWeapon()
	{
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

		if (Input.GetKeyDown(KeyCode.C))
			_ControlData.Crouch = !_ControlData.Crouch;

		if (Input.GetKeyDown(KeyCode.X))
			_ControlData.LowCrouch = !_ControlData.LowCrouch;

		if (Input.GetKeyDown(KeyCode.CapsLock))
			_ControlData.WalkSlow = !_ControlData.WalkSlow;

		_ControlData.Move = new Vector3(h, 0, v);
		_ControlData.Sprint = Input.GetKey(KeyCode.LeftShift);
		_ControlData.Jump = Input.GetKeyDown(KeyCode.Space);
		_ControlData.Interact = Input.GetKeyDown(KeyCode.E);
		_ControlData.Aim = Input.GetKey(KeyCode.Mouse1) || DebugAim;
		_ControlData.Shoot = Input.GetKey(KeyCode.Mouse0);
		_ControlData.ReloadGun = Input.GetKeyDown(KeyCode.R);

		if (h != 0 || v != 0 || _Person.Aim && _ControlData.Aim)
		{
			float difference = (_Camera.rotation.eulerAngles.y + 360) % 360 - (transform.rotation.eulerAngles.y + 360) % 360;
			float difference1 = (difference + 360) % 360;
			float difference2 = (difference - 360) % 360;

			_ControlData.TurnAngle = Mathf.Abs(difference1) < Mathf.Abs(difference2) ? difference1 : difference2;
		}
		else
		{
			_ControlData.TurnAngle = 0;
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

	private void OnDrawGizmosSelected()
	{
		// Draw TakebleChecker BoxCollider
		Gizmos.DrawWireCube(transform.position + Vector3.up * _InteractiveCheckerSize.y / 2, _InteractiveCheckerSize);
	}
}

public class Comands
{
	public Vector3 Move;
	public float TurnAngle;
	public int WeaponIndexDelta;
	public bool Crouch, LowCrouch, Jump, WalkSlow, Sprint, Interact, Aim, Shoot, ReloadGun;
}