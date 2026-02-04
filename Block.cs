using OpenTK.Mathematics;

public enum BlockType : byte
{
    Air = 0,
    Bedrock = 1,
    Dirt = 2,
    GrassBlock = 3,
    Stone = 4,
    Planks_Oak = 5,
    Wool_White = 6,
    Wool_Red = 7,
    Wool_Blue = 8,
    End_Stone = 9,
    Wooden_Sword = 10
}

public struct Block
{
    public BlockType Type;

    public Block(BlockType type)
    {
        Type = type;
    }

    public bool IsSolid => Type != BlockType.Air && Type != BlockType.Wooden_Sword;
    public bool IsPlaceable => Type != BlockType.Air && Type != BlockType.Wooden_Sword;
    
    // Simplistic texture mapping for now. 
    // In a real atlas, we'd return UV coordinates or an Index into the array texture.
    public int GetTextureFace(int faceIndex) 
    {
        // 0: Back, 1: Front, 2: Left, 3: Right, 4: Bottom, 5: Top
        switch (Type)
        {
            case BlockType.GrassBlock:
                if (faceIndex == 5) return 0; // Top (Grass)
                if (faceIndex == 4) return 2; // Bottom (Dirt)
                return 1; // Side (Grass Side)
            case BlockType.Dirt: return 2;
            case BlockType.Stone: return 3;
            case BlockType.Planks_Oak: return 4;
            case BlockType.Wool_White: return 5;
            case BlockType.Wool_Red: return 6;
            case BlockType.Wool_Blue: return 7;
            case BlockType.Bedrock: return 8;
            case BlockType.End_Stone: return 9;
            default: return 0;
        }
    }

    public int GetItemTextureLayer()
    {
        switch (Type)
        {
            case BlockType.GrassBlock: return 0;
            case BlockType.Dirt: return 1;
            case BlockType.Stone: return 2;
            case BlockType.Planks_Oak: return 3;
            case BlockType.Wool_White: return 4;
            case BlockType.Wool_Red: return 5;
            case BlockType.Wool_Blue: return 6;
            case BlockType.Bedrock: return 7;
            case BlockType.End_Stone: return 8;
            case BlockType.Wooden_Sword: return 9;
            default: return 0;
        }
    }
}
