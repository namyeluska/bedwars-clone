using OpenTK.Mathematics;
using System.Collections.Generic;

public class World
{
    private readonly Dictionary<Vector2i, Chunk> _chunks = new Dictionary<Vector2i, Chunk>();
    
    public World()
    {
        // Initialize a 5x5 area of chunks centered on 0,0
        for (int x = -2; x <= 2; x++)
        {
            for (int z = -2; z <= 2; z++)
            {
                CreateChunk(new Vector2i(x, z));
            }
        }
    }
    
    private void CreateChunk(Vector2i pos)
    {
        Chunk chunk = new Chunk(pos);
        // Basic terrain gen
        for (int x = 0; x < Chunk.SizeX; x++)
        {
            for (int z = 0; z < Chunk.SizeZ; z++)
            {
                // World coordinates
                int wx = pos.X * Chunk.SizeX + x;
                int wz = pos.Y * Chunk.SizeZ + z;
                
                // Flat platform at Y=10 only in center? 
                // Let's make a floor everywhere at Y=0 (Void is below)
                // Bedwars style: Islands.
                
                // Center Island (Chunk 0,0)
                if (pos.X == 0 && pos.Y == 0)
                {
                    if (x >= 3 && x < 13 && z >= 3 && z < 13)
                    {
                        chunk.SetBlock(x, 10, z, new Block(BlockType.GrassBlock));
                    }
                }
                
                // Maybe some scattered islands?
            }
        }
        _chunks[pos] = chunk;
    }

    public Block GetBlock(int x, int y, int z)
    {
        if (y < 0 || y >= Chunk.SizeY) return new Block(BlockType.Air);
        
        Vector2i chunkPos = GetChunkPos(x, z);
        if (_chunks.TryGetValue(chunkPos, out Chunk? chunk))
        {
            int bx = x - chunkPos.X * Chunk.SizeX;
            int bz = z - chunkPos.Y * Chunk.SizeZ;
            return chunk.GetBlock(bx, y, bz); // Chunk handles bounds check internally
        }
        return new Block(BlockType.Air);
    }

    public void SetBlock(int x, int y, int z, Block block)
    {
        if (y < 0 || y >= Chunk.SizeY) return;

        Vector2i chunkPos = GetChunkPos(x, z);
        if (!_chunks.ContainsKey(chunkPos))
        {
            // Dynamically create chunk if we place here?
            CreateChunk(chunkPos);
        }
        
        if (_chunks.TryGetValue(chunkPos, out Chunk? chunk))
        {
            int bx = x - chunkPos.X * Chunk.SizeX;
            int bz = z - chunkPos.Y * Chunk.SizeZ;
            chunk.SetBlock(bx, y, bz, block);
        }
    }

    private Vector2i GetChunkPos(int x, int z)
    {
        int cx = (int)Math.Floor((float)x / Chunk.SizeX);
        int cz = (int)Math.Floor((float)z / Chunk.SizeZ);
        return new Vector2i(cx, cz);
    }
    
    // Raycast across world (naive: step and check blocks)
    public (bool, Vector3i, Vector3i) Raycast(Vector3 origin, Vector3 direction, float maxDistance)
    {
        float step = 0.05f;
        Vector3 pos = origin;
        Vector3 lastPos = origin;
        
        for (float d = 0; d < maxDistance; d += step)
        {
            Vector3i bPos = new Vector3i((int)Math.Floor(pos.X), (int)Math.Floor(pos.Y), (int)Math.Floor(pos.Z));
            Block b = GetBlock(bPos.X, bPos.Y, bPos.Z);
            
            if (b.IsSolid)
            {
                Vector3i lastBPos = new Vector3i((int)Math.Floor(lastPos.X), (int)Math.Floor(lastPos.Y), (int)Math.Floor(lastPos.Z));
                Vector3i normal = lastBPos - bPos;
                if (normal.ManhattanLength != 1) normal = Vector3i.UnitY;
                return (true, bPos, normal);
            }
            lastPos = pos;
            pos += direction * step;
        }
        return (false, Vector3i.Zero, Vector3i.Zero);
    }

    public Dictionary<Vector2i, Chunk> GetChunks() => _chunks;
}
