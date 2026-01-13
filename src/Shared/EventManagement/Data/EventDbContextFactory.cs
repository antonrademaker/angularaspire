using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Shared.EventManagement.Data;

public class EventDbContextFactory : IDesignTimeDbContextFactory<EventDbContext>
{
    public EventDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EventDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=eventdb;Username=postgres;Password=postgres");

        return new EventDbContext(optionsBuilder.Options);
    }
}
