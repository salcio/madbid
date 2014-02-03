using System.Data.Entity;
using MadBidFetcher.Model;

namespace MadBidFetcher
{
    public class MadBidContext : DbContext
    {
        public MadBidContext()
            : base("DefaultConnection")
        {
            Configuration.LazyLoadingEnabled = true;
            Configuration.ProxyCreationEnabled = false;
        }

        public DbSet<Auction> Auctions { get; set; }
        public DbSet<Bid> Bids { get; set; }
        public DbSet<Player> Players { get; set; }


    }
}