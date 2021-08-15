using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
	public Dictionary<WeaponType, Transform> WeaponTypeHolderPairs { get; private set; }
	public Dictionary<WeaponType, int> WeaponTypeIndexPairs { get; private set; }
	public FirearmScript[] Weapons { get; private set; }
	public const int WeaponCellsNumber = 4;

	[SerializeField] Transform KnifeHolder;
	[SerializeField] Transform PistolHolder;
	[SerializeField] Transform MachineGunHolder;

	private void PutWeaponToHolder(int weaponIndex)
	{
		FirearmScript weapon = Weapons[weaponIndex];

		if (weapon != null)
		{
			weapon.transform.SetParent(WeaponTypeHolderPairs[weapon.Type]);
			weapon.transform.localPosition = Vector3.zero;
			weapon.transform.localRotation = Quaternion.Euler(Vector3.zero);
		}
	}

	public FirearmScript PutOutWeaponFromHolder(int index, bool putOtherWeaponsToHolder = true)
	{
		FirearmScript weapon = Weapons[index];

		if (weapon != null)
		{
			weapon.transform.SetParent(null);
		}

		if (putOtherWeaponsToHolder)
			for (int i = 0; i < Weapons.Length; i++)
				if (i != index) PutWeaponToHolder(i);

		return weapon;
	}

	public FirearmScript RemoveWeapon(int index)
	{
		FirearmScript weapon = PutOutWeaponFromHolder(index, false);
		Weapons[index] = null;
		if (weapon != null) UnlinkInteractiveObject(weapon.gameObject);
		
		return weapon;
	}

	/// <summary>
	/// Return previous weapon from index filled by new weapon
	/// </summary>
	/// <param name="weapon"></param>
	/// <returns></returns>
	public FirearmScript AddWeapon(FirearmScript weapon, bool putToHolder)
	{
		if (weapon == null) throw new System.Exception("Null reference exception");

		int index = WeaponTypeIndexPairs[weapon.Type];
		FirearmScript previousWeapon = RemoveWeapon(index);
		SetColliderEnabled(weapon.gameObject, false);
		Weapons[index] = weapon;
		if (putToHolder) PutWeaponToHolder(index);

		return previousWeapon;
	}

	private void SetColliderEnabled(GameObject gObject, bool enabled)
	{
		var boxCollider = gObject.GetComponent<BoxCollider>();
		var capsuleCollider = gObject.GetComponent<CapsuleCollider>();
		var sphereCollider = gObject.GetComponent<SphereCollider>();
		var meshCollider = gObject.GetComponent<MeshCollider>();

		if (boxCollider != null) boxCollider.enabled = enabled;
		if (capsuleCollider != null) capsuleCollider.enabled = enabled;
		if (sphereCollider != null) sphereCollider.enabled = enabled;
		if (meshCollider != null) meshCollider.enabled = enabled;
	}

	private void UnlinkInteractiveObject(GameObject gObject)
	{
		gObject.transform.SetParent(null);
		SetColliderEnabled(gObject, true);
	}

	private void InitHolderDictionary()
	{
		WeaponTypeHolderPairs = new Dictionary<WeaponType, Transform>
		{
			{WeaponType.Knife, KnifeHolder },
			{WeaponType.Pistol, PistolHolder },
			{WeaponType.MachineGun, MachineGunHolder },
		};
	}

	private void InitWeaponIndexDictionary()
	{
		WeaponTypeIndexPairs = new Dictionary<WeaponType, int>
		{
			{WeaponType.Knife, 1 },
			{WeaponType.Pistol, 2 },
			{WeaponType.MachineGun, 3 },
		};
	}

	private void Awake()
	{
		Weapons = new FirearmScript[WeaponCellsNumber];
		InitWeaponIndexDictionary();
		InitHolderDictionary();
	}
}
