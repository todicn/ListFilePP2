using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ListFile.Core.Implementations;
using ListFile.Core.Interfaces;
using ListFile.Core.Services;

namespace ListFile.Demo;

/// <summary>
/// Demo application showcasing the file reading functionality.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== ListFile Demo Application ===");
        Console.WriteLine();

        // Setup dependency injection
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddFileReading(options =>
                {
                    options.EnablePerformanceLogging = true;
                    options.SmallFileThresholdBytes = 512 * 1024; // 512KB
                });
                services.AddLogging(builder => builder.AddConsole());
            })
            .Build();

        IServiceProvider serviceProvider = host.Services;
        ILogger<Program> logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        IFileReader fileReader = serviceProvider.GetRequiredService<IFileReader>();

        try
        {
            logger.LogInformation("Starting file reading demonstrations");

            // Create sample files for demonstration
            await CreateSampleFiles();

            // Demo 1: Basic file reading
            await DemoBasicFileReading(fileReader);

            // Demo 2: Different line counts
            await DemoDifferentLineCounts(fileReader);

            // Demo 3: Edge cases
            await DemoEdgeCases(fileReader);

            // Demo 4: Large file performance
            await DemoLargeFilePerformance(fileReader);

            logger.LogInformation("All demonstrations completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during demonstration");
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            // Cleanup sample files
            CleanupSampleFiles();
        }

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    /// Creates sample files for demonstration purposes.
    /// </summary>
    private static async Task CreateSampleFiles()
    {
        Console.WriteLine("Creating sample files for demonstration...");

        // Create a small sample file
        var smallFileContent = new List<string>();
        for (int i = 1; i <= 20; i++)
        {
            smallFileContent.Add($"This is line {i} of the small sample file.");
        }
        await File.WriteAllLinesAsync("sample_small.txt", smallFileContent);

        // Create a medium sample file
        var mediumFileContent = new List<string>();
        for (int i = 1; i <= 100; i++)
        {
            mediumFileContent.Add($"Line {i}: Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.");
        }
        await File.WriteAllLinesAsync("sample_medium.txt", mediumFileContent);

        // Create a large sample file
        var largeFileContent = new List<string>();
        for (int i = 1; i <= 10000; i++)
        {
            largeFileContent.Add($"Line {i}: This is a large file with many lines to test performance and large file handling capabilities.");
        }
        await File.WriteAllLinesAsync("sample_large.txt", largeFileContent);

        Console.WriteLine("Sample files created successfully.");
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates basic file reading functionality.
    /// </summary>
    private static async Task DemoBasicFileReading(IFileReader fileReader)
    {
        Console.WriteLine("=== Demo 1: Basic File Reading ===");
        Console.WriteLine("Reading the last 10 lines from sample_small.txt:");

        try
        {
            IEnumerable<IFileLine> lines = await fileReader.ReadLastLinesAsync("sample_small.txt");
            
            foreach (var line in lines)
            {
                Console.WriteLine($"  {line}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates reading different numbers of lines.
    /// </summary>
    private static async Task DemoDifferentLineCounts(IFileReader fileReader)
    {
        Console.WriteLine("=== Demo 2: Different Line Counts ===");

        // Read last 5 lines
        Console.WriteLine("Reading the last 5 lines from sample_medium.txt:");
        try
        {
            IEnumerable<IFileLine> lines5 = fileReader.ReadLastLines("sample_medium.txt", 5);
            
            foreach (var line in lines5)
            {
                Console.WriteLine($"  {line.LineNumber}: {line.Content[..Math.Min(50, line.Content.Length)]}...");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();

        // Read last 15 lines
        Console.WriteLine("Reading the last 15 lines from sample_medium.txt:");
        try
        {
            IEnumerable<IFileLine> lines15 = await fileReader.ReadLastLinesAsync("sample_medium.txt", 15);
            
            foreach (var line in lines15)
            {
                Console.WriteLine($"  {line.LineNumber}: {line.Content[..Math.Min(50, line.Content.Length)]}...");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates edge cases.
    /// </summary>
    private static async Task DemoEdgeCases(IFileReader fileReader)
    {
        Console.WriteLine("=== Demo 3: Edge Cases ===");

        // Create an empty file
        await File.WriteAllTextAsync("empty.txt", "");
        
        Console.WriteLine("Edge Case 1 - Empty file:");
        try
        {
            IEnumerable<IFileLine> emptyResult = await fileReader.ReadLastLinesAsync("empty.txt");
            Console.WriteLine($"  Result: {emptyResult.Count()} lines");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error: {ex.Message}");
        }

        // Create a single line file
        await File.WriteAllTextAsync("single_line.txt", "This is the only line in this file.");
        
        Console.WriteLine("Edge Case 2 - Single line file:");
        try
        {
            IEnumerable<IFileLine> singleResult = fileReader.ReadLastLines("single_line.txt", 10);
            foreach (var line in singleResult)
            {
                Console.WriteLine($"  {line}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error: {ex.Message}");
        }

        // Test with non-existent file
        Console.WriteLine("Edge Case 3 - Non-existent file:");
        try
        {
            IEnumerable<IFileLine> nonExistentResult = await fileReader.ReadLastLinesAsync("non_existent.txt");
            Console.WriteLine($"  Result: {nonExistentResult.Count()} lines");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates performance with a large file.
    /// </summary>
    private static async Task DemoLargeFilePerformance(IFileReader fileReader)
    {
        Console.WriteLine("=== Demo 4: Large File Performance ===");
        Console.WriteLine("Reading the last 10 lines from sample_large.txt (10,000 lines):");

        try
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            IEnumerable<IFileLine> lines = await fileReader.ReadLastLinesAsync("sample_large.txt");
            stopwatch.Stop();

            Console.WriteLine($"Performance: {stopwatch.ElapsedMilliseconds}ms");
            Console.WriteLine("Last 10 lines:");
            
            foreach (var line in lines)
            {
                Console.WriteLine($"  {line.LineNumber}: {line.Content[..Math.Min(60, line.Content.Length)]}...");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Cleans up the sample files created for demonstration.
    /// </summary>
    private static void CleanupSampleFiles()
    {
        string[] filesToDelete = {
            "sample_small.txt",
            "sample_medium.txt", 
            "sample_large.txt",
            "empty.txt",
            "single_line.txt"
        };

        foreach (string file in filesToDelete)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not delete {file}: {ex.Message}");
            }
        }
    }
}
