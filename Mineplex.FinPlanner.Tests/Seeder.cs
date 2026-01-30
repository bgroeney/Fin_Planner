using Mineplex.FinPlanner.Api.Data;
using Mineplex.FinPlanner.Api.Models;
using System;
using System.Collections.Generic;

namespace Mineplex.FinPlanner.Tests
{
    public static class Seeder
    {
        public static (User User, Portfolio Portfolio, Account Account, Asset Asset) SeedBasicPortfolio(FinPlannerDbContext context)
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "test@example.com",
                Role = "Editor",
                CreatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);

            var portfolio = new Portfolio
            {
                Id = Guid.NewGuid(),
                OwnerId = user.Id,
                Name = "Test Portfolio",
                TargetAllocation = "{}"
            };
            context.Portfolios.Add(portfolio);

            var account = new Account
            {
                Id = Guid.NewGuid(),
                PortfolioId = portfolio.Id,
                AccountName = "Test Account",
                AccountNumber = "123456",
                Provider = "Test Bank",
                ProductType = "Brokerage"
            };
            context.Accounts.Add(account);

            var asset = new Asset
            {
                Id = Guid.NewGuid(),
                Symbol = "AAPL",
                Name = "Apple Inc.",
                AssetType = "Stock",
                Market = "US"
            };
            context.Assets.Add(asset);

            context.SaveChanges();

            return (user, portfolio, account, asset);
        }

        public static void SeedTransaction(FinPlannerDbContext context, Account account, Asset asset, DateTime date, decimal units, decimal amount, TransactionType type)
        {
            var tx = new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = account.Id,
                AssetId = asset.Id,
                Type = type,
                Units = units,
                Amount = amount,
                EffectiveDate = date,
                Narration = "Test Transaction",
                AttachedOrder = 0
            };
            context.Transactions.Add(tx);
            context.SaveChanges();
        }

        public static void SeedHistoricalPrice(FinPlannerDbContext context, Asset asset, DateTime date, decimal price)
        {
            var hp = new HistoricalPrice
            {
                Id = Guid.NewGuid(),
                AssetId = asset.Id,
                Date = date.Date,
                ClosePrice = price
            };
            context.HistoricalPrices.Add(hp);
            context.SaveChanges();
        }
    }
}
