namespace Surl.API.RequestResponse.Dto
{
    public class UrlAccessProcessingMessage
    {
        public Guid UrlId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Headers { get; set; } = string.Empty;
        public string? IpAddress { get; set; } = string.Empty;
        public DateTime AccessedAt { get; set; } = DateTime.UtcNow;
    }
}
