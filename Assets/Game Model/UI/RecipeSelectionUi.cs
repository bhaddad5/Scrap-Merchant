using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RecipeSelectionUi : MonoBehaviour
{
	[SerializeField] Image icon;
	[SerializeField] TMPro.TextMeshProUGUI nameText;

	private Item recipe;
	private Action<Item> recipeSelected;

	public void Setup(Item recipe, Action<Item> recipeSelected)
	{
		this.recipe = recipe;
		this.recipeSelected = recipeSelected;

		icon.sprite = recipe.Icon;
		nameText.text = recipe.DisplayName;
	}

	public void SelectRecipe()
	{
		recipeSelected(recipe);
	}
}
