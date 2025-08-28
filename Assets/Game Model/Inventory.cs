// Inventory.cs
using System;
using UnityEngine;

public class Inventory : MonoBehaviour
{
	[SerializeField] int size = 10;
	[SerializeField] ItemStack[] slots;

	[SerializeField] int outputSlotIndex = -1;

	public int Size => size;
	public ItemStack Get(int i) => slots[i];
	public void Set(int i, ItemStack s) => slots[i] = s;

	public event Action OnInventoryUpdated;

	void OnValidate()
	{
		if (slots == null || slots.Length != size)
			slots = new ItemStack[size];
	}

	// Merge hand into slot
	public void TryPlaceFromHand(int index, ref ItemStack hand)
	{
		if (index == outputSlotIndex)
			return;

		var slot = slots[index];

		if (hand.IsEmpty) // pick up (swap)
		{
			slots[index] = ItemStack.Empty;
			hand = slot;
			OnInventoryUpdated?.Invoke();
			return;
		}

		if (slot.IsEmpty) // drop whole hand
		{
			slots[index] = hand;
			hand = ItemStack.Empty;
			OnInventoryUpdated?.Invoke();
			return;
		}

		if (slot.item == hand.item) // merge into slot
		{
			int added = slot.AddUpTo(hand.count);
			hand.count -= added;
			slots[index] = slot;
			if (hand.count <= 0) hand = ItemStack.Empty;
			OnInventoryUpdated?.Invoke();
			return;
		}

		// different items -> swap
		slots[index] = hand;
		hand = slot;
		OnInventoryUpdated?.Invoke();
	}

	// Take up to 'n' items out of slot (for splitting to hand)
	public ItemStack TakeFromSlot(int index, int n)
	{
		var slot = slots[index];
		if (slot.IsEmpty || n <= 0) return ItemStack.Empty;

		int take = Mathf.Min(slot.count, n);
		var outStack = new ItemStack { item = slot.item, count = take };
		slot.count -= take;
		if (slot.count <= 0) slot.Clear();
		slots[index] = slot;
		OnInventoryUpdated?.Invoke();
		return outStack;
	}
}
