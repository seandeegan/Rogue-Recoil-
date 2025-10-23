using System;
using Sandbox;

// run after normal simulation so eye angles are final
public sealed class FPArmsFollow : Component
{
    [Property] public PlayerController PlayerController { get; set; }

    [Property] public float RotationSmoothness { get; set; } = 10f;
    [Property] public float PositionSmoothness { get; set; } = 16f;

    // Camera-space placement
    [Property] public float ForwardDistance { get; set; } = 6f;  // how far in front of the eye
    [Property] public float UpOffset       { get; set; } = -2f;  // slightly down so you see the arms
    [Property] public float RightOffset    { get; set; } = 0.5f; // small right bias if you like

    protected override void OnUpdate()
    {
        if (PlayerController == null)
            return;

        // Camera basis
        var eyeAngles = PlayerController.EyeAngles;
        var eyeRot    = Rotation.From( eyeAngles );
        var eyePos    = PlayerController.EyePosition;

        // Camera-space offset -> world
        // Keep a fixed radius around the eye in all directions (pitch and yaw)
        var targetPos =
              eyePos
            + eyeRot.Forward * ForwardDistance
            + eyeRot.Up      * UpOffset
            + eyeRot.Right   * RightOffset;

        // Smooth rotation and position
        float rotLerp = 1f - MathF.Exp( -RotationSmoothness * Time.Delta );
        float posLerp = 1f - MathF.Exp( -PositionSmoothness * Time.Delta );

        WorldRotation = Rotation.Lerp( WorldRotation, eyeRot, rotLerp );
        WorldPosition = Vector3.Lerp( WorldPosition, targetPos, posLerp );
    }
}
