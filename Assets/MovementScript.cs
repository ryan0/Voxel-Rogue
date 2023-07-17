using UnityEngine;
using System.Collections.Generic;

public class MovementScript : MonoBehaviour
{
    public float speed = 5.0f;
    public int maxMoveDiff = 1;
    public float gravity = -9.8f;
    public int npcHeight = 2; // Example value, set to the height of your character in voxels

    private Vector3 velocity;
    private CharacterController characterController;

    private World world;

    private void Start()
    {
        world = FindObjectOfType<World>();
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {

        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = 0f;
        }

        //MoveCharacter content moved from here
        // Handle gravity
        if (!characterController.isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }

    }


    public List<Vector3Int> InvalidVoxels = new List<Vector3Int>();// debug

    public void MoveCharacter(Vector3 move)
    {
       
        if (move != Vector3.zero)
        {
            Vector3 horizontalMove = move.normalized * speed * Time.deltaTime;
            horizontalMove.y = 0; // Ignore vertical component for horizontal movement
            move.x = Mathf.Min(Voxel.size, move.x);//cap movement so it doesnt OVERSHOOT
            move.z = Mathf.Min(Voxel.size, move.z);//cap movement so it doesnt OVERSHOOT

            // Apply horizontal movement first
            characterController.Move(horizontalMove);

            if (characterController.isGrounded)
            {

                // Calculate the height of the new position
                Vector3Int currentVoxelPos = World.WorldCoordToVoxelCoord(transform.position);
                //Debug.Log("move " + move);
                Vector3 newPos = transform.position + move;
                Vector3Int newVoxelPosition = World.WorldCoordToVoxelCoord(newPos);//new Vector3Int(currentPos.x + (int)move.x, currentPos.y, currentPos.z + (int)move.z);
                //Debug.Log("current " + currentVoxelPos.x + " ," + currentVoxelPos.y + " ," + currentVoxelPos.z + " ," + "nextXZ" + newVoxelPosition.x + " ," + newVoxelPosition.y + " ," + newVoxelPosition.z);
                // Find the valid voxel within maxMoveDiff height
                Vector3Int targetVoxel = new Vector3Int();
                bool validVoxelFound = false;
                for (int i = 1; i <= maxMoveDiff; i++)
                {
                    Vector3Int checkVoxel = new Vector3Int(newVoxelPosition.x, newVoxelPosition.y + i, newVoxelPosition.z);
                    //if the voxel is sometihng you can stand on. i.e., solid
                    //Debug.Log(world.GetVoxelType(newVoxelPosition).state);
                    if (world.GetVoxelAt(checkVoxel + new Vector3Int(0,-1,0)).substance.state == State.SOLID)
                    {
                        //Debug.Log(i + " check voxel " + checkVoxel.y);
                        if (WorldGeneration.IsWithinBounds(checkVoxel.x, checkVoxel.y, checkVoxel.z))
                        {
                            bool hasEnoughAirAbove = true;
                            for (int j = 1; j <= npcHeight; j++)
                            {
                                Vector3Int aboveVoxel = new Vector3Int(checkVoxel.x, checkVoxel.y + j, checkVoxel.z);
                                //Debug.Log(world.GetVoxelType(aboveVoxel).name);
                                if (world.GetVoxelAt(aboveVoxel).substance.state == State.SOLID)
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
                                //Debug.Log($"Height difference in voxels: {heightDifference}");
                                break;
                            }
                        }
                    }
                }
                // If valid voxel is found, move the character to the target voxel
                if (validVoxelFound)
                {
                    //Vector3 verticalMove = new Vector3(0, (targetVoxel.y - newVoxelPosition.y) * Voxel.size, 0);
                    //characterController.Move(verticalMove);
                    float newY = (targetVoxel.y - newVoxelPosition.y) * Voxel.size + transform.position.y + .5f;//add .xxf to "jump" up to new pos;
                    //necessary TO AVOID GETTING STUCK WHEN YOU NEED TO TELEPORT UP AND OVER.  Right now you "move horizontally" and teleport up, but sometimes you get stuck and dont go horizointally
                    
                    transform.position = new Vector3(transform.position.x, newY, transform.position.z);
                    float outputY = newY + transform.position.y;
                    //Debug.Log("valid move " + move+  "from " + currentVoxelPos + " to " + newVoxelPosition.x + ", "+ outputY + ", " + newVoxelPosition.z);

                }
                else
                {
                    for (int j = 1; j <= npcHeight; j++)
                    {
                        Vector3Int checkVoxel = new Vector3Int(newVoxelPosition.x, newVoxelPosition.y, newVoxelPosition.z);
                        Vector3Int aboveVoxel = new Vector3Int(checkVoxel.x, checkVoxel.y + j, checkVoxel.z);
                        //Debug.Log(world.GetVoxelType(aboveVoxel).name + " , " + checkVoxel.y);

                        // Add the invalid voxel to the list
                        InvalidVoxels.Add(aboveVoxel);
                    }
                    //Debug.Log("invalid move " + currentVoxelPos + ", "+ newVoxelPosition);
                }
                // ...

            }

        }

    }



}
