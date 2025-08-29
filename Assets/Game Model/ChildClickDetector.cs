using UnityEngine;

[RequireComponent(typeof(Collider))]
[DisallowMultipleComponent]
public class ChildClickDetector : MonoBehaviour
{
	void OnMouseDown()
	{
		foreach(var parent in gameObject.GetComponentsInParent<IChildClickReciever>())
		{
			parent.RecvOnMouseDown();
		}
	}
}
