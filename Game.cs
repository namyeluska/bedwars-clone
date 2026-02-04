using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text.Json;

public class Game : GameWindow
{
    private enum CameraMode
    {
        FirstPerson = 0,
        ThirdPersonBack = 1,
        ThirdPersonFront = 2
    }

    private WorldShader? _worldShader;
    private TextureManager? _textureManager;
    private World? _world;
    private Player? _player;
    private Inventory? _inventory;

    private Dictionary<Vector2i, (int Vao, int Vbo, int Count)> _chunkRenderData = new Dictionary<Vector2i, (int, int, int)>();

    // Interaction
    private bool _hasSelection;
    private Vector3 _selectedBlock;
    private Vector3i _placeNormal;
    private int _unitCubeVao;
    private int _handVao;
    private int _handVbo;
    private float _placeTimer;

    // Breaking
    private bool _isBreaking;
    private float _currentBreakTime; 
    private const float BreakTimeTotal = 0.5f; 
    private Vector3i _breakTarget;
    private int _breakOverlayVao, _breakOverlayVbo;

    // GUI
    private int _crosshairVao, _crosshairVbo;
    private int _widgetsVao, _widgetsVbo;
    private int _widgetSelectorVao, _widgetSelectorVbo;
    private int _quadVao; 
    private int _faceOutlineVao, _faceOutlineVbo;
    private float _hotbarLeft;
    private float _hotbarRight;
    private float _hotbarBottom;
    private float _hotbarTop;
    private Vector2 _hotbarUvOffset;
    private Vector2 _hotbarUvSize;
    private Vector2 _selectorUvOffset;
    private Vector2 _selectorUvSize;
    private float _uiScale = 3.0f;
    private float _hotbarPaddingPx = 4.0f;
    private float _hotbarOffsetXPx = 0.0f;
    private float _hotbarOffsetYPx = 0.0f;
    private float _itemOffsetXPx = 0.0f;
    private Shader? _guiShader; 
    private Vector3 _lastPlayerPos;
    private float _bobTime;
    private bool _isSwinging;
    private float _swingTime;
    private const float SwingDuration = 0.18f;
    // Vanilla item model "handheld" display values (units are 1/16 of a block)
    private const float MinecraftItemUnit = 1.0f / 16.0f;
    private bool _hasHandheldDisplay;
    private Vector3 _handheldRotation;
    private Vector3 _handheldTranslation;
    private Vector3 _handheldScale;
    private bool _hasHandDisplay;
    private Vector3 _handRotation;
    private Vector3 _handTranslation;
    private Vector3 _handScale;
    private int _playerVao;
    private int _playerVbo;
    private float _walkTime;
    private float _limbSwing;
    private float _limbSwingAmount;
    private CameraMode _cameraMode = CameraMode.FirstPerson;
    private bool _prevF5Down;
    private const float ThirdPersonDistance = 4.0f;
    private AudioManager? _audio;
    private float _stepTimer;
    private const float StepInterval = 0.45f;

    public Game() : base(GameWindowSettings.Default, new NativeWindowSettings()
    {
        ClientSize = new Vector2i(1280, 720),
        Title = "Bedwars Clone - UI Refined",
        Flags = ContextFlags.ForwardCompatible,
        APIVersion = new Version(3, 3) 
    })
    {
        CenterWindow();
        CursorState = CursorState.Grabbed;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.CullFace);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.ClearColor(0.49f, 0.70f, 1.0f, 1.0f); 

        _textureManager = new TextureManager();
        _worldShader = new WorldShader();
        _world = new World();
        RegenerateWorldMesh();
        
        _player = new Player(new Vector3(8.0f, 12.0f, 8.0f), _world);
        _inventory = new Inventory();
        _lastPlayerPos = _player.Position;
        _audio = new AudioManager();
        _audio.LoadDefaults("Resources/sounds");
        
