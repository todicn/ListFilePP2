using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ListFile.Core.Configuration;
using ListFile.Core.Interfaces;
using ListFile.Core.Implementations;

namespace ListFile.Core.Services;

/// <summary>
/// Extension methods for configuring file reading services in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds file reading services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureOptions">An optional action to configure the file reader options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddFileReading(
        this IServiceCollection services,
        Action<FileReaderOptions>? configureOptions = null)
    {
        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            // Use default options
            services.Configure<FileReaderOptions>(options => { });
        }

        // Register the file reader service
        services.AddScoped<IFileReader, FileReader>();

        return services;
    }

    /// <summary>
    /// Adds file reading services to the specified <see cref="IServiceCollection"/> with configuration binding.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The configuration instance to bind to.</param>
    /// <param name="sectionName">The configuration section name. Defaults to "FileReader".</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddFileReading(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = FileReaderOptions.SectionName)
    {
        // Bind configuration
        services.Configure<FileReaderOptions>(configuration.GetSection(sectionName));

        // Register the file reader service
        services.AddScoped<IFileReader, FileReader>();

        return services;
    }
} 