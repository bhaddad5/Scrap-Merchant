using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RecipeSelectionUi : MonoBehaviour
{
	public Item Recipe;

	[SerializeField] Image icon;
	[SerializeField] TMPro.TextMeshProUGUI countText;

	public void Setup(Item recipe)
	{
		Recipe = recipe;
	}
}
