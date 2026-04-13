using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ProjectDataLib.Data
{
    public interface ITagRepository : IAsyncDisposable
    {
        Task InitializeAsync(string databasePath);
        Task AddTagAsync(string name, double value, DateTime stamp);
        Task RemoveTagByNameAsync(string name);
        Task<List<TagDTO>> GetAllTagsAsync(bool descending = true);
        Task<List<TagDTO>> GetTagsByStampAsync(DateTime from, DateTime to, bool descending = true);
        Task<List<TagDTO>> GetTagsByNameAsync(string tagName, DateTime from, DateTime to, bool descending = true);
        Task ClearAllTagsAsync();
    }

    public class TagRepository : ITagRepository
    {
        private string _databasePath;

        public async Task InitializeAsync(string databasePath)
        {
            var directory = Path.GetDirectoryName(databasePath);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            _databasePath = databasePath;

            await EnsureEfSchemaCompatibilityAsync();

            using (var context = new TagDbContext(_databasePath))
            {
                await context.Database.EnsureCreatedAsync();
            }
        }

        private async Task EnsureEfSchemaCompatibilityAsync()
        {
            if (!File.Exists(_databasePath))
                return;

            if (!await IsLegacySchemaAsync())
                return;

            string tempDatabasePath = _databasePath + ".ef-migrated";
            string backupDatabasePath = _databasePath + ".legacy.bak";

            if (File.Exists(tempDatabasePath))
                File.Delete(tempDatabasePath);

            List<LegacyTagEntity> legacyTags;
            using (var legacyContext = new LegacyTagDbContext(_databasePath))
            {
                legacyTags = await legacyContext.Tags.AsNoTracking().ToListAsync();
            }

            using (var newContext = new TagDbContext(tempDatabasePath))
            {
                await newContext.Database.EnsureCreatedAsync();

                if (legacyTags.Count > 0)
                {
                    newContext.Tags.AddRange(legacyTags.Select(x => new TagEntity
                    {
                        Name = x.Name,
                        Stamp = x.Stamp,
                        Value = x.Value
                    }));

                    await newContext.SaveChangesAsync();
                }
            }

            ReplaceDatabaseFilesWithRetry(tempDatabasePath, backupDatabasePath);
        }

        private void ReplaceDatabaseFilesWithRetry(string tempDatabasePath, string backupDatabasePath)
        {
            const int maxAttempts = 8;
            const int delayMs = 250;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    SqliteConnection.ClearAllPools();

                    if (File.Exists(backupDatabasePath))
                        File.Delete(backupDatabasePath);

                    File.Move(_databasePath, backupDatabasePath, true);
                    File.Move(tempDatabasePath, _databasePath, true);
                    return;
                }
                catch (IOException) when (attempt < maxAttempts)
                {
                    Task.Delay(delayMs).GetAwaiter().GetResult();
                }
            }

            throw new IOException($"Cannot replace database file '{_databasePath}'. The file is locked by another process.");
        }

        private async Task<bool> IsLegacySchemaAsync()
        {
            try
            {
                using var context = new TagDbContext(_databasePath);
                await context.Tags.Select(t => t.Id).Take(1).ToListAsync();
                return false;
            }
            catch (SqliteException ex) when (ex.Message.Contains("no such column", StringComparison.OrdinalIgnoreCase)
                                             && ex.Message.Contains("Id", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        private sealed class LegacyTagDbContext : DbContext
        {
            private readonly string _dbPath;

            public DbSet<LegacyTagEntity> Tags { get; set; }

            public LegacyTagDbContext(string dbPath)
            {
                _dbPath = dbPath;
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseSqlite($"Data Source={_dbPath};");
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<LegacyTagEntity>(entity =>
                {
                    entity.ToTable("Tags");
                    entity.HasNoKey();
                    entity.Property(e => e.Name).IsRequired();
                    entity.Property(e => e.Stamp).IsRequired();
                    entity.Property(e => e.Value).IsRequired();
                });
            }
        }

        private sealed class LegacyTagEntity
        {
            public string Name { get; set; }
            public DateTime Stamp { get; set; }
            public double Value { get; set; }
        }

        public async Task AddTagAsync(string name, double value, DateTime stamp)
        {
            if (string.IsNullOrEmpty(_databasePath))
                throw new InvalidOperationException("Repository not initialized. Call InitializeAsync first.");

            using (var context = new TagDbContext(_databasePath))
            {
                var entity = new TagEntity
                {
                    Name = name,
                    Value = value,
                    Stamp = stamp
                };

                context.Tags.Add(entity);
                await context.SaveChangesAsync();
            }
        }

        public async Task RemoveTagByNameAsync(string name)
        {
            if (string.IsNullOrEmpty(_databasePath))
                throw new InvalidOperationException("Repository not initialized. Call InitializeAsync first.");

            using (var context = new TagDbContext(_databasePath))
            {
                var tags = await context.Tags.Where(t => t.Name == name).ToListAsync();
                context.Tags.RemoveRange(tags);
                await context.SaveChangesAsync();
            }
        }

        public async Task<List<TagDTO>> GetAllTagsAsync(bool descending = true)
        {
            if (string.IsNullOrEmpty(_databasePath))
                throw new InvalidOperationException("Repository not initialized. Call InitializeAsync first.");

            using (var context = new TagDbContext(_databasePath))
            {
                var query = context.Tags.AsQueryable();

                if (descending)
                    query = query.OrderByDescending(t => t.Stamp);
                else
                    query = query.OrderBy(t => t.Stamp);

                var entities = await query.ToListAsync();

                return entities.Select(e => new TagDTO
                {
                    Name = e.Name,
                    Stamp = e.Stamp,
                    Value = e.Value
                }).ToList();
            }
        }

        public async Task<List<TagDTO>> GetTagsByStampAsync(DateTime from, DateTime to, bool descending = true)
        {
            if (string.IsNullOrEmpty(_databasePath))
                throw new InvalidOperationException("Repository not initialized. Call InitializeAsync first.");

            using (var context = new TagDbContext(_databasePath))
            {
                var query = context.Tags
                    .Where(t => t.Stamp >= from && t.Stamp <= to);

                if (descending)
                    query = query.OrderByDescending(t => t.Stamp);
                else
                    query = query.OrderBy(t => t.Stamp);

                var entities = await query.ToListAsync();

                return entities.Select(e => new TagDTO
                {
                    Name = e.Name,
                    Stamp = e.Stamp,
                    Value = e.Value
                }).ToList();
            }
        }

        public async Task<List<TagDTO>> GetTagsByNameAsync(string tagName, DateTime from, DateTime to, bool descending = true)
        {
            if (string.IsNullOrEmpty(_databasePath))
                throw new InvalidOperationException("Repository not initialized. Call InitializeAsync first.");

            using (var context = new TagDbContext(_databasePath))
            {
                var query = context.Tags
                    .Where(t => t.Name == tagName && t.Stamp >= from && t.Stamp <= to);

                if (descending)
                    query = query.OrderByDescending(t => t.Stamp);
                else
                    query = query.OrderBy(t => t.Stamp);

                var entities = await query.ToListAsync();

                return entities.Select(e => new TagDTO
                {
                    Name = e.Name,
                    Stamp = e.Stamp,
                    Value = e.Value
                }).ToList();
            }
        }

        public async Task ClearAllTagsAsync()
        {
            if (string.IsNullOrEmpty(_databasePath))
                throw new InvalidOperationException("Repository not initialized. Call InitializeAsync first.");

            using (var context = new TagDbContext(_databasePath))
            {
                var allTags = await context.Tags.ToListAsync();
                context.Tags.RemoveRange(allTags);
                await context.SaveChangesAsync();
            }
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            await ValueTask.CompletedTask;
        }
    }
}
