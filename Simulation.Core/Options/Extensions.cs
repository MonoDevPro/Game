using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Simulation.Core.Options;

public static class Extensions
{
    public static IServiceCollection ConfigureCustomOptions<TOptions>(this IServiceCollection services, IConfiguration configuration, string sectionName)
        where TOptions : class, new()
    {
        services.Configure<TOptions>(configuration.GetSection(sectionName));
        services.AddSingleton<TOptions>(sp => 
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<TOptions>>().Value);
        return services;
    }
}