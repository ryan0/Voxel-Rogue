using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PixelArtEffect : MonoBehaviour
{
    public Material pixelArtMaterial;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (pixelArtMaterial != null)
        {
            Graphics.Blit(src, dest, pixelArtMaterial);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }
}