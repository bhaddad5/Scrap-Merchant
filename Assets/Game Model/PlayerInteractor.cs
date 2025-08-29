// PlayerInteractor.cs
using Cinemachine;
using StarterAssets;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
	[Header("Raycast")]
	[SerializeField] Camera cam;
	[SerializeField] float useRange = 3f;
	[SerializeField] LayerMask interactMask = ~0;

	[Header("Movement Freeze")]
	[Tooltip("Movement scripts to disable while UI is open (e.g., your FPS controller, look, etc.).")]
	[SerializeField] GameObject playerCapsule;

	[Header("UI Prompt (optional)")]
	[SerializeField] GameObject prompt; // "Press E"

	WorldContainer lookedAt;

	private Vector3 savedPos;
	private Vector3 savedRot;

	void Update()
	{
		if (ContainerPanelUI.I.IsOpen || CraftingTableMenu.I.IsOpen)
		{
			if (prompt) prompt.SetActive(false);

			// While open, allow close with E or Escape
			if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
				CloseUI();
			return;
		}

		// Find target under crosshair
		lookedAt = null;
		if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, useRange, interactMask, QueryTriggerInteraction.Ignore))
			lookedAt = hit.collider.GetComponentInParent<WorldContainer>();

		if (prompt) prompt.SetActive(lookedAt != null);

		if (lookedAt != null && Input.GetKeyDown(KeyCode.E))
			OpenUI(lookedAt);
	}

	void OpenUI(WorldContainer container)
	{
		if (container is WorldCraftingBench craftingBench)
			CraftingTableMenu.I.Open(craftingBench);
		else
			ContainerPanelUI.I.Open(container);

		SetMovementEnabled(false);
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;

		savedPos = cam.transform.position;
		savedRot = cam.transform.eulerAngles;

		if (container.PoV)
		{
			cam.transform.position = container.PoV.position;
			cam.transform.eulerAngles = container.PoV.eulerAngles;
		}
	}

	void CloseUI()
	{
		ContainerPanelUI.I.Close();
		CraftingTableMenu.I.Close();

		cam.transform.position = savedPos;
		cam.transform.eulerAngles = savedRot;

		SetMovementEnabled(true);
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
	}

	void SetMovementEnabled(bool enabled)
	{
		GetComponent<CinemachineBrain>().enabled = enabled;
		playerCapsule.GetComponent<CharacterController>().enabled = enabled;
		playerCapsule.GetComponent<FirstPersonController>().enabled = enabled;
	}
}
