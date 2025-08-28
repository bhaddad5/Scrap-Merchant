// PickUpOnXZPlane.cs
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class PickUp : MonoBehaviour
{
	[Header("Motion")]
	[Tooltip("How snappy the object follows the mouse intersection on the XZ plane.")]
	public float followLerp = 20f;
	[Tooltip("Freeze rotation while held.")]
	public bool freezeRotationWhileHeld = true;

	[Header("Drop")]
	[Tooltip("Optional small forward push on drop.")]
	public float dropForwardImpulse = 0f;

	Camera cam;
	Rigidbody rb;

	// Drag/hold state
	bool isHeld;
	Plane dragPlane;          // world XZ plane at the object's pickup height (Y)
	float planeY;             // constant Y while dragging
	Vector3 offsetXZ;         // world-space XZ offset so it doesn't snap to cursor
	Transform prevParent;

	// Original physics state
	bool hadRB;
	bool prevUseGravity;
	bool prevIsKinematic;
	RigidbodyConstraints prevConstraints;

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

		// Compute intersection point of mouse ray with the drag plane
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);
		if (dragPlane.Raycast(ray, out float enter))
		{
			Vector3 hit = ray.GetPoint(enter);         // point on plane
			Vector3 target = new Vector3(hit.x + offsetXZ.x, planeY, hit.z + offsetXZ.z);

			// Smoothly move toward target; Y remains fixed at planeY
			transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * followLerp);
		}
	}

	void OnMouseDown()
	{
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

		// Build the drag plane at the current Y (world XZ plane)
		planeY = transform.position.y;
		dragPlane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));

		// Compute XZ offset so the pivot doesn't snap to the cursor
		Ray ray = cam.ScreenPointToRay(Input.mousePosition);
		if (dragPlane.Raycast(ray, out float enter))
		{
			Vector3 hit = ray.GetPoint(enter);
			offsetXZ = new Vector3(transform.position.x - hit.x, 0f, transform.position.z - hit.z);
		}
		else
		{
			offsetXZ = Vector3.zero; // fallback, rare (camera perfectly parallel)
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
