using System.Diagnostics;
using Microsoft.Extensions.Options;
using ListFile.Core.Configuration;
using ListFile.Core.Interfaces;
using ListFile.Core.Services;

namespace ListFile.Core.Implementations;

/// <summary>
/// Provides functionality to read lines from files, particularly the last N lines.
/// Uses the Strategy pattern to select the most appropriate reading algorithm based on file size.
/// </summary>
public class FileReader : IFileReader
{
    private readonly FileReaderOptions options;
    private readonly FileReadingStrategySelector strategySelector;
    private readonly IDiagnosticSubject? diagnosticSubject;
    private readonly object lockObject = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FileReader"/> class.
    /// </summary>
    /// <param name="options">The configuration options for the file reader.</param>
    /// <param name="strategySelector">The strategy selector for choosing the appropriate reading strategy.</param>
    /// <param name="diagnosticSubject">Optional diagnostic subject for observer notifications.</param>
    /// <exception cref="ArgumentNullException">Thrown when options or strategySelector is null.</exception>
    public FileReader(IOptions<FileReaderOptions> options, FileReadingStrategySelector strategySelector, IDiagnosticSubject? diagnosticSubject = null)
    {
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        this.strategySelector = strategySelector ?? throw new ArgumentNullException(nameof(strategySelector));
        this.diagnosticSubject = diagnosticSubject;
    }

    /// <summary>
    /// Reads the last N lines from a file asynchronously.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="lineCount">The number of lines to read from the end of the file. Default is 10.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the last N lines from the file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown when lineCount is less than 1.</exception>
    public async Task<IEnumerable<IFileLine>> ReadLastLinesAsync(string filePath, int lineCount = 10)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        return await Task.Run(() => ReadLastLines(filePath, lineCount));
    }

    /// <summary>
    /// Reads the last N lines from a file synchronously.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="lineCount">The number of lines to read from the end of the file. Default is 10.</param>
    /// <returns>The last N lines from the file.</returns>
    /// <exception cref="ArgumentNullException">Thrown when filePath is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="ArgumentException">Thrown when lineCount is less than 1.</exception>
    public IEnumerable<IFileLine> ReadLastLines(string filePath, int lineCount = 10)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        if (lineCount < 1)
        {
            throw new ArgumentException("Line count must be greater than 0.", nameof(lineCount));
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"File not found: {filePath}");
        }

        lock (lockObject)
        {
            return ReadLastLinesInternal(filePath, lineCount);
        }
    }

    /// <summary>
    /// Internal method to perform the actual file reading logic using the Strategy pattern.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="lineCount">The number of lines to read from the end of the file.</param>
    /// <returns>The last N lines from the file.</returns>
    private IEnumerable<IFileLine> ReadLastLinesInternal(string filePath, int lineCount)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Notify operation started
        diagnosticSubject?.Notify(DiagnosticEvent.FileOperationStarted("FileReader", filePath, "ReadLastLines", lineCount));

        try
        {
            var fileInfo = new FileInfo(filePath);
            var strategy = strategySelector.SelectStrategy(fileInfo, options);
            
            // Notify strategy selected
            diagnosticSubject?.Notify(DiagnosticEvent.StrategySelected("FileReader", strategy.GetType().Name, fileInfo.Length, options.SmallFileThresholdBytes));
            
            var result = strategy.ReadLastLines(filePath, lineCount, options);
            var resultList = result.ToList(); // Materialize to get accurate count
            
            stopwatch.Stop();
            
            // Notify operation completed
            diagnosticSubject?.Notify(DiagnosticEvent.FileOperationCompleted("FileReader", filePath, "ReadLastLines", 
                stopwatch.ElapsedMilliseconds, resultList.Count, fileInfo.Length));
            
            return resultList;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Notify operation failed
            diagnosticSubject?.Notify(DiagnosticEvent.FileOperationFailed("FileReader", filePath, "ReadLastLines", ex));
            
            throw;
        }
    }


} 