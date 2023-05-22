using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    public Inventory inventory;
    public GameObject itemUIPrefab; // A prefab for UI item. It should be a Text or TextMeshPro element.
    public GameObject itemUIContainer; // A container for item UI elements. Should be a RectTransform with Vertical Layout Group component.
    public GameObject menuContainer;
    public Text descriptionText; // New variable, assign the Text component of your new GameObject to it.
    public ScrollRect scrollRect;

    private int selectedItemIndex = -1;

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
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            SelectPreviousItem();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            SelectNextItem();
        }
        if (selectedItemIndex >= 0)
        {
            scrollRect.verticalNormalizedPosition = 1 - ((float)selectedItemIndex / (inventory.items.Count - 1));
        }

    }

    private void SelectPreviousItem()
    {
        selectedItemIndex = Mathf.Max(selectedItemIndex - 1, 0);
        UpdateUI();
    }

    private void SelectNextItem()
    {
        selectedItemIndex = Mathf.Min(selectedItemIndex + 1, inventory.items.Count - 1);
        UpdateUI();
    }

    public void UpdateUI()
    {
        // Remove all current UI items
        foreach (Transform child in itemUIContainer.transform)
        {
            Destroy(child.gameObject);
        }

        // Create new UI items for each item in the inventory
        for (int i = 0; i < inventory.items.Count; i++)
        {
            Item item = inventory.items[i];
            GameObject itemUIObject = Instantiate(itemUIPrefab, itemUIContainer.transform);
            Text textComponent = itemUIObject.GetComponent<Text>();
            textComponent.text = item.name;

            // If this is the selected item, highlight it
            if (i == selectedItemIndex)
            {
                textComponent.color = Color.yellow; // Set the color to yellow
                descriptionText.text = item.description; // Display the description of the selected item.
            }
            else
            {
                textComponent.color = Color.white; // Set the color to white
            }

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
