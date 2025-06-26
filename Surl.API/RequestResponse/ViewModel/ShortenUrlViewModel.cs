namespace Surl.API.RequestResponse.ViewModel
{
    public record ShortenUrlViewModel
    {
        public required string Url { get; set; } = string.Empty;
    }
}
