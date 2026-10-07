using FiapCloudGames.BuildingBlocks.Hosting;
using FiapCloudGames.BuildingBlocks.Web;
using FiapCloudGames.Payments.Api.Configuration;
using FiapCloudGames.Payments.Infrastructure;
using FiapCloudGames.Payments.Presentation;

var builder = WebApplication.CreateBuilder(args);
builder.AddFiapServiceDefaults();
builder.Services.AddFiapApiDefaults();
builder.Services.AddPaymentsInfrastructure(builder.Configuration);
builder.Services.AddPaymentsPresentation();
builder.Services.AddPaymentsOpenApi();

var app = builder.Build();
app.UseFiapApiDefaults();
app.UsePaymentsOpenApi();
app.UseAuthentication();
app.UseAuthorization();
app.MapFiapHealthEndpoints();
app.MapControllers();
await app.RunAsync();

public partial class Program;
