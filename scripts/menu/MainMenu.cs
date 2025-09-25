
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Godot;
using Pheonyx.Game;
using Pheonyx.Maps;
using Pheonyx.Multiplayer;

namespace Pheonyx.Menu
{
	public partial class MainMenu : Control
	{
		public static Control Control;
		public static TextureRect Cursor;

		private static readonly PackedScene chat_message = GD.Load<PackedScene>("res://prefabs/chat_message.tscn");
		private static readonly PackedScene map_button = GD.Load<PackedScene>("res://prefabs/map_button.tscn");
		private static readonly PackedScene leaderboard_score = GD.Load<PackedScene>("res://prefabs/leaderboard_score.tscn");

		private static Panel topBar;
		private static ColorRect background;
		private static Node[] backgroundTiles;
		private static Panel menus;
		private static Panel main;
		private static Panel jukebox;
		private static Button jukeboxButton;
		private static ColorRect jukeboxProgress;
		private static HBoxContainer jukeboxSpectrum;
		private static ColorRect[] jukeboxSpectrumBars;
		private static AudioEffectSpectrumAnalyzerInstance audioSpectrum;
		private static Panel contextMenu;
		private static TextureRect peruchor;
		private static ShaderMaterial mainBackgroundMaterial;

		private static Panel playMenu;
		private static Panel subTopBar;
		private static Button importButton;
		private static Button userFolderButton;
		private static Button settingsButton;
		private static LineEdit searchEdit;
		private static LineEdit searchAuthorEdit;
		private static FileDialog importDialog;
		private static ScrollContainer mapList;
		private static VBoxContainer mapListContainer;
		private static Panel leaderboardPanel;
		private static VBoxContainer leaderboardContainer;
		private static Panel modifiersPanel;
		private static Panel speedPanel;
		private static HSlider speedSlider;
		private static LineEdit speedEdit;
		private static Panel startFromPanel;
		private static HSlider startFromSlider;
		private static LineEdit startFromEdit;
		private static List<TextureButton> modifierButtons;

		private static Panel extras;

		private static Panel multiplayerHolder;
		private static LineEdit ipLine;
		private static LineEdit portLine;
		private static LineEdit chatLine;
		private static Button host;
		private static Button join;
		private static ScrollContainer chatScrollContainer;
		private static VBoxContainer chatHolder;

		private static bool initialized = false;
		private static bool firstFrame = true;
		private static Vector2I windowSize = DisplayServer.WindowGetSize();
		private static double lastFrame = Time.GetTicksUsec();
		private static float scroll = 0;
		private static float targetScroll = 0;
		private static int maxScroll = 0;
		private static Vector2 mousePosition = Vector2.Zero;
		private static bool rightMouseHeld = false;
		private static bool rightClickingButton = false;
		private static List<string> loadedMaps = [];
		private static Dictionary<string, int> mapsOrder = [];
		private static Dictionary<Panel, bool> favoriteMaps = [];
		private static TextureRect[] favoriteMapsTextures = [];
		private static int visibleMaps = 0;
		private static string selectedMapID = null;
		private static Map selectedMap = new();
		private static string currentMenu = "Main";
		private static string lastMenu = currentMenu;
		private static string searchTitle = "";
		private static string searchAuthor = "";
		private static string contextMenuTarget;
		private static Map currentMap;
		private static int passedNotes = 0;
		private static int peruSequenceIndex = 0;
		private static readonly string[] peru_sequence = ["P", "E", "R", "U"];

		public override void _Ready()
		{
			Control = this;


			Phoenyx.Util.Setup();

			Phoenyx.Util.DiscordRPC.Call("Set", "details", "Main Menu");
			Phoenyx.Util.DiscordRPC.Call("Set", "state", "");
			Phoenyx.Util.DiscordRPC.Call("Set", "end_timestamp", 0);

			Input.MouseMode = Input.MouseModeEnum.Hidden;
			DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Mailbox);

			GetTree().AutoAcceptQuit = false;
			windowSize = DisplayServer.WindowGetSize();
			visibleMaps = 0;
			firstFrame = true;


			if (!initialized)
			{
				initialized = true;
				SoundManager.JukeboxPlayed += (Map map) =>
				{
					passedNotes = 0;
					currentMap = map;
				};

				var viewport = GetViewport();
				viewport.SizeChanged += () =>
				{
					if (SceneManager.Scene.Name != "SceneMenu")
					{
						return;
					}

					windowSize = DisplayServer.WindowGetSize();
					UpdateMaxScroll();
					targetScroll = Math.Clamp(targetScroll, 0, maxScroll);
					UpdateSpectrumSpacing();
				};
				viewport.Connect("files_dropped", Callable.From((string[] files) =>
				{
					Import(files);

					foreach (string file in files)
					{
						switch (file.GetExtension())
						{
							case "phxr":
								List<Replay> replays = [];
								List<Replay> matching = [];

								for (int i = 0; i < files.Length; i++)
								{
									Replay replay = new(files[i]);


									if (!replay.Valid)
									{
										continue;
									}

									replays.Add(replay);
								}

								if (replays.Count == 0)
								{
									ToastNotification.Notify("No valid replays", 2);
									return;
								}

								foreach (var replay in replays)
								{
									if (replay == replays[0])
									{
										matching.Add(replay);
									}
									else
									{
										ToastNotification.Notify("Replay doesn't match first", 1);
										Logger.Log($"Replay {replay} doesn't match {replays[0]}");
									}
								}

								if (Runner.Playing)
								{
									Runner.QueueStop();
								}


								SoundManager.Song.Stop();
								SceneManager.Load("res://scenes/game.tscn");
								Runner.Play(MapParser.Decode(matching[0].MapFilePath), matching[0].Speed, matching[0].StartFrom, matching[0].Modifiers, null, [.. matching]);
								break;
						}
					}
				}));

				//string[] args = OS.GetCmdlineArgs();

				string[] args = [
					"--a=\"H:\\Sound Space\\Quantum_Editors\\Sound Space Quantum Editor\\cached\\Camellia feat. Nanahira - べィスドロップ・フリークス (2018 Redrop ver.).asset\"",
					"--t=\"H:\\Sound Space\\Quantum_Editors\\Sound Space Quantum Editor\\assets\\temp\\tempmap.txt\""
				];


				if (args.Length > 0)
				{
					string audioString = "";
					string mapString = "";

					foreach (string arg in args)
					{
						switch (arg.Substr(0, 3))
						{
							case "--a":
								audioString = arg.Substr(5, arg.Length - 1).Trim('"');
								break;
							case "--t":
								mapString = arg.Substr(5, arg.Length - 1).Trim('"');
								break;
						}
					}

					//Select("tempmap", true, false);

					//Map map = MapParser.Decode(mapString, audioString, false, false);
					//map.Ephemeral = true;
					//SoundManager.Song.Stop();
					//SceneManager.Load("res://scenes/game.tscn");
				}
			}

