
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Reflection;
using Pheonyx.Maps;
using Pheonyx.Game;

namespace Pheonyx
{
	public partial class Phoenyx : Node
	{
		public struct Constants
		{
			public static readonly ulong STARTED = Time.GetTicksUsec();
			public static readonly string ROOT_FOLDER = Directory.GetCurrentDirectory();
			public static readonly string USER_FOLDER = OS.GetUserDataDir();
			public static readonly bool TEMP_MAP_MODE = false;//OS.GetCmdlineArgs().Length > 0;

			public static readonly double CURSOR_SIZE = 0.2625;
			public static readonly double GRID_SIZE = 3.0;
			public static readonly Vector2 BOUNDS = new((float)(GRID_SIZE / 2 - CURSOR_SIZE / 2), (float)(GRID_SIZE / 2 - CURSOR_SIZE / 2));
			public static readonly double HIT_BOX_SIZE = 0.07;
			public static readonly double HIT_WINDOW = 55;
			public static readonly int BREAK_TIME = 4000;  // used for skipping breaks mid-map

			public static readonly string[] DIFFICULTIES = ["N/A", "Easy", "Medium", "Hard", "Expert", "Insane"];
			public static readonly Color[] DIFFICULTY_COLORS = [Color.FromHtml("ffffff"), Color.FromHtml("00ff00"), Color.FromHtml("ffff00"), Color.FromHtml("ff0000"), Color.FromHtml("7f00ff"), Color.FromHtml("007fff")];
			public static readonly Color[] SECONDARY_DIFFICULTY_COLORS = [Color.FromHtml("808080"), Color.FromHtml("7fff7f"), Color.FromHtml("ffff7f"), Color.FromHtml("ff007f"), Color.FromHtml("ff00ff"), Color.FromHtml("007fff")];
			public static readonly Godot.Collections.Dictionary<string, double> MODS_MULTIPLIER_INCREMENT = new()
			{
				["NoFail"] = 0,
				["Ghost"] = 0.0675,
				["Spin"] = 0.18,
				["Flashlight"] = 0.1,
				["Chaos"] = 0.07,
				["HardRock"] = 0.08
			};
		}

		public struct Settings
		{
			public static bool Fullscreen { get; set; } = false;
			public static double VolumeMaster { get; set; } = 50;
			public static double VolumeMusic { get; set; } = 50;
			public static double VolumeSFX { get; set; } = 50;
			public static bool AutoplayJukebox { get; set; } = true;
			public static bool AlwaysPlayHitSound { get; set; } = false;
			public static string Skin { get; set; } = "default";
			public static double FoV { get; set; } = 70;
			public static double Sensitivity { get; set; } = 0.5;
			public static double Parallax { get; set; } = 0.1;
			public static double ApproachRate { get; set; } = 32;
			public static double ApproachDistance { get; set; } = 20;
			public static double ApproachTime { get; set; } = ApproachDistance / ApproachRate;
			public static double FadeIn { get; set; } = 15;
			public static bool FadeOut { get; set; } = true;
			public static bool Pushback { get; set; } = true;
			public static double NoteSize { get; set; } = 0.875;
			public static double CursorScale { get; set; } = 1;
			public static bool CursorTrail { get; set; } = false;
			public static double TrailTime { get; set; } = 0.05;
			public static double TrailDetail { get; set; } = 1;
			public static bool CursorDrift { get; set; } = true;
			public static double VideoDim { get; set; } = 80;
			public static double VideoRenderScale { get; set; } = 100;
			public static bool SimpleHUD { get; set; } = false;
			public static string Space { get; set; } = "skin";
			public static bool AbsoluteInput { get; set; } = false;
			public static bool RecordReplays { get; set; } = true;
			public static bool HitPopups { get; set; } = true;
			public static bool MissPopups { get; set; } = true;
			public static double FPS { get; set; } = 240;
			public static bool UnlockFPS { get; set; } = true;

			public static void Save(string profile = null)
			{
				profile ??= Util.GetProfile();

				Dictionary data = new()
				{
					["_Version"] = 1
				};

				foreach (var property in typeof(Settings).GetProperties())
				{
					data[property.Name] = (Variant)typeof(Variant).GetMethod("From").MakeGenericMethod(property.GetValue(null).GetType()).Invoke(null, [property.GetValue(null)]);
				}

				File.WriteAllText($"{Constants.USER_FOLDER}/profiles/{profile}.json", Json.Stringify(data, "\t"));
				Phoenyx.Skin.Save();
				Logger.Log($"Saved settings {profile}");
			}

