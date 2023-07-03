using UnityEngine;

public class MovementScript : MonoBehaviour
{
    public float speed = 5.0f;
    public int maxMoveDiff = 1;
    public float gravity = -9.8f;

    private Vector3 velocity;
    private CharacterController characterController;

    public World world;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = 0f;
        }

        if (move != Vector3.zero)
        {
            Vector3 horizontalMove = move.normalized * speed * Time.deltaTime;
            horizontalMove.y = 0; // Ignore vertical component for horizontal movement

            // Apply horizontal movement first
            characterController.Move(horizontalMove);

            // Calculate the height of the new position
            Vector3Int newVoxelPosition = Vector3Int.FloorToInt(transform.position);

            // Check if the coordinates are within bounds
            if (WorldGeneration.IsWithinBounds(newVoxelPosition.x, newVoxelPosition.y, newVoxelPosition.z))
            {
                int newVoxelHeight = WorldGeneration.terrainHeights[newVoxelPosition.x, newVoxelPosition.z];

                // Check the height difference in terms of voxels
                int heightDifference = newVoxelHeight - Mathf.FloorToInt(transform.position.y / Voxel.size);
                Debug.Log($"Height difference in voxels: {heightDifference}");

                // Apply vertical movement based on height difference
                if (Mathf.Abs(heightDifference) <= maxMoveDiff)
                {
                    Vector3 verticalMove = new Vector3(0, heightDifference * Voxel.size, 0);
                    characterController.Move(verticalMove);
                }
            }
        }

        // Handle gravity
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
}
