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
        world = FindObjectOfType<World>();
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        int npcHeight = 2; // Example value, set to the height of your character in voxels
        
        // Take into account the camera direction
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        cameraForward.y = 0; // Keep only the horizontal component
        cameraRight.y = 0; // Keep only the horizontal component

        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 move = cameraRight * moveX + cameraForward * moveZ;
        move = move * Voxel.size; // Normalize the move vector to have a length of 1 voxel.size

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

            if (characterController.isGrounded)
            {

                // Calculate the height of the new position
                Vector3Int currentVoxelPos = World.WorldCoordToVoxelCoord(transform.position);
                //Debug.Log("move " + move);
                Vector3 newPos = transform.position + move;
                Vector3Int newVoxelPosition = World.WorldCoordToVoxelCoord(newPos);//new Vector3Int(currentPos.x + (int)move.x, currentPos.y, currentPos.z + (int)move.z);
                Debug.Log("current " + currentVoxelPos.x + " ," + currentVoxelPos.y + " ," + currentVoxelPos.z + " ," + "nextXZ" + newVoxelPosition.x + " ," + newVoxelPosition.y + " ," + newVoxelPosition.z);
                // Find the valid voxel within maxMoveDiff height
                Vector3Int targetVoxel = new Vector3Int();
                bool validVoxelFound = false;
                for (int i = 1; i <= maxMoveDiff; i++)
                {
                    Vector3Int checkVoxel = new Vector3Int(newVoxelPosition.x, newVoxelPosition.y + i, newVoxelPosition.z);
                    Debug.Log(i + " check voxel " + checkVoxel.y);
                    if (WorldGeneration.IsWithinBounds(checkVoxel.x, checkVoxel.y, checkVoxel.z))
                    {
                        bool hasEnoughAirAbove = true;
                        for (int j = 1; j <= npcHeight; j++)
                        {
                            Vector3Int aboveVoxel = new Vector3Int(checkVoxel.x, checkVoxel.y + j, checkVoxel.z);
                            Debug.Log(world.GetVoxelType(aboveVoxel).name);
                            if (world.GetVoxelType(aboveVoxel).state == State.SOLID)
                            {
                                hasEnoughAirAbove = false;
                                break;
                            }
                        }

                        if (hasEnoughAirAbove)
                        {
                            targetVoxel = checkVoxel;
                            validVoxelFound = true;
                            // Check the height difference in terms of voxels
                            int heightDifference = targetVoxel.y - currentVoxelPos.y;
                            Debug.Log($"Height difference in voxels: {heightDifference}");
                            break;
                        }
                    }
                }
                // If valid voxel is found, move the character to the target voxel
                if (validVoxelFound)
                {
                    Vector3 verticalMove = new Vector3(0, (targetVoxel.y - newVoxelPosition.y) * Voxel.size, 0);
                    characterController.Move(verticalMove);
                }
            }
        }

        // Handle gravity
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

}