			public static void Load(string profile = null)
			{
				profile ??= Util.GetProfile();

				Exception err = null;

				try
				{
					var file = Godot.FileAccess.Open($"{Constants.USER_FOLDER}/profiles/{profile}.json", Godot.FileAccess.ModeFlags.Read);
					var data = (Dictionary)Json.ParseString(file.GetAsText());

					file.Close();


					foreach (var property in typeof(Settings).GetProperties())
					{
						if (data.ContainsKey(property.Name))
						{
							property.SetValue(null, data[property.Name].GetType().GetMethod("As", BindingFlags.Public | BindingFlags.Instance).MakeGenericMethod(property.GetValue(null).GetType()).Invoke(data[property.Name], null));
						}
					}

					if (Fullscreen)
					{
						DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
					}

					ToastNotification.Notify($"Loaded profile [{profile}]");
				}
				catch (Exception exception)
				{
					err = exception;
				}

				if (!Directory.Exists($"{Constants.USER_FOLDER}/skins/{Skin}"))
				{
					Skin = "default";
					ToastNotification.Notify($"Could not find skin {Skin}", 1);
				}

				Phoenyx.Skin.Load();

				if (err != null)
				{
					ToastNotification.Notify("Settings file corrupted", 2);
					throw Logger.Error($"Settings file corrupted; {err}");
				}

				Logger.Log($"Loaded settings {profile}");
			}
		}

		public struct Skin
		{
			public static Color[] Colors { get; set; } = [Color.FromHtml("#00ffed"), Color.FromHtml("#ff8ff9")];
			public static string RawColors { get; set; } = "00ffed,ff8ff9";
			public static ImageTexture CursorImage { get; set; } = new();
			public static ImageTexture GridImage { get; set; } = new();
			public static ImageTexture PanelLeftBackgroundImage { get; set; } = new();
			public static ImageTexture PanelRightBackgroundImage { get; set; } = new();
			public static ImageTexture HealthImage { get; set; } = new();
			public static ImageTexture HealthBackgroundImage { get; set; } = new();
			public static ImageTexture ProgressImage { get; set; } = new();
			public static ImageTexture ProgressBackgroundImage { get; set; } = new();
			public static ImageTexture HitsImage { get; set; } = new();
			public static ImageTexture MissesImage { get; set; } = new();
			public static ImageTexture MissFeedbackImage { get; set; } = new();
			public static ImageTexture JukeboxPlayImage { get; set; } = new();
			public static ImageTexture JukeboxPauseImage { get; set; } = new();
			public static ImageTexture JukeboxSkipImage { get; set; } = new();
			public static ImageTexture FavoriteImage { get; set; } = new();
			public static ImageTexture ModNofailImage { get; set; } = new();
			public static ImageTexture ModSpinImage { get; set; } = new();
			public static ImageTexture ModGhostImage { get; set; } = new();
			public static ImageTexture ModChaosImage { get; set; } = new();
			public static ImageTexture ModFlashlightImage { get; set; } = new();
			public static ImageTexture ModHardrockImage { get; set; } = new();
			public static byte[] HitSoundBuffer { get; set; } = [];
			public static byte[] FailSoundBuffer { get; set; } = [];
			public static ArrayMesh NoteMesh { get; set; } = new();
			public static string Space { get; set; } = "grid";

			public static void Save()
			{
				File.WriteAllText($"{Constants.USER_FOLDER}/skins/{Settings.Skin}/colors.txt", RawColors);
				File.WriteAllText($"{Constants.USER_FOLDER}/skins/{Settings.Skin}/space.txt", Space);
				Logger.Log($"Saved skin {Settings.Skin}");
			}

