using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingTableMenu : MonoBehaviour
{
	public static CraftingTableMenu I { get; private set; }

	[Header("Wiring")]
	[SerializeField] GameObject root;
	[SerializeField] TMPro.TextMeshProUGUI titleText;
	[SerializeField] Transform slotsParent;
	[SerializeField] InventorySlotUI slotPrefab;

	[SerializeField] Transform recipesParent;
	[SerializeField] RecipeSelectionUi recipePrefab;

	private Item selectedRecipe = null;
	private GameObject spawnedRecipeItem = null;

	Inventory current;
	WorldCraftingBench bench;

	private int blueprintLayer;

	void Awake()
	{
		I = this;
		root.SetActive(false);
		blueprintLayer = LayerMask.NameToLayer("PickupDropPoints");
	}

	public void Open(WorldCraftingBench bench)
	{
		Close();

		this.bench = bench;
		current = bench.Inventory;
		titleText.text = bench.displayName;
		root.SetActive(true);

		UpdateRecipesSelection();
		current.OnInventoryUpdated += UpdateRecipesSelection;

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
		if (bench)
		{
			foreach (var inventorySlot in bench.GetComponentsInChildren<SlotRowVisualizer>())
				inventorySlot.Rebuild();
		}

		if(current)
			current.OnInventoryUpdated -= UpdateRecipesSelection;
		ClearSelectedRecipe();
		current = null;
		bench = null;
		root.SetActive(false);
		// Clear old children
		for (int i = slotsParent.childCount - 1; i >= 0; i--)
			Destroy(slotsParent.GetChild(i).gameObject);
	}

	public bool IsOpen => root.activeSelf;

	public void SelectRecipe(Item recipe)
	{
		selectedRecipe = recipe;

		spawnedRecipeItem = GameObject.Instantiate(recipe.Prefab, bench.BuildTransform);
		var buildHandler = spawnedRecipeItem.AddComponent<ItemBuildHandler>();
		buildHandler.MyItemType = recipe;

		foreach (var comp in spawnedRecipeItem.GetComponentsInChildren<ItemComponent>())
		{
			comp.ShowAsBlueprint(true);
			comp.gameObject.layer = blueprintLayer;
		}

		for (int i = recipesParent.childCount - 1; i >= 0; i--)
			Destroy(recipesParent.GetChild(i).gameObject);
	}

	public void ClearSelectedRecipe()
	{
		selectedRecipe = null;
		GameObject.Destroy(spawnedRecipeItem);
	}

	public void UpdateRecipesSelection()
	{
		ClearSelectedRecipe();

		for (int i = recipesParent.childCount - 1; i >= 0; i--)
			Destroy(recipesParent.GetChild(i).gameObject);

		var recipes = GetAvailableRecipes(current);

		for (int i = 0; i < recipes.Count; i++)
		{
			var recipeButton = Instantiate(recipePrefab, recipesParent);
			recipeButton.Setup(recipes[i], (r) => SelectRecipe(r));
		}
	}

	public List<Item> GetAvailableRecipes(Inventory inventory)
    {
		List<Item> res = new List<Item>();
        foreach(var item in DataLookup.I.AllItems)
        {
			if (CanBuildItem(item, inventory))
				res.Add(item);
		}
		return res;
    }

	private bool CanBuildItem(Item item, Inventory inventory)
	{
		var comps = item.Prefab.GetComponentsInChildren<ItemComponent>();
		if (comps == null || comps.Length == 0) return false;

		if (inventory == null) return false;

		var needed = new HashSet<Item>();
		foreach (var c in comps)
		{
			if (c != null && c.RequiredItem != null)
				needed.Add(c.RequiredItem); // dedupe by item type
		}

		foreach (var neededItem in needed)
			if (!inventory.ContainsItem(neededItem))
				return false;

		return true;
	}	
}
