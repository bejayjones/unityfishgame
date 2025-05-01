using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


public class FlockSystem : SystemBase {

	private EntityQuery m_Group;

	protected override void OnCreate () {
		m_Group = GetEntityQuery (typeof (Translation), typeof (Rotation), ComponentType.ReadOnly<FlockData_IJobChunk> ());
	}

	protected override void OnDestroy () {
		m_Group.Dispose ();
	}


	[BurstCompile]
	public struct FlockJob : IJobChunk {

		[ReadOnly] public float deltaTime;
		public ArchetypeChunkComponentType<Translation> PositionType;
		public ArchetypeChunkComponentType<Rotation> RotationType;
		[ReadOnly] public ArchetypeChunkComponentType<FlockData_IJobChunk> FlockDataType;

		public void Execute (ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex) {
			var position = chunk.GetNativeArray (PositionType);
			var rotation = chunk.GetNativeArray (RotationType);
			var flockData = chunk.GetNativeArray (FlockDataType);

			for (var chi = 0; chi < chunk.Count; chi++) {
				// ROTATE
				Bounds bound = new Bounds (flockData[chi].Origo, flockData[chi].SwimLimits);
				if (!bound.Contains (position[chi].Value)) {
					float3 direction = math.normalize (flockData[chi].Goal - position[chi].Value);
					rotation[chi] = new Rotation {
						Value = math.slerp (rotation[chi].Value, quaternion.LookRotation (direction, math.up ()), flockData[chi].TurnSpeed * deltaTime)
					};
				}
				// MOVE
				position[chi] = new Translation {
					Value = position[chi].Value + (deltaTime * flockData[chi].MoveSpeed) * math.forward (rotation[chi].Value)
				};
			}

		}

	}


	protected override void OnUpdate () {
		var rotationType = GetArchetypeChunkComponentType<Rotation> ();
		var positionType = GetArchetypeChunkComponentType<Translation> ();
		var flockDataType = GetArchetypeChunkComponentType<FlockData_IJobChunk> ();

		var job = new FlockJob () {
			RotationType = rotationType,
			PositionType = positionType,
			FlockDataType = flockDataType,
			deltaTime = Time.DeltaTime
		};

		Dependency = job.Schedule (m_Group, Dependency);
	}

}
