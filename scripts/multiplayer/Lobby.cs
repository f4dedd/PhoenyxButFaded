using System.Collections.Generic;
using Pheonyx.Maps;

namespace Pheonyx.Multiplayer
{
	public class Lobby
	{
		public static Dictionary<string, Player> Players = [];
		public static int PlayerCount = 0;
		public static int PlayersReady = 0;
		public static Map Map;
		public static double Speed = 1;
		public static double StartFrom = 0;
		public static Dictionary<string, bool> Mods = new()
		{
			["NoFail"] = false,
			["Ghost"] = false,
			["Spin"] = false,
			["Flashlight"] = false,
			["Chaos"] = false,
			["HardRock"] = false
		};

		public delegate void AllReady();
		public static event AllReady OnAllReady;

		public static void Enter()
		{
			AddPlayer(new Player("You"));
		}

		public static void Leave()
		{
			Players = [];
			PlayerCount = 0;
		}

		public static void Ready(string name, bool ready = true)
		{
			if (Players.ContainsKey(name))
			{
				if (Players[name].Ready == ready)
				{
					return;
				}

				Players[name].Ready = ready;
			}

			PlayersReady += ready ? 1 : -1;

			if (PlayersReady == PlayerCount)
			{
				OnAllReady?.Invoke();
			}
		}

		public static void Unready(string name)
		{
			Ready(name, false);
		}

		public static void AddPlayer(Player player)
		{
			Players[player.Name] = player;
			PlayerCount++;
		}

		public static void RemovePlayer(Player player)
		{
			Players[player.Name] = null;
			PlayerCount--;
		}
	}
}
