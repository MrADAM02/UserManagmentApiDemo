using System.Net;
using System.Text.Json;
using Microsoft.OpenApi.Models; // Add this for Swagger support

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Error-handling middleware first
app.UseMiddleware<ErrorHandlingMiddleware>();

// Authentication middleware next
//app.UseMiddleware<TokenAuthenticationMiddleware>();

// Logging middleware last
app.UseMiddleware<RequestResponseLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();



public class RequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

    public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        // Log request
        _logger.LogInformation("Incoming Request: {method} {path}", context.Request.Method, context.Request.Path);

        await _next(context);

        // Log response
        _logger.LogInformation("Outgoing Response: {statusCode}", context.Response.StatusCode);
    }
}


public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred.");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var errorResponse = JsonSerializer.Serialize(new { error = "Internal server error." });
            await context.Response.WriteAsync(errorResponse);
        }
    }
}

public class TokenAuthenticationMiddleware
{
    private readonly RequestDelegate _next;

    public TokenAuthenticationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var token = context.Request.Headers["Authorization"].ToString();
        if (string.IsNullOrEmpty(token) || !IsValidToken(token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        await _next(context);
    }

    private bool IsValidToken(string token)
    {
        // Example: Accepts "Bearer mysecrettoken" as valid
        return token == "Bearer mysecrettoken";
    }
}