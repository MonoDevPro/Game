using GameWeb.Domain.Entities;
using GameWeb.Domain.Enums;
using GameWeb.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GameWeb.Infrastructure.Data.Configurations;

public class CharacterConfiguration : IEntityTypeConfiguration<Character>
{
    public void Configure(EntityTypeBuilder<Character> builder)
    {
        builder.HasQueryFilter(c => c.IsActive);
        
        builder.Property(c => c.Name)
            .HasMaxLength(20)
            .IsRequired();
        
        builder.HasIndex(c => c.Id, "IX_Character_Id").IsUnique();
        builder.HasIndex(c => c.Name, "IX_Character_Name").IsUnique();
        
        builder.Property(c => c.Gender)
            .HasConversion(c => Enum.GetName(c), c => Enum.Parse<Gender>(c!));
        builder.Property(c => c.Vocation)
            .HasConversion(c => Enum.GetName(c), c => Enum.Parse<Vocation>(c!));
        
        builder.OwnsOne(c => c.Position, positionBuilder =>
        {
            positionBuilder.Property(p => p.X).HasColumnName("PositionX");
            positionBuilder.Property(p => p.Y).HasColumnName("PositionY");
        });

        builder.OwnsOne(c => c.Direction, directionBuilder =>
        {
            directionBuilder.Property(p => p.X).HasColumnName("DirectionX");
            directionBuilder.Property(p => p.Y).HasColumnName("DirectionY");
        });
    }
}
