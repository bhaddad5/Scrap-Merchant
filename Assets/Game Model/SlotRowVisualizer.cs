// SlotRowVisualizer.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SlotRowVisualizer : MonoBehaviour
{
	[Header("Source")]
	public Inventory inventory;     // inventory that owns the slot
	[Min(0)] public int slotIndex;  // which slot to render

	[Header("Layout")]
	public Transform contentRoot;               // where to parent spawned items (defaults to self)
	[Min(0f)] public float maxWidth = .3f;      // total horizontal width for a full row
	[Min(1)] public int maxRows = 1;        // maximum rows to use
	[Min(0f)] public float rowOffset = .1f;     // Z offset between successive rows
	public Vector3 startLocalOffset = Vector3.zero;   // origin (left/front) of the grid
	public Vector3 itemLocalEuler = new Vector3(0, 30, 0);
	public Vector3 itemLocalScale = Vector3.one;

	[Tooltip("Center the used rows around startLocalOffset.z when fewer than maxRows are used.")]
	public bool centerRows = true;

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

	void SetupSpawnedItem(GameObject go)
	{
		// Attach your pickup script (fields preserved)
		var pickupScript = go.AddComponent<PickUp>();
		pickupScript.MyItemType = GetStack().item;
		pickupScript.StartingContainerIndex = slotIndex;

		// Destroy all child-colliders; we only want to grab the whole object
		var colliders = go.GetComponentsInChildren<Collider>(true);
		for (int c = 0; c < colliders.Length; c++)
		{
			if (colliders[c].gameObject != go)
				Destroy(colliders[c]);
		}

		// Mark so we can safely delete only our spawns
		if (!go.TryGetComponent<SpawnedVisualMarker>(out _))
			go.AddComponent<SpawnedVisualMarker>();
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
		if (stack.IsEmpty || stack.item == null || stack.item.Prefab == null) return;

		int maxStack = Mathf.Max(1, stack.item.MaxStack);
		int count = Mathf.Max(0, stack.count);
		if (count == 0) return;

		// Fixed grid based on maxStack + maxRows so spacing is identical across ALL rows.
		int rowsCap = Mathf.Max(1, maxRows);
		int colsCap = Mathf.Max(1, Mathf.CeilToInt((float)maxStack / rowsCap));

		// How many rows we actually use for this count
		int rowsUsed = Mathf.Min(rowsCap, Mathf.CeilToInt((float)count / colsCap));

		// Fixed horizontal step for the grid (edge -> edge across maxWidth)
		float xStep = (colsCap > 1) ? (maxWidth / (colsCap - 1)) : 0f;

		// Optional vertical centering of the USED rows around startLocalOffset.z
		float baseZ = startLocalOffset.z;
		if (centerRows && rowsUsed > 1)
			baseZ -= (rowsUsed - 1) * rowOffset * 0.5f;

		// Spawn items row-major using fixed columns so every row shares the same x-positions,
		// and each row starts at the left edge (startLocalOffset.x).
		for (int n = 0; n < count; n++)
		{
			int row = n / colsCap;
			int col = n % colsCap;

			float xLocal = startLocalOffset.x + col * xStep;          // starts at left edge
			float yLocal = startLocalOffset.y;
			float zLocal = baseZ + row * rowOffset;

			Vector3 localPos = new Vector3(xLocal, yLocal, zLocal);

			var go = Instantiate(stack.item.Prefab, contentRoot);
			go.transform.localPosition = localPos;
			go.transform.localEulerAngles = itemLocalEuler;
			go.transform.localScale = itemLocalScale;

			SetupSpawnedItem(go);
		}
	}

	void ClearSpawned()
	{
		if (!contentRoot) return;

		// Destroy only children we spawned (have marker)
		var markers = contentRoot.GetComponentsInChildren<SpawnedVisualMarker>(true);
		for (int i = markers.Length - 1; i >= 0; i--)
		{
			Destroy(markers[i].gameObject);
		}
	}
}
