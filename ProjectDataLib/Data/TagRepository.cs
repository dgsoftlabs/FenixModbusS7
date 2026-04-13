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
            
            // Ensure database is created
            using (var context = new TagDbContext(_databasePath))
            {
                await context.Database.EnsureCreatedAsync();
            }
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
            // No persistent context to dispose
            await ValueTask.CompletedTask;
        }
    }
}
