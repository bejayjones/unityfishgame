using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Rendering;
using Random = Unity.Mathematics.Random;
using System.Collections;
using Unity.Collections;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif


[System.Serializable]
public class Unit {
	public int count;
	public Mesh mesh;
	public Material material;
	public ShadowCastingMode castShadow = ShadowCastingMode.Off;
}


public class FlockBox : MonoBehaviour {

	public Unit[] units;
	[SerializeField] [MinMaxSlider (1f, 10f)] protected MinMax size = new MinMax (1f, 2f);
	[SerializeField] [MinMaxSlider (1f, 10f)] protected MinMax moveSpeed = new MinMax (2f, 5f);
	[SerializeField] [MinMaxSlider (0.2f, 1f)] protected MinMax turnSpeed = new MinMax (0.3f, 0.6f);
	[SerializeField] [MinMaxSlider (1f, 10f)] protected MinMax changeGoal = new MinMax (1f, 3f);

	public bool showGizmo;
	public Color gizmoColor = new Color (0, 1f, 0.79f, 1f);

	private Vector3 goalPos;
	EntityManager entityManager;
	EntityArchetype flockArch;
	NativeArray<Entity>[] entities;
	World activeWorld;
	Random rand;

	//	DRAW FLOCK AREA
#if UNITY_EDITOR
	public Color wireColor;
	void OnDrawGizmosSelected () {
		Gizmos.color = gizmoColor;
		Gizmos.DrawCube (transform.position, new Vector3 (transform.localScale.x * 2f, transform.localScale.y * 2f, transform.localScale.z * 2f));
		wireColor = gizmoColor;
		wireColor.a = 1f;
		Gizmos.color = wireColor;
		Gizmos.DrawWireCube (transform.position, new Vector3 (transform.localScale.x * 2f, transform.localScale.y * 2f, transform.localScale.z * 2f));
	}

	void OnDrawGizmos () {
		if (showGizmo) {
			Gizmos.color = gizmoColor;
			Gizmos.DrawCube (transform.position, new Vector3 (transform.localScale.x * 2f, transform.localScale.y * 2f, transform.localScale.z * 2f));
			wireColor = gizmoColor;
			wireColor.a = 1f;
			Gizmos.color = wireColor;
			Gizmos.DrawWireCube (transform.position, new Vector3 (transform.localScale.x * 2f, transform.localScale.y * 2f, transform.localScale.z * 2f));
		}
	}
#endif


	void Start () {
		if (units.Length == 0) return;

		rand = new Random ((uint)UnityEngine.Random.Range (100, 999));

		activeWorld = World.DefaultGameObjectInjectionWorld;
		entityManager = activeWorld.EntityManager;

		flockArch = entityManager.CreateArchetype (
			ComponentType.ReadWrite<Translation> (),
			ComponentType.ReadWrite<Rotation> (),
			ComponentType.ReadWrite<NonUniformScale> (),
			ComponentType.ReadWrite<RenderMesh> (),
			ComponentType.ReadWrite<FlockData_IJobChunk> (),
			ComponentType.ReadOnly<LocalToWorld> (),
			ComponentType.ReadOnly<RenderBounds> ()
			//typeof (RenderBounds)
		);

		goalPos = GetGoal ();

		entities = new NativeArray<Entity>[units.Length];

		for (int u = 0; u < units.Length; u++) {
			entities[u] = new NativeArray<Entity> (units[u].count, Allocator.Persistent);
			entityManager.CreateEntity (flockArch, entities[u]);

			for (int i = 0; i < units[u].count; i++) {
				entityManager.SetComponentData (entities[u][i], new Translation {
					Value = transform.position + new Vector3 (
					rand.NextFloat (-transform.localScale.x, transform.localScale.x),
					rand.NextFloat (-transform.localScale.y, transform.localScale.y),
					rand.NextFloat (-transform.localScale.z, transform.localScale.z))
				});

				Quaternion randRot = Quaternion.LookRotation (new Vector3 (rand.NextFloat (-10f, 10f), 0, rand.NextFloat (-10f, 10f)), transform.up);
				entityManager.SetComponentData (entities[u][i], new Rotation { Value = randRot });

				entityManager.SetSharedComponentData (entities[u][i], new RenderMesh {
					mesh = units[u].mesh,
					material = units[u].material,
					castShadows = units[u].castShadow,
					receiveShadows = true
				});

				entityManager.SetComponentData (entities[u][i], new NonUniformScale { Value = size.RandomValue });

				entityManager.SetComponentData (entities[u][i], new FlockData_IJobChunk {
					Origo = transform.position,
					Goal = goalPos,
					MoveSpeed = moveSpeed.RandomValue,
					TurnSpeed = turnSpeed.RandomValue,
					SwimLimits = transform.localScale * 2f
				});
			}

		}

		StartCoroutine (ChangeTarget ());

	}   // END of START


