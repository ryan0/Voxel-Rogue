using System.Collections.Generic;
using UnityEngine;

public class FluidFlowSystem
{
    //private HashSet<Chunk> activeChunksH;
    private HashSet<Voxel> staticVoxels;
    System.Random rng;
    int numVoxels;
    int updateSize;
    List<Voxel> waterVoxels = new List<Voxel>();
    List<Voxel> waterVoxelsAnySize = new List<Voxel>();
    private Dictionary<Chunk, int> chunkUpdateIndices = new Dictionary<Chunk, int>();//cycling index update for optimization


    private class FluidFlowInteraction
    {

        public readonly int triggerSubstanceId;
        public readonly int createdSubstanceId;

        public FluidFlowInteraction(int triggerSubstanceId, int createdSubstanceId)
        {
            this.triggerSubstanceId = triggerSubstanceId;
            this.createdSubstanceId = createdSubstanceId;
        }
    }

    private List<FluidFlowInteraction>[] flowInteractions = new List<FluidFlowInteraction>[Substance.NumberSubstances()];

    public FluidFlowSystem()
    {
        //activeChunksH = new HashSet<Chunk>();
        staticVoxels = new HashSet<Voxel>();
        rng = new System.Random(123);
        numVoxels = (int)(Mathf.Pow(Chunk.depth, 3));
        updateSize = numVoxels / 8;//update size is optimization

        //HYBRID LOGIC
        // Configure fluid flow interactions
        flowInteractions[Substance.water.id] = new List<FluidFlowInteraction>
        {
            new FluidFlowInteraction(Substance.air.id, Substance.water.id)
        };
        // Add more interactions if required
    }

    // Check if there is any non-fluid neighbor (e.g., air, different fluid)
    private bool HasGasNeighbor(Voxel[] adjacentVoxels, int fluidId)
    {
        for (int i = 0; i < adjacentVoxels.Length; i++)
        {
            Voxel v = adjacentVoxels[i];
            if (v != null && v.substance.id == Substance.air.id && v!=adjacentVoxels[1])//NOT THE TOP NEIGHBOR
            {
                return true;
            }
        }

        return false;
    }

    public void UpdateFluidFlow(HashSet<Chunk> activeChunksH)
    {

        // Convert activeChunks to an array for efficient indexing
        Chunk[] activeChunksArray = new Chunk[activeChunksH.Count];
        activeChunksH.CopyTo(activeChunksArray);

        // Choose a random start index to ensure the update chunks are distributed randomly
        //int startIndex = rng.Next(activeChunksArray.Length);

        for (int i = 0; i < activeChunksArray.Length; i++)
        {
            Chunk chunk = activeChunksArray[i];
            Voxel[,,] voxels = chunk.getVoxels();
            bool signalMeshRegen = false;
            chunkUpdateIndices.TryGetValue(chunk, out int yIndex);

            // If updateSize is greater than the number of chunk voxels
            if (updateSize >= voxels.Length)
            {
                updateSize = voxels.Length;
            }

            for (int u = 0; u < updateSize; u++)//update updateSize random voxels within chunk
            {
                // Select random voxel within the chunk
                int x = rng.Next(0, Chunk.width);
                int y = rng.Next(0, Chunk.height);
                int z = rng.Next(0, Chunk.depth);
                Voxel voxel = voxels[x, yIndex, z];
                // Check if voxel is static
                if (staticVoxels.Contains(voxel))
                {
                    continue;
                }
                if (voxel.substance.id == Substance.water.id || voxel.substance.id == Substance.lava.id)
                {
                    Voxel[] adjacentVoxels = chunk.GetVoxelsAdjacentTo(x, yIndex, z);
                    // Only flow if not surrounded by the same fluid
                    if (HasGasNeighbor(adjacentVoxels, voxel.substance.id))
                    {
                        Flow(voxel, chunk, voxels, x, yIndex, z, voxel.substance);
                        signalMeshRegen = true;
                    }
                }
            }

            if (signalMeshRegen)
            {
                chunk.SignalMeshRegen();
            }
            // Increment the index for the next cycle, or reset it if we've reached the top
            yIndex = (yIndex + 1) % Chunk.height;
            chunkUpdateIndices[chunk] = yIndex;
        }
    }


