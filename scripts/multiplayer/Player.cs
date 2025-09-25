namespace Pheonyx.Multiplayer
{
	public class Player
	{
		public string Name { get; set; }
		public bool Ready { get; set; }

		public Player(string name = "Player")
		{
			Name = name;
			Ready = false;
		}

		public override string ToString() => $"{Name}";
	}
}
