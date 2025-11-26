using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ReverseSentence.Models;

namespace ReverseSentence.Repositories
{
    public class ReverseRepository : IReverseRepository
    {
        private readonly IMongoCollection<ReverseRequest> collection;
        private readonly ILogger<ReverseRepository> logger;
        private static bool indexesCreated = false;
        private static readonly object indexLock = new();

        public ReverseRepository(IMongoClient mongoClient, IOptions<MongoDbSettings> settings, ILogger<ReverseRepository> logger)
        {
            if (mongoClient == null) throw new ArgumentNullException(nameof(mongoClient));
            if (settings?.Value == null) throw new ArgumentNullException(nameof(settings));
            
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var database = mongoClient.GetDatabase(settings.Value.DatabaseName);
            collection = database.GetCollection<ReverseRequest>(settings.Value.CollectionName);

            EnsureIndexesAsync().GetAwaiter().GetResult();
        }

        private async Task EnsureIndexesAsync()
        {
            if (indexesCreated) return;

            lock (indexLock)
            {
                if (indexesCreated) return;

                try
                {
                    var indexKeysDefinition = Builders<ReverseRequest>.IndexKeys
                        .Text(r => r.OriginalSentence)
                        .Text(r => r.ReversedSentence);

                    var indexOptions = new CreateIndexOptions { Background = true };
                    var indexModel = new CreateIndexModel<ReverseRequest>(indexKeysDefinition, indexOptions);
                    
                    collection.Indexes.CreateOneAsync(indexModel).GetAwaiter().GetResult();
                    logger.LogInformation("Database text indexes created successfully");
                    indexesCreated = true;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to create database indexes. Application will continue but search performance may be degraded.");
                }
            }
        }

        public async Task<ReverseRequest> CreateAsync(ReverseRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            
            await collection.InsertOneAsync(request);
            return request;
        }

        public async Task<(IEnumerable<ReverseRequest> Items, long TotalCount)> GetPagedAsync(string userId, int page, int pageSize)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (page < 1) throw new ArgumentException("Page must be greater than 0", nameof(page));
            if (pageSize < 1) throw new ArgumentException("PageSize must be greater than 0", nameof(pageSize));

            var filter = Builders<ReverseRequest>.Filter.Eq(r => r.UserId, userId);
            var totalCount = await collection.CountDocumentsAsync(filter);

            var items = await collection.Find(filter)
                .SortByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<IEnumerable<ReverseRequest>> SearchByWordAsync(string userId, string word)
        {
            if (string.IsNullOrEmpty(userId)) throw new ArgumentNullException(nameof(userId));
            if (string.IsNullOrEmpty(word)) throw new ArgumentNullException(nameof(word));

            var userFilter = Builders<ReverseRequest>.Filter.Eq(r => r.UserId, userId);
            
            // Exact match filter - matches complete sentence only
            var wordFilter = Builders<ReverseRequest>.Filter.Or(
                Builders<ReverseRequest>.Filter.Eq(r => r.OriginalSentence, word),
                Builders<ReverseRequest>.Filter.Eq(r => r.ReversedSentence, word)
            );

            var combinedFilter = Builders<ReverseRequest>.Filter.And(userFilter, wordFilter);

            return await collection.Find(combinedFilter)
                .SortByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
