using System;
using System.Linq;
using MadBidFetcher.Extensions;
using MadBidFetcher.Model;

namespace MadBidFetcher.Services
{
	public class Displayer
	{
		public void Display(Updater updater, int auctionId, int skipCountLessThen,Action<object> a)
		{
			var results = updater.Auctions
			.GetOrAdd(auctionId, () => new Model.Auction())
			.Players
			.Values
			.OrderByDescending(g => g.IsHighPlayer())
			//.ThenByDescending(g => g.Delta)
			.ThenByDescending(g => g.Bids.Count)
			.ToList();

			a(results.Where(c => c.Bids.Count >= skipCountLessThen));

			//a(string.Format("Deltas last reset : {0}", updater.Auctions[auctionId].LastDeltaReset));
			//a(string.Format("Number of users with delta : {0}", results.Count(r => r.Delta > 0)));
		}
	}
}