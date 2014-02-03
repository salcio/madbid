namespace MadBidFetcher.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Init : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Auctions",
                c => new
                    {
                        Id = c.Int(nullable: false),
                        LastBidDate = c.DateTime(),
                        BidTimeOut = c.Int(nullable: false),
                        Status = c.Int(nullable: false),
                        Title = c.String(),
                        AllImages = c.String(),
                        Description = c.String(),
                        CreditCost = c.Single(nullable: false),
                        Price = c.Single(nullable: false),
                        RetailPrice = c.Single(nullable: false),
                        EndTime = c.String(),
                        StartTime = c.String(),
                        DateOpens = c.DateTime(),
                        CurrentUserAuction = c.Boolean(nullable: false),
                        Pinned = c.Boolean(nullable: false),
                        ProductId = c.Int(nullable: false),
                        TimingsInterval = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Bids",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        AuctionId = c.Int(nullable: false),
                        PlayerName = c.String(maxLength: 128),
                        Value = c.Single(nullable: false),
                        Time = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Auctions", t => t.AuctionId, cascadeDelete: true)
                .ForeignKey("dbo.Players", t => t.PlayerName)
                .Index(t => t.AuctionId)
                .Index(t => t.PlayerName);
            
            CreateTable(
                "dbo.Players",
                c => new
                    {
                        Name = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Name);
            
            CreateTable(
                "dbo.PlayerAuctions",
                c => new
                    {
                        Player_Name = c.String(nullable: false, maxLength: 128),
                        Auction_Id = c.Int(nullable: false),
                    })
                .PrimaryKey(t => new { t.Player_Name, t.Auction_Id })
                .ForeignKey("dbo.Players", t => t.Player_Name, cascadeDelete: true)
                .ForeignKey("dbo.Auctions", t => t.Auction_Id, cascadeDelete: true)
                .Index(t => t.Player_Name)
                .Index(t => t.Auction_Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Bids", "PlayerName", "dbo.Players");
            DropForeignKey("dbo.PlayerAuctions", "Auction_Id", "dbo.Auctions");
            DropForeignKey("dbo.PlayerAuctions", "Player_Name", "dbo.Players");
            DropForeignKey("dbo.Bids", "AuctionId", "dbo.Auctions");
            DropIndex("dbo.Bids", new[] { "PlayerName" });
            DropIndex("dbo.PlayerAuctions", new[] { "Auction_Id" });
            DropIndex("dbo.PlayerAuctions", new[] { "Player_Name" });
            DropIndex("dbo.Bids", new[] { "AuctionId" });
            DropTable("dbo.PlayerAuctions");
            DropTable("dbo.Players");
            DropTable("dbo.Bids");
            DropTable("dbo.Auctions");
        }
    }
}
