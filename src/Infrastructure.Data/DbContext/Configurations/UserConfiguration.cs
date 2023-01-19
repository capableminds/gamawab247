﻿using Domain.Entities.Identities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.DbContext.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            //builder.HasOne(x => x.Business)
            //    .WithMany()
            //    .HasForeignKey(x => x.BusinessId)
            //    .OnDelete(DeleteBehavior.NoAction);
            builder.HasIndex(x => x.Email).IsUnique();

            builder.Property(x => x.Email).IsRequired();
            builder.Property(x => x.FirstName).IsRequired();
            builder.Property(x => x.LastName).IsRequired();
        }
        public static void ApplyUserIdentityConfigurations(ModelBuilder builder)
        {
            builder.Entity<User>(entity =>
            {
                entity.ToTable(name: "Users");
            });

            builder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);

            builder.Entity<Role>(entity =>
            {
                entity.ToTable(name: "Roles");
            });
            builder.Entity<UserRole>(entity =>
            {
                entity.ToTable("UserRoles");
            });

            builder.Entity<UserClaim>(entity =>
            {
                entity.ToTable("UserClaims");
            });

            builder.Entity<UserLogin>(entity =>
            {
                entity.ToTable("UserLogins");
            });

            builder.Entity<RoleClaim>(entity =>
            {
                entity.ToTable("RoleClaims");
            });
            
            builder.Entity<UserToken>(entity =>
            {
                entity.ToTable("UserToken");
            });
        }
    }
}
