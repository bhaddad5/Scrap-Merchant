// InventorySlotUI.cs
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
	public Inventory inventory;
	public int index;

	[SerializeField] Image icon;
	[SerializeField] TMPro.TextMeshProUGUI countText;

	public void Refresh()
	{
		var s = inventory.Get(index);
		bool has = !s.IsEmpty;
		icon.enabled = has;
		countText.enabled = has && s.count > 1;
		if (has)
		{
			icon.sprite = s.item.Icon;
			countText.text = s.count.ToString();
		}
	}

	void Update() => Refresh();

	public void OnPointerClick(PointerEventData e)
	{
		if (e.button == PointerEventData.InputButton.Left)
		{
			// Place/pick/swap with hand
			var hand = HandController.I.Hand;
			inventory.TryPlaceFromHand(index, ref hand);
			HandController.I.Hand = hand;
			Refresh();
		}
		else if (e.button == PointerEventData.InputButton.Right)
		{
			var s = inventory.Get(index);
			if (s.IsEmpty) return;
			SplitStackPanel.I.Open(this, s);
		}
	}

	// Called by split panel when confirmed
	public void TakeFromSlotToHand(int amount)
	{
		var taken = inventory.TakeFromSlot(index, amount);
		if (!taken.IsEmpty)
		{
			// If hand empty or same item, just add; otherwise try swap into slot:
			var hand = HandController.I.Hand;
			if (hand.IsEmpty || hand.item == taken.item)
			{
				if (hand.IsEmpty) HandController.I.Hand = taken;
				else HandController.I.Hand.count += taken.count;
			}
			else
			{
				// hand has another item -> put taken back (cancel)
				var s = inventory.Get(index);
				// merge back
				if (s.IsEmpty) inventory.Set(index, taken);
				else if (s.item == taken.item)
				{
					s.count += taken.count; inventory.Set(index, s);
				}
				// else: ignore to keep minimal; could also drop to world, etc.
			}
		}
		Refresh();
	}
}
