// SplitStackPanel.cs
using UnityEngine;
using UnityEngine.UI;

public class SplitStackPanel : MonoBehaviour
{
	public static SplitStackPanel I { get; private set; }

	[SerializeField] GameObject root;
	[SerializeField] Slider slider;
	[SerializeField] TMPro.TMP_InputField input;
	[SerializeField] TMPro.TextMeshProUGUI title;

	InventorySlotUI currentSlot;
	int maxCount;

	void Awake()
	{
		I = this;
		root.SetActive(false);
		slider.wholeNumbers = true;
		slider.onValueChanged.AddListener(OnSlider);
		input.onValueChanged.AddListener(OnInput);
	}

	public void Open(InventorySlotUI slot, ItemStack stack)
	{
		currentSlot = slot;
		maxCount = stack.count;
		title.text = $"Split '{stack.item.DisplayName}'";
		slider.minValue = 1;
		slider.maxValue = maxCount;
		slider.value = Mathf.Clamp(maxCount / 2, 1, maxCount);
		input.text = slider.value.ToString();
		root.SetActive(true);
	}

	public void Confirm()
	{
		int amount = Mathf.Clamp(int.Parse(input.text), 1, maxCount);
		currentSlot.TakeFromSlotToHand(amount);
		Close();
	}

	public void Cancel() => Close();

	void Close()
	{
		root.SetActive(false);
		currentSlot = null;
	}

	void OnSlider(float v) => input.text = ((int)v).ToString();
	void OnInput(string s)
	{
		if (int.TryParse(s, out int v))
			slider.value = Mathf.Clamp(v, 1, maxCount);
	}
}
