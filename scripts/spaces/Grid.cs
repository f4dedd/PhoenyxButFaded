using Godot;
using Pheonyx.Game;

namespace Pheonyx.Spaces
{
    public partial class Grid : Node3D
    {
        private double lastFrame = Time.GetTicksUsec();
        private StandardMaterial3D tileMaterial;
        private Environment environment;

        public Color Colour = Color.Color8(255, 255, 255);

        public override void _Ready()
        {
            tileMaterial = (GetNode<MeshInstance3D>("Top").Mesh as PlaneMesh).Material as StandardMaterial3D;
            environment = GetNode<WorldEnvironment>("WorldEnvironment").Environment;
        }

        public override void _Process(double delta)
        {
            ulong now = Time.GetTicksUsec();
            delta = (now - lastFrame) / 1000000;
            lastFrame = now;
            Colour = Colour.Lerp(Runner.CurrentAttempt.LastHitColour, (float)delta * 8);

            tileMaterial.AlbedoColor = Colour;
            tileMaterial.Uv1Offset += Vector3.Up * (float)delta * 3;
        }
    }
}
