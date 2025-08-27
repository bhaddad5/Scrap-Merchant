// ContainerPanelUI.cs
using UnityEngine;
using UnityEngine.UI;

public class ContainerPanelUI : MonoBehaviour
{
	public static ContainerPanelUI I { get; private set; }

	[Header("Wiring")]
	[SerializeField] GameObject root;
	[SerializeField] TMPro.TextMeshProUGUI titleText;
	[SerializeField] Transform slotsParent; // e.g., a GridLayoutGroup
	[SerializeField] InventorySlotUI slotPrefab;

	Inventory current;

	void Awake()
	{
		I = this;
		root.SetActive(false);
	}

	public void Open(Inventory inv, string title = "Container")
	{
		Close();

		current = inv;
		titleText.text = title;
		root.SetActive(true);

		// Build slots
		for (int i = 0; i < current.Size; i++)
		{
			var slot = Instantiate(slotPrefab, slotsParent);
			slot.inventory = current;
			slot.index = i;
			slot.Refresh();
		}
	}

	public void Close()
	{
		current = null;
		root.SetActive(false);
		// Clear old children
		for (int i = slotsParent.childCount - 1; i >= 0; i--)
			Destroy(slotsParent.GetChild(i).gameObject);
	}

	public bool IsOpen => root.activeSelf;
}
