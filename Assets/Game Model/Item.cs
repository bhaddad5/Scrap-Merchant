using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Game/Item")]
public class Item : ScriptableObject
{
    public string DisplayName;
    public Sprite Icon;
    public GameObject Prefab;
    [Min(1)] public int MaxStack;
}
