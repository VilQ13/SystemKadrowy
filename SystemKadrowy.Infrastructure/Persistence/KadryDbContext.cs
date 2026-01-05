using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SystemKadrowy.Core.Domain;

namespace SystemKadrowy.Infrastructure.Persistence
{
    public class KadryDbContext : DbContext
    {
        public KadryDbContext(DbContextOptions<KadryDbContext> options) : base(options)
        {
        }

        public DbSet<Pracownik> Pracownicy { get; set; } 
        public DbSet<Umowa> Umowy { get; set; }
        public DbSet<Wyplata> Wyplaty { get; set; }
        public DbSet<Nieobecnosc> Nieobecnosci { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Umowa>()
                .Property(u => u.StawkaBrutto)
                .HasColumnType("decimal(18,2)");

            modelBuilder.Entity<Pracownik>()
                .HasMany(p => p.Umowy)
                .WithOne(u => u.Pracownik)
                .HasForeignKey(u => u.PracownikId);

            foreach (var property in modelBuilder.Entity<Wyplata>().Metadata.GetProperties())
            {
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                {
                    property.SetColumnType("decimal(18, 2)");
                }
            }

            foreach (var property in modelBuilder.Entity<Nieobecnosc>().Metadata.GetProperties())
            {
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                {
                    property.SetColumnType("decimal(18, 2)");
                }
            }


            base.OnModelCreating(modelBuilder);
        }
    }
}
