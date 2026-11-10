using CourtBook.Application;
using CourtBook.Application.Common.Interfaces;
using CourtBook.Functions;
using CourtBook.Infrastructure;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

var builder = FunctionsApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("CourtBookDb")
    ?? throw new InvalidOperationException("Connection string 'CourtBookDb' is missing.");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<ICurrentUser, BackgroundJobCurrentUser>();

builder.Build().Run();