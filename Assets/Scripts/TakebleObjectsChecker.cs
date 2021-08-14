using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TakebleObjectsChecker : MonoBehaviour
{
	public PersonController Controller;

	private void OnTriggerStay(Collider other)
	{
		Controller.OnInteractiveObjectDetected(other.gameObject);
	}

	private void OnTriggerExit(Collider other)
	{
		Controller.OnInteractiveObjectLost(other.gameObject);
	}
}