			// General


			Cursor = GetNode<TextureRect>("Cursor");
			topBar = GetNode<Panel>("TopBar");
			background = GetNode<ColorRect>("Background");
			backgroundTiles = background.GetNode("TileHolder").GetChildren().ToArray();
			menus = GetNode<Panel>("Menus");
			main = menus.GetNode<Panel>("Main");
			extras = menus.GetNode<Panel>("Extras");
			jukebox = GetNode<Panel>("Jukebox");
			jukeboxButton = jukebox.GetNode<Button>("Button");
			jukeboxProgress = jukebox.GetNode("Progress").GetNode<ColorRect>("Main");
			jukeboxSpectrum = jukebox.GetNode<HBoxContainer>("Spectrum");
			audioSpectrum = (AudioEffectSpectrumAnalyzerInstance)AudioServer.GetBusEffectInstance(0, 0);
			contextMenu = GetNode<Panel>("ContextMenu");
			peruchor = main.GetNode<TextureRect>("Peruchor");
			mainBackgroundMaterial = background.Material as ShaderMaterial;
			loadedMaps = [];
			favoriteMaps = [];

			Cursor.Texture = Phoenyx.Skin.CursorImage;
			Cursor.Size = new Vector2(32 * (float)Phoenyx.Settings.CursorScale, 32 * (float)Phoenyx.Settings.CursorScale);

			var jukeboxBars = jukeboxSpectrum.GetChildren();

			jukeboxSpectrumBars = new ColorRect[jukeboxBars.Count];

			for (int i = 0; i < jukeboxBars.Count; i++)
			{
				jukeboxSpectrumBars[i] = jukeboxBars[i].GetNode<ColorRect>("Main");
			}

			var buttons = main.GetNode<VBoxContainer>("Buttons");

			buttons.GetNode<Button>("Play").Pressed += () =>
			{
				transition("Play");
			};
			buttons.GetNode<Button>("Settings").Pressed += () =>
			{
				SettingsManager.ShowSettings();
			};
			buttons.GetNode<Button>("Extras").Pressed += () =>
			{
				foreach (Panel holder in extras.GetNode("Stats").GetNode("ScrollContainer").GetNode("VBoxContainer").GetChildren())
				{
					string value = "";

					switch (holder.Name)
					{
						case "GamePlaytime":
							value = $"{Math.Floor((double)Phoenyx.Stats.GamePlaytime / 36) / 100} h";
							break;
						case "TotalPlaytime":
							value = $"{Math.Floor((double)Phoenyx.Stats.TotalPlaytime / 36) / 100} h";
							break;
						case "GamesOpened":
							value = Lib.String.PadMagnitude(Phoenyx.Stats.GamesOpened.ToString());
							break;
						case "TotalDistance":
							value = $"{Lib.String.PadMagnitude(((double)Phoenyx.Stats.TotalDistance / 1000).ToString())} m";
							break;
						case "NotesHit":
							value = Lib.String.PadMagnitude(Phoenyx.Stats.NotesHit.ToString());
							break;
						case "NotesMissed":
							value = Lib.String.PadMagnitude(Phoenyx.Stats.NotesMissed.ToString());
							break;
						case "HighestCombo":
							value = Lib.String.PadMagnitude(Phoenyx.Stats.HighestCombo.ToString());
							break;
						case "Attempts":
							value = Lib.String.PadMagnitude(Phoenyx.Stats.Attempts.ToString());
							break;
						case "Passes":
							value = Lib.String.PadMagnitude(Phoenyx.Stats.Passes.ToString());
							break;
						case "FullCombos":
							value = Lib.String.PadMagnitude(Phoenyx.Stats.FullCombos.ToString());
							break;
						case "HighestScore":
							value = Lib.String.PadMagnitude(Phoenyx.Stats.HighestScore.ToString());
							break;
						case "TotalScore":
							value = Lib.String.PadMagnitude(Phoenyx.Stats.TotalScore.ToString());
							break;
						case "AverageAccuracy":
							double sum = 0;

							foreach (double accuracy in Phoenyx.Stats.PassAccuracies)
							{
								sum += accuracy;
							}

							value = $"{(Phoenyx.Stats.PassAccuracies.Count == 0 ? 0 : Math.Floor(sum / Phoenyx.Stats.PassAccuracies.Count * 100) / 100).ToString().PadDecimals(2)}%";
							break;
						case "RageQuits":
							value = Lib.String.PadMagnitude(Phoenyx.Stats.RageQuits.ToString());
							break;
						case "FavouriteMap":
							string mostPlayedID = null;
							ulong mostPlayedCount = 0;

							foreach (var entry in Phoenyx.Stats.FavouriteMaps)
							{
								if (entry.Value > mostPlayedCount)
								{
									mostPlayedID = entry.Key;
									mostPlayedCount = entry.Value;
								}
							}

							value = mostPlayedID != null ? MapParser.Decode($"{Phoenyx.Constants.USER_FOLDER}/maps/{mostPlayedID}.phxm").PrettyTitle : "None";
							break;
					}

					holder.GetNode<Label>("Value").Text = value;
				}

				transition("Extras");
			};
			buttons.GetNode<Button>("Quit").Pressed += () =>
			{
				Control.GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
			};

			topBar.GetNode<TextureButton>("Discord").Pressed += () =>
			{
				OS.ShellOpen("https://discord.gg/aSyC7btWDX");
			};

			jukeboxButton.MouseEntered += () =>
			{
				var title = jukebox.GetNode<Label>("Title");
				var tween = title.CreateTween();
				tween.TweenProperty(title, "modulate", Color.Color8(255, 255, 255), 0.25).SetTrans(Tween.TransitionType.Quad);
				tween.Play();
			};
			jukeboxButton.MouseExited += () =>
			{
				var title = jukebox.GetNode<Label>("Title");
				var tween = title.CreateTween();
				tween.TweenProperty(title, "modulate", Color.Color8(194, 194, 194), 0.25).SetTrans(Tween.TransitionType.Quad);
				tween.Play();
			};
			jukeboxButton.Pressed += () =>
			{
				string fileName = SoundManager.JukeboxQueue[SoundManager.JukeboxIndex].GetFile().GetBaseName();
				var mapButton = mapListContainer.GetNode<Panel>(fileName);
				targetScroll = Math.Clamp(mapButton.Position.Y + mapButton.Size.Y - windowSize.Y / 2, 0, maxScroll);
			};

