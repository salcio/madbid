using System;

namespace MadBidFetcher.Model
{
	[Serializable]
	public class Bid
	{
		protected bool Equals(Bid other)
		{
			return Value.Equals(other.Value);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((Bid) obj);
		}

		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}

		public Auction Auction { get; set; }
		public Player Player { get; set; }
		public float Value { get; set; }
		public DateTime? Time { get; set; }
	}
}