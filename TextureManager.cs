using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using System.Collections.Generic;
using System.IO;
using System;

public class TextureManager
{
    public int Handle;
    public int WidgetTextureHandle;
    public int FontTextureHandle;
    public int ItemTextureHandle;
    public int SkinTextureHandle;
    private readonly List<string> _textureFiles;
    private readonly List<string> _itemTextureFiles;
    private readonly List<string> _itemFallbackBlockFiles;
    private const string BasePath = "Resources/Old_Default_1.13.2/assets/minecraft/textures/block/";
    private const string ItemBasePath = "Resources/Old_Default_1.13.2/assets/minecraft/textures/block/";
    private const string ItemBasePathAlt = "Resources/Old_Default_1.13.2/assets/textures/block/";

    public TextureManager()
    {
        _textureFiles = new List<string>
        {
            "grass_block_top.png",      // 0
            "grass_block_side.png",     // 1
            "dirt.png",                 // 2
            "stone.png",                // 3
            "oak_planks.png",           // 4
            "white_wool.png",           // 5
            "red_wool.png",             // 6
            "blue_wool.png",            // 7
            "bedrock.png",              // 8
            "end_stone.png",            // 9
            "destroy_stage_0.png",      // 10
            "destroy_stage_1.png",      // 11
            "destroy_stage_2.png",      // 12
            "destroy_stage_3.png",      // 13
            "destroy_stage_4.png",      // 14
            "destroy_stage_5.png",      // 15
            "destroy_stage_6.png",      // 16
            "destroy_stage_7.png",      // 17
            "destroy_stage_8.png",      // 18
            "destroy_stage_9.png"       // 19
        };

        LoadTextureArray();
        
        _itemTextureFiles = new List<string>
        {
            "grass_block.png",  // 0
            "dirt.png",         // 1
            "stone.png",        // 2
            "oak_planks.png",   // 3
            "white_wool.png",   // 4
            "red_wool.png",     // 5
            "blue_wool.png",    // 6
            "bedrock.png",      // 7
            "end_stone.png",    // 8
            "wooden_sword.png"  // 9
        };
        
        _itemFallbackBlockFiles = new List<string>
        {
            "grass_block_top.png",
            "dirt.png",
            "stone.png",
            "oak_planks.png",
            "white_wool.png",
            "red_wool.png",
            "blue_wool.png",
            "bedrock.png",
            "end_stone.png",
            "oak_planks.png"
        };
        
        LoadItemTextureArray();
        LoadWidgetTexture();
        LoadFontTexture();
        LoadSkinTexture();
    }

    private void LoadFontTexture()
    {
        string path = "Resources/Old_Default_1.13.2/assets/minecraft/textures/font/ascii.png";
        if (!File.Exists(path)) { System.Console.WriteLine("Missing ascii.png"); return; }
        
        FontTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, FontTextureHandle);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        
        StbImage.stbi_set_flip_vertically_on_load(1);
        ImageResult image = ImageResult.FromStream(File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
    }

    private void LoadWidgetTexture()
    {
        string path = "Resources/Old_Default_1.13.2/assets/minecraft/textures/gui/widgets.png";
        if (!File.Exists(path)) { System.Console.WriteLine("Missing widgets.png"); return; }

        WidgetTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, WidgetTextureHandle);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        
        StbImage.stbi_set_flip_vertically_on_load(1); // GUI often needs flip or consistent UVs
        ImageResult image = ImageResult.FromStream(File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
    }

    private void LoadSkinTexture()
    {
        string path = "Resources/skin.png";
        if (!File.Exists(path)) { System.Console.WriteLine("Missing skin.png"); return; }

        SkinTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, SkinTextureHandle);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        StbImage.stbi_set_flip_vertically_on_load(1);
        ImageResult image = ImageResult.FromStream(File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, image.Width, image.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
    }

    public void UseWidgets(TextureUnit unit = TextureUnit.Texture1)
    {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, WidgetTextureHandle);
    }

    public void UseItems(TextureUnit unit = TextureUnit.Texture0)
    {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2DArray, ItemTextureHandle);
    }

    public void UseSkin(TextureUnit unit = TextureUnit.Texture0)
    {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2D, SkinTextureHandle);
    }

    private void LoadTextureArray()
    {
        Handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2DArray, Handle);

        // Parameters
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        int width = 16;
        int height = 16;
        int layers = _textureFiles.Count;

        // Allocate storage
        GL.TexImage3D(TextureTarget.Texture2DArray, 0, PixelInternalFormat.Rgba, width, height, layers, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

        for (int i = 0; i < layers; i++)
        {
            string path = BasePath + _textureFiles[i];
            if (!File.Exists(path))
            {
                System.Console.WriteLine($"Texture missing: {path}");
                continue;
            }

            // Flip vertically because OpenGL expects 0,0 at bottom-left
            StbImage.stbi_set_flip_vertically_on_load(1); 

            ImageResult image = ImageResult.FromStream(File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);
            
            if (image.Width != width || image.Height != height)
            {
                 // Ideally resize, but for now just warn or skip. Minecraft textures are usually 16x16.
                 System.Console.WriteLine($"Warning: Texture {path} is {image.Width}x{image.Height}, expected {width}x{height}");
            }

            GL.TexSubImage3D(TextureTarget.Texture2DArray, 0, 0, 0, i, image.Width, image.Height, 1, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
        }

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);
    }

    private void LoadItemTextureArray()
    {
        ItemTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2DArray, ItemTextureHandle);

        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        int width = 16;
        int height = 16;
        int layers = _itemTextureFiles.Count;

        GL.TexImage3D(TextureTarget.Texture2DArray, 0, PixelInternalFormat.Rgba, width, height, layers, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

        for (int i = 0; i < layers; i++)
        {
            string path = ResolveItemTexturePath(_itemTextureFiles[i], _itemFallbackBlockFiles[i]);
            if (path == null)
            {
                System.Console.WriteLine($"Item texture missing: {_itemTextureFiles[i]}");
                continue;
            }

            StbImage.stbi_set_flip_vertically_on_load(1);
            ImageResult image = ImageResult.FromStream(File.OpenRead(path), ColorComponents.RedGreenBlueAlpha);

            if (image.Width != width || image.Height != height)
            {
                System.Console.WriteLine($"Warning: Item texture {path} is {image.Width}x{image.Height}, expected {width}x{height}");
            }

            GL.TexSubImage3D(TextureTarget.Texture2DArray, 0, 0, 0, i, image.Width, image.Height, 1, PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);
        }

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);
    }

    private string? ResolveItemTexturePath(string itemFile, string fallbackBlockFile)
    {
        string itemPath = ItemBasePath + itemFile;
        if (File.Exists(itemPath)) return itemPath;

        string itemAltPath = ItemBasePathAlt + itemFile;
        if (File.Exists(itemAltPath)) return itemAltPath;

        string itemItemPath = "Resources/Old_Default_1.13.2/assets/minecraft/textures/item/" + itemFile;
        if (File.Exists(itemItemPath)) return itemItemPath;

        string itemItemAltPath = "Resources/Old_Default_1.13.2/assets/textures/item/" + itemFile;
        if (File.Exists(itemItemAltPath)) return itemItemAltPath;

        return null;
    }

    public void Use(TextureUnit unit = TextureUnit.Texture0)
    {
        GL.ActiveTexture(unit);
        GL.BindTexture(TextureTarget.Texture2DArray, Handle);
    }
}
