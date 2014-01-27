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

		protected bool Equals(Player other)
		{
			return string.Equals(Name, other.Name);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((Player) obj);
		}

		public override int GetHashCode()
		{
			return (Name != null ? Name.GetHashCode() : 0);
		}
	}
}