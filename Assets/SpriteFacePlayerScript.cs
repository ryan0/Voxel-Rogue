using UnityEngine;

public class SpriteFacePlayerScript : MonoBehaviour
{
    private Transform player;

    private void Start()
    {
        // Assume the player object has the tag "Player"
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        FacePlayer();
    }

    void FacePlayer()
    {
        // Look at the player
        transform.LookAt(player);

        // Assuming the sprite is facing the up direction (in 2D view), rotate 90 degrees around X axis
        //transform.Rotate(new Vector3(90, 0, 0));

        // If your sprite faces the right direction (in 2D view), you won't need the rotation
    }
}
