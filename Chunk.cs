using OpenTK.Mathematics;
using System.Collections.Generic;

public class Chunk
{
    public const int SizeX = 16;
    public const int SizeY = 256;
    public const int SizeZ = 16;

    private readonly Block[,,] _blocks;
    public Vector2i Position { get; private set; } // Chunk coords (e.g. 0,0)

    public Chunk(Vector2i position)
    {
        Position = position;
        _blocks = new Block[SizeX, SizeY, SizeZ];
    }

    public Block GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= SizeX || y < 0 || y >= SizeY || z < 0 || z >= SizeZ)
            return new Block(BlockType.Air);
        return _blocks[x, y, z];
    }

    public void SetBlock(int x, int y, int z, Block block)
    {
        if (x >= 0 && x < SizeX && y >= 0 && y < SizeY && z >= 0 && z < SizeZ)
        {
            _blocks[x, y, z] = block;
        }
    }

    // Build mesh
    public void GenerateMesh(out float[] vertices, out int vertexCount)
    {
        List<float> verts = new List<float>();

        for (int x = 0; x < SizeX; x++)
        {
            for (int y = 0; y < SizeY; y++)
            {
                for (int z = 0; z < SizeZ; z++)
                {
                    Block block = _blocks[x, y, z];
                    if (!block.IsSolid) continue;

                    Vector3 pos = new Vector3(Position.X * SizeX + x, y, Position.Y * SizeZ + z);

                    // Add faces if neighbor is air
                    if (IsTransparent(x, y, z - 1)) AddFace(verts, pos, 0, block.GetTextureFace(0)); // Back
                    if (IsTransparent(x, y, z + 1)) AddFace(verts, pos, 1, block.GetTextureFace(1)); // Front
                    if (IsTransparent(x - 1, y, z)) AddFace(verts, pos, 2, block.GetTextureFace(2)); // Left
                    if (IsTransparent(x + 1, y, z)) AddFace(verts, pos, 3, block.GetTextureFace(3)); // Right
                    if (IsTransparent(x, y - 1, z)) AddFace(verts, pos, 4, block.GetTextureFace(4)); // Bottom
                    if (IsTransparent(x, y + 1, z)) AddFace(verts, pos, 5, block.GetTextureFace(5)); // Top
                }
            }
        }

        vertices = verts.ToArray();
        vertexCount = verts.Count / 6; // 6 floats per vertex? Pos(3) + Tex(2) + Layer(1) ?
    }

    private bool IsTransparent(int x, int y, int z)
    {
        // Simple check: out of bounds or air
        if (x < 0 || x >= SizeX || y < 0 || y >= SizeY || z < 0 || z >= SizeZ) return true;
        return !_blocks[x, y, z].IsSolid;
    }

    // Vertex Format: Pos.x, Pos.y, Pos.z, U, V, Layer
    private void AddFace(List<float> verts, Vector3 pos, int face, int textureLayer)
    {
        // Face definitions (Standard Cube)
        // 0: Back (0, 0, -1) -> Z=0 face? Local coords relative to block origin commonly defined as 0..1 or -0.5..0.5
        // Let's use 0..1 relative to pos.
        
        // Vertices for each face [0..1]
        // 0: Back (Z=0)
        if (face == 0) // Back (Neighbour Z-1) -> Face at Z=0
        {
            AddVertex(verts, pos.X + 1, pos.Y + 0, pos.Z + 0, 0, 0, textureLayer);
            AddVertex(verts, pos.X + 0, pos.Y + 0, pos.Z + 0, 1, 0, textureLayer);
            AddVertex(verts, pos.X + 0, pos.Y + 1, pos.Z + 0, 1, 1, textureLayer);
            AddVertex(verts, pos.X + 0, pos.Y + 1, pos.Z + 0, 1, 1, textureLayer);
            AddVertex(verts, pos.X + 1, pos.Y + 1, pos.Z + 0, 0, 1, textureLayer);
            AddVertex(verts, pos.X + 1, pos.Y + 0, pos.Z + 0, 0, 0, textureLayer);
        }
        else if (face == 1) // Front (Neighbour Z+1) -> Face at Z=1
        {
            AddVertex(verts, pos.X + 0, pos.Y + 0, pos.Z + 1, 0, 0, textureLayer);
            AddVertex(verts, pos.X + 1, pos.Y + 0, pos.Z + 1, 1, 0, textureLayer);
            AddVertex(verts, pos.X + 1, pos.Y + 1, pos.Z + 1, 1, 1, textureLayer);
            AddVertex(verts, pos.X + 1, pos.Y + 1, pos.Z + 1, 1, 1, textureLayer);
            AddVertex(verts, pos.X + 0, pos.Y + 1, pos.Z + 1, 0, 1, textureLayer);
            AddVertex(verts, pos.X + 0, pos.Y + 0, pos.Z + 1, 0, 0, textureLayer);
        }
        else if (face == 2) // Left (Neighbour X-1) -> Face at X=0
        {
            AddVertex(verts, pos.X + 0, pos.Y + 0, pos.Z + 0, 0, 0, textureLayer);
            AddVertex(verts, pos.X + 0, pos.Y + 0, pos.Z + 1, 1, 0, textureLayer);
            AddVertex(verts, pos.X + 0, pos.Y + 1, pos.Z + 1, 1, 1, textureLayer);
            AddVertex(verts, pos.X + 0, pos.Y + 1, pos.Z + 1, 1, 1, textureLayer);
            AddVertex(verts, pos.X + 0, pos.Y + 1, pos.Z + 0, 0, 1, textureLayer);
            AddVertex(verts, pos.X + 0, pos.Y + 0, pos.Z + 0, 0, 0, textureLayer);
        }
        else if (face == 3) // Right (Neighbour X+1) -> Face at X=1
        {
            AddVertex(verts, pos.X + 1, pos.Y + 0, pos.Z + 1, 0, 0, textureLayer);
            AddVertex(verts, pos.X + 1, pos.Y + 0, pos.Z + 0, 1, 0, textureLayer);
            AddVertex(verts, pos.X + 1, pos.Y + 1, pos.Z + 0, 1, 1, textureLayer);
            AddVertex(verts, pos.X + 1, pos.Y + 1, pos.Z + 0, 1, 1, textureLayer);
            AddVertex(verts, pos.X + 1, pos.Y + 1, pos.Z + 1, 0, 1, textureLayer);
            AddVertex(verts, pos.X + 1, pos.Y + 0, pos.Z + 1, 0, 0, textureLayer);
        }
        else if (face == 4) // Bottom (Neighbour Y-1) -> Face at Y=0
        {
            AddVertex(verts, pos.X + 0, pos.Y + 0, pos.Z + 0, 0, 1, textureLayer);
            AddVertex(verts, pos.X + 1, pos.Y + 0, pos.Z + 0, 1, 1, textureLayer);
            AddVertex(verts, pos.X + 1, pos.Y + 0, pos.Z + 1, 1, 0, textureLayer);
            AddVertex(verts, pos.X + 1, pos.Y + 0, pos.Z + 1, 1, 0, textureLayer);
            AddVertex(verts, pos.X + 0, pos.Y + 0, pos.Z + 1, 0, 0, textureLayer);
            AddVertex(verts, pos.X + 0, pos.Y + 0, pos.Z + 0, 0, 1, textureLayer);
        }
        else if (face == 5) // Top (Neighbour Y+1) -> Face at Y=1
        {
            AddVertex(verts, pos.X + 0, pos.Y + 1, pos.Z + 1, 0, 0, textureLayer);
            AddVertex(verts, pos.X + 1, pos.Y + 1, pos.Z + 1, 1, 0, textureLayer);
            AddVertex(verts, pos.X + 1, pos.Y + 1, pos.Z + 0, 1, 1, textureLayer);
            AddVertex(verts, pos.X + 1, pos.Y + 1, pos.Z + 0, 1, 1, textureLayer);
            AddVertex(verts, pos.X + 0, pos.Y + 1, pos.Z + 0, 0, 1, textureLayer);
            AddVertex(verts, pos.X + 0, pos.Y + 1, pos.Z + 1, 0, 0, textureLayer);
        }
    }

    private void AddVertex(List<float> verts, float x, float y, float z, float u, float v, float layer)
    {
        verts.Add(x);
        verts.Add(y);
        verts.Add(z);
        verts.Add(u);
        verts.Add(v);
        verts.Add(layer);
    }

    // Simple Raycast
    // Returns: (Hit?, BlockPos, FaceNormal)
    public (bool, Vector3i, Vector3i) Raycast(Vector3 origin, Vector3 direction, float maxDistance)
    {
        // Simple stepping method (not perfect DDA but sufficient for short resource)
        // 5 blocks * 20 steps/block = 100 steps
        float step = 0.05f;
        Vector3 pos = origin;
        Vector3 lastPos = origin;
        
        for (float d = 0; d < maxDistance; d += step)
        {
            Vector3i bPos = new Vector3i((int)Math.Floor(pos.X), (int)Math.Floor(pos.Y), (int)Math.Floor(pos.Z));
            
            // Check bounds
            if (bPos.X >= 0 && bPos.X < SizeX && bPos.Y >= 0 && bPos.Y < SizeY && bPos.Z >= 0 && bPos.Z < SizeZ)
            {
                 Block b = _blocks[bPos.X, bPos.Y, bPos.Z];
                 if (b.IsSolid)
                 {
                     // Calculate Face Normal based on entry
                     // Determine which face we entered from by comparing with last pos
                     Vector3i lastBPos = new Vector3i((int)Math.Floor(lastPos.X), (int)Math.Floor(lastPos.Y), (int)Math.Floor(lastPos.Z));
                     Vector3i normal = lastBPos - bPos;
                     
                     // If for some reason we started inside or jumped, normal might be 0 or diagonal.
                     // Fallback to simplistic normal if diagonal
                     if (normal.ManhattanLength != 1) 
                     {
                         // Find dominant axis?
                         // For now, return Up if undefined (placing on top)
                         normal = Vector3i.UnitY;
                     }

                     return (true, bPos, normal);
                 }
            }
            
            lastPos = pos;
            pos += direction * step;
        }
        
        return (false, Vector3i.Zero, Vector3i.Zero);
    }
}
