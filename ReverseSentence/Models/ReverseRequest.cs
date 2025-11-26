using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ReverseSentence.Models
{
    public class ReverseRequest
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("originalSentence")]
        public string OriginalSentence { get; set; } = string.Empty;

        [BsonElement("reversedSentence")]
        public string ReversedSentence { get; set; } = string.Empty;

        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
