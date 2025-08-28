// SlotRowVisualizer.cs
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SlotRowVisualizer : MonoBehaviour
{
	[Header("Source")]
	public Inventory inventory;     // inventory that owns the slot
	[Min(0)] public int slotIndex;  // which slot to render

	[Header("Layout")]
	public Transform contentRoot;   // where to parent spawned items (defaults to self)
	[Min(0f)] public float maxWidth = .3f;   // total width used when maxStack items are present
	public Vector3 startLocalOffset = Vector3.zero;   // left edge offset
	public Vector3 itemLocalEuler = new Vector3(0, 30, 0);
	public Vector3 itemLocalScale = Vector3.one;

	[Header("Behavior")]
	public bool autoRebuild = true;          // rebuild when stack changes (play & edit)
	public bool hideWhenEmpty = false;       // optionally hide content root when slot empty

	Item _prevItem;
	int _prevCount;

	void OnEnable()
	{
		if (!contentRoot) contentRoot = transform;
		TryAutoRebuild(force: true);
	}

	void Update()
	{
		if (autoRebuild) TryAutoRebuild(force: false);
	}

	void TryAutoRebuild(bool force)
	{
		var stack = GetStack();
		if (!force && stack.item == _prevItem && stack.count == _prevCount) return;
		_prevItem = stack.item;
		_prevCount = stack.count;
		Rebuild();
	}

	ItemStack GetStack()
	{
		if (!inventory || slotIndex < 0 || slotIndex >= inventory.Size)
			return ItemStack.Empty;
		return inventory.Get(slotIndex);
	}

	public void Rebuild()
	{
		// Clear previously spawned visuals (only our marked ones)
		ClearSpawned();

		var stack = GetStack();

		if (hideWhenEmpty && contentRoot) contentRoot.gameObject.SetActive(!stack.IsEmpty);

		if (stack.IsEmpty || !stack.item || !stack.item.Prefab)
			return;

		int maxStack = Mathf.Max(1, stack.item.MaxStack);
		int count = Mathf.Max(0, stack.count);

		// Step so that maxStack items span [0 .. maxWidth]
		float step = (maxStack > 1) ? (maxWidth / (maxStack - 1)) : 0f;

		for (int i = 0; i < count; i++)
		{
			// Position i occupies x = i * step; this keeps spacing tied to maxStack.
			Vector3 localPos = startLocalOffset + new Vector3(i * step, 0f, 0f);

			var go = SafeInstantiate(stack.item.Prefab, contentRoot);
			go.transform.localPosition = localPos;
			go.transform.localEulerAngles = itemLocalEuler;
			go.transform.localScale = itemLocalScale;

			var pickupScript = go.AddComponent<PickUp>();
			pickupScript.MyItemType = stack.item;
			pickupScript.StartingContainerIndex = slotIndex;

			//Destroy all the child-colliders, we only wanna grab the whole object
			var colliders = go.GetComponentsInChildren<Collider>();
			for(int c = 0; c < colliders.Length; c++)
			{
				if (c != 0)
					Collider.Destroy(colliders[c]);
			}

			// Ensure physics/interaction won’t mess with your scene preview
			DisablePhysics(go);

			// Mark so we can safely delete only our spawns
			if (!go.TryGetComponent<SpawnedVisualMarker>(out _))
				go.AddComponent<SpawnedVisualMarker>();
		}
	}

	void ClearSpawned()
	{
		if (!contentRoot) return;

		// Destroy only children we spawned (have marker)
		var markers = contentRoot.GetComponentsInChildren<SpawnedVisualMarker>(true);
		for (int i = markers.Length - 1; i >= 0; i--)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying) DestroyImmediate(markers[i].gameObject);
			else Destroy(markers[i].gameObject);
#else
            Destroy(markers[i].gameObject);
#endif
		}
	}

	static GameObject SafeInstantiate(GameObject prefab, Transform parent)
	{
#if UNITY_EDITOR
		if (!Application.isPlaying)
			return (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
#endif
		return Instantiate(prefab, parent);
	}

	static void DisablePhysics(GameObject go)
	{
		var rb = go.GetComponent<Rigidbody>();
		if (rb) { rb.isKinematic = true; rb.useGravity = false; }
	}
}
