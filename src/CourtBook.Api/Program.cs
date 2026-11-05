using CourtBook.Infrastructure;
using Scalar.AspNetCore;
using CourtBook.Application;



var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("CourtBookDb")
    ?? throw new InvalidOperationException("Connection string 'CourtBookDb' was not found.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

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
