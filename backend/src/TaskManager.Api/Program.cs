using System.Diagnostics;
using System.Text.Json.Serialization;
using TaskManager.Api.ErrorHandling;
using TaskManager.Application;
using TaskManager.Infrastructure;
using TaskManager.Infrastructure.Persistence;

const string FrontendCorsPolicy = "Frontend";

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Para que Swagger también muestre los enums como texto y no como números
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
        context.ProblemDetails.Extensions["traceId"] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
    });
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<TaskManagerDbContext>("sqlserver");

builder.Services.AddCors(options => options.AddPolicy(FrontendCorsPolicy, policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
        .AllowAnyHeader()
        .AllowAnyMethod()));

builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "TaskManager API v1");
        options.RoutePrefix = "swagger";
    });
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);

app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

/// <summary>Se necesita público para las pruebas de integración</summary>
public partial class Program;
