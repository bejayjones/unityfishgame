using Unity.Entities;
using Unity.Mathematics;
using System;


[Serializable]
public struct FlockData_IJobChunk : IComponentData {
	public float3 Origo;
	public float3 Goal;
	public float MoveSpeed;
	public float TurnSpeed;
	public float3 SwimLimits;
}

