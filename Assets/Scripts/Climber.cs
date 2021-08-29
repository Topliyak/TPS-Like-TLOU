using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
class Climber
{
	[HideInInspector] public ClimbType CurrentClimbType;
	[HideInInspector] public Vector3 Offset => ClimbTypeOffsetPairs[CurrentClimbType];
	[HideInInspector] public float DistanceToClimbPoint;
	[HideInInspector] public bool FinishingClimb = false;

	[SerializeField] Vector3 ClimbLowOffset;
	[SerializeField] Vector3 ClimbLowMiddleOffset;
	[SerializeField] Vector3 ClimbMiddleOffset;
	[SerializeField] Vector3 ClimbHighOffset;
	public float MinDistanceToClimbRef;
	public float OffsetLerp;

	public enum ClimbType : byte { ClimbLow, ClimbLowMiddle, ClimbMiddle, ClimbHigh, };
	public Dictionary<ClimbType, bool> ClimbTypeStatusPairs;
	public Dictionary<ClimbType, Vector3> ClimbTypeOffsetPairs;

	public void Initialize()
	{
		ClimbTypeStatusPairs = new Dictionary<ClimbType, bool>
		{
			{ClimbType.ClimbLow, false },
			{ClimbType.ClimbLowMiddle, false },
			{ClimbType.ClimbMiddle, false },
			{ClimbType.ClimbHigh, false },
		};

		ClimbTypeOffsetPairs = new Dictionary<ClimbType, Vector3>
		{
			{ClimbType.ClimbLow, ClimbLowOffset },
			{ClimbType.ClimbLowMiddle, ClimbLowMiddleOffset },
			{ClimbType.ClimbMiddle, ClimbMiddleOffset },
			{ClimbType.ClimbHigh, ClimbHighOffset },
		};
	}
}
