// PlayerInteractor.cs
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
	[Header("Raycast")]
	[SerializeField] Camera cam;
	[SerializeField] float useRange = 3f;
	[SerializeField] LayerMask interactMask = ~0;

	[Header("Movement Freeze")]
	[Tooltip("Movement scripts to disable while UI is open (e.g., your FPS controller, look, etc.).")]
	[SerializeField] MonoBehaviour[] movementScriptsToDisable;

	[Header("UI Prompt (optional)")]
	[SerializeField] GameObject prompt; // "Press E"

	WorldContainer lookedAt;

	void Update()
	{
		if (ContainerPanelUI.I.IsOpen)
		{
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
		ContainerPanelUI.I.Open(container.Inventory, container.displayName);
		SetMovementEnabled(false);
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
	}

	void CloseUI()
	{
		ContainerPanelUI.I.Close();
		SetMovementEnabled(true);
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
	}

	void SetMovementEnabled(bool enabled)
	{
		foreach (var m in movementScriptsToDisable)
			if (m) m.enabled = enabled;
	}
}
