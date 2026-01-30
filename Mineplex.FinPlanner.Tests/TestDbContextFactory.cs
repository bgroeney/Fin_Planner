using Microsoft.EntityFrameworkCore;
using Mineplex.FinPlanner.Api.Data;
using System;

namespace Mineplex.FinPlanner.Tests
{
    public class TestDbContextFactory
    {
        public static FinPlannerDbContext Create()
        {
            var options = new DbContextOptionsBuilder<FinPlannerDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new FinPlannerDbContext(options);
        }
    }
}
