using System.Linq;

public struct ItemStack
{
    public BlockType Type;
    public int Count;

    public ItemStack(BlockType type, int count)
    {
        Type = type;
        Count = count;
    }
}

public class Inventory
{
    public ItemStack[] MainSlots = new ItemStack[36]; // 9 Hotbar + 27 Main
    public int SelectedSlot = 0; // 0-8

    public Inventory()
    {
        // Start with Wooden Sword in slot 0
        MainSlots[0] = new ItemStack(BlockType.Wooden_Sword, 1);
        // Blocks
        MainSlots[1] = new ItemStack(BlockType.Wool_Red, 64);
        MainSlots[2] = new ItemStack(BlockType.Planks_Oak, 64);
        MainSlots[3] = new ItemStack(BlockType.Stone, 64);
        MainSlots[4] = new ItemStack(BlockType.End_Stone, 64); // kasfnjsdagnj
    }

    public ItemStack GetHandItem()
    {
        return MainSlots[SelectedSlot];
    }
    
    public void UseItem()
    {
        if (MainSlots[SelectedSlot].Count > 0)
        {
            if (MainSlots[SelectedSlot].Type == BlockType.Wooden_Sword) return;
            // Creative mode: don't decrease? 
            // Bedwars: consuming blocks.
            MainSlots[SelectedSlot].Count--;
            if (MainSlots[SelectedSlot].Count <= 0) 
            {
                MainSlots[SelectedSlot].Type = BlockType.Air;
            }
        }
    }
}
