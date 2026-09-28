using System.Text.Json.Serialization;

using PaymentGateway.Api.Middleware;
using PaymentGateway.Api.Observability;
using PaymentGateway.Application;
using PaymentGateway.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddObservability();
builder.AddApplication();
builder.Services.AddInfrastructure();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Keeps serializer messages, which name .NET types, out of 400 responses.
        options.AllowInputFormatterExceptionMessages = false;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseObservability();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.MapHealthEndpoints();

app.Run();
