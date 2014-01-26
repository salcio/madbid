using System;

namespace MadBidFetcher.Model
{
	[Serializable]
	public class Player
	{
		public static int HighPlayerCount = 200;
		public string Name { get; set; }
		public int Count { get; set; }

		public bool IsHighPlayer()
		{
			return Count > HighPlayerCount;
		}
	}
}