			public static void Load()
			{
				RawColors = File.ReadAllText($"{Constants.USER_FOLDER}/skins/{Settings.Skin}/colors.txt").TrimSuffix(",");

				string[] split = RawColors.Split(",");
				var colors = new Color[split.Length];

				for (int i = 0; i < split.Length; i++)
				{
					split[i] = split[i].TrimPrefix("#").Substr(0, 6);
					split[i] = new Regex("[^a-fA-F0-9$]").Replace(split[i], "f");
					colors[i] = Color.FromHtml(split[i]);
				}


				Colors = colors;

				foreach (var property in typeof(Skin).GetProperties())
				{
					if (!property.Name.Contains("Image"))
					{
						continue;
					}

					property.SetValue(null, ImageTexture.CreateFromImage(Image.LoadFromFile($"{Constants.USER_FOLDER}/skins/{Settings.Skin}/{property.Name.TrimSuffix("Image").ToSnakeCase()}.png")));
				}

				Space = File.ReadAllText($"{Constants.USER_FOLDER}/skins/{Settings.Skin}/space.txt");

				if (File.Exists($"{Constants.USER_FOLDER}/skins/{Settings.Skin}/note.obj"))
				{
					NoteMesh = (ArrayMesh)Util.OBJParser.Call("load_obj", $"{Constants.USER_FOLDER}/skins/{Settings.Skin}/note.obj");
				}
				else
				{
					NoteMesh = GD.Load<ArrayMesh>($"res://skin/note.obj");
				}

				if (File.Exists($"{Constants.USER_FOLDER}/skins/{Settings.Skin}/hit.mp3"))
				{
					var file = Godot.FileAccess.Open($"{Constants.USER_FOLDER}/skins/{Settings.Skin}/hit.mp3", Godot.FileAccess.ModeFlags.Read);
					HitSoundBuffer = file.GetBuffer((long)file.GetLength());
					file.Close();
				}

				if (File.Exists($"{Constants.USER_FOLDER}/skins/{Settings.Skin}/fail.mp3"))
				{
					var file = Godot.FileAccess.Open($"{Constants.USER_FOLDER}/skins/{Settings.Skin}/fail.mp3", Godot.FileAccess.ModeFlags.Read);
					FailSoundBuffer = file.GetBuffer((long)file.GetLength());
					file.Close();
				}


				ToastNotification.Notify($"Loaded skin [{Settings.Skin}]");
				Logger.Log($"Loaded skin {Settings.Skin}");
			}
		}

		public class Stats
		{
			public static ulong GamePlaytime = 0;
			public static ulong TotalPlaytime = 0;
			public static ulong GamesOpened = 0;
			public static ulong TotalDistance = 0;
			public static ulong NotesHit = 0;
			public static ulong NotesMissed = 0;
			public static ulong HighestCombo = 0;
			public static ulong Attempts = 0;
			public static ulong Passes = 0;
			public static ulong FullCombos = 0;
			public static ulong HighestScore = 0;
			public static ulong TotalScore = 0;
			public static ulong RageQuits = 0;
			public static Array<double> PassAccuracies = [];
			public static Godot.Collections.Dictionary<string, ulong> FavouriteMaps = [];

			public static void Save()
			{
				File.SetAttributes($"{Constants.USER_FOLDER}/stats", FileAttributes.None);
				var file = Godot.FileAccess.Open($"{Constants.USER_FOLDER}/stats", Godot.FileAccess.ModeFlags.Write);
				string accuraciesJson = Json.Stringify(PassAccuracies);
				string mapsJson = Json.Stringify(FavouriteMaps);


				file.Store8(1);
				file.Store64(GamePlaytime);
				file.Store64(TotalPlaytime);
				file.Store64(GamesOpened);
				file.Store64(TotalDistance);
				file.Store64(NotesHit);
				file.Store64(NotesMissed);
				file.Store64(HighestCombo);
				file.Store64(Attempts);
				file.Store64(Passes);
				file.Store64(FullCombos);
				file.Store64(HighestScore);
				file.Store64(TotalScore);
				file.Store64(RageQuits);
				file.Store32((uint)accuraciesJson.Length);
				file.StoreString(accuraciesJson);
				file.Store32((uint)mapsJson.Length);
				file.StoreString(mapsJson);
				file.Close();

				byte[] bytes = File.ReadAllBytes($"{Constants.USER_FOLDER}/stats");
				byte[] hash = new byte[32];


				SHA256.HashData(bytes, hash);

				file = Godot.FileAccess.Open($"{Constants.USER_FOLDER}/stats", Godot.FileAccess.ModeFlags.Write);
				file.StoreBuffer(bytes);
				file.StoreBuffer(hash);
				file.Close();

				File.SetAttributes($"{Constants.USER_FOLDER}/stats", FileAttributes.Hidden);
				Logger.Log("Saved stats");
			}