        SetupCrosshair();
        SetupBreakOverlay();
        SetupHotbar();
        CreateGenericQuad();
        SetupHandMesh();
        SetupPlayerMesh();
        SetupFaceOutline();
        LoadHandheldDisplay();
        LoadHandDisplay();
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        _audio?.Dispose();
    }

    private void CreateGenericQuad()
    {
        // 0..1 generic quad
        float[] verts = {
            0,0,0, 0,0,
            1,0,0, 1,0,
            1,1,0, 1,1,
            1,1,0, 1,1,
            0,1,0, 0,1,
            0,0,0, 0,0
        };
        _quadVao = GL.GenVertexArray();
        GL.BindVertexArray(_quadVao);
        int vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
    }

    private void SetupHandMesh()
    {
        float[] verts = BuildHandMesh();
        _handVao = GL.GenVertexArray();
        GL.BindVertexArray(_handVao);
        _handVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _handVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
    }

    private void SetupPlayerMesh()
    {
        _playerVao = GL.GenVertexArray();
        _playerVbo = GL.GenBuffer();
        GL.BindVertexArray(_playerVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _playerVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, 0, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
    }

    private float[] BuildHandMesh()
    {
        // Right arm (4x12x4) in pixels, centered around origin.
        const float w = 4f;
        const float h = 12f;
        const float d = 4f;
        float minX = -w / 2f;
        float maxX = w / 2f;
        float minY = -h;
        float maxY = 0f;
        float minZ = -d / 2f;
        float maxZ = d / 2f;

        List<float> verts = new List<float>();

        // UVs based on Minecraft skin layout (64x64), right arm.
        AddHandFace(verts, // Front
            new Vector3(minX, minY, maxZ),
            new Vector3(maxX, minY, maxZ),
            new Vector3(maxX, maxY, maxZ),
            new Vector3(minX, maxY, maxZ),
            44, 20, 48, 32);

        AddHandFace(verts, // Back
            new Vector3(maxX, minY, minZ),
            new Vector3(minX, minY, minZ),
            new Vector3(minX, maxY, minZ),
            new Vector3(maxX, maxY, minZ),
            52, 20, 56, 32);

        AddHandFace(verts, // Right
            new Vector3(maxX, minY, maxZ),
            new Vector3(maxX, minY, minZ),
            new Vector3(maxX, maxY, minZ),
            new Vector3(maxX, maxY, maxZ),
            40, 20, 44, 32);

        AddHandFace(verts, // Left
            new Vector3(minX, minY, minZ),
            new Vector3(minX, minY, maxZ),
            new Vector3(minX, maxY, maxZ),
            new Vector3(minX, maxY, minZ),
            48, 20, 52, 32);

        AddHandFace(verts, // Top
            new Vector3(minX, maxY, maxZ),
            new Vector3(maxX, maxY, maxZ),
            new Vector3(maxX, maxY, minZ),
            new Vector3(minX, maxY, minZ),
            44, 16, 48, 20);

        AddHandFace(verts, // Bottom
            new Vector3(minX, minY, minZ),
            new Vector3(maxX, minY, minZ),
            new Vector3(maxX, minY, maxZ),
            new Vector3(minX, minY, maxZ),
            48, 16, 52, 20);

        return verts.ToArray();
    }

    private void AddHandFace(List<float> verts, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, int x1, int y1, int x2, int y2)
    {
        // Convert pixel rect to UVs (skin is 64x64, origin top-left).
        const float texW = 64f;
        const float texH = 64f;
        float u0 = x1 / texW;
        float u1 = x2 / texW;
        float v0 = 1.0f - (y2 / texH);
        float v1 = 1.0f - (y1 / texH);

        AddHandVertex(verts, p0, u0, v0);
        AddHandVertex(verts, p1, u1, v0);
        AddHandVertex(verts, p2, u1, v1);
        AddHandVertex(verts, p2, u1, v1);
        AddHandVertex(verts, p3, u0, v1);
        AddHandVertex(verts, p0, u0, v0);
    }

    private void AddHandVertex(List<float> verts, Vector3 pos, float u, float v)
    {
        verts.Add(pos.X * MinecraftItemUnit);
        verts.Add(pos.Y * MinecraftItemUnit);
        verts.Add(pos.Z * MinecraftItemUnit);
        verts.Add(u);
        verts.Add(v);
    }

    private void SetupHotbar()
    {
        float texW = 256f;
        float texH = 256f;

        // widgets.png atlas coordinates
        _hotbarUvOffset = new Vector2(0f / texW, 1.0f - (22f / texH));
        _hotbarUvSize = new Vector2(182f / texW, 22f / texH);
        _selectorUvOffset = new Vector2(0f / texW, 1.0f - (46f / texH));
        _selectorUvSize = new Vector2(24f / texW, 24f / texH);

        // optional tweak points for placement
        _hotbarOffsetXPx = 0.0f;
        _hotbarOffsetYPx = 0.0f;
    }

    private void RegenerateWorldMesh() { foreach (var kvp in _world!.GetChunks()) UpdateChunkMesh(kvp.Key, kvp.Value); }

    private void UpdateChunkMesh(Vector2i chunkPos, Chunk chunk)
    {
        chunk.GenerateMesh(out float[] vertices, out int count);
        int vao, vbo;
        if (_chunkRenderData.TryGetValue(chunkPos, out var data)) { vao = data.Vao; vbo = data.Vbo; }
        else { vao = GL.GenVertexArray(); vbo = GL.GenBuffer(); }
        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
        int stride = 6 * sizeof(float);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0); 
        GL.EnableVertexAttribArray(0); 
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        GL.EnableVertexAttribArray(1); 
        GL.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, stride, 5 * sizeof(float));
        GL.EnableVertexAttribArray(2); 
        _chunkRenderData[chunkPos] = (vao, vbo, count);
    }
    
    private void UpdateChunkAt(int x, int z) {
        int cx = (int)Math.Floor((float)x / 16); int cz = (int)Math.Floor((float)z / 16);
        Vector2i chunkPos = new Vector2i(cx, cz);
        if (_world!.GetChunks().TryGetValue(chunkPos, out Chunk? chunk)) UpdateChunkMesh(chunkPos, chunk);
    }

    private void SetupBreakOverlay()
    {
        float[] verts = {
            0,0,1, 0,0, 1,0,1, 1,0, 1,1,1, 1,1, 1,1,1, 1,1, 0,1,1, 0,1, 0,0,1, 0,0,
            0,0,0, 1,0, 1,0,0, 0,0, 1,1,0, 0,1, 1,1,0, 0,1, 0,1,0, 1,1, 0,0,0, 1,0,
            0,0,0, 0,0, 0,0,1, 1,0, 0,1,1, 1,1, 0,1,1, 1,1, 0,1,0, 0,1, 0,0,0, 0,0,
            1,0,1, 0,0, 1,0,0, 1,0, 1,1,0, 1,1, 1,1,0, 1,1, 1,1,1, 0,1, 1,0,1, 0,0,
            0,1,1, 0,0, 1,1,1, 1,0, 1,1,0, 1,1, 1,1,0, 1,1, 0,1,0, 0,1, 0,1,1, 0,0,
            0,0,0, 0,0, 1,0,0, 1,0, 1,0,1, 1,1, 1,0,1, 1,1, 0,0,1, 0,1, 0,0,0, 0,0
        };
        _breakOverlayVao = GL.GenVertexArray();
        GL.BindVertexArray(_breakOverlayVao);
        _breakOverlayVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _breakOverlayVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));
        GL.EnableVertexAttribArray(1);
    }

    private void SetupCrosshair()
    {
        float[] verts = { -0.015f, -0.01f, 0, 0, 0.015f, 0, 0, 0.015f, 0, 0.015f, -0.01f, 0 };
        _crosshairVao = GL.GenVertexArray();
        GL.BindVertexArray(_crosshairVao);
        _crosshairVbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _crosshairVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        _guiShader = new Shader();
    }

    private void SetupFaceOutline()
    {
        _faceOutlineVao = GL.GenVertexArray();
        _faceOutlineVbo = GL.GenBuffer();
        GL.BindVertexArray(_faceOutlineVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _faceOutlineVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, 5 * 3 * sizeof(float), IntPtr.Zero, BufferUsageHint.DynamicDraw);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
    }

    private void UpdateHotbarLayout()
    {
        const float hotbarTexW = 182f;
        const float hotbarTexH = 22f;

        float pxToHud = 2.0f / ClientSize.Y;
        float width = hotbarTexW * _uiScale * pxToHud;
        float height = hotbarTexH * _uiScale * pxToHud;

        float offsetX = _hotbarOffsetXPx * _uiScale * pxToHud;
        float offsetY = _hotbarOffsetYPx * _uiScale * pxToHud;
        float bottom = -1.0f + (_hotbarPaddingPx * _uiScale * pxToHud) + offsetY;

        _hotbarLeft = -width * 0.5f + offsetX;
        _hotbarRight = _hotbarLeft + width;
        _hotbarBottom = bottom;
        _hotbarTop = bottom + height;
    }

    private void DrawGuiQuad(float left, float bottom, float width, float height, Vector2 uvOffset, Vector2 uvSize, Matrix4 hudModel)
    {
        _guiShader!.SetVector2("uvOffset", uvOffset);
        _guiShader.SetVector2("uvSize", uvSize);
        Matrix4 model = Matrix4.CreateScale(width, height, 1.0f) * Matrix4.CreateTranslation(left, bottom, 0.0f) * hudModel;
        _guiShader.SetMatrix4("model", model);
        GL.BindVertexArray(_quadVao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    private Matrix4 BuildFirstPersonRightHandTransform(float scale, Vector3 rotationDeg, Vector3 translation)
    {
        Matrix4 t = Matrix4.CreateTranslation(translation);
        Matrix4 r =
            Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rotationDeg.X)) *
            Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotationDeg.Y)) *
            Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rotationDeg.Z));
        Matrix4 s = Matrix4.CreateScale(scale);
        return s * r * t;
    }

    private Matrix4 BuildItemDisplayTransform(Vector3 rotationDeg, Vector3 translation, Vector3 scale)
    {
        Matrix4 t = Matrix4.CreateTranslation(translation);
        Matrix4 r =
            Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rotationDeg.X)) *
            Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotationDeg.Y)) *
            Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rotationDeg.Z));
        Matrix4 s = Matrix4.CreateScale(scale);
        return s * r * t;
    }

    private Matrix4 GetCameraViewMatrix()
    {
        float eyeHeight = _player!.IsSneaking ? 1.27f : 1.62f;
        Vector3 eyePos = _player.Position + new Vector3(0, eyeHeight, 0);
        Vector3 front = _player.GetFront();

        if (_cameraMode == CameraMode.FirstPerson)
        {
            return Matrix4.LookAt(eyePos, eyePos + front, Vector3.UnitY);
        }

        Vector3 cameraPos;
        if (_cameraMode == CameraMode.ThirdPersonBack)
        {
            cameraPos = eyePos - front * ThirdPersonDistance;
        }
        else
        {
            cameraPos = eyePos + front * ThirdPersonDistance;
        }

        return Matrix4.LookAt(cameraPos, eyePos, Vector3.UnitY);
    }

    private void LoadHandheldDisplay()
    {
        _hasHandheldDisplay = TryLoadHandheldDisplay(
            "Resources/Old_Default_1.13.2/assets/minecraft/models/item/handheld.json",
            out _handheldRotation,
            out _handheldTranslation,
            out _handheldScale);
        if (!_hasHandheldDisplay)
        {
            // Fallback to vanilla handheld defaults if JSON is missing or invalid.
            _handheldRotation = new Vector3(0.0f, -90.0f, 25.0f);
            _handheldTranslation = new Vector3(1.13f, 3.2f, 1.13f);
            _handheldScale = new Vector3(0.68f, 0.68f, 0.68f);
        }
    }

    private void LoadHandDisplay()
    {
        _hasHandDisplay = TryLoadHandheldDisplay(
            "Resources/hand_display.json",
            out _handRotation,
            out _handTranslation,
            out _handScale);
        if (!_hasHandDisplay)
        {
            _handRotation = Vector3.Zero;
            _handTranslation = Vector3.Zero;
            _handScale = Vector3.One;
        }
    }

    private bool TryLoadHandheldDisplay(string path, out Vector3 rotation, out Vector3 translation, out Vector3 scale)
    {
        rotation = Vector3.Zero;
        translation = Vector3.Zero;
        scale = Vector3.One;

        if (!File.Exists(path)) return false;

        try
        {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
            JsonElement fp;
            if (doc.RootElement.TryGetProperty("display", out JsonElement display) &&
                display.TryGetProperty("firstperson_righthand", out fp))
            {
                // ok
            }
            else if (doc.RootElement.TryGetProperty("firstperson_righthand", out fp))
            {
                // allow root-level for custom files like hand_display.json
            }
            else
            {
                return false;
            }

            rotation = ReadVec3(fp, "rotation", Vector3.Zero);
            translation = ReadVec3(fp, "translation", Vector3.Zero);
            scale = ReadVec3(fp, "scale", new Vector3(1.0f, 1.0f, 1.0f));
            return true;
        }
        catch
        {
            return false;
        }
    }

    private Vector3 ReadVec3(JsonElement parent, string name, Vector3 fallback)
    {
        if (!parent.TryGetProperty(name, out JsonElement arr)) return fallback;
        if (arr.ValueKind != JsonValueKind.Array) return fallback;
        if (arr.GetArrayLength() < 3) return fallback;
        return new Vector3(arr[0].GetSingle(), arr[1].GetSingle(), arr[2].GetSingle());
    }

    private struct FaceUV
    {
        public Vector4 Front;
        public Vector4 Back;
        public Vector4 Left;
        public Vector4 Right;
        public Vector4 Top;
        public Vector4 Bottom;
    }

    private Vector4 UVRect(int x1, int y1, int x2, int y2)
    {
        const float texW = 64f;
        const float texH = 64f;
        float u0 = x1 / texW;
        float u1 = x2 / texW;
        float v0 = 1.0f - (y2 / texH);
        float v1 = 1.0f - (y1 / texH);
        return new Vector4(u0, v0, u1, v1);
    }

    private float[] BuildSkinnedBox(float sx, float sy, float sz, FaceUV uv)
    {
        float hx = sx * 0.5f;
        float hy = sy * 0.5f;
        float hz = sz * 0.5f;

        Vector3 p000 = new Vector3(-hx, -hy, -hz);
        Vector3 p001 = new Vector3(-hx, -hy,  hz);
        Vector3 p010 = new Vector3(-hx,  hy, -hz);
        Vector3 p011 = new Vector3(-hx,  hy,  hz);
        Vector3 p100 = new Vector3( hx, -hy, -hz);
        Vector3 p101 = new Vector3( hx, -hy,  hz);
        Vector3 p110 = new Vector3( hx,  hy, -hz);
        Vector3 p111 = new Vector3( hx,  hy,  hz);

        List<float> verts = new List<float>(36 * 5);

        AddBoxFace(verts, p001, p101, p111, p011, uv.Front);  // front (z+)
        AddBoxFace(verts, p100, p000, p010, p110, uv.Back);   // back  (z-)
        AddBoxFace(verts, p000, p001, p011, p010, uv.Left);   // left  (x-)
        AddBoxFace(verts, p101, p100, p110, p111, uv.Right);  // right (x+)
        AddBoxFace(verts, p011, p111, p110, p010, uv.Top);    // top   (y+)
        AddBoxFace(verts, p000, p100, p101, p001, uv.Bottom); // bottom(y-)

        return verts.ToArray();
    }

    private void AddBoxFace(List<float> verts, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector4 uv)
    {
        AddBoxVertex(verts, a, uv.X, uv.Y);
        AddBoxVertex(verts, b, uv.Z, uv.Y);
        AddBoxVertex(verts, c, uv.Z, uv.W);
        AddBoxVertex(verts, c, uv.Z, uv.W);
        AddBoxVertex(verts, d, uv.X, uv.W);
        AddBoxVertex(verts, a, uv.X, uv.Y);
    }

    private void AddBoxVertex(List<float> verts, Vector3 pos, float u, float v)
    {
        verts.Add(pos.X * MinecraftItemUnit);
        verts.Add(pos.Y * MinecraftItemUnit);
        verts.Add(pos.Z * MinecraftItemUnit);
        verts.Add(u);
        verts.Add(v);
    }

    private void RenderPlayerModel()
    {
        _guiShader!.Use();
        _guiShader.SetMatrix4("view", GetCameraViewMatrix());
        _guiShader.SetMatrix4("projection", Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), ClientSize.X / (float)ClientSize.Y, 0.1f, 100.0f));
        _guiShader.SetVector2("uvOffset", Vector2.Zero);
        _guiShader.SetVector2("uvSize", Vector2.One);
        _guiShader.SetFloat("useOverrideColor", 0.0f);
        _textureManager!.UseSkin(TextureUnit.Texture0);
        _guiShader.SetInt("texture0", 0);

        float swing = (float)Math.Sin(_limbSwing * 0.6662f);
        float armSwing = swing * 45f * _limbSwingAmount;
        float legSwing = -swing * 45f * _limbSwingAmount;

        float yaw = _player!.Yaw;
        float pitch = _player.Pitch;
        Matrix4 baseModel = Matrix4.CreateTranslation(_player.Position) *
                            Matrix4.CreateRotationY(MathHelper.DegreesToRadians(yaw - 90f));

        // UVs from Minecraft skin layout (64x64) — matches vanilla.
        FaceUV headUv = new FaceUV
        {
            Right = UVRect(16, 8, 24, 16),
            Front = UVRect(8, 8, 16, 16),
            Left = UVRect(0, 8, 8, 16),
            Back = UVRect(24, 8, 32, 16),
            Top = UVRect(8, 0, 16, 8),
            Bottom = UVRect(16, 0, 24, 8)
        };
        FaceUV hatUv = new FaceUV
        {
            Right = UVRect(48, 8, 56, 16),
            Front = UVRect(40, 8, 48, 16),
            Left = UVRect(32, 8, 40, 16),
            Back = UVRect(56, 8, 64, 16),
            Top = UVRect(40, 0, 48, 8),
            Bottom = UVRect(48, 0, 56, 8)
        };

        FaceUV bodyUv = new FaceUV
        {
            Right = UVRect(28, 20, 32, 32),
            Front = UVRect(20, 20, 28, 32),
            Left = UVRect(16, 20, 20, 32),
            Back = UVRect(32, 20, 40, 32),
            Top = UVRect(20, 16, 28, 20),
            Bottom = UVRect(28, 16, 36, 20)
        };
        FaceUV jacketUv = new FaceUV
        {
            Right = UVRect(28, 36, 32, 48),
            Front = UVRect(20, 36, 28, 48),
            Left = UVRect(16, 36, 20, 48),
            Back = UVRect(32, 36, 40, 48),
            Top = UVRect(20, 32, 28, 36),
            Bottom = UVRect(28, 32, 36, 36)
        };

        FaceUV rArmUv = new FaceUV
        {
            Right = UVRect(48, 20, 52, 32),
            Front = UVRect(44, 20, 48, 32),
            Left = UVRect(40, 20, 44, 32),
            Back = UVRect(52, 20, 56, 32),
            Top = UVRect(44, 16, 48, 20),
            Bottom = UVRect(48, 16, 52, 20)
        };
        FaceUV rSleeveUv = new FaceUV
        {
            Right = UVRect(48, 36, 52, 48),
            Front = UVRect(44, 36, 48, 48),
            Left = UVRect(40, 36, 44, 48),
            Back = UVRect(52, 36, 56, 48),
            Top = UVRect(44, 32, 48, 36),
            Bottom = UVRect(48, 32, 52, 36)
        };

        FaceUV lArmUv = new FaceUV
        {
            Right = UVRect(40, 52, 44, 64),
            Front = UVRect(36, 52, 40, 64),
            Left = UVRect(32, 52, 36, 64),
            Back = UVRect(44, 52, 48, 64),
            Top = UVRect(36, 48, 40, 52),
            Bottom = UVRect(40, 48, 44, 52)
        };
        FaceUV lSleeveUv = new FaceUV
        {
            Right = UVRect(56, 52, 60, 64),
            Front = UVRect(52, 52, 56, 64),
            Left = UVRect(48, 52, 52, 64),
            Back = UVRect(60, 52, 64, 64),
            Top = UVRect(52, 48, 56, 52),
            Bottom = UVRect(56, 48, 60, 52)
        };

        FaceUV rLegUv = new FaceUV
        {
            Right = UVRect(8, 20, 12, 32),
            Front = UVRect(4, 20, 8, 32),
            Left = UVRect(0, 20, 4, 32),
            Back = UVRect(12, 20, 16, 32),
            Top = UVRect(4, 16, 8, 20),
            Bottom = UVRect(8, 16, 12, 20)
        };
        FaceUV rPantsUv = new FaceUV
        {
            Right = UVRect(8, 36, 12, 48),
            Front = UVRect(4, 36, 8, 48),
            Left = UVRect(0, 36, 4, 48),
            Back = UVRect(12, 36, 16, 48),
            Top = UVRect(4, 32, 8, 36),
            Bottom = UVRect(8, 32, 12, 36)
        };

        FaceUV lLegUv = new FaceUV
        {
            Right = UVRect(24, 52, 28, 64),
            Front = UVRect(20, 52, 24, 64),
            Left = UVRect(16, 52, 20, 64),
            Back = UVRect(28, 52, 32, 64),
            Top = UVRect(20, 48, 24, 52),
            Bottom = UVRect(24, 48, 28, 52)
        };
        FaceUV lPantsUv = new FaceUV
        {
            Right = UVRect(8, 52, 12, 64),
            Front = UVRect(4, 52, 8, 64),
            Left = UVRect(0, 52, 4, 64),
            Back = UVRect(12, 52, 16, 64),
            Top = UVRect(4, 48, 8, 52),
            Bottom = UVRect(8, 48, 12, 52)
        };

        // Base sizes (Steve). Overlay is slightly larger: +0.5px for body/arms/legs, +1px for head.
        const float headSize = 8f;
        const float headOverlay = 9f;
        const float bodyW = 8f, bodyH = 12f, bodyD = 4f;
        const float limbW = 4f, limbH = 12f, limbD = 4f;
        const float bodyOverlayW = 8.5f, bodyOverlayH = 12.5f, bodyOverlayD = 4.5f;
        const float limbOverlayW = 4.5f, limbOverlayH = 12.5f, limbOverlayD = 4.5f;

        // Head + hat
        DrawSkinnedPart(baseModel, new Vector3(0, 28, 0), Vector3.Zero, headSize, headSize, headSize, headUv, pivotAtBottom: true);
        DrawSkinnedPart(baseModel, new Vector3(0, 28, 0), Vector3.Zero, headOverlay, headOverlay, headOverlay, hatUv, pivotAtBottom: true);

        // Body + jacket
        DrawSkinnedPart(baseModel, new Vector3(0, 18, 0), Vector3.Zero, bodyW, bodyH, bodyD, bodyUv);
        DrawSkinnedPart(baseModel, new Vector3(0, 18, 0), Vector3.Zero, bodyOverlayW, bodyOverlayH, bodyOverlayD, jacketUv);

        // Right Arm + sleeve
        DrawSkinnedPart(baseModel, new Vector3(-6, 18, 0), new Vector3(armSwing, 0, 0), limbW, limbH, limbD, rArmUv, pivotAtTop: true);
        DrawSkinnedPart(baseModel, new Vector3(-6, 18, 0), new Vector3(armSwing, 0, 0), limbOverlayW, limbOverlayH, limbOverlayD, rSleeveUv, pivotAtTop: true);

        // Left Arm + sleeve
        DrawSkinnedPart(baseModel, new Vector3(6, 18, 0), new Vector3(-armSwing, 0, 0), limbW, limbH, limbD, lArmUv, pivotAtTop: true);
        DrawSkinnedPart(baseModel, new Vector3(6, 18, 0), new Vector3(-armSwing, 0, 0), limbOverlayW, limbOverlayH, limbOverlayD, lSleeveUv, pivotAtTop: true);

        // Right Leg + pants
        DrawSkinnedPart(baseModel, new Vector3(-2, 6, 0), new Vector3(legSwing, 0, 0), limbW, limbH, limbD, rLegUv, pivotAtTop: true);
        DrawSkinnedPart(baseModel, new Vector3(-2, 6, 0), new Vector3(legSwing, 0, 0), limbOverlayW, limbOverlayH, limbOverlayD, rPantsUv, pivotAtTop: true);

        // Left Leg + pants
        DrawSkinnedPart(baseModel, new Vector3(2, 6, 0), new Vector3(-legSwing, 0, 0), limbW, limbH, limbD, lLegUv, pivotAtTop: true);
        DrawSkinnedPart(baseModel, new Vector3(2, 6, 0), new Vector3(-legSwing, 0, 0), limbOverlayW, limbOverlayH, limbOverlayD, lPantsUv, pivotAtTop: true);
    }

    private void DrawSkinnedPart(Matrix4 baseModel, Vector3 center, Vector3 rotationDeg, float sx, float sy, float sz, FaceUV uv, bool pivotAtTop = false, bool pivotAtBottom = false)
    {
        float[] verts = BuildSkinnedBox(sx, sy, sz, uv);
        GL.BindVertexArray(_playerVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _playerVbo);
        GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.DynamicDraw);

        Matrix4 part =
            Matrix4.CreateTranslation(center * MinecraftItemUnit) *
            Matrix4.CreateRotationX(MathHelper.DegreesToRadians(rotationDeg.X)) *
            Matrix4.CreateRotationY(MathHelper.DegreesToRadians(rotationDeg.Y)) *
            Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(rotationDeg.Z));

        Matrix4 model = baseModel * part;
        _guiShader!.SetMatrix4("model", model);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
    }

    private string GetSoundMaterial(BlockType type)
    {
        switch (type)
        {
            case BlockType.Wool_White:
            case BlockType.Wool_Red:
            case BlockType.Wool_Blue:
                return "cloth";
            case BlockType.Stone:
            case BlockType.Bedrock:
            case BlockType.End_Stone:
                return "stone";
            default:
                return "grass";
        }
    }

    private void PlayStepFor(BlockType type)
    {
        if (_audio == null) return;
        string mat = GetSoundMaterial(type);
        _audio.PlayRandom($"step_{mat}", 0.6f);
    }

    private void PlayDigFor(BlockType type)
    {
        if (_audio == null) return;
        string mat = GetSoundMaterial(type);
        _audio.PlayRandom($"dig_{mat}", 0.9f);
    }

    private void PlayPlaceFor(BlockType type)
    {
        if (_audio == null) return;
        string mat = GetSoundMaterial(type);
        if (!_audio.PlayRandom($"place_{mat}", 0.85f))
        {
            _audio.PlayRandom($"step_{mat}", 0.85f);
        }
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        if (!IsFocused) return;
        if (KeyboardState.IsKeyDown(Keys.Escape)) Close();
        bool f5Down = KeyboardState.IsKeyDown(Keys.F5);
        if (f5Down && !_prevF5Down)
        {
            _cameraMode = (CameraMode)(((int)_cameraMode + 1) % 3);
        }
        _prevF5Down = f5Down;
        for (int i = 0; i < 9; i++) if (KeyboardState.IsKeyDown(Keys.D1 + i)) _inventory!.SelectedSlot = i;
        _player!.Update(KeyboardState, MouseState, (float)args.Time);
        Vector3 deltaPos = _player.Position - _lastPlayerPos;
        _lastPlayerPos = _player.Position;
        bool moving = KeyboardState.IsKeyDown(Keys.W) || KeyboardState.IsKeyDown(Keys.A) || KeyboardState.IsKeyDown(Keys.S) || KeyboardState.IsKeyDown(Keys.D);
        if (moving && deltaPos.LengthSquared > 0.000001f)
        {
            _bobTime += (float)args.Time * 7.0f;
            _walkTime += (float)args.Time * 8.0f;
        }
        else
        {
            _bobTime = 0.0f;
            _limbSwingAmount = 0.0f;
        }
        float dt = (float)args.Time;
        if (dt > 0)
        {
            float horizSpeed = new Vector2(deltaPos.X, deltaPos.Z).Length / dt;
            _limbSwingAmount = MathHelper.Clamp(horizSpeed / 4.5f, 0.0f, 1.0f);
            _limbSwing += dt * (horizSpeed * 1.2f);
        }
        if (_stepTimer > 0) _stepTimer -= (float)args.Time;
        if (moving && _player.IsGrounded && _stepTimer <= 0)
        {
            Block below = _world!.GetBlock(
                (int)Math.Floor(_player.Position.X),
                (int)Math.Floor(_player.Position.Y - 0.05f),
                (int)Math.Floor(_player.Position.Z));
            PlayStepFor(below.Type);
            _stepTimer = StepInterval;
        }
        Vector3 eyePos = _player.Position + new Vector3(0, _player.IsSneaking ? 1.27f : 1.62f, 0);
        var result = _world!.Raycast(eyePos, _player.GetFront(), 5.0f);
        _hasSelection = result.Item1;
        _selectedBlock = new Vector3(result.Item2.X, result.Item2.Y, result.Item2.Z);
        _placeNormal = result.Item3;
        Vector3i currentTarget = new Vector3i((int)_selectedBlock.X, (int)_selectedBlock.Y, (int)_selectedBlock.Z);

        if (MouseState.IsButtonPressed(MouseButton.Left))
        {
            _isSwinging = true;
            _swingTime = 0.0f;
        }
        if (_isSwinging)
        {
            _swingTime += (float)args.Time;
            if (_swingTime >= SwingDuration)
            {
                _isSwinging = false;
                _swingTime = 0.0f;
            }
        }

        // Only allow breaking with empty hand or Wooden_Sword
        ItemStack handItem = _inventory!.GetHandItem();
        bool canBreak = handItem.Type == BlockType.Air || handItem.Type == BlockType.Wooden_Sword;

        if (_hasSelection && MouseState.IsButtonDown(MouseButton.Left) && canBreak)
        {
            if (!_isBreaking || currentTarget != _breakTarget) { _isBreaking = true; _breakTarget = currentTarget; _currentBreakTime = 0; }
            _currentBreakTime += (float)args.Time;
            if (_currentBreakTime >= BreakTimeTotal)
            {
                Block broken = _world.GetBlock(_breakTarget.X, _breakTarget.Y, _breakTarget.Z);
                _world.SetBlock(_breakTarget.X, _breakTarget.Y, _breakTarget.Z, new Block(BlockType.Air));
                UpdateChunkAt(_breakTarget.X, _breakTarget.Z);
                PlayDigFor(broken.Type);
                _isBreaking = false;
                _currentBreakTime = 0;
            }
        }
        else { _isBreaking = false; _currentBreakTime = 0; }

        if (_placeTimer > 0) _placeTimer -= (float)args.Time;
        if (_hasSelection)
        {
             if (MouseState.IsButtonPressed(MouseButton.Right) || (MouseState.IsButtonDown(MouseButton.Right) && _placeTimer <= 0))
             {
                 Vector3i placePos = result.Item2 + result.Item3;
                 float pw = Player.Width / 2f; float ph = _player.IsSneaking ? Player.SneakHeight : Player.Height;
                 Vector3 pMin = new Vector3(_player.Position.X - pw, _player.Position.Y, _player.Position.Z - pw);
                 Vector3 pMax = new Vector3(_player.Position.X + pw, _player.Position.Y + ph, _player.Position.Z + pw);
                 bool intersect = (pMin.X < placePos.X+1 && pMax.X > placePos.X) && (pMin.Y < placePos.Y+1 && pMax.Y > placePos.Y) && (pMin.Z < placePos.Z+1 && pMax.Z > placePos.Z);
                 if (!intersect)
                 {
                     ItemStack handItem2 = _inventory!.GetHandItem();
                     if (handItem2.Type != BlockType.Air && handItem2.Count > 0 && new Block(handItem2.Type).IsPlaceable)
                     {
                         _world.SetBlock(placePos.X, placePos.Y, placePos.Z, new Block(handItem2.Type));
                         UpdateChunkAt(placePos.X, placePos.Z);
                         PlayPlaceFor(handItem2.Type);
                         _inventory.UseItem();
                         _placeTimer = 0.15f;
                     }
                 }
             }
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        GL.Disable(EnableCap.Blend);
        _worldShader!.Use(); _textureManager!.Use();
        Matrix4 view = GetCameraViewMatrix();
        Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90), ClientSize.X / (float)ClientSize.Y, 0.1f, 100.0f);
        _worldShader.SetMatrix4("view", view); _worldShader.SetMatrix4("projection", projection); _worldShader.SetMatrix4("model", Matrix4.Identity); _worldShader.SetInt("textureArray", 0);
        foreach(var chunkData in _chunkRenderData.Values) { GL.BindVertexArray(chunkData.Vao); GL.DrawArrays(PrimitiveType.Triangles, 0, chunkData.Count); }

        if (_cameraMode != CameraMode.FirstPerson)
        {
            RenderPlayerModel();
        }

        GL.Enable(EnableCap.Blend);
        if (_isBreaking)
        {
             _textureManager!.Use(TextureUnit.Texture0);
             _worldShader.SetInt("textureArray", 0);
             int stage = (int)((_currentBreakTime / BreakTimeTotal) * 10.0f); if (stage > 9) stage = 9;
             float layer = 10.0f + stage;
             Matrix4 breakModel = Matrix4.CreateTranslation(-0.5f, -0.5f, -0.5f) * Matrix4.CreateScale(1.01f) * Matrix4.CreateTranslation(0.5f, 0.5f, 0.5f) * Matrix4.CreateTranslation(_breakTarget.X, _breakTarget.Y, _breakTarget.Z);
             _worldShader.SetMatrix4("model", breakModel);
             GL.BindVertexArray(_breakOverlayVao);
             GL.DisableVertexAttribArray(2);
             GL.VertexAttrib1(2, layer);
             GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
             GL.EnableVertexAttribArray(2);
             _worldShader.SetMatrix4("model", Matrix4.Identity);
        }

        GL.Disable(EnableCap.DepthTest);
        if (_cameraMode == CameraMode.FirstPerson)
        {
            // Held item in right hand (stick to camera, always on top)
            ItemStack heldItem = _inventory!.GetHandItem();
            if (heldItem.Type != BlockType.Air)
            {
            _worldShader!.Use();
            _worldShader.SetMatrix4("view", Matrix4.Identity);
            _worldShader.SetMatrix4("projection", projection);
            _worldShader.SetInt("textureArray", 0);
            _worldShader.SetFloat("useOverrideColor", 0.0f);

            float bob = (float)Math.Sin(_bobTime) * 0.03f;
            float swingT = _isSwinging ? (_swingTime / SwingDuration) : 0.0f;
            float swing = (float)Math.Sin(swingT * Math.PI);
            Vector3 fpRotation = new Vector3(-25f * swing, 10f * swing, -35f * swing);
            Vector3 fpTranslation = new Vector3(0.78f + (0.08f * swing), -0.72f + bob - (0.15f * swing), -1.25f - (0.22f * swing));
            float fpScale = 0.52f;
            if (heldItem.Type == BlockType.Wooden_Sword)
            {
                fpTranslation = new Vector3(
                    fpTranslation.X - (0.06f * swing), // left
                    fpTranslation.Y,
                    fpTranslation.Z + (0.22f * swing)  // forward
                );
            }
            Matrix4 camLocal = BuildFirstPersonRightHandTransform(fpScale, fpRotation, fpTranslation);

            if (heldItem.Type == BlockType.Wooden_Sword)
            {
                _textureManager!.UseItems(TextureUnit.Texture0);
                int layer = new Block(heldItem.Type).GetItemTextureLayer();
                // Vanilla handheld display: rotation [0, -90, 25], translation [1.13, 3.2, 1.13], scale [0.68, 0.68, 0.68]
                Vector3 mcRotation = _handheldRotation;
                Vector3 mcTranslation = _handheldTranslation * MinecraftItemUnit;
                Vector3 mcScale = _handheldScale;
                Matrix4 center = Matrix4.CreateTranslation(-0.5f, -0.5f, 0.0f);
                Matrix4 swordLocal = BuildItemDisplayTransform(mcRotation, mcTranslation, mcScale) * center;
                Matrix4 swordModel = swordLocal * camLocal;
                _worldShader.SetMatrix4("model", swordModel);
                GL.BindVertexArray(_quadVao);
                GL.DisableVertexAttribArray(2);
                GL.VertexAttrib1(2, (float)layer);
                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
            }
            else
            {
                _textureManager!.Use(TextureUnit.Texture0);
                _worldShader.SetMatrix4("model", camLocal);
                GL.BindVertexArray(_breakOverlayVao);
                GL.DisableVertexAttribArray(2);
                Block block = new Block(heldItem.Type);
                for (int face = 0; face < 6; face++)
                {
                    int layer = block.GetTextureFace(face);
                    GL.VertexAttrib1(2, (float)layer);
                    GL.DrawArrays(PrimitiveType.Triangles, face * 6, 6);
                }
            }

            GL.EnableVertexAttribArray(2);
            _worldShader.SetMatrix4("model", Matrix4.Identity);
        }
            else
            {
            _guiShader!.Use();
            _guiShader.SetMatrix4("view", Matrix4.Identity);
            _guiShader.SetMatrix4("projection", projection);
            _guiShader.SetVector2("uvOffset", Vector2.Zero);
            _guiShader.SetVector2("uvSize", Vector2.One);
            _guiShader.SetFloat("useOverrideColor", 0.0f);

            _textureManager!.UseSkin(TextureUnit.Texture0);
            _guiShader.SetInt("texture0", 0);

            float bob = (float)Math.Sin(_bobTime) * 0.03f;
            float swingT = _isSwinging ? (_swingTime / SwingDuration) : 0.0f;
            float swing = (float)Math.Sin(swingT * Math.PI);
            Vector3 fpRotation = new Vector3(-25f * swing, 10f * swing, -35f * swing);
            Vector3 fpTranslation = new Vector3(0.78f + (0.08f * swing), -0.72f + bob - (0.15f * swing), -1.25f - (0.22f * swing));
            float fpScale = 0.52f;
            Matrix4 camLocal = BuildFirstPersonRightHandTransform(fpScale, fpRotation, fpTranslation);

            Matrix4 handAdjust = BuildItemDisplayTransform(_handRotation, _handTranslation * MinecraftItemUnit, _handScale);
            _guiShader.SetMatrix4("model", handAdjust * camLocal);
            GL.BindVertexArray(_handVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);
            }
        }
        // "Old" Block Outlines: Hide during placement (Right Mouse Button) and make thin
        if (_hasSelection && !MouseState.IsButtonDown(MouseButton.Right))
        {
             _guiShader!.Use(); _guiShader.SetMatrix4("view", view); _guiShader.SetMatrix4("projection", projection);
             _guiShader.SetVector2("uvOffset", Vector2.Zero); _guiShader.SetVector2("uvSize", Vector2.One);
             _guiShader.SetMatrix4("model", Matrix4.Identity); _guiShader.SetVector4("overrideColor", new Vector4(0, 0, 0, 1)); _guiShader.SetFloat("useOverrideColor", 1.0f);
             UpdateFaceOutline(_selectedBlock, _placeNormal);
             GL.LineWidth(1.0f);
             GL.BindVertexArray(_faceOutlineVao);
             GL.DrawArrays(PrimitiveType.LineStrip, 0, 5);
             _guiShader.SetFloat("useOverrideColor", 0.0f);
        }

        _guiShader!.Use(); _guiShader.SetMatrix4("view", Matrix4.Identity); _guiShader.SetMatrix4("projection", Matrix4.Identity);
        float aspect = ClientSize.X / (float)ClientSize.Y;
        Matrix4 hudModel = Matrix4.CreateScale(1.0f / aspect, 1.0f, 1.0f);
        _guiShader.SetMatrix4("model", hudModel);
        _guiShader.SetFloat("useOverrideColor", 0.0f);

        UpdateHotbarLayout();

        _textureManager!.UseWidgets(TextureUnit.Texture0); _guiShader.SetInt("texture0", 0);
        float hotbarWidth = _hotbarRight - _hotbarLeft;
        float hotbarHeight = _hotbarTop - _hotbarBottom;
        DrawGuiQuad(_hotbarLeft, _hotbarBottom, hotbarWidth, hotbarHeight, _hotbarUvOffset, _hotbarUvSize, hudModel);

        const int hotbarWidthPx = 182;
        const int hotbarHeightPx = 22;
        const int slotSizePx = 20;
        const int slotStartX = 1;
        const int slotTopY = 3;
        const int itemInsetX = 2;
        const int itemSizePx = 16;
        float itemWidth = (itemSizePx / (float)hotbarWidthPx) * hotbarWidth;
        float itemHeight = (itemSizePx / (float)hotbarHeightPx) * hotbarHeight;

        int selSlotX = slotStartX + _inventory!.SelectedSlot * slotSizePx + itemInsetX;
        float selItemLeft = _hotbarLeft + ((selSlotX + _itemOffsetXPx) / hotbarWidthPx) * hotbarWidth;
        float selItemTop = _hotbarTop - (slotTopY / (float)hotbarHeightPx) * hotbarHeight;
        float selItemBottom = selItemTop - itemHeight;
        float selectorWidth = (24f / hotbarWidthPx) * hotbarWidth;
        float selectorHeight = (24f / hotbarHeightPx) * hotbarHeight;
        float selectorLeft = selItemLeft - ((selectorWidth - itemWidth) * 0.5f);
        float selectorBottom = selItemBottom - ((selectorHeight - itemHeight) * 0.5f);
        DrawGuiQuad(selectorLeft, selectorBottom, selectorWidth, selectorHeight, _selectorUvOffset, _selectorUvSize, hudModel);

        // 2D Item Sprites & Numbers
        _textureManager!.UseItems(TextureUnit.Texture0);
        _worldShader.SetInt("textureArray", 0);
        _worldShader.SetFloat("useOverrideColor", 0.0f);
        for (int i = 0; i < 9; i++)
        {
            ItemStack stack = _inventory.MainSlots[i];
            if (stack.Type != BlockType.Air && stack.Count > 0)
            {
                int slotX = slotStartX + i * slotSizePx + itemInsetX;
                float itemLeft = _hotbarLeft + ((slotX + _itemOffsetXPx) / hotbarWidthPx) * hotbarWidth;
                float itemTop = _hotbarTop - (slotTopY / (float)hotbarHeightPx) * hotbarHeight;
                float itemBottom = itemTop - itemHeight;
                Matrix4 itemModel = Matrix4.CreateScale(itemWidth, itemHeight, 1.0f) * Matrix4.CreateTranslation(itemLeft, itemBottom, 0.0f) * hudModel;
                _worldShader.Use(); _worldShader.SetMatrix4("projection", Matrix4.Identity); _worldShader.SetMatrix4("view", Matrix4.Identity);
                _worldShader.SetMatrix4("model", itemModel);
                
                int layer = new Block(stack.Type).GetItemTextureLayer();
                GL.BindVertexArray(_quadVao);
                GL.DisableVertexAttribArray(2);
                GL.VertexAttrib1(2, (float)layer);
                GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
                GL.EnableVertexAttribArray(2);
            }
        }

        _guiShader!.Use(); _guiShader.SetMatrix4("view", Matrix4.Identity); _guiShader.SetMatrix4("projection", Matrix4.Identity);
        _guiShader.SetVector2("uvOffset", Vector2.Zero); _guiShader.SetVector2("uvSize", Vector2.One);
        _guiShader.SetMatrix4("model", Matrix4.CreateScale(1.0f/aspect, 1.0f, 1.0f)); _guiShader.SetVector4("overrideColor", new Vector4(1, 1, 1, 1)); _guiShader.SetFloat("useOverrideColor", 1.0f);
        GL.BindVertexArray(_crosshairVao); GL.DrawArrays(PrimitiveType.Lines, 0, 4);
        
        // Held block count (top-left)
        ItemStack held = _inventory!.GetHandItem();
        if (held.Type != BlockType.Air && held.Type != BlockType.Wooden_Sword)
        {
            int total = 0;
            for (int i = 0; i < _inventory.MainSlots.Length; i++)
            {
                var s = _inventory.MainSlots[i];
                if (s.Type == held.Type) total += s.Count;
            }
            if (total > 256) total = 256;
            RenderText(total.ToString(), -0.97f, 0.9f, 0.045f, aspect);
        }
        GL.Enable(EnableCap.DepthTest); 
        SwapBuffers();
    }

    private void UpdateFaceOutline(Vector3 blockPos, Vector3i normal)
    {
        float nudge = 0.002f;
        float nx = normal.X;
        float ny = normal.Y;
        float nz = normal.Z;

        float x0 = blockPos.X;
        float x1 = blockPos.X + 1.0f;
        float y0 = blockPos.Y;
        float y1 = blockPos.Y + 1.0f;
        float z0 = blockPos.Z;
        float z1 = blockPos.Z + 1.0f;

        float[] verts;
        if (normal == -Vector3i.UnitZ)
        {
            float z = z0 - nudge;
            verts = new float[] { x0, y0, z,  x1, y0, z,  x1, y1, z,  x0, y1, z,  x0, y0, z };
        }
        else if (normal == Vector3i.UnitZ)
        {
            float z = z1 + nudge;
            verts = new float[] { x0, y0, z,  x1, y0, z,  x1, y1, z,  x0, y1, z,  x0, y0, z };
        }
        else if (normal == -Vector3i.UnitX)
        {
            float x = x0 - nudge;
            verts = new float[] { x, y0, z0,  x, y0, z1,  x, y1, z1,  x, y1, z0,  x, y0, z0 };
        }
        else if (normal == Vector3i.UnitX)
        {
            float x = x1 + nudge;
            verts = new float[] { x, y0, z0,  x, y0, z1,  x, y1, z1,  x, y1, z0,  x, y0, z0 };
        }
        else if (normal == -Vector3i.UnitY)
        {
            float y = y0 - nudge;
            verts = new float[] { x0, y, z0,  x1, y, z0,  x1, y, z1,  x0, y, z1,  x0, y, z0 };
        }
        else
        {
            float y = y1 + nudge;
            verts = new float[] { x0, y, z0,  x1, y, z0,  x1, y, z1,  x0, y, z1,  x0, y, z0 };
        }

        GL.BindBuffer(BufferTarget.ArrayBuffer, _faceOutlineVbo);
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, verts.Length * sizeof(float), verts);
    }

    private void RenderText(string text, float x, float y, float size, float aspect)
    {
        _guiShader!.Use();
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, _textureManager!.FontTextureHandle);
        _guiShader.SetInt("texture0", 0);
        _guiShader.SetFloat("useOverrideColor", 0.0f);
        
        float charW = size / aspect;
        float div = 16.0f;
        
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            int idx = (int)c;
            float u = (idx % 16) / div;
            float v = (idx / 16) / div;
            
            _guiShader.SetVector2("uvOffset", new Vector2(u, 1.0f - v - (1.0f/div)));
            _guiShader.SetVector2("uvSize", new Vector2(1.0f/div, 1.0f/div));
            
            // i * charW * 0.7f hərflərin bir-birinə çox yaxın olmasını təmin edir
            Matrix4 charModel = Matrix4.CreateScale(charW, size, 1.0f) * Matrix4.CreateTranslation(x + i * charW * 0.8f, y, 0.0f);
            _guiShader.SetMatrix4("model", charModel);
            
            GL.BindVertexArray(_quadVao);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 6);
        }
    }

    private void CreateUnitCube()
    {
        float[] verts = { 0,0,0, 1,0,0, 1,0,0, 1,1,0, 1,1,0, 0,1,0, 0,1,0, 0,0,0, 0,0,1, 1,0,1, 1,0,1, 1,1,1, 1,1,1, 0,1,1, 0,1,1, 0,0,1, 0,0,0, 0,0,1, 1,0,0, 1,0,1, 1,1,0, 1,1,1, 0,1,0, 0,1,1 };
        _unitCubeVao = GL.GenVertexArray(); GL.BindVertexArray(_unitCubeVao); int vbo = GL.GenBuffer(); GL.BindBuffer(BufferTarget.ArrayBuffer, vbo); GL.BufferData(BufferTarget.ArrayBuffer, verts.Length * sizeof(float), verts, BufferUsageHint.StaticDraw); GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0); GL.EnableVertexAttribArray(0);
    }

    protected override void OnResize(ResizeEventArgs e) { base.OnResize(e); GL.Viewport(0, 0, ClientSize.X, ClientSize.Y); }
}
