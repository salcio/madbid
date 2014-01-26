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
				.ThenByDescending(a => a.Status)
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
				Players = auction
					.Players
					.Values
					.OrderByDescending(a=>a.Count)
					.ToList(),
				Auction = auction,
				AutoUpdate = false
			};
			return View(model);
		}

		public ActionResult Latest(int id, int skipCountLessThen = 100)
		{
			Auction auction;
			if (!Updater.Instance.Auctions.TryGetValue(id, out auction))
				return Index();
			var model = new AuctionModel
			{
				Players = auction
					.Players
					.Values
					.Where(c => c.Count >= skipCountLessThen || c.Delta > 0)
					.OrderByDescending(g => g.IsHighPlayer())
					.ThenByDescending(g => g.Delta)
					.ThenByDescending(g => g.Count)
					.ToList(),
				Auction = auction,
				AutoUpdate = true
			};

			return View("Show", model);
		}

		public ActionResult Reset(int id)
		{
			Updater.Instance.Auctions[id].ResetDeltas();
			return new ContentResult() { Content = "OK" };
		}
	}
}
