using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ItemStack.cs
using System;

[Serializable]
public struct ItemStack
{
	public Item item;
	public int count;

	public bool IsEmpty => item == null || count <= 0;
	public static ItemStack Empty => new ItemStack { item = null, count = 0 };

	public int SpaceLeft => IsEmpty ? 0 : Math.Max(0, item.MaxStack - count);

	public void Clear() { item = null; count = 0; }

	public int AddUpTo(int amount) // returns actually added
	{
		if (IsEmpty || amount <= 0) return 0;
		int add = Math.Min(SpaceLeft, amount);
		count += add;
		return add;
	}
}