			foreach (var child in jukebox.GetChildren())
			{
				if (child.GetType().Name != "TextureButton")
				{
					continue;
				}

				var button = child as TextureButton;

				button.MouseEntered += () =>
				{
					var tween = button.CreateTween();
					tween.TweenProperty(button, "self_modulate", Color.Color8(255, 255, 255), 0.25).SetTrans(Tween.TransitionType.Quad);
					tween.Play();
				};
				button.MouseExited += () =>
				{
					var tween = button.CreateTween();
					tween.TweenProperty(button, "self_modulate", Color.Color8(194, 194, 194), 0.25).SetTrans(Tween.TransitionType.Quad);
					tween.Play();
				};
				button.Pressed += () =>
				{
					switch (button.Name)
					{
						case "Pause":
							SoundManager.JukeboxPaused = !SoundManager.JukeboxPaused;
							SoundManager.Song.PitchScale = SoundManager.JukeboxPaused ? 0.00000000001f : (float)Lobby.Speed;    // bruh

							UpdateJukeboxButtons();
							break;
						case "Skip":
							SoundManager.JukeboxIndex++;
							SoundManager.PlayJukebox();
							break;
						case "Rewind":
							ulong now = Time.GetTicksMsec();

							if (now - SoundManager.LastRewind < 1000)
							{
								SoundManager.JukeboxIndex--;
								SoundManager.PlayJukebox();
							}
							else
							{
								SoundManager.Song.Seek(0);
							}

							SoundManager.LastRewind = now;
							break;
					}
				};
			}

			// Map selection


			playMenu = menus.GetNode<Panel>("Play");
			subTopBar = playMenu.GetNode<Panel>("SubTopBar");
			importButton = subTopBar.GetNode<Button>("Import");
			userFolderButton = subTopBar.GetNode<Button>("UserFolder");
			settingsButton = subTopBar.GetNode<Button>("Settings");
			searchEdit = subTopBar.GetNode<LineEdit>("Search");
			searchAuthorEdit = subTopBar.GetNode<LineEdit>("SearchAuthor");
			importDialog = GetNode<FileDialog>("ImportDialog");

			mapList = playMenu.GetNode<ScrollContainer>("MapList");
			mapListContainer = mapList.GetNode<VBoxContainer>("Container");
			leaderboardPanel = playMenu.GetNode<Panel>("Leaderboard");
			leaderboardContainer = leaderboardPanel.GetNode("ScrollContainer").GetNode<VBoxContainer>("VBoxContainer");
			modifiersPanel = playMenu.GetNode<Panel>("Modifiers");
			speedPanel = modifiersPanel.GetNode<Panel>("Speed");
			speedSlider = speedPanel.GetNode<HSlider>("HSlider");
			speedEdit = speedPanel.GetNode<LineEdit>("LineEdit");
			startFromPanel = modifiersPanel.GetNode<Panel>("StartFrom");
			startFromSlider = startFromPanel.GetNode<HSlider>("HSlider");
			startFromEdit = startFromPanel.GetNode<LineEdit>("LineEdit");
			modifierButtons = [];

			foreach (TextureButton mod in modifiersPanel.GetNode("Decrease").GetChildren())
			{
				modifierButtons.Add(mod);
			}

			foreach (TextureButton mod in modifiersPanel.GetNode("Increase").GetChildren())
			{
				modifierButtons.Add(mod);
			}


			importButton.Pressed += importDialog.Show;
			userFolderButton.Pressed += () =>
			{
				OS.ShellOpen($"{Phoenyx.Constants.USER_FOLDER}");
			};
			settingsButton.Pressed += () =>
			{
				SettingsManager.ShowSettings();
			};
			searchEdit.TextChanged += (string text) =>
			{
				searchTitle = text.ToLower();

				if (searchTitle == "")
				{
					searchEdit.ReleaseFocus();
				}

				Search();
			};
			searchAuthorEdit.TextChanged += (string text) =>
			{
				searchAuthor = text.ToLower();

				if (searchAuthor == "")
				{
					searchAuthorEdit.ReleaseFocus();
				}

				Search();
			};
			importDialog.FilesSelected += (string[] files) =>
			{
				Import(files);
			};

			UpdateMapList();

			searchEdit.Text = searchTitle;
			searchAuthorEdit.Text = searchAuthor;

			searchEdit.ReleaseFocus();
			searchAuthorEdit.ReleaseFocus();

			foreach (Panel map in mapListContainer.GetChildren())
			{
				if (map.FindChild("Holder") == null)
				{
					continue;
				}

				map.Visible = map.GetNode("Holder").GetNode<Label>("Title").Text.ToLower().Contains(searchTitle);
			}

			// Context Menu


			contextMenu.GetNode("Container").GetNode<Button>("Favorite").Pressed += () =>
			{
				contextMenu.Visible = false;

				string favorites = File.ReadAllText($"{Phoenyx.Constants.USER_FOLDER}/favorites.txt");
				bool favorited = favorites.Split("\n").ToList().Contains(contextMenuTarget);
				var mapButton = mapListContainer.GetNode<Panel>(contextMenuTarget);
				var favorite = mapButton.GetNode("Holder").GetNode<TextureRect>("Favorited");
				favorite.Visible = !favorited;

				if (favorited)
				{
					File.WriteAllText($"{Phoenyx.Constants.USER_FOLDER}/favorites.txt", favorites.Replace($"{contextMenuTarget}\n", ""));
					favoriteMaps[mapButton] = false;
				}
				else
				{
					favorite.Texture = Phoenyx.Skin.FavoriteImage;
					File.WriteAllText($"{Phoenyx.Constants.USER_FOLDER}/favorites.txt", $"{favorites}{contextMenuTarget}\n");
					favoriteMaps[mapButton] = true;
				}


				SortMapList();
				UpdateFavoriteMapsTextures();

				ToastNotification.Notify($"Successfully {(favorited ? "removed" : "added")} map {(favorited ? "from" : "to")} favorites");
			};
			contextMenu.GetNode("Container").GetNode<Button>("Delete").Pressed += () =>
			{
				contextMenu.Visible = false;

				var mapButton = mapListContainer.GetNode<Panel>(contextMenuTarget);

				favoriteMaps.Remove(mapButton);
				mapButton.QueueFree();
				loadedMaps.Remove(contextMenuTarget);


				if (contextMenuTarget.Contains(searchTitle))
				{
					visibleMaps--;
				}

				File.Delete($"{Phoenyx.Constants.USER_FOLDER}/maps/{contextMenuTarget}.phxm");


				if (Directory.Exists($"{Phoenyx.Constants.USER_FOLDER}/cache/maps/{contextMenuTarget}"))
				{
					foreach (string file in Directory.GetFiles($"{Phoenyx.Constants.USER_FOLDER}/cache/maps/{contextMenuTarget}"))
					{
						File.Delete(file);
					}

					Directory.Delete($"{Phoenyx.Constants.USER_FOLDER}/cache/maps/{contextMenuTarget}");
				}

				ToastNotification.Notify("Successfuly deleted map");
			};
			contextMenu.GetNode("Container").GetNode<Button>("VideoAdd").Pressed += () =>
			{
				contextMenu.Visible = false;
				GetNode<FileDialog>("VideoDialog").Visible = true;
			};
			contextMenu.GetNode("Container").GetNode<Button>("VideoRemove").Pressed += () =>
			{
				contextMenu.Visible = false;
				var map = MapParser.Decode($"{Phoenyx.Constants.USER_FOLDER}/maps/{contextMenuTarget}.phxm");

				File.Delete($"{Phoenyx.Constants.USER_FOLDER}/maps/{contextMenuTarget}.phxm");

				map.VideoBuffer = null;

				MapParser.Encode(map);

				if (Directory.Exists($"{Phoenyx.Constants.USER_FOLDER}/cache/maps/{contextMenuTarget}"))
				{
					foreach (string filePath in Directory.GetFiles($"{Phoenyx.Constants.USER_FOLDER}/cache/maps/{contextMenuTarget}"))
					{
						File.Delete(filePath);
					}

					Directory.Delete($"{Phoenyx.Constants.USER_FOLDER}/cache/maps/{contextMenuTarget}");
				}

				ToastNotification.Notify("Successfully removed video from map");
			};
			GetNode<FileDialog>("VideoDialog").FileSelected += (string path) =>
			{
				if (path.GetExtension() != "mp4")
				{
					ToastNotification.Notify("Only .mp4 files are allowed", 1);
					return;
				}

				var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
				byte[] videoBuffer = file.GetBuffer((long)file.GetLength());
				file.Close();
				var map = MapParser.Decode($"{Phoenyx.Constants.USER_FOLDER}/maps/{contextMenuTarget}.phxm");

				File.Delete($"{Phoenyx.Constants.USER_FOLDER}/maps/{contextMenuTarget}.phxm");

				map.VideoBuffer = videoBuffer;

				MapParser.Encode(map);

				if (Directory.Exists($"{Phoenyx.Constants.USER_FOLDER}/cache/maps/{contextMenuTarget}"))
				{
					foreach (string filePath in Directory.GetFiles($"{Phoenyx.Constants.USER_FOLDER}/cache/maps/{contextMenuTarget}"))
					{
						File.Delete(filePath);
					}

					Directory.Delete($"{Phoenyx.Constants.USER_FOLDER}/cache/maps/{contextMenuTarget}");
				}

				ToastNotification.Notify("Successfully added video to map");
			};

