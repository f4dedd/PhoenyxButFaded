using System;
using Godot;

namespace Pheonyx
{
	public partial class ToastNotification : Node
	{
		private static readonly PackedScene template = GD.Load<PackedScene>("res://prefabs/notification.tscn");

		private static int active_notifications = 0;

		public static async void Notify(string message, int severity = 0)
		{
			var notification = template.Instantiate<ColorRect>();
			SceneManager.Scene.AddChild(notification);
			Color color = new();

			switch (Math.Clamp(severity, 0, 2))
			{
				case 0:
					color = Color.Color8(0, 255, 0);
					break;
				case 1:
					color = Color.Color8(255, 255, 0);
					break;
				case 2:
					color = Color.Color8(255, 0, 0);
					break;
			}

			notification.GetNode<Label>("Label").Text = message;
			notification.GetNode<ColorRect>("Severity").Color = color;
			notification.Visible = true;
			notification.Position += Vector2.Up * active_notifications * (notification.Size.Y + 8);

			var inTween = notification.CreateTween();
			inTween.TweenProperty(notification, "position", notification.Position + Vector2.Left * (notification.Size.X + 8), 0.8).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
			inTween.Play();

			active_notifications++;

			await notification.ToSignal(notification.GetTree().CreateTimer(4), "timeout");

			var outTween = notification.CreateTween();
			outTween.TweenProperty(notification, "position", notification.Position + Vector2.Right * (notification.Size.X + 8), 0.8).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
			outTween.TweenCallback(Callable.From(() =>
			{
				active_notifications--;
				notification.QueueFree();
			}));
			outTween.Play();
		}
	}
}
