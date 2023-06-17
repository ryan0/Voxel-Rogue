using System.Collections.Generic;
using UnityEngine;

public class GasFlowSystem
{
    private HashSet<Chunk> activeChunks;
    private HashSet<Voxel> staticVoxels;
    private const int MAX_GAS_HEIGHT = 80;

    public GasFlowSystem()
    {
        activeChunks = new HashSet<Chunk>();
        staticVoxels = new HashSet<Voxel>();
    }

    private bool HasGasNeighbor(Voxel[] adjacentVoxels, int fluidId)
    {
        for (int i = 0; i < adjacentVoxels.Length; i++)
        {
            Voxel v = adjacentVoxels[i];
            if (v != null && v.substance.id == fluidId && v.substance.state == State.SOLID)
            {
                return true;
            }
        }

        return false;
    }

    public void UpdateGasFlow(List<Chunk> activeChunks, int updateSize = Chunk.depth / 8)
    {
        Chunk[] activeChunksArray = activeChunks.ToArray();
        if (updateSize >= activeChunksArray.Length)
        {
            updateSize = activeChunksArray.Length;
        }

        int startIndex = new System.Random().Next(activeChunksArray.Length);

        for (int i = 0; i < updateSize; i++)
        {
            int chunkIndex = (startIndex + i) % activeChunksArray.Length;
            Chunk chunk = activeChunksArray[chunkIndex];
            Voxel[,,] voxels = chunk.getVoxels();
            bool signalMeshRegen = false;

            for (int y = Chunk.height - 1; y >= 0; y--)
            {
                for (int z = 0; z < Chunk.depth; z++)
                {
                    for (int x = 0; x < Chunk.width; x++)
                    {
                        Voxel voxel = voxels[x, y, z];
                        if (staticVoxels.Contains(voxel))
                        {
                            continue;
                        }

                        if (voxel.substance.id == Substance.smoke.id || voxel.substance.id == Substance.steam.id)
                        {
                            Voxel[] adjacentVoxels = chunk.GetVoxelsAdjacentTo(x, y, z);
                            if (HasGasNeighbor(adjacentVoxels, voxel.substance.id))
                            {
                                Flow(voxel, chunk, voxels, x, y, z, voxel.substance);
                                signalMeshRegen = true;
                            }
                        }
                    }
                }
            }

            if (signalMeshRegen)
            {
                chunk.SignalMeshRegen();
            }
        }
    }

    public bool Flow(Voxel voxel, Chunk chunk, Voxel[,,] voxels, int x, int y, int z, Substance gasType)
    {
        System.Random rng = new System.Random(123);
        voxel.framesSinceLastChange++;
        if (voxel.framesSinceLastChange > 5)
        {
            staticVoxels.Add(voxel);
            return false;
        }

        bool signalMeshRegen = false;
        Voxel[] adjacentVoxels = chunk.GetVoxelsAdjacentTo(x, y, z);

        if (voxel.motes > 1)
        {
            List<Voxel> gasVoxels = new List<Voxel>();
            for (int i = 0; i < adjacentVoxels.Length; i++)
            {
                Voxel v = adjacentVoxels[i];
                if (v != null)
                {
                    if (v.substance.id == gasType.id && v.motes < voxel.motes)
                    {
                        gasVoxels.Add(v);
                    }
                }
            }

            if (gasVoxels.Count > 0)
            {
                gasVoxels.Sort((v1, v2) => v1.motes.CompareTo(v2.motes));
                int motesToDistribute = voxel.motes / 2;
                voxel.motes -= motesToDistribute;

                foreach (Voxel v in gasVoxels)
                {
                    int transferMotes = Mathf.Min(motesToDistribute, MAX_GAS_HEIGHT - v.motes);
                    v.motes += transferMotes;
                    motesToDistribute -= transferMotes;

                    if (motesToDistribute <= 0)
                    {
                        break;
                    }
                }

                if (motesToDistribute > 0)
                {
                    voxel.motes += motesToDistribute;
                }

                signalMeshRegen = true;
            }
        }

        if (voxel.motes <= 0)
        {
            voxel.substance = Substance.air;
            signalMeshRegen = true;
        }

        return signalMeshRegen;
    }
}
