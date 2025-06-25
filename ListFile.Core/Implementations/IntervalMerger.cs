using System.Diagnostics;
using System.Text;
using Microsoft.Extensions.Options;
using ListFile.Core.Configuration;
using ListFile.Core.Interfaces;

namespace ListFile.Core.Implementations;

/// <summary>
/// Provides functionality to read lines from files, particularly the last N lines.
/// </summary>
public class FileReader : IFileReader
{
    private readonly FileReaderOptions options;
    private readonly object lockObject = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FileReader"/> class.
    /// </summary>
    /// <param name="options">The configuration options for the file reader.</param>
    /// <exception cref="ArgumentNullException">Thrown when options is null.</exception>
    public FileReader(IOptions<FileReaderOptions> options)
    {
        this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
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
    /// Internal method to perform the actual file reading logic.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="lineCount">The number of lines to read from the end of the file.</param>
    /// <returns>The last N lines from the file.</returns>
    private IEnumerable<IFileLine> ReadLastLinesInternal(string filePath, int lineCount)
    {
        Stopwatch? stopwatch = null;
        if (options.EnablePerformanceLogging)
        {
            stopwatch = Stopwatch.StartNew();
        }

        try
        {
            var fileInfo = new FileInfo(filePath);
            
            // Check file size limit
            if (fileInfo.Length > options.MaxFileSizeBytes)
            {
                throw new ArgumentException($"File size exceeds maximum allowed ({options.MaxFileSizeBytes} bytes).", nameof(filePath));
            }

            // For small files, read all lines and take the last N
            if (fileInfo.Length <= options.SmallFileThresholdBytes)
            {
                return ReadSmallFile(filePath, lineCount);
            }
            else
            {
                // For large files, read from the end backwards
                return ReadLargeFile(filePath, lineCount);
            }
        }
        finally
        {
            if (stopwatch != null && options.EnablePerformanceLogging)
            {
                stopwatch.Stop();
                Console.WriteLine($"File reading completed in {stopwatch.ElapsedMilliseconds}ms");
            }
        }
    }

    /// <summary>
    /// Reads the last N lines from a small file by reading all lines.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="lineCount">The number of lines to read from the end of the file.</param>
    /// <returns>The last N lines from the file.</returns>
    private IEnumerable<IFileLine> ReadSmallFile(string filePath, int lineCount)
    {
        var allLines = File.ReadAllLines(filePath, Encoding.UTF8);
        var startLine = Math.Max(0, allLines.Length - lineCount);
        var result = new List<IFileLine>();

        for (int i = startLine; i < allLines.Length; i++)
        {
            result.Add(new FileLine(i + 1, allLines[i]));
        }

        return result;
    }

    /// <summary>
    /// Reads the last N lines from a large file by reading backwards from the end.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="lineCount">The number of lines to read from the end of the file.</param>
    /// <returns>The last N lines from the file.</returns>
    private IEnumerable<IFileLine> ReadLargeFile(string filePath, int lineCount)
    {
        var foundLines = new List<string>();
        
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fileStream, Encoding.UTF8))
        {
            // Read backwards using a buffer approach
            var buffer = new char[options.BufferSize];
            var stringBuilder = new StringBuilder();
            
            // Start from the end of the file
            fileStream.Seek(0, SeekOrigin.End);
            var position = fileStream.Position;
            
            while (position > 0 && foundLines.Count < lineCount)
            {
                var bytesToRead = (int)Math.Min(options.BufferSize, position);
                position -= bytesToRead;
                fileStream.Seek(position, SeekOrigin.Begin);
                
                var bytesRead = reader.Read(buffer, 0, bytesToRead);
                
                // Process the buffer backwards
                for (int i = bytesRead - 1; i >= 0; i--)
                {
                    if (buffer[i] == '\n')
                    {
                        if (stringBuilder.Length > 0)
                        {
                            var line = stringBuilder.ToString();
                            if (line.EndsWith('\r'))
                            {
                                line = line.Substring(0, line.Length - 1);
                            }
                            foundLines.Add(line);
                            stringBuilder.Clear();
                            
                            if (foundLines.Count >= lineCount)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        stringBuilder.Insert(0, buffer[i]);
                    }
                }
            }
            
            // Add any remaining content as the first line
            if (stringBuilder.Length > 0 && foundLines.Count < lineCount)
            {
                var line = stringBuilder.ToString();
                if (line.EndsWith('\r'))
                {
                    line = line.Substring(0, line.Length - 1);
                }
                foundLines.Add(line);
            }
        }

        // Reverse the lines since we read them backwards
        foundLines.Reverse();
        
        // Get total line count for proper line numbering
        var totalLines = File.ReadLines(filePath).Count();
        var startLineNumber = Math.Max(1, totalLines - foundLines.Count + 1);
        
        var result = new List<IFileLine>();
        for (int i = 0; i < foundLines.Count; i++)
        {
            result.Add(new FileLine(startLineNumber + i, foundLines[i]));
        }

        return result;
    }
} 