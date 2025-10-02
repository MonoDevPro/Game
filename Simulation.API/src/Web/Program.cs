using GameWeb.Application;
using GameWeb.Application.Common.Options;
using GameWeb.Infrastructure;
using GameWeb.Infrastructure.Data;
using GameWeb.Web;

var builder = WebApplication.CreateBuilder(args);

// debug temporário: listar ConnectionStrings visíveis
foreach (var kv in builder.Configuration.AsEnumerable().Where(k => k.Key?.StartsWith("ConnectionStrings") == true))
{
    Console.WriteLine($"CONF: {kv.Key} = {kv.Value}");
}

// também printar EnvironmentName e ContentRoot para checar caminho
Console.WriteLine($"ENV: {builder.Environment.EnvironmentName}; ContentRoot: {builder.Environment.ContentRootPath}");

builder.Services.Configure<NetworkOptions>(builder.Configuration.GetSection(NetworkOptions.SectionName));
builder.Services.Configure<WorldOptions>(builder.Configuration.GetSection(WorldOptions.SectionName));
builder.Services.Configure<MapOptions>(builder.Configuration.GetSection(MapOptions.SectionName));

// Add services to the container.
builder.AddKeyVaultIfConfigured();
builder.AddApplicationServices();
builder.AddInfrastructureServices();
builder.AddWebServices();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    await app.InitialiseDatabaseAsync();
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHealthChecks("/health");
// TODO: app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseSwaggerUi(settings =>
{
    settings.Path = "/api";
    settings.DocumentPath = "/api/specification.json";
});

// Use centralized exception handling registered via IExceptionHandler
app.UseExceptionHandler();

// CORS
app.UseCors();

// Authentication/Authorization for protected endpoints
app.UseAuthentication();
app.UseAuthorization();

app.Map("/", () => Results.Redirect("/api"));

app.MapEndpoints();

app.Run();

namespace GameWeb.Web
{
    public partial class Program { }
}
