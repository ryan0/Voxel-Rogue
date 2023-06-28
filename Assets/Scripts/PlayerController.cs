using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5.0f;
    public float mouseSensitivity = 2.0f;

    private float verticalLookRotation;
    private Vector3 moveDirection;

    private CharacterController characterController;
    private Transform cameraTransform;
    private float rayDistance = 100f;

    [SerializeField]
    private World world;
    [SerializeField]
    private InventoryUI inventoryUI;


    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        cameraTransform = Camera.main.transform;
        Cursor.visible = false;
    }

    private void Update()
    {
        Look();
        Move();
        if (Input.GetMouseButtonDown(0))
        {
            BreakVoxel();
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            PickUpDebris();
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            SpawnWaterVoxel(Substance.water);///debug water method
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            HighlightAdjacentVoxels();
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            SetOnFire();
        }
    }

    private void SetOnFire()
    {
        if (RayCastToVoxel(out Vector3Int hitCoord))
        {
            world.setFireVoxel(hitCoord);
        }
    }

    private void HighlightAdjacentVoxels()
    {
        if (RayCastToVoxel(out Vector3Int hitCoord))
        {
            world.HighlightAdjVoxel(hitCoord);
        }
    }

   
    private void SpawnWaterVoxel(Substance voxelType)
    {
        if (RayCastToVoxel(out Vector3Int hitCoord))
        {
            world.spawnVoxelAt(hitCoord, voxelType, 5);
        }
    }

    private void Move()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Flight");
        float moveZ = Input.GetAxisRaw("Vertical");
        

        Vector3 moveHorizontal = transform.right * moveX;
        Vector3 moveVertical = transform.up * moveY;
        Vector3 moveForward = transform.forward * moveZ;

        moveDirection = (moveHorizontal + moveVertical + moveForward).normalized * speed;

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        verticalLookRotation += mouseY;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(-verticalLookRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void BreakVoxel()
    {
        if (RayCastToVoxel(out Vector3Int hitCoord))
        {
            world.destroyVoxelAt(hitCoord);
        }
    }

    private void PickUpDebris()
    {
        Camera camera = Camera.main;
        Ray ray = new (camera.transform.position, camera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            PickupAble debris = hit.transform.GetComponent<PickupAble>();
            if (debris != null)
            {
                Item existingItem = inventoryUI.inventory.items.Find(item => item.substance == debris.substance);
                if (existingItem != null)
                {
                    existingItem.amount += debris.amount;
                }
                else
                {
                    inventoryUI.inventory.items.Add(new Item(debris.itemName, debris.itemDescription, debris.substance, debris.amount));
                }
                Destroy(debris.gameObject);
            }
        }
    }


    private bool RayCastToVoxel(out Vector3Int hitCoordinatesRef)
    {
        Camera camera = Camera.main;
        hitCoordinatesRef = new Vector3Int();

        Ray ray = new(camera.transform.position, camera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance))
        {
            Vector3 point = hit.point * (1 / Voxel.size);
            Vector3 normal = hit.normal;

            Debug.Log("Raycast hit: " + point.x + ", " + point.y + ", " + point.z);
            Debug.Log("Raycast Normal: " + normal.x + ", " + normal.y + ", " + normal.z);

            hitCoordinatesRef.x = (int)point.x;
            hitCoordinatesRef.y = (int)point.y;
            hitCoordinatesRef.z = (int)point.z;

            if (normal.x == 1) hitCoordinatesRef.x -= 1;
            if (normal.y == 1) hitCoordinatesRef.y -= 1;
            if (normal.z == 1) hitCoordinatesRef.z -= 1;

            Debug.DrawLine(ray.origin, hit.point, Color.red); // Draw a red line to the point of collision
            return true;
        }
        else
        {
            Debug.DrawLine(ray.origin, ray.origin + ray.direction * rayDistance, Color.green); // Draw a green line to the maximum distance of the ray
            return false;
        }
    }

}
