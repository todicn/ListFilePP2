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

        // Register file reading strategies
        services.AddScoped<IFileReadingStrategy, SmallFileReadingStrategy>();
        services.AddScoped<IFileReadingStrategy, LargeFileReadingStrategy>();
        
        // Register strategy selector
        services.AddScoped<FileReadingStrategySelector>();

        // Register diagnostic services
        services.AddSingleton<IDiagnosticSubject, DiagnosticSubject>();
        
        // Register diagnostic observers
        services.AddScoped<PerformanceDiagnosticObserver>();
        services.AddScoped<MonitoringDiagnosticObserver>();

        // Register the file reader service
        services.AddScoped<IFileReader, FileReader>();
        
        // Register the file monitor service
        services.AddScoped<IFileMonitor, FileMonitor>();

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

        // Register file reading strategies
        services.AddScoped<IFileReadingStrategy, SmallFileReadingStrategy>();
        services.AddScoped<IFileReadingStrategy, LargeFileReadingStrategy>();
        
        // Register strategy selector
        services.AddScoped<FileReadingStrategySelector>();

        // Register diagnostic services
        services.AddSingleton<IDiagnosticSubject, DiagnosticSubject>();
        
        // Register diagnostic observers
        services.AddScoped<PerformanceDiagnosticObserver>();
        services.AddScoped<MonitoringDiagnosticObserver>();

        // Register the file reader service
        services.AddScoped<IFileReader, FileReader>();
        
        // Register the file monitor service
        services.AddScoped<IFileMonitor, FileMonitor>();

        return services;
    }

    /// <summary>
    /// Configures diagnostic observers for file reading operations.
    /// </summary>
    /// <param name="serviceProvider">The service provider to get services from.</param>
    /// <param name="enablePerformanceObserver">Whether to enable the performance diagnostic observer.</param>
    /// <param name="enableMonitoringObserver">Whether to enable the monitoring diagnostic observer.</param>
    /// <param name="enableConsoleOutput">Whether to enable console output for performance metrics.</param>
    public static void ConfigureDiagnosticObservers(
        this IServiceProvider serviceProvider,
        bool enablePerformanceObserver = true,
        bool enableMonitoringObserver = true,
        bool enableConsoleOutput = true)
    {
        var diagnosticSubject = serviceProvider.GetService<IDiagnosticSubject>();
        if (diagnosticSubject == null) return;

        if (enablePerformanceObserver)
        {
            var performanceObserver = serviceProvider.GetService<PerformanceDiagnosticObserver>();
            if (performanceObserver != null)
            {
                diagnosticSubject.Attach(performanceObserver);
            }
        }

        if (enableMonitoringObserver)
        {
            var monitoringObserver = serviceProvider.GetService<MonitoringDiagnosticObserver>();
            if (monitoringObserver != null)
            {
                diagnosticSubject.Attach(monitoringObserver);
            }
        }
    }
} 