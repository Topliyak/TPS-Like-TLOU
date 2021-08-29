using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
	public Dictionary<WeaponType, Transform> WeaponTypeHolderPairs { get; private set; }
	public Dictionary<WeaponType, int> WeaponTypeIndexPairs { get; private set; }
	public Firearm[] Weapons { get; private set; }
	public const int WeaponCellsNumber = 4;

	[SerializeField] Transform KnifeHolder;
	[SerializeField] Transform PistolHolder;
	[SerializeField] Transform MachineGunHolder;

	private void PutWeaponToHolder(int weaponIndex)
	{
		Firearm weapon = Weapons[weaponIndex];

		if (weapon != null)
		{
			weapon.transform.SetParent(WeaponTypeHolderPairs[weapon.Type]);
			weapon.transform.localPosition = Vector3.zero;
			weapon.transform.localRotation = Quaternion.Euler(Vector3.zero);
		}
	}

	public Firearm PutOutWeaponFromHolder(int index, bool putOtherWeaponsToHolder = true)
	{
		Firearm weapon = Weapons[index];

		if (weapon != null)
		{
			weapon.transform.SetParent(null);
		}

		if (putOtherWeaponsToHolder)
			for (int i = 0; i < Weapons.Length; i++)
				if (i != index) PutWeaponToHolder(i);

		return weapon;
	}

	public Firearm RemoveWeapon(int index)
	{
		Firearm weapon = PutOutWeaponFromHolder(index, false);
		Weapons[index] = null;
		if (weapon != null) UnlinkWeapon(weapon.gameObject);
		
		return weapon;
	}

	/// <summary>
	/// Return previous weapon from index filled by new weapon
	/// </summary>
	/// <param name="weapon"></param>
	/// <returns></returns>
	public Firearm AddWeapon(Firearm weapon, bool putToHolder)
	{
		if (weapon == null) throw new System.Exception("Null reference exception");

		int index = WeaponTypeIndexPairs[weapon.Type];
		Firearm previousWeapon = RemoveWeapon(index);
		LinkWeapon(weapon.gameObject);
		Weapons[index] = weapon;
		if (putToHolder) PutWeaponToHolder(index);

		return previousWeapon;
	}

	private void LinkWeapon(GameObject gObject)
	{
		Rigidbody rb = gObject.GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.useGravity = false;
			rb.constraints = RigidbodyConstraints.FreezeAll;
		}

		gObject.GetComponent<Collider>().enabled = false;
	}

	private void UnlinkWeapon(GameObject gObject)
	{
		gObject.transform.SetParent(null);
		Rigidbody rb = gObject.GetComponent<Rigidbody>();
		if (rb != null)
		{
			rb.useGravity = true;
			rb.constraints = RigidbodyConstraints.None;
		}

		gObject.GetComponent<Collider>().enabled = true;
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
		Weapons = new Firearm[WeaponCellsNumber];
		InitWeaponIndexDictionary();
		InitHolderDictionary();
	}
}
