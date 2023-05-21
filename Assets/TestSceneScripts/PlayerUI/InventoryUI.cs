using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryUI : MonoBehaviour
{
    public Inventory inventory;
    public GameObject itemUIPrefab; // A prefab for UI item. It should be a Text or TextMeshPro element.
    public GameObject itemUIContainer; // A container for item UI elements. Should be a RectTransform with Vertical Layout Group component.
    public GameObject menuContainer;

    private void Start()
    {
        UpdateUI();
        menuContainer.SetActive(false);

    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            menuContainer.SetActive(!menuContainer.activeSelf);
        }

    }

    public void UpdateUI()
    {
        // Remove all current UI items
        foreach (Transform child in itemUIContainer.transform)
        {
            Destroy(child.gameObject);
        }

        // Create new UI items for each item in the inventory
        foreach (Item item in inventory.items)
        {
            GameObject itemUIObject = Instantiate(itemUIPrefab, itemUIContainer.transform);
            itemUIObject.GetComponent<Text>().text = item.name;
            itemUIObject.AddComponent<EventTrigger>();

            // Add mouse event
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((eventData) => { OnItemClick(item); });
            itemUIObject.GetComponent<EventTrigger>().triggers.Add(entry);
        }
    }

    private void OnItemClick(Item item)
    {
        // Do something when an item is clicked. For example, print item description.
        Debug.Log(item.description);
    }
}
