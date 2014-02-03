using System;

namespace MadBidFetcher
{
	[Serializable]
	public class Bid
	{
        public int Id { get; set; }
        public int AuctionId { get; set; }
        public string PlayerName { get; set; }

        public Auction Auction { get; set; }
        public Player Player { get; set; }
        public float Value { get; set; }
        public DateTime? Time { get; set; }
        
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

	    public static Bid FromModel(Model.Bid bid, Auction auction)
	    {
	        return new Bid
	            {
                    Auction = auction,
                    PlayerName = bid.Player.Name,
                    Time = bid.Time,
                    Value = bid.Value
	            };
	    }

	}
}