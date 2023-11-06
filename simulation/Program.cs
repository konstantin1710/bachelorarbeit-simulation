using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Rewrite;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.OpenApi.Models;
using simulation.Extensions;
using simulation.Managers.Articles;
using simulation.Managers.Path;
using simulation.Managers.Picklists;
using simulation.Managers.Simulation;
using simulation.Managers.Stock;
using simulation.Managers.Strategies;
using simulation.Providers.Content;
using simulation.Repository.Articles;
using simulation.Repository.Pickpool;
using simulation.Repository.Reservation;
using simulation.Repository.Stock;
using simulation.Repository.Storage;
using simulation.Repository.Warehouse;

var builder = WebApplication.CreateBuilder(args);

var environment = builder.Environment.EnvironmentName;

builder.Services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "Test01", Version = "v1" }); c.AddSecurityRequirement(new OpenApiSecurityRequirement { { new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }, new string[] { } } }); });
builder.Services.AddAuthorization(options => { options.AddPolicy("GetAccess", policy => { policy.RequireRole("admin"); }); });

#region configure connection strings

var connectionStringsPath = builder.Configuration.GetValue<string>("ConnectionStringsPath");
ArgumentNullException.ThrowIfNull(connectionStringsPath);
builder.Configuration.AddJsonFile(connectionStringsPath, false);

#endregion

#region configure CORS

builder.Services.ConfigureCors(environment);

#endregion

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<IArticleManager, ArticleManager>();
builder.Services.AddScoped<IPathManager, PathManager>();
builder.Services.AddScoped<IPicklistManager, PicklistManager>();
builder.Services.AddScoped<ISimulationManager, SimulationManager>();
builder.Services.AddScoped<IStockManager, StockManager>();
builder.Services.AddScoped<IStrategyManager, StrategyManager>();
builder.Services.AddScoped<IContentProvider, ContentProvider>();
builder.Services.AddScoped<IArticleRepository, ArticleRepository>();
builder.Services.AddScoped<IPickpoolRepository, PickpoolRepository>();
builder.Services.AddScoped<IReservationRepository, ReservationRepository>();
builder.Services.AddScoped<IStockRepository, StockRepository>();
builder.Services.AddScoped<IStorageRepository, StorageRepository>();
builder.Services.AddScoped<IWarehouseRepository, WarehouseRepository>();

builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var option = new RewriteOptions();
option.AddRedirect("^$", "swagger");
app.UseRewriter(option);

app.UseAuthorization();
app.UseCookiePolicy();
app.UseAuthentication();

app.UseCors("Private");

app.MapControllers();

app.Run();
