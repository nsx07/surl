using System.Text.Json.Serialization;

namespace Surl.API.RequestResponse.Dto
{
    public record UrlShortenedDto
    {
        public string OriginalUrl { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        [JsonIgnore]
        public string Code { get; set; } = string.Empty;
    }
}
