using HuffmanFileCompressor.App.Data;
using Microsoft.EntityFrameworkCore;
using log4net; 
using log4net.Config;
using System.Reflection;
using Microsoft.Extensions.Logging; // Required for ILogger

var builder = WebApplication.CreateBuilder(args);

// --- LOGGING CONFIGURATION ---
// 1. Clear any default logging providers (optional, but common for custom logging)
builder.Logging.ClearProviders();
// 2. Add log4net provider - ensure this is AFTER ClearProviders if you use it.
builder.Logging.AddLog4Net();

// Configure log4net from the XML file
var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
// --- END LOGGING CONFIGURATION ---

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers();


// Add db context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"))
);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Used for serving static files, like client-side assets

app.MapControllers(); // Maps controller routes

app.Run();