			public static void Load()
			{
				try
				{
					FileParser file = new($"{Constants.USER_FOLDER}/stats");

					byte[] bytes = file.Get((int)file.Length - 32);

					file.Seek(0);

					byte version = file.Get(1)[0];

					switch (version)
					{
						case 1:
						{
							GamePlaytime = file.GetUInt64();
							TotalPlaytime = file.GetUInt64();
							GamesOpened = file.GetUInt64();
							TotalDistance = file.GetUInt64();
							NotesHit = file.GetUInt64();
							NotesMissed = file.GetUInt64();
							HighestCombo = file.GetUInt64();
							Attempts = file.GetUInt64();
							Passes = file.GetUInt64();
							FullCombos = file.GetUInt64();
							HighestScore = file.GetUInt64();
							TotalScore = file.GetUInt64();
							RageQuits = file.GetUInt64();
							PassAccuracies = (Array<double>)Json.ParseString(file.GetString((int)file.GetUInt32()));
							FavouriteMaps = (Godot.Collections.Dictionary<string, ulong>)Json.ParseString(file.GetString((int)file.GetUInt32()));

							byte[] hash = file.Get(32);
							byte[] newHash = new byte[32];

							SHA256.HashData(bytes, newHash);

							for (int i = 0; i < 32; i++)
							{
								if (hash[i] != newHash[i])
								{
									throw new("Wrong hash lol");
								}
							}

							break;
						}
					}
				}
				catch (Exception exception)
				{
					ToastNotification.Notify("Stats file corrupt or modified", 2);
					throw Logger.Error($"Stats file corrupt or modified; {exception.Message}");
				}

				Logger.Log("Loaded stats");
			}
		}

		public class Util
		{
			private static bool initialized = false;
			private static bool loaded = false;
			private static string[] user_directories = ["maps", "profiles", "skins", "replays", "pbs"];
			private static string[] skin_files = ["cursor.png", "grid.png", "health.png", "hits.png", "misses.png", "miss_feedback.png", "health_background.png", "progress.png", "progress_background.png", "panel_left_background.png", "panel_right_background.png", "jukebox_play.png", "jukebox_pause.png", "jukebox_skip.png", "favorite.png", "mod_nofail.png", "mod_spin.png", "mod_ghost.png", "mod_chaos.png", "mod_flashlight.png", "mod_hardrock.png", "hit.mp3", "fail.mp3", "colors.txt"];

			public static GodotObject DiscordRPC = (GodotObject)GD.Load<GDScript>("res://scripts/DiscordRPC.gd").New();
			public static GodotObject OBJParser = (GodotObject)GD.Load<GDScript>("res://scripts/OBJParser.gd").New();

			public static bool Quitting = false;

