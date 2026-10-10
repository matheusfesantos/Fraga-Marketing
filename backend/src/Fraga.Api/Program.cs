using System.Text.Json;
using System.Text.Json.Serialization;
using Fraga.Api.Services;
using Fraga.Application.Abstractions;
using Fraga.Application.Accounts;
using Fraga.Application.Transactions;
using Fraga.Infrastructure.Data;
using Fraga.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;


const string CorsPolicyName = "FrontendPolicy";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Contrato: "CREDIT" | "DEBIT" na entrada e na saída.
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(
                JsonNamingPolicy.SnakeCaseUpper,
                allowIntegerValues: false));
    });

builder.Services.AddOpenApi();

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgres");

builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddSingleton<ILogService, LogService>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbInitializer.InitializeAsync(context);
}

app.UseExceptionHandler();

app.MapOpenApi();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Fraga API v1");
});

// Em container só há HTTP; o redirecionamento fica restrito ao desenvolvimento local.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors(CorsPolicyName);

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();