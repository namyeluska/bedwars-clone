using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

public class Camera
{
    public Vector3 Position { get; set; }
    public Vector3 Front { get; private set; } = -Vector3.UnitZ;
    public Vector3 Up { get; private set; } = Vector3.UnitY;
    public Vector3 Right { get; private set; } = Vector3.UnitX;

    private float _pitch;
    private float _yaw = -90f;
    private float _sensitivity = 0.2f;

    public Camera(Vector3 startPosition)
    {
        Position = startPosition;
        UpdateVectors();
    }

    public Matrix4 GetViewMatrix() => Matrix4.LookAt(Position, Position + Front, Up);

    private float _verticalVelocity;
    private const float Gravity = 32.0f;
    private const float JumpImpulse = 8.0f;
    private const float WalkSpeed = 4.317f;
    private const float SprintSpeed = 5.612f;
    private const float CrouchSpeed = 1.31f;
    
    // Height settings
    private const float GroundLevel = 0.5f; // Top of our 3x3 block platform
    private const float StandingEyeHeight = 1.62f;
    private const float CrouchingEyeHeight = 1.27f;
    private float _currentEyeHeight = StandingEyeHeight;

    public void Update(KeyboardState input, float deltaX, float deltaY, float time)
    {
        // Mouse Look
        _yaw += deltaX * _sensitivity;
        _pitch -= deltaY * _sensitivity;
        _pitch = MathHelper.Clamp(_pitch, -89f, 89f);

        UpdateVectors();

        // --- Movement & Mechanics ---
        bool isSprinting = input.IsKeyDown(Keys.LeftControl);
        bool isCrouching = input.IsKeyDown(Keys.LeftShift);

        // 1. Determine Speed & Target Height
        float currentSpeed = WalkSpeed;
        float targetEyeHeight = StandingEyeHeight;

        if (isCrouching)
        {
            currentSpeed = CrouchSpeed;
            targetEyeHeight = CrouchingEyeHeight;
        }
        else if (isSprinting)
        {
            currentSpeed = SprintSpeed;
        }

        // Smoothly interpolate eye height for crouching
        _currentEyeHeight = MathHelper.Lerp(_currentEyeHeight, targetEyeHeight, 10f * time);

        // 2. Horizontal Movement
        float velocity = currentSpeed * time;
        Vector3 horizontalFront = new Vector3(Front.X, 0f, Front.Z).Normalized();
        Vector3 horizontalRight = new Vector3(Right.X, 0f, Right.Z).Normalized();

        Vector3 potentialPosition = Position;

        if (input.IsKeyDown(Keys.W)) potentialPosition += horizontalFront * velocity;
        if (input.IsKeyDown(Keys.S)) potentialPosition -= horizontalFront * velocity;
        if (input.IsKeyDown(Keys.A)) potentialPosition -= horizontalRight * velocity;
        if (input.IsKeyDown(Keys.D)) potentialPosition += horizontalRight * velocity;

        // Platform bounds: -0.5 to 9.5.
        // Bridging: Allow overhang. Blocks effectively become wider for physics when crouching?
        // Or we just allow the player center to go slightly off.
        // Let's define "Safe Platform" as the area where you don't fall.
        // If crouching, this area is wider.
        
        float margin = isCrouching ? 0.7f : 0.0f; // Allow 0.7 units overhang when crouching (half block + bit more)
        
        bool IsOverPlatform(Vector3 pos, float bridgeMargin) {
            return pos.X >= -0.5f - bridgeMargin && pos.X <= 9.5f + bridgeMargin && 
                   pos.Z >= -0.5f - bridgeMargin && pos.Z <= 9.5f + bridgeMargin;
        }

        // Safe Sneak: Prevent moving off the "Bridging Safe Zone"
        if (isCrouching && IsOverPlatform(Position, margin)) 
        {
            if (!IsOverPlatform(potentialPosition, margin))
            {
                // Stop at the edge of the overhang
                potentialPosition = Position; 
            }
        }

        Position = potentialPosition;

        // 3. Physics & Jumping
        // "Feet" position logic
        float currentFeetY = Position.Y - _currentEyeHeight;

        // Apply Gravity
        _verticalVelocity -= Gravity * time;
        currentFeetY += _verticalVelocity * time;

        // Ground Check - Use same margin logic!
        // If you are 'bridging' (hanging off edge), you should still be grounded.
        bool isGrounded = false;
        
        // We use the same loose margin for ground check if crouching, essentially simulating 
        // that the player is "holding on" with their feet.
        // Actually, for Minecraft bridging, you just don't fall off. 
        // So the ground collision needs to extend.
        
        bool onSolidGround = IsOverPlatform(Position, isCrouching ? 0.7f : 0.0f);

        if (onSolidGround && currentFeetY <= GroundLevel)
        {
            currentFeetY = GroundLevel;
            _verticalVelocity = 0;
            isGrounded = true;
        }

        // Jump
        if (isGrounded && input.IsKeyDown(Keys.Space))
        {
            _verticalVelocity = JumpImpulse;
            // Lift off slightly to avoid sticking
            currentFeetY += 0.01f; 
        }

        // Update Camera Position
        Position = new Vector3(Position.X, currentFeetY + _currentEyeHeight, Position.Z);
    }

    private void UpdateVectors()
    {
        Front = new Vector3(
            (float)Math.Cos(MathHelper.DegreesToRadians(_pitch)) * (float)Math.Cos(MathHelper.DegreesToRadians(_yaw)),
            (float)Math.Sin(MathHelper.DegreesToRadians(_pitch)),
            (float)Math.Cos(MathHelper.DegreesToRadians(_pitch)) * (float)Math.Sin(MathHelper.DegreesToRadians(_yaw))
        ).Normalized();
        Right = Vector3.Cross(Front, Vector3.UnitY).Normalized();
        Up = Vector3.Cross(Right, Front).Normalized();
    }
}