			public static void Setup()
			{
				if (initialized)
				{
					return;
				}

				initialized = true;

				DiscordRPC.Call("Set", "app_id", 1272588732834254878);
				DiscordRPC.Call("Set", "large_image", "short");


				if (!File.Exists($"{Constants.USER_FOLDER}/favorites.txt"))
				{
					File.WriteAllText($"{Constants.USER_FOLDER}/favorites.txt", "");
				}

				if (!Directory.Exists($"{Constants.USER_FOLDER}/cache"))
				{
					Directory.CreateDirectory($"{Constants.USER_FOLDER}/cache");
				}

				if (!Directory.Exists($"{Constants.USER_FOLDER}/cache/maps"))
				{
					Directory.CreateDirectory($"{Constants.USER_FOLDER}/cache/maps");
				}

				foreach (string cacheFile in Directory.GetFiles($"{Constants.USER_FOLDER}/cache"))
				{
					File.Delete(cacheFile);
				}

				for (int i = 0; i < user_directories.Length; i++)
				{
					string Folder = user_directories[i];

					if (!Directory.Exists($"{Constants.USER_FOLDER}/{Folder}"))
					{
						Directory.CreateDirectory($"{Constants.USER_FOLDER}/{Folder}");
					}
				}

				if (!Directory.Exists($"{Constants.USER_FOLDER}/skins/default"))
				{
					Directory.CreateDirectory($"{Constants.USER_FOLDER}/skins/default");
				}


				foreach (string skinFile in skin_files)
				{
					try
					{
						byte[] buffer = [];

						if (skinFile.GetExtension() == "txt")
						{
							var file = Godot.FileAccess.Open($"res://skin/{skinFile}", Godot.FileAccess.ModeFlags.Read);
							buffer = file.GetBuffer((long)file.GetLength());
						}
						else
						{
							var source = ResourceLoader.Load($"res://skin/{skinFile}");

							switch (source.GetType().Name)
							{
								case "CompressedTexture2D":
									buffer = (source as CompressedTexture2D).GetImage().SavePngToBuffer();
									break;
								case "AudioStreamMP3":
									buffer = (source as AudioStreamMP3).Data;
									break;
							}
						}

						if (buffer.Length == 0)
						{
							continue;
						}

						var target = Godot.FileAccess.Open($"{Constants.USER_FOLDER}/skins/default/{skinFile}", Godot.FileAccess.ModeFlags.Write);
						target.StoreBuffer(buffer);
						target.Close();
					}
					catch (Exception exception)
					{
						Logger.Log($"Couldn't copy default skin file {skinFile}; {exception}");
					}
				}

				if (!File.Exists($"{Constants.USER_FOLDER}/current_profile.txt"))
				{
					File.WriteAllText($"{Constants.USER_FOLDER}/current_profile.txt", "default");
				}

				if (!File.Exists($"{Constants.USER_FOLDER}/profiles/default.json"))
				{
					Settings.Save("default");
				}


				try
				{
					Settings.Load();
				}
				catch
				{
					Settings.Save();
				}

				if (!File.Exists($"{Constants.USER_FOLDER}/stats"))
				{
					Logger.Log("Stats file not found");
					File.WriteAllText($"{Constants.USER_FOLDER}/stats", "");
					Stats.Save();
				}


				try
				{
					Stats.Load();
				}
				catch
				{
					Stats.GamePlaytime = 0;
					Stats.TotalPlaytime = 0;
					Stats.GamesOpened = 0;
					Stats.TotalDistance = 0;
					Stats.NotesHit = 0;
					Stats.NotesMissed = 0;
					Stats.HighestCombo = 0;
					Stats.Attempts = 0;
					Stats.Passes = 0;
					Stats.FullCombos = 0;
					Stats.HighestScore = 0;
					Stats.TotalScore = 0;
					Stats.RageQuits = 0;
					Stats.PassAccuracies = [];
					Stats.FavouriteMaps = [];

					Stats.Save();
				}

				SettingsManager.UpdateSettings();
				Stats.GamesOpened++;

				List<string> import = [];

				foreach (string file in Directory.GetFiles($"{Constants.USER_FOLDER}/maps"))
				{
					if (file.GetExtension() == "sspm" || file.GetExtension() == "txt")
					{
						import.Add(file);
					}
				}

				MapParser.BulkImport([.. import]);

				loaded = true;
			}

			public static void Quit()
			{
				if (Quitting)
				{
					return;
				}

				Quitting = true;

				if (!Runner.CurrentAttempt.IsReplay)
				{
					Runner.CurrentAttempt.Stop();
				}

				Stats.TotalPlaytime += (Time.GetTicksUsec() - Constants.STARTED) / 1000000;

				if (loaded)
				{
					Settings.Save();
					Stats.Save();
				}

				if (File.Exists($"{Constants.USER_FOLDER}/maps/NA_tempmap.phxm"))
				{
					File.Delete($"{Constants.USER_FOLDER}/maps/NA_tempmap.phxm");
				}


				DiscordRPC.Call("Set", "end_timestamp", 0);
				DiscordRPC.Call("Clear");

				if (SceneManager.Scene.Name == "SceneMenu")
				{
					var tween = SceneManager.Scene.CreateTween();
					tween.TweenProperty(SceneManager.Scene, "modulate", Color.Color8(1, 1, 1, 0), 0.5).SetTrans(Tween.TransitionType.Quad);
					tween.TweenCallback(Callable.From(() =>
					{
						SceneManager.Scene.GetTree().Quit();
					}));
					tween.Play();
				}
				else
				{
					SceneManager.Scene.GetTree().Quit();
				}
			}

			public static string GetProfile()
			{
				return File.ReadAllText($"{Constants.USER_FOLDER}/current_profile.txt");
			}

			public static ImageTexture GetModIcon(string mod)
			{
				ImageTexture tex = new();

				switch (mod)
				{
					case "NoFail":
						tex = Skin.ModNofailImage;
						break;
					case "Spin":
						tex = Skin.ModSpinImage;
						break;
					case "Ghost":
						tex = Skin.ModGhostImage;
						break;
					case "Chaos":
						tex = Skin.ModChaosImage;
						break;
					case "Flashlight":
						tex = Skin.ModFlashlightImage;
						break;
					case "HardRock":
						tex = Skin.ModHardrockImage;
						break;
				}

				return tex;
			}
		}

		public override void _Notification(int what)
		{
			if (what == NotificationWMCloseRequest)
			{
				if (SceneManager.Scene.Name == "SceneGame")
				{
					Stats.RageQuits++;
				}

				Util.Quit();
			}
		}
	}
}
