using Godot;

namespace Pheonyx
{
	public partial class SceneManager : Node
	{
		private static Node node;
		private static bool skip_next_transition = false;

		public static Node Scene;

		public override void _Ready()
		{
			node = this;

			node.GetTree().Connect("node_added", Callable.From((Node child) =>
			{
				if (child.Name != "SceneMenu" && child.Name != "SceneGame" && child.Name != "SceneResults")
				{
					return;
				}

				Scene = child;

				if (skip_next_transition)
				{
					skip_next_transition = false;
					return;
				}

				var inTransition = Scene.GetNode<ColorRect>("Transition");
				inTransition.SelfModulate = Color.FromHtml("ffffffff");
				var inTween = inTransition.CreateTween();
				inTween.TweenProperty(inTransition, "self_modulate", Color.FromHtml("ffffff00"), 0.25).SetTrans(Tween.TransitionType.Quad);
				inTween.Play();
			}));
		}

		public static void Load(string path, bool skipTransition = false)
		{
			if (skipTransition)
			{
				skip_next_transition = true;
				node.GetTree().ChangeSceneToFile(path);
			}
			else
			{
				var outTransition = Scene.GetNode<ColorRect>("Transition");
				var outTween = outTransition.CreateTween();
				outTween.TweenProperty(outTransition, "self_modulate", Color.FromHtml("ffffffff"), 0.25).SetTrans(Tween.TransitionType.Quad);
				outTween.TweenCallback(Callable.From(() =>
				{
					node.GetTree().ChangeSceneToFile(path);
				}));
				outTween.Play();
			}
		}
	}
}
