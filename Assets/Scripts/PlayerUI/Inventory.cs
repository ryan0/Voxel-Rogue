using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Item
{
    public string name;
    public string description;
    public Substance substance;
    public int amount;

    public Item(string name, string description, Substance substance, int amount)
    {
        this.name = name;
        this.description = description;
        this.substance = substance;
        this.amount = amount;
    }
}


public class Inventory : MonoBehaviour
{
    public List<Item> items;
}
