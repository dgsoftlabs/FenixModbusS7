using Microsoft.EntityFrameworkCore;
using System;
using System.IO;

namespace ProjectDataLib.Data
{
    public class TagDbContext : DbContext
    {
        private readonly string _databasePath;

        public DbSet<TagEntity> Tags { get; set; }

        public TagDbContext(string databasePath)
        {
            _databasePath = databasePath;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = $"Data Source={_databasePath};";
            optionsBuilder
                .UseSqlite(connectionString)
                .EnableSensitiveDataLogging(false);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TagEntity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                entity.Property(e => e.Stamp)
                    .IsRequired();
                entity.Property(e => e.Value)
                    .IsRequired();

                entity.HasIndex(e => new { e.Name, e.Stamp });
                entity.HasIndex(e => e.Stamp);
            });
        }
    }
}
