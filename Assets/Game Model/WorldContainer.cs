// WorldContainer.cs
using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class WorldContainer : MonoBehaviour
{
	public string displayName = "Container";
	public Inventory Inventory => GetComponent<Inventory>();
	public Transform PoV;
}
