using ChessAnalyzer.Application.Features.GameAnalysis.Commands;
using ChessAnalyzer.Application.Interfaces.Chess;
using ChessAnalyzer.Infrastructure.Chess;
using ChessAnalyzer.Infrastructure.Chess.Stockfish;
using MediatR;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Support Render / Cloud dynamic PORT assignment
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

builder.Services.AddScoped<IChessEngine, StockfishChessEngine>();

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddMediatR(typeof(AnalyzeGameCommandHandler).Assembly);
builder.Services.AddScoped<IPgnParser, PgnParser>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularClient", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.Configure<StockfishOptions>(
    builder.Configuration.GetSection("Stockfish"));

var app = builder.Build();

// Forward headers from reverse proxy (Render edge routers)
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Serve Angular static SPA assets if deployed together in wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors("AngularClient");

app.UseAuthorization();

app.MapControllers();

// SPA fallback so Angular client routing works on direct refresh
var wwwrootPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
if (Directory.Exists(wwwrootPath) && File.Exists(Path.Combine(wwwrootPath, "index.html")))
{
    app.MapFallbackToFile("index.html");
}

app.Run();
