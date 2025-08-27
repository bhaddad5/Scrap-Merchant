// ObjectSlotUI.cs
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ObjectSlotUI : MonoBehaviour, IPointerClickHandler
{
	[SerializeField] Image icon;
	[SerializeField] TMPro.TextMeshProUGUI countText;
	[SerializeField] ItemStack slot;

	void Update()
	{
		bool has = !slot.IsEmpty;
		icon.enabled = has;
		countText.enabled = has && slot.count > 1;
		if (has)
		{
			icon.sprite = slot.item.Icon;
			countText.text = slot.count.ToString();
		}
	}

	public void OnPointerClick(PointerEventData e)
	{
		var hand = HandController.I.Hand;

		if (e.button == PointerEventData.InputButton.Left)
		{
			if (hand.IsEmpty) { HandController.I.Hand = slot; slot = ItemStack.Empty; }
			else if (slot.IsEmpty) { slot = hand; HandController.I.Hand = ItemStack.Empty; }
			else if (slot.item == hand.item)
			{
				int add = slot.AddUpTo(hand.count);
				hand.count -= add;
				HandController.I.Hand = hand.count > 0 ? hand : ItemStack.Empty;
			}
			else
			{
				var tmp = slot; slot = hand; HandController.I.Hand = tmp;
			}
		}
		else if (e.button == PointerEventData.InputButton.Right)
		{
			// Optional: open a split to move N into/out of object slot
			// (similar to InventorySlotUI + SplitStackPanel)
		}
	}
}