	IEnumerator ChangeTarget () {
		WaitForSeconds changeTime;

		while (true) {
			goalPos = GetGoal ();

			for (int u = 0; u < units.Length; u++) {
				for (int i = 0; i < units[u].count; i++) {
					FlockData_IJobChunk floDat = entityManager.GetComponentData<FlockData_IJobChunk> (entities[u][i]);
					entityManager.SetComponentData (entities[u][i], new FlockData_IJobChunk {
						Origo = transform.position,
						Goal = goalPos,
						MoveSpeed = floDat.MoveSpeed,
						TurnSpeed = floDat.TurnSpeed,
						SwimLimits = transform.localScale
					});
				}
			}

			changeTime = new WaitForSeconds (changeGoal.RandomValue);
			yield return changeTime;
		}
	}


	private Vector3 GetGoal () {
		Vector3 gl = transform.position + new Vector3 (
				rand.NextFloat (-transform.localScale.x, transform.localScale.x),
				rand.NextFloat (-transform.localScale.y, transform.localScale.y),
				rand.NextFloat (-transform.localScale.z, transform.localScale.z)
				);
		return gl;
	}


	private void OnDisable () {
		if (units.Length == 0) return;

		StopAllCoroutines ();

		if (World.DefaultGameObjectInjectionWorld == activeWorld) {
			var eManager = activeWorld.EntityManager;
			for (int i = 0; i < entities.Length; i++) {
				if (entities[i].IsCreated) {
					eManager.DestroyEntity (entities[i]);
					entities[i].Dispose ();
				}
			}
		}

	}

}   // END of EntityFlock


#if UNITY_EDITOR
[CustomEditor (typeof (FlockBox))]
public class EntityFlockGUI : Editor {

	public override void OnInspectorGUI () {
		serializedObject.Update ();

		GUILayout.Space (3);
		EditorGUILayout.BeginHorizontal ();
		GUILayout.FlexibleSpace ();
		GUILayout.Label ("F L O C K   S E T U P");
		GUILayout.FlexibleSpace ();
		EditorGUILayout.EndHorizontal ();
		GUILayout.Space (5);

		SerializedProperty unitField = serializedObject.FindProperty ("units");
		if (unitField.arraySize == 0) unitField.arraySize = 1;
		EditorGUILayout.PropertyField (unitField.FindPropertyRelative ("Array.size"), new GUIContent ("Render Units"));
		for (int i = 0; i < unitField.arraySize; i++) {
			SerializedProperty item = unitField.GetArrayElementAtIndex (i);
			if (item.FindPropertyRelative ("count").intValue == 0) item.FindPropertyRelative ("count").intValue = 1;
			EditorGUILayout.BeginHorizontal ();
			GUILayout.Label ("Count");
			EditorGUILayout.PropertyField (item.FindPropertyRelative ("count"), GUIContent.none);
			GUILayout.Label ("Mesh");
			EditorGUILayout.PropertyField (item.FindPropertyRelative ("mesh"), GUIContent.none);
			GUILayout.Label ("Material");
			EditorGUILayout.PropertyField (item.FindPropertyRelative ("material"), GUIContent.none);
			GUILayout.Label ("Shadow");
			EditorGUILayout.PropertyField (item.FindPropertyRelative ("castShadow"), GUIContent.none);
			EditorGUILayout.EndHorizontal ();
		}
		GUILayout.Space (10);

		EditorGUILayout.PropertyField (serializedObject.FindProperty ("size"));
		GUILayout.Space (5);
		EditorGUILayout.PropertyField (serializedObject.FindProperty ("moveSpeed"));
		GUILayout.Space (5);
		EditorGUILayout.PropertyField (serializedObject.FindProperty ("turnSpeed"));
		GUILayout.Space (5);
		EditorGUILayout.PropertyField (serializedObject.FindProperty ("changeGoal"));
		GUILayout.Space (10);
		EditorGUILayout.BeginHorizontal ();
		EditorGUILayout.PropertyField (serializedObject.FindProperty ("showGizmo"));
		EditorGUILayout.PropertyField (serializedObject.FindProperty ("gizmoColor"), GUIContent.none);
		EditorGUILayout.EndHorizontal ();
		GUILayout.Space (5);

		serializedObject.ApplyModifiedProperties ();
	}

}
#endif