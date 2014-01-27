using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;
using MadBidFetcher.Extensions;
using MadBidFetcher.Model;
using MadBidFetcher.Model.MadBitl;

namespace MadBidFetcher.Services
{
	public class TestUpdater:Updater
	{
		private readonly int _auctionId;

		public TestUpdater(int auctionId)
		{
			_auctionId = auctionId;
		}

		public override void Update()
		{
			(new AuctionUpdate[]
				 {
					 new AuctionUpdate {auction_id = _auctionId, highest_bidder = "a"},
					 new AuctionUpdate {auction_id = _auctionId, highest_bidder = "b"},
					 new AuctionUpdate {auction_id = _auctionId, highest_bidder = "c"},
					 new AuctionUpdate {auction_id = _auctionId, highest_bidder = "d"},
					 new AuctionUpdate {auction_id = _auctionId, highest_bidder = "a"},
					 new AuctionUpdate {auction_id = _auctionId, highest_bidder = "a"},
				 })
				.ToList()
				.ForEach(a =>
					         {
						         var auction = Auctions.GetOrAdd(a.auction_id, () => new Auction {Id = a.auction_id});
						         var player = auction.Players.GetOrAdd(a.highest_bidder, () => new Player {Name = a.highest_bidder});
								 //player.Count++;
						         //player.Delta++;
					         });

		}
	}
}