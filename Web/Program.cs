using SkyHorizont.Infrastructure.Configuration;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        var services = builder.Services;

        // Repositories & DBContexts
        services.AddSkyHorizontSimulationServices();

        var app = builder.Build();
        // app.MapControllers(), etc.
        app.Run();
    }
}
