// HandController.cs
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HandController : MonoBehaviour
{
	public static HandController I { get; private set; }

	public ItemStack Hand; // what we are carrying
	[SerializeField] Image background;
	[SerializeField] Image icon;
	[SerializeField] TMPro.TextMeshProUGUI countText;
	[SerializeField] Canvas parentCanvas;

	void Awake() => I = this;

	void Update()
	{
		// follow cursor
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			(RectTransform)parentCanvas.transform,
			Input.mousePosition, parentCanvas.worldCamera, out var local);
		((RectTransform)transform).anchoredPosition = local;

		// visuals
		bool show = !Hand.IsEmpty;
		icon.enabled = show;
		background.enabled = show;
		countText.enabled = show;
		if (show)
		{
			//gameObject.SetActive(true);
			icon.sprite = Hand.item.Icon;
			countText.text = Hand.count > 1 ? Hand.count.ToString() : "";
		}
		else
		{
			//gameObject.SetActive(false);
		}
	}

	public void AddToHand(ItemStack stack) // merge or set
	{
		if (Hand.IsEmpty) { Hand = stack; return; }
		if (Hand.item == stack.item)
		{
			int add = Mathf.Min(Hand.item.MaxStack - Hand.count, stack.count);
			Hand.count += add;
			stack.count -= add;
		}
		// Note: caller should handle any leftover in 'stack' before calling.
	}
}
