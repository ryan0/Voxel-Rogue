using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Item
{
    public string name;
    public string description;

    public Item(string name, string description)
    {
        this.name = name;
        this.description = description;
    }
}

public class Inventory : MonoBehaviour
{
    public List<Item> items;
}
