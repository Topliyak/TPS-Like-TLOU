using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponScript: MonoBehaviour
{
	[SerializeField] private WeaponType _WeaponType;
	[SerializeField] private Transform _LeftHand;

	[HideInInspector] public WeaponType Type => _WeaponType;
	[HideInInspector] public Transform LeftHand => _LeftHand;
}

public enum WeaponType
{
	Knife,
	Pistol,
	MachineGun,
}
