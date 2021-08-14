using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryScript : MonoBehaviour
{
	public WeaponScript[] Weapons { get; private set; } = new WeaponScript[4];
	public Dictionary<WeaponType, int> WeaponTypeIndexInArrayPairs { get; } = new Dictionary<WeaponType, int>
	{
		{WeaponType.Knife, 1 },
		{WeaponType.Pistol, 2 },
		{WeaponType.MachineGun, 3 },
	};

	public void AddItem(GameObject item)
	{
		WeaponScript weaponScript = item.GetComponent<WeaponScript>();

		if (weaponScript != null) AddWeapon(weaponScript);
	}

	public WeaponScript RemoveWeapon(int index)
	{
		WeaponScript weapon = Weapons[index];
		Weapons[index] = null;
		
		return weapon;
	}

	/// <summary>
	/// Return previous weapon from index filled by new weapon
	/// </summary>
	/// <param name="weapon"></param>
	/// <returns></returns>
	public WeaponScript AddWeapon(WeaponScript weapon)
	{
		int index = WeaponTypeIndexInArrayPairs[weapon.Type];
		WeaponScript previousWeapon = Weapons[index];
		Weapons[index] = weapon;

		return previousWeapon;
	}
}
