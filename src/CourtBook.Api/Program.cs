using CourtBook.Infrastructure;
using Scalar.AspNetCore;
using CourtBook.Application;
using CourtBook.Api.ErrorHandling;
using System.Text.Json.Serialization;
using CourtBook.Application.Bookings.Pricing;
using CourtBook.Infrastructure.Identity;
using CourtBook.Infrastructure.Authentication;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("CourtBookDb")
    ?? throw new InvalidOperationException("Connection string 'CourtBookDb' was not found.");

builder.Services.AddApplication();
builder.Services.Configure<PricingOptions>(
    builder.Configuration.GetSection(PricingOptions.SectionName));
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseExceptionHandler();
await IdentitySeeder.SeedRolesAsync(app.Services);
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
