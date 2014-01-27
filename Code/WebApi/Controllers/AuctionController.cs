using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MadBidFetcher.Model;
using MadBidFetcher.Services;

namespace WebApi.Controllers
{
	public class AuctionController : Controller
	{
		public ActionResult Index()
		{
			return View(Updater.Instance.Auctions.Values
				.OrderByDescending(a => a.Status == AuctionStatus.Running)
				.ThenByDescending(a => a.CurrentUserAuction)
				.ThenByDescending(a => a.Status)
				//.ThenByDescending(a => a.ActivePlayers[Auction.ActivaPlayersTimes[0]].Count)
				//.ThenByDescending(a => a.ActivePlayers[Auction.ActivaPlayersTimes[1]].Count)
				.ThenBy(a => a.BidTimeOut)
				.ToList());
		}

		//public ActionResult Populate()
		//{
		//	if (Request.HttpMethod == "GET")
		//	return View();
		//	Updater.Instance.
		//}

		public ActionResult Show(int id)
		{
			Auction auction;
			if (!Updater.Instance.Auctions.TryGetValue(id, out auction))
				return Index();
			var model = new AuctionModel
			{
				Auction = auction,
			};
			return View(model);
		}

	}
}