			// Modifiers


			speedSlider.ValueChanged += (double value) =>
			{
				speedEdit.Text = value.ToString();
				Lobby.Speed = value / 100;

				if (!SoundManager.JukeboxPaused)
				{
					SoundManager.Song.PitchScale = (float)Lobby.Speed;
				}
			};
			speedEdit.TextSubmitted += (string text) =>
			{
				speedSlider.Value = Math.Clamp(text.ToFloat(), 25, 1000);
				speedEdit.ReleaseFocus();
			};

			startFromSlider.ValueChanged += (double value) =>
			{
				value *= selectedMap.Length;

				startFromEdit.Text = $"{Lib.String.FormatTime(value / 1000)}";
				Lobby.StartFrom = Math.Floor(value);
			};
			startFromSlider.DragEnded += (bool _) =>
			{
				if (!SoundManager.JukeboxPaused)
				{
					SoundManager.Song.Seek((float)startFromSlider.Value * selectedMap.Length / 1000);
				}
			};
			startFromEdit.TextSubmitted += (string text) =>
			{
				startFromSlider.Value = Math.Clamp(text.ToFloat(), 0, 1);
				startFromEdit.ReleaseFocus();
			};

			foreach (var modButton in modifierButtons)
			{
				modButton.SelfModulate = Color.Color8(255, 255, 255, (byte)(Lobby.Mods[modButton.Name] ? 255 : 128));
				modButton.Pressed += () =>
				{
					Lobby.Mods[modButton.Name] = !Lobby.Mods[modButton.Name];

					var tween = modButton.CreateTween();
					tween.TweenProperty(modButton, "self_modulate", Color.Color8(255, 255, 255, (byte)(Lobby.Mods[modButton.Name] ? 255 : 128)), 1 / 4);
					tween.Play();
				};
			}


			speedSlider.Value = Lobby.Speed * 100;
			startFromSlider.Value = Lobby.StartFrom / Math.Max(1, currentMap.Length);

			// Extras


			var soundSpace = extras.GetNode<Button>("SoundSpace");


			soundSpace.MouseEntered += () =>
			{
				soundSpace.GetNode<RichTextLabel>("RichTextLabel").Text = "[center][color=ffffff40]Inspired by [color=ffffff80]Sound Space";
			};
			soundSpace.MouseExited += () =>
			{
				soundSpace.GetNode<RichTextLabel>("RichTextLabel").Text = "[center][color=ffffff40]Inspired by Sound Space";
			};
			soundSpace.Pressed += () =>
			{
				OS.ShellOpen("https://www.roblox.com/games/2677609345");
			};

			// Multiplayer

			//MultiplayerHolder = PlayMenu.GetNode<Panel>("Multiplayer");
			//IPLine = MultiplayerHolder.GetNode<LineEdit>("IP");
			//PortLine = MultiplayerHolder.GetNode<LineEdit>("Port");
			//ChatLine = MultiplayerHolder.GetNode<LineEdit>("ChatInput");
			//Host = MultiplayerHolder.GetNode<Button>("Host");
			//Join = MultiplayerHolder.GetNode<Button>("Join");
			//ChatScrollContainer = MultiplayerHolder.GetNode<ScrollContainer>("Chat");
			//ChatHolder = ChatScrollContainer.GetNode<VBoxContainer>("Holder");
			//
			//Host.Pressed += () => {
			//	try
			//	{
			//
			//	}
			//	catch (Exception exception)
			//	{
			//		ToastNotification.Notify($"{exception.Message}", 2);
			//		return;
			//	}
			//
			//	Host.Disabled = true;
			//	Join.Disabled = true;
			//};
			//Join.Pressed += () => {
			//	try
			//	{
			//
			//	}
			//	catch (Exception exception)
			//	{
			//		ToastNotification.Notify($"{exception.Message}", 2);
			//		return;
			//	}
			//
			//	Host.Disabled = true;
			//	Join.Disabled = true;
			//};

			// Finish


			SoundManager.UpdateJukeboxQueue();
			SoundManager.JukeboxIndex = new Random().Next(0, SoundManager.JukeboxQueue.Length);

			if (!SoundManager.Song.Playing)
			{
				SoundManager.PlayJukebox();
				SoundManager.JukeboxPaused = !Phoenyx.Settings.AutoplayJukebox;
			}
			else
			{
				jukebox.GetNode<Label>("Title").Text = Runner.CurrentAttempt.Map.PrettyTitle;
				SoundManager.JukeboxPaused = false;
				currentMap = Runner.CurrentAttempt.Map;

				transition("Play", true);
			}

			SoundManager.Song.PitchScale = SoundManager.JukeboxPaused ? 0.00000000001f : (float)Lobby.Speed;    // bruh


			UpdateJukeboxButtons();
			UpdateSpectrumSpacing();
			UpdateLeaderboard();

