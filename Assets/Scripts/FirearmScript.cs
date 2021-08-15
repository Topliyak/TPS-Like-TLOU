using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirearmScript: MonoBehaviour
{
	public static FireMode ShootMode = FireMode.PhysicBullet;

	private bool _CanShoot;
	private const string _SetCanShootMethodName = "SetCanShoot";

	[SerializeField] WeaponType _WeaponType;
	[SerializeField] Transform _LeftHand;
	[SerializeField] [Tooltip("Min delay between fire in seconds")] [Range(0, Mathf.Infinity)] float _Delay;

	/// <summary>
	/// Used when ShootMode = PhysicBullet
	/// </summary>
	[SerializeField] float _BulletMass;
	/// <summary>
	/// Used when ShootMode = PhysicBullet
	/// </summary>
	[SerializeField] float _BulletStartVelocity;
	/// <summary>
	/// Used when ShootMode = PhysicBullet
	/// </summary>
	[SerializeField] GameObject _BulletPrefab;
	/// <summary>
	/// Used when ShootMode = PhysicBullet
	/// </summary>
	[SerializeField] Transform _BulletSpawn;

	[HideInInspector] public WeaponType Type => _WeaponType;
	[HideInInspector] public Transform LeftHand => _LeftHand;

	private void Awake()
	{
		_CanShoot = true;
	}

	public virtual void Shoot(Vector3 target)
	{
		if (!_CanShoot) return;

		if (ShootMode == FireMode.Raycast) ShootByRaycast(target);
		else if (ShootMode == FireMode.PhysicBullet) ShootByPhysicBullet(target);

		_CanShoot = false;
		Invoke(_SetCanShootMethodName, _Delay);
	}

	private void ShootByRaycast(Vector3 target)
	{
		print("Raycast shoot");
	}

	private void ShootByPhysicBullet(Vector3 target)
	{
		BulletScript bullet = Instantiate(_BulletPrefab, _BulletSpawn.position, _BulletSpawn.rotation).GetComponent<BulletScript>();
		bullet.Launch(target, _BulletStartVelocity, _BulletMass);

		print("Launch Bullet");
	}

	private void SetCanShoot() => _CanShoot = true;
}

public enum WeaponType
{
	Knife,
	Pistol,
	MachineGun,
}

public enum FireMode
{
	Raycast,
	PhysicBullet,
}
