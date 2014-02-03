using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Xml.Serialization;
using MadBidFetcher.Model;
using MadBidFetcher.Services;

namespace MadBidFetcher
{
    [Serializable]
    public class Auction
    {
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public DateTime? LastBidDate { get; set; }
        public int BidTimeOut { get; set; }
        public AuctionStatus Status { get; set; }
        public string Title { get; set; }

        public string AllImages { get { return Images == null ? null : string.Join(";", Images); } set { Images = value == null ? null : value.Split(';'); } }
        [NotMapped]
        public string[] Images { get; set; }
        public string Description { get; set; }
        public float CreditCost { get; set; }
        public float Price { get; set; }
        public float RetailPrice { get; set; }
        public string EndTime { get; set; }
        public string StartTime { get; set; }
        public DateTime? DateOpens { get; set; }
        public bool CurrentUserAuction { get; set; }
        public bool Pinned { get; set; }
        public int ProductId { get; set; }
        public int TimingsInterval { get; set; }

        public ICollection<Player> Players { get; set; }
        public ICollection<Bid> Bids { get; set; }

        public Auction()
        {
            Description = "";
            Title = "";
        }

        public static Auction FromModel(Model.Auction auction)
        {
            return new Auction
                {
                    Id = auction.Id,
                    LastBidDate = auction.LastBidDate,
                    BidTimeOut = auction.BidTimeOut,
                    Status = auction.Status,
                    Title = auction.Title,
                    Images = auction.Images,
                    Description = auction.Description,
                    CreditCost = auction.CreditCost,
                    Price = auction.Price,
                    RetailPrice = auction.RetailPrice,
                    EndTime = auction.EndTime,
                    StartTime = auction.StartTime,
                    DateOpens = auction.DateOpens,
                    CurrentUserAuction = auction.CurrentUserAuction,
                    Pinned = auction.Pinned,
                    ProductId = auction.ProductId,
                    TimingsInterval = auction.TimingsInterval
                };
        }
    }
}