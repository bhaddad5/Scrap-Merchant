// DemoBootstrap.cs
using UnityEngine;

public class DemoBootstrap : MonoBehaviour
{
	public Inventory playerInv;
	public Item metalStick;
	public Item arrowhead;

	void Start()
	{
		// Put some starter items in the first slots
		playerInv.Set(0, new ItemStack { item = metalStick, count = 8 });
		playerInv.Set(1, new ItemStack { item = arrowhead, count = 7 });
	}
}