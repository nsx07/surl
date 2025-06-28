using Microsoft.EntityFrameworkCore;
using Surl.API.Broker;
using Surl.API.Data;
using Surl.API.HostedService;
using Surl.API.RequestResponse.ViewModel;
using Surl.API.Services.UrlShortener;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTransient<IUrlShortenerService, UrlShortenerService>();
builder.Services.AddDbContext<AppDbContext>();
builder.Services.AddTransient<IMessageProducer, MessageProducerRabbitMQ>();
builder.Services.AddHostedService<ProcessClicksHostedService>();
builder.Services.AddMemoryCache();

var app = builder.Build();

app.MapPost("/shorten", async (IUrlShortenerService urlShortenerService, ShortenUrlViewModel request) =>
{
    try
    {
        return Results.Ok(await urlShortenerService.ShortenUrlAsync(request));
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

app.MapDelete("shorten/{urlcode}", async (IUrlShortenerService urlShortenerService, string urlcode) => 
{
    try
    {
        await urlShortenerService.DeleteShortenUrlAsync(urlcode);
        return Results.Ok();
    }
    catch (ArgumentException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message, statusCode: 500);
    }
});

app.MapGet("/r/{code}", async (IUrlShortenerService urlShortenerService, HttpContext httpContext, string code) =>
{
    try
    {
        string? ipAddress = httpContext.Request.Headers.Where(h => h.Key == "X-Forwarded-For").Select(h => h.Value.ToString()).FirstOrDefault() 
            ?? httpContext.Connection.RemoteIpAddress?.ToString();

        string url = await urlShortenerService.GetLinkAsync(code, httpContext.Request.Headers, ipAddress);
        return Results.Redirect(url, permanent: true, preserveMethod: false);
    } catch (ArgumentException exception)
    {
        return Results.BadRequest(exception.Message);
    } catch (KeyNotFoundException exception)
    {
        return Results.NotFound(exception.Message);
    } catch (Exception exception)
    {
        return Results.Problem(exception.Message);
    }
});

app.MapGet("/shorts", async (AppDbContext context) => Results.Ok((await context.UrlShorten.Include(x => x.UrlShortenAccesses).ToListAsync()).Select(x => new
{
    x.Id,
    x.ShortenedUrl,
    x.OriginalUrl,
    x.ClickCount,
    x.Code,
    x.LastAccessedAt,
    Access = x.UrlShortenAccesses.Select(s => new
    {
        s.IpAddress,
        s.HeadersRaw,
    })
})));

app.Run();