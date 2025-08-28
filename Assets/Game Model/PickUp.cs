// PickUp.cs
using UnityEngine;
using UnityEngine.EventSystems;

public class PickUp : MonoBehaviour
{
	[Header("Holding")]
	public Transform holdPoint;         // Optional: set to a child of the camera (e.g., "HoldPoint")
	public float holdDistance = 2.0f;   // Used if holdPoint is null (in front of camera)
	public float followLerp = 18f;      // Higher = snappier follow
	public bool alignRotation = true;   // Face same rotation as camera / holdPoint while held

	[Header("Physics")]
	public bool freezeRotationWhileHeld = true;
	public float dropForwardImpulse = 0f; // e.g., 1.5f to toss a bit forward on drop

	Camera cam;
	Rigidbody rb;
	bool isHeld;

	void Awake()
	{
		GetComponent<Collider>().enabled = true;

		cam = Camera.main;
	}

	void LateUpdate()
	{
		if (!isHeld) return;

		// Target position/rotation to follow
		Vector3 targetPos;
		Quaternion targetRot;

		if (holdPoint)
		{
			targetPos = holdPoint.position;
			targetRot = holdPoint.rotation;
		}
		else
		{
			if (!cam) cam = Camera.main;
			var origin = cam ? cam.transform : transform;
			targetPos = origin.position + origin.forward * holdDistance;
			targetRot = origin.rotation;
		}

		// Smoothly move/rotate while kinematic
		transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followLerp);
		if (alignRotation)
			transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followLerp);
	}

	void OnMouseDown()
	{
		Debug.Log("Mouse Down!");

		// Ignore clicks through UI
		if (EventSystem.current && EventSystem.current.IsPointerOverGameObject()) return;

		// Ensure we have a Rigidbody
		if (!rb) rb = gameObject.AddComponent<Rigidbody>();

		// Prep for holding
		rb.useGravity = false;
		rb.isKinematic = true; // we'll manually move it
		if (freezeRotationWhileHeld) rb.constraints |= RigidbodyConstraints.FreezeRotation;

		// Optionally parent to holdPoint so it also follows if that moves
		if (holdPoint) transform.SetParent(holdPoint, true);

		isHeld = true;
	}

	void OnMouseUp()
	{
		Drop();
	}

	void OnDisable()
	{
		// Safety: if it gets disabled while held, drop and restore
		if (isHeld) Drop();
	}

	void Drop()
	{
		isHeld = false;

		// Restore physics (or make it fall if it didn't have RB before)
		if (rb)
		{

			Rigidbody.Destroy(rb);
		}
	}
}