			SoundManager.Song.VolumeDb = -180;
		}

		public override void _Process(double delta)
		{
			ulong now = Time.GetTicksUsec();
			delta = (now - lastFrame) / 1000000;
			lastFrame = now;
			scroll = Mathf.Lerp(scroll, targetScroll, 8 * (float)delta);
			mapList.ScrollVertical = (int)scroll;
			Cursor.Position = mousePosition - new Vector2(Cursor.Size.X / 2, Cursor.Size.Y / 2);

			if (firstFrame)
			{
				firstFrame = false;

				Search();

				if (selectedMapID != null)
				{
					var selectedMapHolder = mapListContainer.GetNode(selectedMapID).GetNode<Panel>("Holder");
					selectedMapHolder.GetNode<Panel>("Normal").Visible = false;
					selectedMapHolder.GetNode<Panel>("Selected").Visible = true;
					selectedMapHolder.Size = new Vector2(mapListContainer.Size.X, selectedMapHolder.Size.Y);
					selectedMapHolder.Position = new Vector2(0, selectedMapHolder.Position.Y);
				}
			}


			if (SoundManager.Song.Stream != null)
			{
				jukeboxProgress.AnchorRight = (float)Math.Clamp(SoundManager.Song.GetPlaybackPosition() / SoundManager.Song.Stream.GetLength(), 0, 1);
				SoundManager.Song.VolumeDb = Mathf.Lerp(SoundManager.Song.VolumeDb, Phoenyx.Util.Quitting ? -80 : -80 + 70 * (float)Math.Pow(Phoenyx.Settings.VolumeMusic / 100, 0.1) * (float)Math.Pow(Phoenyx.Settings.VolumeMaster / 100, 0.1), (float)Math.Clamp(delta * 2, 0, 1));
			}

			float prevHz = 0;


			for (int i = 0; i < 32; i++)
			{
				float hz = (i + 1) * 4000 / 32;
				float magnitude = audioSpectrum.GetMagnitudeForFrequencyRange(prevHz, hz).Length();
				float energy = (60 + Mathf.LinearToDb(magnitude)) / 30;
				prevHz = hz;


				jukeboxSpectrumBars[i].AnchorTop = Math.Clamp(Mathf.Lerp(jukeboxSpectrumBars[i].AnchorTop, 1 - energy * (SoundManager.JukeboxPaused ? 0.0000000001f : 1), (float)delta * 12), 0, 1);    // oh god not again

			}

			for (int i = 0; i < favoriteMapsTextures.Length; i++)
			{
				var modulate = Color.Color8(255, 255, 255, (byte)(196 + (59 * Math.Sin(Math.PI * now / 1000000 + i))));
				favoriteMapsTextures[i].Rotation = (float)now / 1000000;
				favoriteMapsTextures[i].Modulate = modulate;
			}

			foreach (ColorRect tile in backgroundTiles)
			{
				tile.Color = tile.Color.Lerp(Color.Color8(255, 255, 255, 0), (float)delta * 8);
			}

			for (int i = passedNotes; i < currentMap.Notes.Length; i++)
			{
				if (currentMap.Notes[i].Millisecond > SoundManager.Song.GetPlaybackPosition() * 1000)
				{
					break;
				}

				Vector2I pos = new(Math.Clamp((int)Math.Floor(currentMap.Notes[i].X + 1.5), 0, 2), Math.Clamp((int)Math.Floor(currentMap.Notes[i].Y + 1.5), 0, 2));
				int tile = 0;


				tile += pos.X;
				tile += 3 * pos.Y;

				(backgroundTiles[tile] as ColorRect).Color = Color.Color8(255, 255, 255, 12);

				passedNotes = i + 1;
			}

			main.Position = main.Position.Lerp((Size / 2 - mousePosition) * (4 / Size.Y), Math.Min(1, (float)delta * 16));
			extras.Position = main.Position;

			if (Phoenyx.Util.Quitting)
			{
				mainBackgroundMaterial.SetShaderParameter("opaqueness", Mathf.Lerp((float)mainBackgroundMaterial.GetShaderParameter("opaqueness"), 0, delta * 8));
			}


			mainBackgroundMaterial.SetShaderParameter("window_position", DisplayServer.WindowGetPosition());
		}

		public override void _Input(InputEvent @event)
		{
			if (@event is InputEventKey eventKey && eventKey.Pressed)
			{
				if (eventKey.AsText() == peru_sequence[peruSequenceIndex])
				{
					peruSequenceIndex++;


					if (peruSequenceIndex >= 4)
					{
						peruSequenceIndex = 0;
						peruchor.Visible = true;

						var tween = peruchor.CreateTween();
						tween.TweenProperty(peruchor, "modulate", Color.Color8(255, 255, 255, 255), 3);
						tween.Play();
					}
				}
				else
				{
					peruSequenceIndex = 0;
				}

				switch (eventKey.Keycode)
				{
					case Key.Space:
						if (selectedMapID != null && !searchEdit.HasFocus() && !searchAuthorEdit.HasFocus())
						{
							var map = MapParser.Decode($"{Phoenyx.Constants.USER_FOLDER}/maps/{selectedMapID}.phxm");

							SoundManager.Song.Stop();
							SceneManager.Load("res://scenes/game.tscn");
							Runner.Play(map, Lobby.Speed, Lobby.StartFrom, Lobby.Mods);
						}
						break;
					case Key.Mediaplay:
						SoundManager.JukeboxPaused = !SoundManager.JukeboxPaused;
						SoundManager.Song.PitchScale = SoundManager.JukeboxPaused ? 0.00000000001f : (float)Lobby.Speed;    // bruh

						UpdateJukeboxButtons();
						break;
					case Key.Medianext:
						SoundManager.JukeboxIndex++;
						SoundManager.PlayJukebox();
						break;
					case Key.Mediaprevious:
						ulong now = Time.GetTicksMsec();

						if (now - SoundManager.LastRewind < 1000)
						{
							SoundManager.JukeboxIndex--;
							SoundManager.PlayJukebox();
						}
						else
						{
							SoundManager.Song.Seek(0);
						}

						SoundManager.LastRewind = now;
						break;
					default:
						if (SettingsManager.FocusedLineEdit == null && !searchAuthorEdit.HasFocus() && !speedEdit.HasFocus() && !startFromEdit.HasFocus() && !eventKey.CtrlPressed && !eventKey.AltPressed && eventKey.Keycode != Key.Ctrl && eventKey.Keycode != Key.Shift && eventKey.Keycode != Key.Alt && eventKey.Keycode != Key.Escape && eventKey.Keycode != Key.Enter && eventKey.Keycode != Key.F11)
						{
							searchEdit.GrabFocus();
						}
						break;
				}
			}
			else if (@event is InputEventMouseButton eventMouseButton)
			{
				if (!SettingsManager.Shown && !eventMouseButton.CtrlPressed)
				{
					switch (eventMouseButton.ButtonIndex)
					{
						case MouseButton.Right:
							rightMouseHeld = eventMouseButton.Pressed;


							if (!rightClickingButton)
							{
								targetScroll = Math.Clamp((mousePosition.Y - 50) / (DisplayServer.WindowGetSize().Y - 100), 0, 1) * maxScroll;
							}

							if (!rightMouseHeld && rightClickingButton)
							{
								rightClickingButton = false;
							}


							break;
						case MouseButton.WheelUp:
							contextMenu.Visible = false;
							targetScroll = Math.Max(0, targetScroll - 80);
							break;
						case MouseButton.WheelDown:
							contextMenu.Visible = false;
							targetScroll = Math.Min(maxScroll, targetScroll + 80);
							break;
						case MouseButton.Xbutton1:
							if (eventMouseButton.Pressed && currentMenu != "Main")
							{
								transition("Main");
							}
							break;
						case MouseButton.Xbutton2:
							if (eventMouseButton.Pressed && currentMenu != lastMenu)
							{
								transition(lastMenu);
							}
							break;
					}
				}
			}
			else if (@event is InputEventMouseMotion eventMouseMotion)
			{
				mousePosition = eventMouseMotion.Position;

				if (rightMouseHeld && !rightClickingButton)
				{
					targetScroll = Math.Clamp((mousePosition.Y - 50) / (DisplayServer.WindowGetSize().Y - 100), 0, 1) * maxScroll;
				}
			}
		}

