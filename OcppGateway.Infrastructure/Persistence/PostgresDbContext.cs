using Microsoft.EntityFrameworkCore;
using OcppGateway.Domain;
using OcppGateway.Domain.Entities;

namespace OcppGateway.Infrastructure.Persistence
{
    public class PostgresDbContext : DbContext
    {
        public PostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options)
        {
        }

        public DbSet<Charger> Chargers { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Tariff> Tariffs { get; set; }
        public DbSet<MeterValue> MeterValues { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Charger>()
                .HasKey(c => c.ChargePointId);

            modelBuilder.Entity<Charger>()
                .HasOne(c => c.Tariff)
                .WithMany(t => t.Chargers)
                .HasForeignKey(c => c.TariffId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Tariff)
                .WithMany()
                .HasForeignKey(u => u.TariffId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Transaction>()
                .HasKey(t => t.TransactionId);

            modelBuilder.Entity<Transaction>()
                .HasOne(t => t.Charger)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.ChargePointId);

            modelBuilder.Entity<MeterValue>()
                .HasKey(m => m.Id);
        }
    }
}
