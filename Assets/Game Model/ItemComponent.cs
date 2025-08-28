using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemComponent : MonoBehaviour
{
	public Item RequiredItem;

    private Material myMat;

	private void Awake()
	{
		myMat = GetComponent<Renderer>().material;
	}

	public void ShowAsBlueprint(bool showBp)
    {
		GetComponent<Renderer>().material = showBp ? DataLookup.I.BlueprintGridMat : myMat;
    }
}
