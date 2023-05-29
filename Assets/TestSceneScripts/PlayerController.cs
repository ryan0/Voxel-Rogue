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
        if (Input.GetMouseButtonDown(0)) {
            rayCast();

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

    private void rayCast()
    {
        Camera camera = Camera.main;

        Ray ray = new Ray(camera.transform.position, camera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            Vector3 point = hit.point * (1 / Voxel.size);

            int x = (int)point.x;
            int y = (int)point.y;
            int z = (int)point.z;

            //Debug.Log("hit voxel: " + x + ", " + y + ", " + z);

            world.destroyVoxelAt(x, y, z);


            Debug.DrawLine(ray.origin, hit.point, Color.red); // Draw a red line to the point of collision
        }
        else
        {
            Debug.DrawLine(ray.origin, ray.origin + ray.direction * rayDistance, Color.green); // Draw a green line to the maximum distance of the ray
        }
    }

}
