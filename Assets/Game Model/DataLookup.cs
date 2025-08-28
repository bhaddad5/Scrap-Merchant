using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataLookup : MonoBehaviour
{
	public static DataLookup I;

	public Material BlueprintGridMat;

	public List<Item> AllItems;	

	private void Awake()
	{
		I = this;
	}
}
