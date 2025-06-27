namespace Surl.API.RequestResponse.Dto
{
    public class UrlShortenedStatisticsDto
    {
        public string Code { get; set; } = string.Empty;
        public string OriginalUrl { get; set; } = string.Empty;
        public string ShortenedUrl { get; set; } = string.Empty;
        public int ClickCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastAccessedAt { get; set; }
    }
}
