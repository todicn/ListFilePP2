using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ListFile.Core.Configuration;
using ListFile.Core.Interfaces;
using ListFile.Core.Services;
using Xunit;

namespace ListFile.Tests.Services;

/// <summary>
/// Tests for the ServiceCollectionExtensions class.
/// </summary>
public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddFileReading_WithoutOptions_RegistersServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFileReading();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var fileReader = serviceProvider.GetService<IFileReader>();
        Assert.NotNull(fileReader);
        
        var options = serviceProvider.GetService<IOptions<FileReaderOptions>>();
        Assert.NotNull(options);
        Assert.NotNull(options.Value);
    }

    [Fact]
    public void AddFileReading_WithOptionsConfiguration_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var expectedMaxFileSize = 5 * 1024 * 1024L; // 5MB
        var expectedThreshold = 512 * 1024L; // 512KB

        // Act
        services.AddFileReading(options =>
        {
            options.MaxFileSizeBytes = expectedMaxFileSize;
            options.SmallFileThresholdBytes = expectedThreshold;
            options.EnablePerformanceLogging = true;
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<IOptions<FileReaderOptions>>();
        Assert.Equal(expectedMaxFileSize, options.Value.MaxFileSizeBytes);
        Assert.Equal(expectedThreshold, options.Value.SmallFileThresholdBytes);
        Assert.True(options.Value.EnablePerformanceLogging);
    }

    [Fact]
    public void AddFileReading_WithConfiguration_BindsFromConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurationData = new Dictionary<string, string?>
        {
            ["FileReader:MaxFileSizeBytes"] = "10485760", // 10MB
            ["FileReader:SmallFileThresholdBytes"] = "1048576", // 1MB
            ["FileReader:EnablePerformanceLogging"] = "true",
            ["FileReader:BufferSize"] = "8192"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        // Act
        services.AddFileReading(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<IOptions<FileReaderOptions>>();
        Assert.Equal(10485760L, options.Value.MaxFileSizeBytes);
        Assert.Equal(1048576L, options.Value.SmallFileThresholdBytes);
        Assert.True(options.Value.EnablePerformanceLogging);
        Assert.Equal(8192, options.Value.BufferSize);
    }

    [Fact]
    public void AddFileReading_WithCustomSectionName_BindsFromCustomSection()
    {
        // Arrange
        var services = new ServiceCollection();
        var configurationData = new Dictionary<string, string?>
        {
            ["CustomSection:MaxFileSizeBytes"] = "20971520", // 20MB
            ["CustomSection:EnablePerformanceLogging"] = "false"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configurationData)
            .Build();

        // Act
        services.AddFileReading(configuration, "CustomSection");
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<IOptions<FileReaderOptions>>();
        Assert.Equal(20971520L, options.Value.MaxFileSizeBytes);
        Assert.False(options.Value.EnablePerformanceLogging);
    }

    [Fact]
    public void AddFileReading_MultipleRegistrations_LastConfigurationWins()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFileReading(options => options.MaxFileSizeBytes = 1024);
        services.AddFileReading(options => options.MaxFileSizeBytes = 2048);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetRequiredService<IOptions<FileReaderOptions>>();
        Assert.Equal(2048L, options.Value.MaxFileSizeBytes);
    }

    [Fact]
    public void AddFileReading_ServiceLifetime_IsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddFileReading();
        var serviceProvider = services.BuildServiceProvider();

        // Assert - Create two scopes and verify different instances
        using (var scope1 = serviceProvider.CreateScope())
        using (var scope2 = serviceProvider.CreateScope())
        {
            var fileReader1 = scope1.ServiceProvider.GetRequiredService<IFileReader>();
            var fileReader2 = scope2.ServiceProvider.GetRequiredService<IFileReader>();
            
            Assert.NotSame(fileReader1, fileReader2);
        }

        // Assert - Within same scope, same instance
        using (var scope = serviceProvider.CreateScope())
        {
            var fileReader1 = scope.ServiceProvider.GetRequiredService<IFileReader>();
            var fileReader2 = scope.ServiceProvider.GetRequiredService<IFileReader>();
            
            Assert.Same(fileReader1, fileReader2);
        }
    }
} 