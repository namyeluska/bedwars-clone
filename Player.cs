using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;

public class Player
{
    public Vector3 Position { get; set; }
    public Vector3 Velocity;
    
    // Camera
    public float Pitch;
    public float Yaw = -90f;
    private float _sensitivity = 0.2f;

    // AABB
    public const float Width = 0.6f;
    public const float Height = 2.0f;   // Normal height # 1.8
    public const float SneakHeight = 1.0f; // 1.13+ sneak height is 1.5, eye height 1.27 # 1.5                       

    // Eye Offsets
    private const float EyeHeightNormal = 1.62f;
    private const float EyeHeightSneak = 1.27f;
    
    // Physics Constants (Approximate Minecraft 1.8/1.13 values in m/s)
    // 20 TPS. Gravity per tick is 0.08 blocks/tick. 
    // In seconds: 0.08 * 20 * 20 = 32 m/s^2 ? 
    // Actually MC uses per-tick velocity decay.
    // Let's use simple Euler integration for now with MC-like values.
    private const float Gravity = 32.0f; 
    private const float JumpImpulse = 9.0f; // Approx sqrt(2*grav*1.25m) ~ 9
    private const float WalkSpeed = 4.317f;
    private const float SprintSpeed = 7.0f; // 5.612
    private const float SneakSpeed = 1.31f;

    public bool IsGrounded;
    public bool IsSneaking;
    public bool IsSprinting;

    private World _world; // Collisions

    public Player(Vector3 startPos, World world)
    {
        Position = startPos;
        _world = world;
    }

    public Matrix4 GetViewMatrix()
    {
        // Smoothly interpolate eye height for crouching
        // 1.8 Style: Instant Snap (or very fast)
        float targetEyeHeight = IsSneaking ? EyeHeightSneak : EyeHeightNormal;
        float _currentEyeHeight = targetEyeHeight; // Instant
        Vector3 eyePos = Position + new Vector3(0, _currentEyeHeight, 0);
        
        // 2. Horizontal Movement
        Vector3 front;
        front.X = (float)Math.Cos(MathHelper.DegreesToRadians(Pitch)) * (float)Math.Cos(MathHelper.DegreesToRadians(Yaw));
        front.Y = (float)Math.Sin(MathHelper.DegreesToRadians(Pitch));
        front.Z = (float)Math.Cos(MathHelper.DegreesToRadians(Pitch)) * (float)Math.Sin(MathHelper.DegreesToRadians(Yaw));
        front = Vector3.Normalize(front);

        return Matrix4.LookAt(eyePos, eyePos + front, Vector3.UnitY);
    }
    
    public Vector3 GetFront()
    {
         Vector3 front;
         front.X = (float)Math.Cos(MathHelper.DegreesToRadians(Pitch)) * (float)Math.Cos(MathHelper.DegreesToRadians(Yaw));
         front.Y = (float)Math.Sin(MathHelper.DegreesToRadians(Pitch));
         front.Z = (float)Math.Cos(MathHelper.DegreesToRadians(Pitch)) * (float)Math.Sin(MathHelper.DegreesToRadians(Yaw));
         return Vector3.Normalize(front);
    }

