using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Helper : MonoBehaviour
{
	public bool forward, back, left, right;
	public Vector3 Min { get; private set; }
	public Vector3 Max { get; private set; }

	[SerializeField] BoxCollider _Box;

	private void Start()
	{
		UpdateBounds();
	}

	private void UpdateBounds()
	{
		Min = _Box.center - _Box.size / 2;
		Max = _Box.center + _Box.size / 2;

		float[] min = new float[3] { Min.x, Min.y, Min.z };
		float[] max = new float[3] { Max.x, Max.y, Max.z };

		for (int i = 0; i < 3; i++)
			if (min[i] > max[i])
			{
				float t = min[i];
				min[i] = max[i];
				max[i] = t;
			}

		Min.Set(min[0], min[1], min[2]);
		Max.Set(max[0], max[1], max[2]);
	}
}
