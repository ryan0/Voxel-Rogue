using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxel
{
    public enum Type {
        gas,
        solid,
        liquid
    }

    public static string typeAsString(Type type)
    {
        if (type == Type.gas) return "Gas";
        if (type == Type.liquid) return "Liquid";
        if (type == Type.solid) return "Solid";

        return "You done goofed";
    }


    public Vector3 position;
    public Type type;

    public Voxel(Type type_, Vector3 position_)
    {
        type = type_;
        position = position_;
    }
}
