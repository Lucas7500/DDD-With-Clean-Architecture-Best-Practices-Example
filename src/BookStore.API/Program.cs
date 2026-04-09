using BookStore.API.Extensions;
using BookStore.Application.IoC;
using BookStore.Infra.IoC;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDatabaseContext(builder.Configuration);
builder.Services.AddDatabaseMigrations(builder.Configuration);
builder.Services.AddUnitOfWork();
builder.Services.AddQueryServices();
builder.Services.AddApplicationUseCases();
builder.Services.AddApplicationValidators();
builder.Services.AddValidation();
builder.Services.AddApiVersioningConfiguration();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfiguration();
builder.Services.AddRoutingAdditionalConfiguration();
builder.Services.AddRateLimiterConfiguration(builder.Configuration);

WebApplication app = builder.Build();

app.RunDatabaseMigrations();
app.UseSwaggerInDevelopment();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers().RequireFixedWindowRateLimiting();
app.AddExceptionHandler();

await app.RunAsync();
