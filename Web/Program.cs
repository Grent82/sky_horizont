using SkyHorizont.Infrastructure.Configuration;

var builder = Host.CreateApplicationBuilder(args);
var services = builder.Services;

// Repositories & DBContexts
services.AddSkyHorizontSimulationServices();

var app = builder.Build();
// app.MapControllers(), etc.
app.Run();
