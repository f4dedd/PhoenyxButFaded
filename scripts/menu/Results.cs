using Godot;
using Pheonyx.Game;
using Pheonyx.Maps;
using System;
using System.IO;

namespace Pheonyx.Menu
{
	public partial class Results : Control
	{
		private static TextureRect cursor;
		private static Panel footer;
		private static Panel holder;
		private static TextureRect cover;

		public static double LastFrame = 0;
		public static Vector2 MousePosition = Vector2.Zero;

		public override void _Ready()
		{
			cursor = GetNode<TextureRect>("Cursor");
			footer = GetNode<Panel>("Footer");
			holder = GetNode<Panel>("Holder");
			cover = GetNode<TextureRect>("Cover");

			Input.MouseMode = Input.MouseModeEnum.Hidden;
			DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Mailbox);

			cursor.Texture = Phoenyx.Skin.CursorImage;
			cursor.Size = new Vector2(32 * (float)Phoenyx.Settings.CursorScale, 32 * (float)Phoenyx.Settings.CursorScale);

			holder.GetNode<Label>("Title").Text = (Runner.CurrentAttempt.IsReplay ? "[REPLAY] " : "") + Runner.CurrentAttempt.Map.PrettyTitle;
			holder.GetNode<Label>("Difficulty").Text = Runner.CurrentAttempt.Map.DifficultyName;
			holder.GetNode<Label>("Mappers").Text = $"by {Runner.CurrentAttempt.Map.PrettyMappers}";
			holder.GetNode<Label>("Accuracy").Text = $"{Runner.CurrentAttempt.Accuracy.ToString().PadDecimals(2)}%";
			holder.GetNode<Label>("Score").Text = $"{Lib.String.PadMagnitude(Runner.CurrentAttempt.Score.ToString())}";
			holder.GetNode<Label>("Hits").Text = $"{Lib.String.PadMagnitude(Runner.CurrentAttempt.Hits.ToString())} / {Lib.String.PadMagnitude(Runner.CurrentAttempt.Sum.ToString())}";
			holder.GetNode<Label>("Status").Text = Runner.CurrentAttempt.IsReplay ? Runner.CurrentAttempt.Replays[0].Status : Runner.CurrentAttempt.Alive ? (Runner.CurrentAttempt.Qualifies ? "PASSED" : "DISQUALIFIED") : "FAILED";
			holder.GetNode<Label>("Speed").Text = $"{Runner.CurrentAttempt.Speed.ToString().PadDecimals(2)}x";

			var modifiersContainer = holder.GetNode("Modifiers").GetNode<HBoxContainer>("HBoxContainer");
			var modTemplate = modifiersContainer.GetNode<TextureRect>("ModifierTemplate");

			foreach (var mod in Runner.CurrentAttempt.Mods)
			{
				if (mod.Value)
				{
					var icon = modTemplate.Duplicate() as TextureRect;

					icon.Visible = true;
					icon.Texture = Phoenyx.Util.GetModIcon(mod.Key);

					modifiersContainer.AddChild(icon);
				}
			}

			if (Runner.CurrentAttempt.Map.CoverBuffer != null)
			{
				var file = Godot.FileAccess.Open($"{Phoenyx.Constants.USER_FOLDER}/cache/cover.png", Godot.FileAccess.ModeFlags.Write);
				file.StoreBuffer(Runner.CurrentAttempt.Map.CoverBuffer);
				file.Close();

				cover.Texture = ImageTexture.CreateFromImage(Image.LoadFromFile($"{Phoenyx.Constants.USER_FOLDER}/cache/cover.png"));
				GetNode<TextureRect>("CoverBackground").Texture = cover.Texture;
			}

			if (Runner.CurrentAttempt.Map.AudioBuffer != null)
			{
				if (!SoundManager.Song.Playing)
				{
					SoundManager.Song.Play();
				}
			}

			SoundManager.Song.PitchScale = (float)Runner.CurrentAttempt.Speed;

			if (!Runner.CurrentAttempt.Map.Ephemeral)
			{
				SoundManager.JukeboxIndex = SoundManager.JukeboxQueueInverse[Runner.CurrentAttempt.Map.ID];
			}

			var replayButton = footer.GetNode<Button>("Replay");

			footer.GetNode<Button>("Back").Pressed += Stop;
			footer.GetNode<Button>("Play").Pressed += Replay;
			replayButton.Visible = !Runner.CurrentAttempt.Map.Ephemeral;
			replayButton.Pressed += () =>
			{
				string path;

				if (Runner.CurrentAttempt.IsReplay)
				{
					path = $"{Phoenyx.Constants.USER_FOLDER}/replays/{Runner.CurrentAttempt.Replays[0].ID}.phxr";
				}
				else
				{
					path = Runner.CurrentAttempt.ReplayFile.GetPath();
				}

				if (File.Exists(path))
				{
					Replay replay = new(path);
					SoundManager.Song.Stop();
					SceneManager.Load("res://scenes/game.tscn");
					Runner.Play(MapParser.Decode(replay.MapFilePath), replay.Speed, replay.StartFrom, replay.Modifiers, null, [replay]);
				}
			};
		}

		public override void _Process(double delta)
		{
			ulong now = Time.GetTicksUsec();
			delta = (now - LastFrame) / 1000000;
			LastFrame = now;

			cursor.Position = MousePosition - new Vector2(cursor.Size.X / 2, cursor.Size.Y / 2);
			holder.Position = holder.Position.Lerp((Size / 2 - MousePosition) * (8 / Size.Y), Math.Min(1, (float)delta * 16));
		}

		public override void _Input(InputEvent @event)
		{
			if (@event is InputEventKey eventKey && eventKey.Pressed)
			{
				switch (eventKey.PhysicalKeycode)
				{
					case Key.Escape:
						Stop();
						break;
					case Key.Quoteleft:
						Replay();
						break;
				}
			}
			else if (@event is InputEventMouseMotion eventMouseMotion)
			{
				MousePosition = eventMouseMotion.Position;
			}
			else if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.Pressed)
			{
				switch (eventMouseButton.ButtonIndex)
				{
					case MouseButton.Xbutton1:
						Stop();
						break;
				}
			}
		}

		public static void UpdateVolume()
		{
			SoundManager.Song.VolumeDb = -80 + 70 * (float)Math.Pow(Phoenyx.Settings.VolumeMusic / 100, 0.1) * (float)Math.Pow(Phoenyx.Settings.VolumeMaster / 100, 0.1);
		}

		public static void Replay()
		{
			var map = MapParser.Decode(Runner.CurrentAttempt.Map.FilePath);
			map.Ephemeral = Runner.CurrentAttempt.Map.Ephemeral;
			SoundManager.Song.Stop();
			SceneManager.Load("res://scenes/game.tscn");
			Runner.Play(map, Runner.CurrentAttempt.Speed, Runner.CurrentAttempt.StartFrom, Runner.CurrentAttempt.Mods);
		}

		public static void Stop()
		{
			SceneManager.Load("res://scenes/main_menu.tscn");
		}
	}
}
