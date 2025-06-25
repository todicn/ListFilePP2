using ListFile.Core.Configuration;
using ListFile.Core.Interfaces;

namespace ListFile.Core.Services;

/// <summary>
/// Service responsible for selecting the appropriate file reading strategy based on file characteristics.
/// </summary>
public class FileReadingStrategySelector
{
    private readonly IEnumerable<IFileReadingStrategy> strategies;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileReadingStrategySelector"/> class.
    /// </summary>
    /// <param name="strategies">The available file reading strategies.</param>
    public FileReadingStrategySelector(IEnumerable<IFileReadingStrategy> strategies)
    {
        this.strategies = strategies ?? throw new ArgumentNullException(nameof(strategies));
    }

    /// <summary>
    /// Selects the most appropriate strategy for reading the given file.
    /// </summary>
    /// <param name="fileInfo">Information about the file to read.</param>
    /// <param name="options">Configuration options for file reading.</param>
    /// <returns>The best strategy for reading the file.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no suitable strategy is found.</exception>
    public IFileReadingStrategy SelectStrategy(FileInfo fileInfo, FileReaderOptions options)
    {
        var strategy = strategies.FirstOrDefault(s => s.CanHandle(fileInfo, options));
        
        if (strategy == null)
        {
            throw new InvalidOperationException($"No suitable strategy found for file: {fileInfo.FullName}");
        }

        return strategy;
    }
} 