		public override void _UnhandledInput(InputEvent @event)
		{
			if (@event is InputEventKey eventKey && eventKey.Pressed)
			{
				if (eventKey.CtrlPressed)
				{
					switch (eventKey.Keycode)
					{
						case Key.O:
							SettingsManager.ShowSettings(!SettingsManager.Shown);
							break;
					}
				}

				switch (eventKey.Keycode)
				{
					case Key.Escape:
						if (SettingsManager.Shown)
						{
							SettingsManager.HideSettings();
						}
						else
						{
							if (currentMenu != "Main")
							{
								transition("Main");
							}
							else
							{
								Control.GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
							}
						}
						break;
				}
			}
			else if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.Pressed)
			{
				switch (eventMouseButton.ButtonIndex)
				{
					case MouseButton.Left:
						contextMenu.Visible = false;
						break;
				}
			}
		}

		public static Dictionary<string, bool> Import(string[] files)
		{
			List<string> maps = [];

			foreach (string file in files)
			{
				if (file.GetExtension() == "phxm" || file.GetExtension() == "sspm" || file.GetExtension() == "txt")
				{
					maps.Add(file);
				}
			}

			var results = MapParser.BulkImport([.. maps]);

			if (maps.Count == 0)
			{
				return results;
			}

			SoundManager.UpdateJukeboxQueue();


			if (SceneManager.Scene.Name == "SceneMenu")
			{
				UpdateMapList();
				Search();
				Select(maps[0].GetFile().GetBaseName(), true);
			}

			return results;
		}

		public static void Search()
		{
			visibleMaps = 0;

			foreach (Panel map in mapListContainer.GetChildren())
			{
				map.Visible = !Phoenyx.Constants.TEMP_MAP_MODE && map.GetNode("Holder").GetNode<Label>("Title").Text.ToLower().Contains(searchTitle) && map.GetNode("Holder").GetNode<RichTextLabel>("Extra").Text.ToLower().Split(" - ")[^1].Contains(searchAuthor);

				if (map.Visible)
				{
					mapsOrder[map.Name] = visibleMaps;
					visibleMaps++;
				}
			}

			UpdateMaxScroll();
			targetScroll = Math.Clamp(targetScroll, 0, maxScroll);
		}

		public static void Select(string fileName, bool fromImport = false, bool selectInMapList = true)
		{
			if (selectInMapList)
			{
				var mapButton = mapListContainer.GetNode<Panel>(fileName);

				if (mapButton == null)
				{
					Logger.Log($"Tried to select map {fileName}, but it wasn't found in the map list");
					return;
				}

				var holder = mapButton.GetNode<Panel>("Holder");

				if (selectedMapID != null)
				{
					var selectedHolder = mapListContainer.GetNode(selectedMapID).GetNode<Panel>("Holder");
					selectedHolder.GetNode<Panel>("Normal").Visible = true;
					selectedHolder.GetNode<Panel>("Selected").Visible = false;

					var deselectTween = selectedHolder.CreateTween().SetParallel();
					deselectTween.TweenProperty(selectedHolder, "size", new Vector2(mapListContainer.Size.X - 60, selectedHolder.Size.Y), 0.25).SetTrans(Tween.TransitionType.Quad);
					deselectTween.TweenProperty(selectedHolder, "position", new Vector2(60, selectedHolder.Position.Y), 0.25).SetTrans(Tween.TransitionType.Quad);
					deselectTween.Play();

					if (mapListContainer.GetNode(selectedMapID) == mapButton && !fromImport)
					{
						var map = MapParser.Decode($"{Phoenyx.Constants.USER_FOLDER}/maps/{fileName}.phxm");

						SoundManager.Song.Stop();
						SceneManager.Load("res://scenes/game.tscn");
						Runner.Play(map, Lobby.Speed, Lobby.StartFrom, Lobby.Mods);
					}
				}

				holder.GetNode<Panel>("Normal").Visible = false;
				holder.GetNode<Panel>("Selected").Visible = true;


				if (fromImport)
				{
					holder.CallDeferred("set_size", new Vector2(mapListContainer.Size.X, holder.Size.Y));
					holder.CallDeferred("set_position", new Vector2(0, holder.Position.Y));
				}
				else
				{
					var selectTween = holder.CreateTween().SetParallel();
					selectTween.TweenProperty(holder, "size", new Vector2(mapListContainer.Size.X, holder.Size.Y), 0.25).SetTrans(Tween.TransitionType.Quad);
					selectTween.TweenProperty(holder, "position", new Vector2(0, holder.Position.Y), 0.25).SetTrans(Tween.TransitionType.Quad);
					selectTween.Play();
				}

				targetScroll = Math.Clamp((mapsOrder[fileName] + 1) * (mapButton.Size.Y + 10) - mapList.Size.Y / 2, 0, maxScroll);


				int index = SoundManager.JukeboxQueueInverse[mapButton.Name];


				if (SoundManager.JukeboxIndex != index)
				{
					SoundManager.JukeboxIndex = index;
					SoundManager.JukeboxPaused = false;
					SoundManager.Song.PitchScale = (float)Lobby.Speed;
					SoundManager.PlayJukebox();
					UpdateJukeboxButtons();
				}
			}

			bool firstTimeSelected = fileName != selectedMapID;

			if (selectedMapID != fileName)
			{
				startFromSlider.Value = 0;
				startFromEdit.Text = "0:00";
				Lobby.StartFrom = 0;
			}

			selectedMapID = fileName;
			selectedMap = MapParser.Decode($"{Phoenyx.Constants.USER_FOLDER}/maps/{selectedMapID}.phxm");

			if (firstTimeSelected)
			{
				UpdateLeaderboard();
			}

			transition("Play");
		}

		public static void SortMapList()
		{
			List<Node> favorites = [];
			string[] maps = Directory.GetFiles($"{Phoenyx.Constants.USER_FOLDER}/maps");

			for (int i = 0; i < maps.Length; i++)
			{
				var mapButton = mapListContainer.GetNode(maps[i].GetFile().GetBaseName());

				if (mapButton == null)
				{
					continue;
				}

				mapListContainer.MoveChild(mapButton, i);
			}

			foreach (var entry in favoriteMaps)
			{
				if (!entry.Value)
				{
					continue;
				}

				favorites.Add(entry.Key);
			}

			for (int i = favorites.Count - 1; i >= 0; i--)
			{
				mapListContainer.MoveChild(favorites[i], 0);
			}

			var mapButtons = mapListContainer.GetChildren();

			for (int i = 0; i < mapButtons.Count; i++)
			{
				mapsOrder[mapButtons[i].Name] = i;
			}
		}

		//public static void Chat(string message)
		//{
		//	Label chatMessage = ChatMessage.Instantiate<Label>();
		//	chatMessage.Text = message;
		//
		//	ChatHolder.AddChild(chatMessage);
		//	ChatScrollContainer.ScrollVertical += 100;
		//}
		//
		//private static void SendMessage()
		//{
		//	if (ChatLine.Text.Replace(" ", "") == "")
		//	{
		//		return;
		//	}
		//
		//	ChatLine.Text = "";
		//}


		private static void transition(string menuName, bool instant = false)
		{
			lastMenu = currentMenu;
			currentMenu = menuName;

			switch (currentMenu)
			{
				case "Main":
					Phoenyx.Util.DiscordRPC.Call("Set", "details", "Main Menu");
					break;
				case "Play":
					Phoenyx.Util.DiscordRPC.Call("Set", "details", "Browsing Maps");
					break;
				case "Extras":
					Phoenyx.Util.DiscordRPC.Call("Set", "details", "Extras");
					break;
			}

			SettingsManager.FocusedLineEdit?.ReleaseFocus();

			var outTween = Control.CreateTween();


			foreach (Panel menu in menus.GetChildren())
			{
				if (menu.Name == currentMenu)
				{
					continue;
				}
				outTween.Parallel().TweenProperty(menu, "modulate", Color.Color8(255, 255, 255, 0), instant ? 0 : 0.15).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
			}

			outTween.TweenCallback(Callable.From(() =>
			{
				foreach (Panel menu in menus.GetChildren())
				{
					if (menu.Name == currentMenu)
					{
						continue;
					}
					menu.Visible = false;
				}
			}));
			outTween.Play();

			var inMenu = menus.GetNode<Panel>(menuName);
			inMenu.Visible = true;

			var inTween = Control.CreateTween();
			inTween.TweenProperty(inMenu, "modulate", Color.Color8(255, 255, 255, 255), instant ? 0 : 0.15).SetTrans(Tween.TransitionType.Quad);
			inTween.Play();
		}

		public static void UpdateVolume()
		{
			SettingsManager.Holder.GetNode("Categories").GetNode("Audio").GetNode("Container").GetNode("VolumeMaster").GetNode<HSlider>("HSlider").Value = Phoenyx.Settings.VolumeMaster;
		}

		public static void UpdateMapList()
		{
			double start = Time.GetTicksUsec();
			int i = 0;
			var black = Color.Color8(0, 0, 0, 1);
			List<string> favorites = [.. File.ReadAllText($"{Phoenyx.Constants.USER_FOLDER}/favorites.txt").Split("\n")];

			foreach (string mapFile in Directory.GetFiles($"{Phoenyx.Constants.USER_FOLDER}/maps"))
			{
				try
				{
					string fileName = mapFile.GetFile().GetBaseName();

					if (loadedMaps.Contains(fileName))
					{
						continue;
					}

					bool favorited = favorites.Contains(fileName);
					string title;
					string difficultyName;
					string mappers = "";
					int difficulty;
					string coverFile = null;

					if (!Directory.Exists($"{Phoenyx.Constants.USER_FOLDER}/cache/maps/{fileName}"))
					{
						Directory.CreateDirectory($"{Phoenyx.Constants.USER_FOLDER}/cache/maps/{fileName}");
						var map = MapParser.Decode(mapFile, null, false);

						File.WriteAllText($"{Phoenyx.Constants.USER_FOLDER}/cache/maps/{fileName}/metadata.json", map.EncodeMeta());

						//if (map.CoverBuffer != null)
						//{
						//	Godot.FileAccess cover = Godot.FileAccess.Open($"{Phoenyx.Constants.UserFolder}/cache/maps/{fileName}/cover.png", Godot.FileAccess.ModeFlags.Write);
						//	cover.StoreBuffer(map.CoverBuffer);
						//	cover.Close();
						//
						//	Image coverImage = Image.LoadFromFile($"{Phoenyx.Constants.UserFolder}/cache/maps/{fileName}/cover.png");
						//	coverImage.Resize(128, 128);
						//	coverImage.SavePng($"{Phoenyx.Constants.UserFolder}/cache/maps/{fileName}/cover.png");
						//	coverFile = $"{Phoenyx.Constants.UserFolder}/cache/maps/{fileName}/cover.png";
						//}


						title = map.PrettyTitle;
						difficultyName = map.DifficultyName;
						mappers = map.PrettyMappers;
						difficulty = map.Difficulty;
					}
					else
					{
						var metaFile = Godot.FileAccess.Open($"{Phoenyx.Constants.USER_FOLDER}/cache/maps/{fileName}/metadata.json", Godot.FileAccess.ModeFlags.Read);
						var metadata = (Godot.Collections.Dictionary)Json.ParseString(Encoding.UTF8.GetString(metaFile.GetBuffer((long)metaFile.GetLength())));
						metaFile.Close();

						//if (File.Exists($"{Phoenyx.Constants.UserFolder}/cache/maps/{fileName}/cover.png"))
						//{
						//	coverFile = $"{Phoenyx.Constants.UserFolder}/cache/maps/{fileName}/cover.png";
						//}


						foreach (string mapper in (string[])metadata["Mappers"])
						{
							mappers += $"{mapper}, ";
						}

						mappers = mappers.Substr(0, mappers.Length - 2);
						difficultyName = (string)metadata["DifficultyName"];
						title = (string)metadata["Artist"] != "" ? $"{(string)metadata["Artist"]} - {(string)metadata["Title"]}" : (string)metadata["Title"];
						difficulty = (int)metadata["Difficulty"];
					}

					loadedMaps.Add(fileName);
					visibleMaps++;

					var mapButton = MainMenu.map_button.Instantiate<Panel>();
					var holder = mapButton.GetNode<Panel>("Holder");

					favoriteMaps[mapButton] = favorited;


					if (coverFile != null)
					{
						holder.GetNode<TextureRect>("Cover").Texture = ImageTexture.CreateFromImage(Image.LoadFromFile(coverFile));
					}

					holder.GetNode<Label>("Title").Text = title;
					holder.GetNode<RichTextLabel>("Extra").Text = $"[color={Phoenyx.Constants.SECONDARY_DIFFICULTY_COLORS[difficulty].ToHtml(false)}]{difficultyName}[color=808080] - {mappers}".ReplaceLineEndings("");

					mapListContainer.AddChild(mapButton);
					mapButton.Name = fileName;

					if (favorited)
					{
						var favorite = holder.GetNode<TextureRect>("Favorited");
						favorite.Texture = Phoenyx.Skin.FavoriteImage;
						favorite.Visible = true;
					}

					holder.GetNode<Button>("Button").MouseEntered += () =>
					{
						holder.GetNode<ColorRect>("Hover").Color = Color.FromHtml("#ffffff10");
					};


					holder.GetNode<Button>("Button").MouseExited += () =>
					{
						holder.GetNode<ColorRect>("Hover").Color = Color.FromHtml("#ffffff00");
					};

					holder.GetNode<Button>("Button").Pressed += () =>
					{
						contextMenu.Visible = false;


						if (!rightMouseHeld)
						{
							Select(fileName);
						}
						else
						{
							rightClickingButton = true;
							targetScroll = Math.Clamp(mapButton.Position.Y + mapButton.Size.Y - windowSize.Y / 2, 0, maxScroll);
							contextMenu.Visible = true;
							contextMenu.Position = mousePosition;
							contextMenuTarget = fileName;

							bool favorited = favoriteMaps[mapButton];

							contextMenu.GetNode("Container").GetNode<Button>("Favorite").Text = favorited ? "Unfavorite" : "Favorite";
						}
					};

					i++;
				}
				catch
				{
					continue;
				}
			}

			UpdateMaxScroll();
			SortMapList();
			UpdateFavoriteMapsTextures();

			Logger.Log($"MAPLIST UPDATE: {(Time.GetTicksUsec() - start) / 1000}ms");
		}

		public static void UpdateLeaderboard()
		{
			foreach (var child in leaderboardContainer.GetChildren())
			{
				leaderboardContainer.RemoveChild(child);
			}

			Leaderboard leaderboard = new();


			if (File.Exists($"{Phoenyx.Constants.USER_FOLDER}/pbs/{selectedMapID}"))
			{
				leaderboard = new(selectedMapID, $"{Phoenyx.Constants.USER_FOLDER}/pbs/{selectedMapID}");
			}


			leaderboardPanel.GetNode<Label>("NoScores").Visible = leaderboard.ScoreCount == 0;

			if (!leaderboard.Valid)
			{
				return;
			}

			int count = 0;

			foreach (var score in leaderboard.Scores)
			{
				var scorePanel = leaderboard_score.Instantiate<Panel>();
				var playerLabel = scorePanel.GetNode<Label>("Player");
				var scoreLabel = scorePanel.GetNode<Label>("Score");

				playerLabel.Text = score.Player;
				scorePanel.GetNode<ColorRect>("Bright").Visible = (count + 1) % 2 == 0;
				scorePanel.GetNode<Label>("Accuracy").Text = $"{score.Accuracy.ToString().PadDecimals(2)}%";
				scorePanel.GetNode<Label>("Speed").Text = $"{score.Speed.ToString().PadDecimals(2)}x";
				scorePanel.GetNode<Label>("Time").Text = Lib.String.FormatUnixTimePretty(Time.GetUnixTimeFromSystem(), score.Time);

				if (score.Qualifies)
				{
					scoreLabel.Text = Lib.String.PadMagnitude(score.Value.ToString());
				}
				else
				{
					playerLabel.LabelSettings = playerLabel.LabelSettings.Duplicate() as LabelSettings;
					playerLabel.LabelSettings.FontColor = Color.Color8(255, 255, 255, 64);
					scoreLabel.LabelSettings = scoreLabel.LabelSettings.Duplicate() as LabelSettings;
					scoreLabel.LabelSettings.FontColor = Color.Color8(255, 255, 255, 64);
					scoreLabel.Text = $"{Lib.String.FormatTime(score.Progress / 1000)} / {Lib.String.FormatTime(score.MapLength / 1000)}";
				}

				scorePanel.GetNode<Button>("Button").Pressed += () =>
				{
					if (File.Exists($"{Phoenyx.Constants.USER_FOLDER}/replays/{score.AttemptID}.phxr"))
					{
						Replay replay = new($"{Phoenyx.Constants.USER_FOLDER}/replays/{score.AttemptID}.phxr");
						SoundManager.Song.Stop();
						SceneManager.Load("res://scenes/game.tscn");
						Runner.Play(MapParser.Decode(replay.MapFilePath), replay.Speed, replay.StartFrom, replay.Modifiers, null, [replay]);
					}
				};

				var modifiersContainer = scorePanel.GetNode<HBoxContainer>("Modifiers");
				var modifierTemplate = modifiersContainer.GetNode<TextureRect>("ModifierTemplate");

				foreach (var entry in score.Modifiers)
				{
					if (entry.Value)
					{
						var mod = modifierTemplate.Duplicate() as TextureRect;
						mod.Texture = Phoenyx.Util.GetModIcon(entry.Key);
						mod.Visible = true;
						modifiersContainer.AddChild(mod);
					}
				}

				leaderboardContainer.AddChild(scorePanel);
				count++;
			}
		}

		public static void UpdateMaxScroll()
		{
			maxScroll = Math.Max(0, (int)(visibleMaps * 90 - mapList.Size.Y));
		}

		public static void UpdateSpectrumSpacing()
		{
			jukeboxSpectrum.AddThemeConstantOverride("separation", ((int)jukeboxSpectrum.Size.X - 32 * 6) / 48);
		}

		public static void UpdateFavoriteMapsTextures()
		{
			List<Panel> favorites = [];

			foreach (var entry in favoriteMaps)
			{
				if (entry.Value)
				{
					favorites.Add(entry.Key);
				}
			}

			favoriteMapsTextures = new TextureRect[favorites.Count];

			for (int i = 0; i < favorites.Count; i++)
			{
				favoriteMapsTextures[i] = favorites[i].GetNode("Holder").GetNode<TextureRect>("Favorited");
			}
		}

		public static void UpdateJukeboxButtons()
		{
			jukebox.GetNode<TextureButton>("Pause").TextureNormal = SoundManager.JukeboxPaused ? Phoenyx.Skin.JukeboxPlayImage : Phoenyx.Skin.JukeboxPauseImage;
		}
	}
}
