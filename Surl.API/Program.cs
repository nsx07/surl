using Microsoft.EntityFrameworkCore;
using Surl.API.Data;
using Surl.API.Model;
using Surl.API.RequestResponse.Dto;
using Surl.API.RequestResponse.ViewModel;
using Surl.API.Services.UrlShortener;
using System.Text.RegularExpressions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<IUrlShortenerService, UrlShortenerService>();
builder.Services.AddDbContext<AppDbContext>();

var app = builder.Build();

app.MapPost("/shorten", async (IUrlShortenerService urlShortenerService, AppDbContext context, ShortenUrlViewModel request) =>
{
    if (request == null || string.IsNullOrWhiteSpace(request.Url) || Uri.TryCreate(request.Url, UriKind.Absolute, out _))
    {
        return Results.BadRequest("Invalid URL provided.");
    }

    var httpsFragment = new Regex(@"^https?://", RegexOptions.IgnoreCase);
    if (!httpsFragment.IsMatch(request.Url))
    {
        request.Url = "http://" + request.Url;
    }

    var existingUrl = await context.UrlShorten
        .FirstOrDefaultAsync(u => u.OriginalUrl == request.Url);

    if (existingUrl != null)
    {
        return Results.Ok(new UrlShortenedDto
        {
            OriginalUrl = existingUrl.OriginalUrl,
            Url = urlShortenerService.FormatUrlShortened(existingUrl.Code)
        });
    }

    using var transaction = await context.Database.BeginTransactionAsync();

    var result = await urlShortenerService.ShortenUrlAsync(request);
    UrlShorten shorten = UrlShorten.CreateOne(originalUrl: result.OriginalUrl, shortenedUrl: result.Url, code: result.Code);
    context.UrlShorten.Add(shorten);
    await context.SaveChangesAsync();
    await transaction.CommitAsync();

    return Results.Ok(result);
});

app.MapDelete("shorten/{urlcode}", async (AppDbContext context, string urlcode) => 
{
    if (string.IsNullOrEmpty(urlcode))
    {
        return Results.BadRequest("Please inform a URL or code");
    }

    var existent = await context.UrlShorten.Where(x => urlcode.Equals(x.OriginalUrl) || urlcode.Equals(x.Code)).FirstOrDefaultAsync();

    if (existent == null)
    {
        return Results.NotFound("Url not found!");
    }

    context.UrlShorten.Remove(existent);
    await context.SaveChangesAsync();
    return Results.Ok();
});

app.MapGet("/r/{code}", async (AppDbContext context, string code) =>
{
    var url = await context.UrlShorten.FirstOrDefaultAsync(u => u.Code == code);
    if (url == null)
    {
        return Results.NotFound("URL not found.");
    }
    return Results.Redirect(url.OriginalUrl, permanent: true, preserveMethod: false);
});

app.Run();