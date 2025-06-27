using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Surl.API.Model
{
    public class UrlShorten
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }
        public required string OriginalUrl { get; set; }
        public required string ShortenedUrl { get; set; }
        public required string Code { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public int ClickCount { get; set; }
        public IList<UrlShortenAccess> UrlShortenAccesses { get; } = [];

        public static UrlShorten CreateOne(string originalUrl, string shortenedUrl, string code)
        {
            return new UrlShorten { Code = code, OriginalUrl = originalUrl, ShortenedUrl = shortenedUrl, CreatedAt = DateTime.UtcNow };
        }
    }
}
