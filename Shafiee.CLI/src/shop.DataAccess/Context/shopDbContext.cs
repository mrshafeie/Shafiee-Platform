namespace shop.DataAccess.Context;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ;

public sealed class ApplicationEfContext : DbContext, IUnitOfWork
{
    public ApplicationEfContext(DbContextOptions<ApplicationEfContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationEfContext).Assembly);
    }

    public override int SaveChanges()
    {
        ApplyRules();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyRules();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyRules()
    {
        // Apply Audit rules
        // Apply Soft Delete rules
        // Apply Persian text normalization
        // Apply other shared persistence rules
    }
}