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

	List<GameObject> spawnedItems = new List<GameObject>();

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
		ClearSpawned();

		for (int n = 0; n < GetStack().count; n++)
		{
			var go = Instantiate(GetStack().item.Prefab, contentRoot);

			var placement = GetNextPlacementTransform();

			go.transform.localPosition = placement.localPosition;
			go.transform.localEulerAngles = placement.localEuler;
			go.transform.localScale = placement.localScale;

			SetupSpawnedItem(go);
		}
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

		spawnedItems.Add(go);
	}

	void ClearSpawned()
	{
		if (!contentRoot) return;

		foreach(var item in spawnedItems)
		{
			GameObject.Destroy(item);
		}
		spawnedItems.Clear();
	}

	/// <summary>
	/// Computes the local transform (position, euler, scale) where the next item
	/// will be placed, based on current layout settings and spawnedItems.Count.
	/// Does not instantiate or modify the scene.
	/// </summary>
	public (Vector3 localPosition, Vector3 localEuler, Vector3 localScale) GetNextPlacementTransform()
	{
		// Ensure we have a content root
		if (!contentRoot) contentRoot = transform;

		// Read the stack to determine maxStack/columns like Rebuild()
		var stack = GetStack();
		int maxStack = 1;
		if (stack.item != null)
			maxStack = Mathf.Max(1, stack.item.MaxStack);

		int rowsCap = Mathf.Max(1, maxRows);
		int colsCap = Mathf.Max(1, Mathf.CeilToInt((float)maxStack / rowsCap));

		// Which index are we placing next?
		int n = Mathf.Max(0, spawnedItems.Count);

		// Rows used after placing the next item (affects centering)
		int countIfPlaced = n + 1;
		int rowsUsed = Mathf.Min(rowsCap, Mathf.CeilToInt((float)countIfPlaced / colsCap));

		// Horizontal spacing across the full maxWidth (fixed by colsCap)
		float xStep = (colsCap > 1) ? (maxWidth / (colsCap - 1)) : 0f;

		// Base Z with optional centering of the USED rows
		float baseZ = startLocalOffset.z;
		if (centerRows && rowsUsed > 1)
			baseZ -= (rowsUsed - 1) * rowOffset * 0.5f;

		// Row/column for this next index (row-major)
		int row = n / colsCap;
		int col = n % colsCap;

		// Local position (each row starts at left edge = startLocalOffset.x)
		float xLocal = startLocalOffset.x + col * xStep;
		float yLocal = startLocalOffset.y;
		float zLocal = baseZ + row * rowOffset;

		Vector3 localPos = new Vector3(xLocal, yLocal, zLocal);

		// Orientation & scale come from the class settings
		return (localPos, itemLocalEuler, itemLocalScale);
	}

}
