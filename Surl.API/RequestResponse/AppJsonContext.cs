using System.Text.Json.Serialization;
using Surl.API.RequestResponse.Dto;
using Surl.API.RequestResponse.ViewModel;

[JsonSerializable(typeof(ShortenUrlViewModel))]
[JsonSerializable(typeof(UrlShortenedDto))]
public partial class AppJsonContext : JsonSerializerContext
{
}
