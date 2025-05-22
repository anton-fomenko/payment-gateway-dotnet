using Microsoft.Extensions.Options;

using PaymentGateway.Api.Configuration;
using PaymentGateway.Api.Mappings;
using PaymentGateway.Api.Validation;
using PaymentGateway.Infrastructure.Clients;
using PaymentGateway.Infrastructure.Repositories;
using PaymentGateway.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAutoMapper(typeof(MappingProfile));

// Used for Idempotency Keys
builder.Services.AddMemoryCache();

builder.Services.Configure<BankOptions>(
    builder.Configuration.GetSection(nameof(BankOptions)));

builder.Services.Configure<IdempotencyOptions>(
    builder.Configuration.GetSection(nameof(IdempotencyOptions)));

// Add services to the container.
builder.Services
    .AddControllers(options => options.Filters.Add<ValidationFilter>())
    .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);

builder.Services.AddScoped<ValidationFilter>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IPaymentsRepository, PaymentsRepository>();
builder.Services.AddScoped<IPaymentsService, PaymentsService>();
builder.Services.AddScoped<IBankClient, BankClient>();

builder.Services.AddHttpClient<IBankClient, BankClient>((serviceProvider, client) =>
{
    var acquiringBankSettings = serviceProvider.GetRequiredService<IOptions<BankOptions>>().Value;
    client.BaseAddress = new Uri(acquiringBankSettings.BaseUri);
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Set the URL Kestrel listens on using the PORT environment variable provided by Heroku
var port = Environment.GetEnvironmentVariable("PORT");

if(!String.IsNullOrWhiteSpace(port))
{
    builder.WebHost.UseUrls($"http://*:{port}");
}

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

// To make this file visible for WebApplicationFactory in Integration Tests
public partial class Program { }