    public void Update(KeyboardState input, MouseState mouse, float dt)
    {
        // 1. Rotation
        Yaw += mouse.Delta.X * _sensitivity;
        Pitch -= mouse.Delta.Y * _sensitivity;
        Pitch = MathHelper.Clamp(Pitch, -89f, 89f);

        // 2. Input State
        IsSprinting = input.IsKeyDown(Keys.LeftControl);
        IsSneaking = input.IsKeyDown(Keys.LeftShift);

        // 3. Movement Wish Dir This Frame
        float speed = IsSneaking ? SneakSpeed : (IsSprinting ? SprintSpeed : WalkSpeed);
        Vector3 forward = new Vector3((float)Math.Cos(MathHelper.DegreesToRadians(Yaw)), 0, (float)Math.Sin(MathHelper.DegreesToRadians(Yaw))).Normalized();
        Vector3 right = Vector3.Cross(forward, Vector3.UnitY).Normalized();
        
        Vector3 wishDir = Vector3.Zero;
        if (input.IsKeyDown(Keys.W)) wishDir += forward;
        if (input.IsKeyDown(Keys.S)) wishDir -= forward;
        if (input.IsKeyDown(Keys.A)) wishDir -= right;
        if (input.IsKeyDown(Keys.D)) wishDir += right;
        if (wishDir.LengthSquared > 0.001f) wishDir.Normalize();

        Vector3 moveVel = wishDir * speed;
        
        // Apply to Velocity X/Z directly (instant acceleration for response feel, simplified)
        Velocity.X = moveVel.X;
        Velocity.Z = moveVel.Z;

        // Jump
        if (IsGrounded && input.IsKeyDown(Keys.Space))
        {
            Velocity.Y = JumpImpulse;
            IsGrounded = false;
        }

        // Gravity
        Velocity.Y -= Gravity * dt;

        // Apply Move
        Vector3 delta = Velocity * dt;

        // Collision & Sneak Edge Logic
        if (IsSneaking && IsGrounded)
        {
             // Check if moving would make us fall
             // This is simplified "Sneak Edge" logic
             // We check deep down?
             if (WouldFall(Position + new Vector3(delta.X, 0, 0))) delta.X = 0;
             if (WouldFall(Position + new Vector3(0, 0, delta.Z))) delta.Z = 0;
             
             // Also clamp remaining delta
             if (WouldFall(Position + new Vector3(delta.X, 0, delta.Z))) {
                 delta.X = 0; 
                 delta.Z = 0;
             }
        }
        
        // X Collision
        if (CheckCollision(Position.X + delta.X, Position.Y, Position.Z)) 
            delta.X = 0;
        
        Position += new Vector3(delta.X, 0, 0);

        // Z Collision
        if (CheckCollision(Position.X, Position.Y, Position.Z + delta.Z)) 
           delta.Z = 0;
           
        Position += new Vector3(0, 0, delta.Z);

        // Y Collision
        IsGrounded = false;
        if (CheckCollision(Position.X, Position.Y + delta.Y, Position.Z))
        {
            if (delta.Y < 0) IsGrounded = true;
            delta.Y = 0;
            Velocity.Y = 0; // Stop vertical force
            
            // Snap to block top?
             if (IsGrounded) {
                 Position = new Vector3(Position.X, (float)Math.Floor(Position.Y), Position.Z); // Basic snap
             }
        }
        
        Position += new Vector3(0, delta.Y, 0);
        
        // Ground Clamp - if we fell slightly inside a block due to float errors? 
        // Or if we are just above a block.
        if (!IsGrounded && Velocity.Y <= 0) {
            // Check immediately below
             if (CheckCollision(Position.X, Position.Y - 0.01f, Position.Z)) {
                 IsGrounded = true;
                 Velocity.Y = 0;
             }
        }
        
        // Void Respawn
        if (Position.Y < -20.0f)
        {
            Position = new Vector3(8.0f, 12.0f, 8.0f); // Reset to spawn
            Velocity = Vector3.Zero;
        }
    }

    private bool WouldFall(Vector3 newPos)
    {
        // Check if there is NO block under the bounding box at newPos
        // We scan the bottom corners of the player.
        // If ALL corners are AIR, we are falling.
        // Actually, MC sneak prevents falling if ANY part of the AABB would leave solid ground? 
        // No, it prevents moving if the center goes off? No, if the edge goes off such that you would fall.
        
        // Simple check: Look -1 below center. If Air, we are falling.
        // But sneak allows you to hang off.
        // If sneak is TRUE, we must ensure at least ONE point of our base is on something solid.
        
        float hw = Width / 2f;
        
        bool FL = IsSolid(newPos.X - hw, newPos.Y - 1, newPos.Z + hw);
        bool FR = IsSolid(newPos.X + hw, newPos.Y - 1, newPos.Z + hw);
        bool BL = IsSolid(newPos.X - hw, newPos.Y - 1, newPos.Z - hw);
        bool BR = IsSolid(newPos.X + hw, newPos.Y - 1, newPos.Z - hw);
        
        // If ALL are non-solid, we fall.
        return (!FL && !FR && !BL && !BR);
    }

    private bool CheckCollision(float x, float y, float z)
    {
        // Bounding Box check against blocks
        // Player Box: [x-w/2, x+w/2] [y, y+h] [z-w/2, z+w/2]
        
        float w = Width / 2.0f;
        float h = IsSneaking ? SneakHeight : Height;

        int minX = (int)Math.Floor(x - w);
        int maxX = (int)Math.Floor(x + w);
        int minY = (int)Math.Floor(y);
        int maxY = (int)Math.Floor(y + h);
        int minZ = (int)Math.Floor(z - w);
        int maxZ = (int)Math.Floor(z + w);

        for (int bx = minX; bx <= maxX; bx++)
        {
            for (int by = minY; by <= maxY; by++)
            {
                for (int bz = minZ; bz <= maxZ; bz++)
                {
                    Block b = _world.GetBlock(bx, by, bz);
                    if (b.IsSolid) return true;
                }
            }
        }
        return false;
    }

    private bool IsSolid(float x, float y, float z)
    {
        return _world.GetBlock((int)Math.Floor(x), (int)Math.Floor(y), (int)Math.Floor(z)).IsSolid;
    }
}
