using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxel
{
    public Substance substance = Substance.air;
    public int Temperature = 60;
    public const float size = .5f;
    

    public Voxel(Substance substance_)
    {
        substance = substance_;
        
    }

    public bool changeState()
    {
        bool stateChanged = false;


        if (substance.name == "water" || substance.name == "steam" || substance.name == "ice")
        {
            Debug.Log(substance.name);
            Debug.Log(substance.id);
            Debug.Log(Temperature);
        }

        if (Temperature >= 100)
        {
            if (substance == Substance.water)
            {
                substance = Substance.steam;
                stateChanged = true;
            }
            if (substance == Substance.wood || substance == Substance.oil)
            {
                substance = Substance.smoke;
                stateChanged = true;
            }
            if (substance == Substance.stone)
            {
                substance = Substance.lava;
                stateChanged = true;
            }
        }
        else if (Temperature <= 0)
        {
            if (substance == Substance.water)
            {
                substance = Substance.ice;
                stateChanged = true;
            }
        }
        else
        {
            if (substance == Substance.lava)
            {
                substance = Substance.stone;
                stateChanged = true;
            }
            if (substance == Substance.ice || substance == Substance.steam)
            {
                substance = Substance.water;
                stateChanged = true;
            }
        }

        return stateChanged;
    }

}

