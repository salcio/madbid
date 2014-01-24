using System;

namespace MadBidFetcher.Model
{
	[Serializable]
	public class Player
	{
		public static int HighPlayerCount = 200;

		public string Name { get; set; }
		public int Count { get; set; }
		private int _delta;
		public int Delta
		{
			get { return _delta; }
			set { _delta = value > 0 ? value : 0; }
		}

		public bool IsHighPlayer()
		{
			return Delta > 0 && Count > HighPlayerCount;
		}
	}
}