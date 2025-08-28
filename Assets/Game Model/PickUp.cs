// PickUpOnXZPlane.cs
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class PickUp : MonoBehaviour
{
	[Header("Motion")]
	[Tooltip("How snappy the object follows the mouse intersection on the XZ plane or snap target.")]
	public float followLerp = 20f;
	[Tooltip("Freeze rotation while held.")]
	public bool freezeRotationWhileHeld = true;

	[Header("Drop")]
	[Tooltip("Optional small forward push on drop.")]
	public float dropForwardImpulse = 0f;

	[Header("Blueprint Snapping")]
	[Tooltip("Align rotation to the ItemComponent transform while snapped.")]
	public bool snapAlignRotation = true;

	public Item MyItemType { get; set; }
	public int StartingContainerIndex { get; set; }
	public bool IsNewItem { get; set; }

	Camera cam;
	Rigidbody rb;

	// Drag/hold state
	bool isHeld;
	Plane dragPlane;          // world XZ plane at the object's initial Y (firstY)
	Vector3 offsetXZ;         // world-space XZ offset so it doesn't snap to cursor
	Transform prevParent;

	// Original physics state
	bool hadRB;
	bool prevUseGravity;
	bool prevIsKinematic;
	RigidbodyConstraints prevConstraints;

	// Snapping state
	int blueprintMask;
	ItemComponent currentSnapTarget;  // null when not snapping
	SlotRowVisualizer currentContainer;

	private bool firstPickup = true;
	private float firstY = 0;

	void Awake()
	{
		cam = Camera.main;

		// Remember existing rigidbody state if present
		rb = GetComponent<Rigidbody>();
		hadRB = rb != null;
		if (rb)
		{
			prevUseGravity = rb.useGravity;
			prevIsKinematic = rb.isKinematic;
			prevConstraints = rb.constraints;
		}

		// Only raycast against PickupDropPoints layer (must exist in project)
		blueprintMask = LayerMask.GetMask("PickupDropPoints");
	}

	void Update()
	{
		if (!isHeld) return;

		// Allow dropping even if the cursor isn't over the object anymore
		if (Input.GetMouseButtonUp(0))
		{
			Drop();
			return;
		}

		Vector3 target = transform.position; // where we want to move this frame

		// Ray from mouse for snapping and/or plane dragging
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);

		// Try to snap to a matching ItemComponent on ItemBlueprints layer
		ItemComponent newSnap = null;
		currentContainer = null;
		if (blueprintMask != 0 &&
			Physics.Raycast(ray, out RaycastHit snapHit, 1000f, blueprintMask, QueryTriggerInteraction.Collide))
		{
			currentContainer = snapHit.collider.GetComponent<SlotRowVisualizer>();

			if (currentContainer && IsNewItem && currentContainer.inventory.outputSlotIndex != currentContainer.slotIndex)
				currentContainer = null;
			if (currentContainer && !IsNewItem && currentContainer.inventory.outputSlotIndex == currentContainer.slotIndex)
				currentContainer = null;

			// Get ItemComponent on the hit object (or its parents)
			newSnap = snapHit.collider.GetComponent<ItemComponent>();
			if (!newSnap) newSnap = snapHit.collider.GetComponentInParent<ItemComponent>();

			// Validate match against our held item type
			if (newSnap && newSnap.RequiredItem != MyItemType)
				newSnap = null; // not a match -> treat as no snap
		}

		// If we have a valid snap target, move toward it; else drag on XZ plane
		if (newSnap != null)
		{
			currentSnapTarget = newSnap;
			target = currentSnapTarget.transform.position;

			if (snapAlignRotation)
			{
				transform.rotation = Quaternion.Slerp(
					transform.rotation,
					currentSnapTarget.transform.rotation,
					Time.deltaTime * followLerp
				);
			}
		}
		else
		{
			currentSnapTarget = null;

			// Compute intersection point of mouse ray with the drag plane
			if (dragPlane.Raycast(ray, out float enter))
			{
				Vector3 hit = ray.GetPoint(enter); // point on plane
				target = new Vector3(hit.x + offsetXZ.x, firstY, hit.z + offsetXZ.z);
			}
		}

		// Smoothly move toward target
		transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * followLerp);
	}

	void OnMouseDown()
	{
		if (firstPickup)
		{
			firstY = transform.position.y;
			firstPickup = false;
		}

		// Ignore clicks through UI
		if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;

		// Ensure a Rigidbody exists
		if (!rb) rb = gameObject.AddComponent<Rigidbody>();

		prevParent = transform.parent;

		// Store original physics
		prevUseGravity = rb.useGravity;
		prevIsKinematic = rb.isKinematic;
		prevConstraints = rb.constraints;

		// Prep physics for manual motion
		rb.useGravity = false;
		rb.isKinematic = true;
		if (freezeRotationWhileHeld) rb.constraints |= RigidbodyConstraints.FreezeRotation;

		// Build the drag plane at the initial Y (world XZ plane)
		dragPlane = new Plane(Vector3.up, new Vector3(0f, firstY, 0f));

		// Compute XZ offset so the pivot doesn't snap to the cursor
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);
		if (dragPlane.Raycast(ray, out float enter))
		{
			Vector3 hit = ray.GetPoint(enter);
			offsetXZ = new Vector3(transform.position.x - hit.x, 0f, transform.position.z - hit.z);
		}
		else
		{
			offsetXZ = Vector3.zero; // fallback (rare)
		}

		isHeld = true;
	}

	void OnDisable()
	{
		if (isHeld) Drop();
	}

	void Drop()
	{
		isHeld = false;

		if (currentSnapTarget)
		{
			currentSnapTarget.ShowAsBlueprint(false);
			currentSnapTarget.MarkReady(StartingContainerIndex);
			GameObject.Destroy(gameObject);

			currentContainer = null;
			currentSnapTarget = null;

			return;
		}

		if(currentContainer && currentContainer.inventory.SlotCanAcceptItem(currentContainer.slotIndex, MyItemType, 1))
		{
			if (IsNewItem)
			{
				foreach(var component in GetComponentsInChildren<ItemComponent>())
					currentContainer.inventory.TakeFromSlot(component.SrcInventorySlot, 1);
				currentContainer.inventory.PlaceInSlot(currentContainer.slotIndex, new ItemStack() { count = 1, item = MyItemType});
			}
			else
			{
				var itemGrab = currentContainer.inventory.TakeFromSlot(StartingContainerIndex, 1);
				currentContainer.inventory.PlaceInSlot(currentContainer.slotIndex, itemGrab);
			}

			GameObject.Destroy(gameObject);

			currentContainer = null;
			currentSnapTarget = null;

			return;
		}
		

		if (!rb) return;

		// Restore physics
		rb.isKinematic = false;
		rb.useGravity = true;
		rb.constraints = prevConstraints;

		// Optional small forward toss
		if (dropForwardImpulse > 0f)
			rb.AddForce((cam ? cam.transform.forward : transform.forward) * dropForwardImpulse, ForceMode.VelocityChange);
	}
}
