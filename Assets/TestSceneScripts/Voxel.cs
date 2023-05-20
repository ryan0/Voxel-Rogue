using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxel
{
    public Substance substance = Substance.air;
    public int Temperature = 60;
    

    public Voxel(Substance substance_)
    {
        substance = substance_;
        
    }

    void changeState()
    {
        if (Temperature >= 100)
        {
            if (substance == Substance.water)
            {
                substance = Substance.steam;
            }
            if (substance == Substance.wood || substance == Substance.oil)
            {
                substance = Substance.smoke;
            }
            if (substance == Substance.stone)
            {
                substance = Substance.lava;
            }
        }
        if (Temperature <= 0)
        {
            if (substance == Substance.water)
            {
                substance = Substance.ice;
            }
        }
        else
        {
            if (substance == Substance.lava)
            {
                substance = Substance.stone;
            }
            if (substance == Substance.ice || substance == Substance.steam)
            {
                substance = Substance.water;
            }
        }
    }

}

