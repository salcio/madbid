using System;
using System.Collections.Generic;

namespace MadBidFetcher.Model
{
	[Serializable]
	public class Player
	{
		public static int HighPlayerCount = 200;
		private List<Bid> _bids;
		public string Name { get; set; }
		public List<Bid> Bids
		{
			get { return _bids = _bids ?? new List<Bid>(); }
			set { _bids = value; }
		}

		public Player()
		{
		}

		public bool IsHighPlayer()
		{
			return Bids.Count > HighPlayerCount;
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
			return Equals((Player)obj);
		}

		public override int GetHashCode()
		{
			return (Name != null ? Name.GetHashCode() : 0);
		}
	}
}