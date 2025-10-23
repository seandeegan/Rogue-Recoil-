using Sandbox;

public sealed class SmoothPlayerCamera : Component
{
    [Property] public CameraComponent Camera { get; set; }
    [Property] public float Sensitivity { get; set; } = 2.5f;
    [Property] public float SmoothSpeed { get; set; } = 12f;
    [Property] public float PitchClamp { get; set; } = 85f;
    [Property] public float EyeHeight { get; set; } = 64f;

    private float yaw;
    private float pitch;
    private Vector3 targetPos;

    protected override void OnStart()
    {
        yaw = WorldRotation.Yaw();

        if (Camera == null)
            Log.Warning("SmoothPlayerCamera: No Camera assigned!");
    }

    private void OnLateUpdate()
    {
        if (Camera == null) return;

        var look = Input.AnalogLook;

        // Correct FPS-style pitch
        pitch += look.pitch * Sensitivity;
        pitch = pitch.Clamp(-PitchClamp, PitchClamp);
        yaw += look.yaw * Sensitivity;

        // Construct rotations
        var bodyRot = Rotation.FromYaw(yaw);
        var camRot = Rotation.FromPitch(pitch) * bodyRot;

        // Instantly rotate player (tight FPS feel)
        WorldRotation = bodyRot;

        // Smoothly follow position for slight camera stability
        targetPos = WorldPosition + Vector3.Up * EyeHeight;
        Camera.WorldPosition = Vector3.Lerp(Camera.WorldPosition, targetPos, Time.Delta * SmoothSpeed);

        // Snap rotation directly for responsive look
        Camera.WorldRotation = camRot;


    }

    public Rotation GetCameraRotation() => Camera.WorldRotation;
    public float GetPitch() => pitch;
    public float GetYaw() => yaw;
}
