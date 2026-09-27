using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ;

namespace shop.DataAccess.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(x => x.Id);




        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(250);



        builder.Property(x => x.Price)
            .IsRequired()
            .HasPrecision(18, 2);



        builder.Property(x => x.Stock)
            .IsRequired()
;



    }
}