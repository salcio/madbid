using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;

namespace MadBidFetcher
{
    [Serializable]
    public class Player
    {
        public static int HighPlayerCount = 200;
        [Key]
        public string Name { get; set; }
        public virtual ICollection<Bid> Bids { get; set; }
        public virtual ICollection<Auction> Auctions { get; set; }

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

        public static Player FromModel(Model.Player arg)
        {
            return new Player { Name = arg.Name};
        }
    }
}