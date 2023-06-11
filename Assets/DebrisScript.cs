using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebrisScript : MonoBehaviour
{
    public float initialForce = 1f;
    public float windStrength = 1f;
    public float noiseScale = 0.1f;
    public bool isLeaf = false;
    //public bool isLeaf = false;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (isLeaf)
        {

        }
        else
        {
            // Assuming the debris has a Rigidbody component, you can add some force to make the debris scatter
            if (rb != null)
            {
                Vector3 randomForce = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f),
                    Random.Range(-1f, 1f)
                ).normalized * initialForce; // You may need to adjust the force value

                rb.AddForce(randomForce, ForceMode.Impulse);
            }
        }
    }



    private void FixedUpdate()
    {
        if (isLeaf)
        {
            float noise = Mathf.PerlinNoise(Time.time * noiseScale, 0f);
            Vector3 windDirection = new Vector3(noise - 0.5f, noise - 0.5f, noise - 0.5f); // 0.5f added to Y component to counteract gravity
            rb.AddForce(windDirection * windStrength);
        }

    }
}