    public bool Flow(Voxel voxel, Chunk chunk, Voxel[,,] voxels, int x, int y, int z, Substance fluidType)
    {
        //System.Random rng = new System.Random(123);
        voxel.framesSinceLastChange++;
        if (voxel.framesSinceLastChange > 5)//SOME_THRESHOLD for optimization
        {
            staticVoxels.Add(voxel);
            return false;
        }

        bool signalMeshRegen = false;
        Voxel[] adjacentVoxels = chunk.GetVoxelsAdjacentTo(x, y, z);
        //get global Voxel coords
        int globalX = voxels[x, y, z].globalX;
        int globalY = voxels[x, y, z].globalY;
        int globalZ = voxels[x, y, z].globalZ;


        // Manually filter adjacent voxels
        List<Voxel> filteredAdjacentVoxels = new List<Voxel>();
        for (int i = 0; i < adjacentVoxels.Length; i++)
        {
            Voxel v = adjacentVoxels[i];
            if (v != null)
            {
                if (v.substance.id == fluidType.id || v.substance.id == Substance.air.id)
                {
                    filteredAdjacentVoxels.Add(v);
                }
            }

        }
        // Drain functionality for when there's only one mote of water left
        if (voxel.motes == 1)
        {
            //List<Voxel> waterVoxelsAnySize = new List<Voxel>();
            // = filteredAdjacentVoxels.FindAll(v => v.substance.id == fluidType.id);
            waterVoxelsAnySize.Clear();  // Clear the list before using
            for (int i = 0; i < filteredAdjacentVoxels.Count; i++)
            {
                Voxel v = filteredAdjacentVoxels[i];
                if (v.substance.id == fluidType.id)
                {
                    waterVoxelsAnySize.Add(v);
                }
            }

            if (waterVoxelsAnySize.Count == 0)//when there is NO neighboring water of any mote quantity
            {
                Voxel voxelBelow;
                if (y > 0)
                {
                    voxelBelow = voxels[x, y - 1, z];
                }
                else
                {
                    voxelBelow = adjacentVoxels[4];//voxel.chunk.bottomNeighbour.getVoxels()[x, voxel.chunk.heightPub - 1, z];//adjacentVoxels.Find(v => v.chunk.yIndex < chunk.yIndex); // Neighbor below has a lower y-coordinate
                }
                // If there is air directly below
                if (voxelBelow != null && voxelBelow.substance.id == Substance.air.id)
                {
                    voxelBelow.substance = fluidType;
                    voxelBelow.motes = 1;
                    voxel.substance = Substance.air;
                    voxel.motes = 0;
                    signalMeshRegen = true;
                    voxel.framesSinceLastChange = 0;
                    staticVoxels.Remove(voxel);
                }
            }
            else
            {

                // Look for adjacent water voxels to draw from that have MORE than 1 mote
                //List<Voxel> waterVoxels = new List<Voxel>();//
                // = filteredAdjacentVoxels.FindAll(v => v.substance.id == fluidType.id && v.motes > 1);
                waterVoxels.Clear();  // Clear the list before using
                for (int i = 0; i < filteredAdjacentVoxels.Count; i++)
                {
                    Voxel v = filteredAdjacentVoxels[i];
                    if (v.substance.id == fluidType.id && v.motes > 1)
                    {
                        waterVoxels.Add(v);
                    }
                }


                if (waterVoxels.Count > 0)
                {
                    // Draw from a random water voxel
                    Voxel waterVoxel = waterVoxels[rng.Next(waterVoxels.Count)];
                    waterVoxel.motes--;
                    voxel.motes++;
                    signalMeshRegen = false;

                }
                else
                {
                    // If there are no water voxels to draw from, move into the empty space
                    // Find adjacent air voxels that are not above the current voxel
                    //List<Voxel> airVoxels = filteredAdjacentVoxels.FindAll(v => v.substance.id == Substance.air.id && v.y <= y);
                    Voxel[] airVoxels;
                    int count = 0;
                    foreach (Voxel adjV in filteredAdjacentVoxels)
                    {
                        if (adjV.substance.id == Substance.air.id && adjV.globalY <= globalY)
                        {
                            count++;
                        }
                    }
                    airVoxels = new Voxel[count];
                    count = 0;
                    foreach (Voxel adjV in filteredAdjacentVoxels)
                    {
                        if (adjV.substance.id == Substance.air.id && adjV.globalY <= globalY)
                        {
                            airVoxels[count] = adjV;
                            count++;
                        }
                    }
                    if (count > 0)
                    {
                        // Choose a random air voxel and move into it
                        Voxel airVoxel = airVoxels[rng.Next(count)];
                        airVoxel.substance = fluidType;
                        airVoxel.motes = 1;

                        // Leave air behind
                        voxel.substance = Substance.air;
                        voxel.motes = 0;
                        signalMeshRegen = true;
                        voxel.framesSinceLastChange = 0;
                        staticVoxels.Remove(voxel);

                    }


                }

            }
        }
        else if (voxel.motes > 1) // Original fluid flow for water with more than one mote
        {
            // Remove voxels that are above the current voxel
            //filteredAdjacentVoxels.RemoveAll(v => v.y > y);
            Voxel[] airVoxels;
            int count = 0;
            foreach (Voxel adjV in filteredAdjacentVoxels)
            {
                if (adjV.substance.id == Substance.air.id && adjV.globalY <= globalY)
                {
                    count++;
                }
            }
            airVoxels = new Voxel[count];
            count = 0;
            foreach (Voxel adjV in filteredAdjacentVoxels)
            {
                if (adjV.substance.id == Substance.air.id && adjV.globalY <= globalY)
                {
                    airVoxels[count] = adjV;
                    count++;
                }
            }
            if (count > 0)//if there is any valid air voxel
            {
                Voxel targetVoxel;

                // Check if the voxel below is eligible
                Voxel voxelBelow;
                if (y > 0)
                {
                    voxelBelow = voxels[x, y - 1, z];
                }
                else//bottom chunk neighbor
                {
                    voxelBelow = adjacentVoxels[4];//voxel.chunk.bottomNeighbour.getVoxels()[x, voxel.chunk.heightPub - 1, z];//adjacentVoxels.Find(v => v.chunk.yIndex < chunk.yIndex); // Neighbor below has a lower y-coordinate
                }
                if (voxelBelow != null && (voxelBelow.substance.id == Substance.air.id || (voxelBelow.substance.id == fluidType.id)))// && voxelBelow.motes < voxel.motes))
                {
                    targetVoxel = voxelBelow;
                }
                else
                {
                    // If the voxel below is not eligible,  select an adjacent voxel to receive the mote
                    targetVoxel = airVoxels[rng.Next(count)];
                }

                voxel.motes--;

                if (targetVoxel != null)
                {
                    //targetVoxel.motes++;

                    // If the target voxel was air, change it to water and set motes to 1
                    if (targetVoxel.substance.id == Substance.air.id)
                    {
                        targetVoxel.substance = fluidType;
                        targetVoxel.motes = 1;
                    }
                    else if (targetVoxel.substance.id == fluidType.id)
                    {
                        targetVoxel.motes++;

                    }
                    signalMeshRegen = true;
                    voxel.framesSinceLastChange = 0;
                    staticVoxels.Remove(voxel);

                }
            }
        }
        return signalMeshRegen;
    }


    //private Substance Hybridize(Substance a, Substance b)
    //{
    // Implement your fluid hybridization logic here
    // For simplicity, we will just return a new Substance
    //return new Substance(/*Your fluid hybridization parameters here*/);
    //}
}

