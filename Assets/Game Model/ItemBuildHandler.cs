using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemBuildHandler : MonoBehaviour
{
    public Item MyItemType { get; set; }

    public void CheckBuildComplete()
    {
        foreach(var comp in GetComponentsInChildren<ItemComponent>())
        {
            if (!comp.Ready)
                return;
        }

        if(gameObject.GetComponent<PickUp>() == null)
        {
			var pickupScript = gameObject.AddComponent<PickUp>();
			pickupScript.MyItemType = MyItemType;
			pickupScript.IsNewItem = true;
		}
	}
}
