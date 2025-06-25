using System.Text;
using ListFile.Core.Configuration;
using ListFile.Core.Interfaces;

namespace ListFile.Core.Implementations;

/// <summary>
/// Strategy for reading large files using backward reading with buffering.
/// This strategy is memory efficient and can handle files of any size.
/// </summary>
public class LargeFileReadingStrategy : IFileReadingStrategy
{
    /// <summary>
    /// Determines if this strategy can handle the given file.
    /// Large files are those above the configured threshold.
    /// </summary>
    /// <param name="fileInfo">Information about the file to read.</param>
    /// <param name="options">Configuration options for file reading.</param>
    /// <returns>True if the file is large enough to require this strategy.</returns>
    public bool CanHandle(FileInfo fileInfo, FileReaderOptions options)
    {
        return fileInfo.Length > options.SmallFileThresholdBytes;
    }

    /// <summary>
    /// Reads the last N lines from a large file by reading backwards from the end.
    /// This method uses buffered reading to be memory efficient for files of any size.
    /// </summary>
    /// <param name="filePath">The path to the file to read.</param>
    /// <param name="lineCount">The number of lines to read from the end of the file.</param>
    /// <param name="options">Configuration options for file reading.</param>
    /// <returns>The last N lines from the file.</returns>
    public IEnumerable<IFileLine> ReadLastLines(string filePath, int lineCount, FileReaderOptions options)
    {
        var encoding = GetEncoding(options.DefaultEncoding);
        var foundLines = new List<string>();
        
        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fileStream, encoding))
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
            
            // Add any remaining content as the first line (only if it's not empty)
            if (stringBuilder.Length > 0 && foundLines.Count < lineCount)
            {
                var line = stringBuilder.ToString();
                if (line.EndsWith('\r'))
                {
                    line = line.Substring(0, line.Length - 1);
                }
                // Only add non-empty lines
                if (!string.IsNullOrEmpty(line))
                {
                    foundLines.Add(line);
                }
            }
        }

        // Reverse the lines since we read them backwards
        foundLines.Reverse();
        
        // Get total line count for proper line numbering
        var totalLines = File.ReadLines(filePath, encoding).Count();
        var startLineNumber = Math.Max(1, totalLines - foundLines.Count + 1);
        
        var result = new List<IFileLine>();
        for (int i = 0; i < foundLines.Count; i++)
        {
            result.Add(new FileLine(startLineNumber + i, foundLines[i]));
        }

        return result;
    }

    /// <summary>
    /// Gets the encoding from the configuration string.
    /// </summary>
    /// <param name="encodingName">The name of the encoding.</param>
    /// <returns>The encoding instance.</returns>
    private static Encoding GetEncoding(string encodingName)
    {
        return encodingName.ToUpperInvariant() switch
        {
            "UTF-8" => Encoding.UTF8,
            "UTF-16" => Encoding.Unicode,
            "UTF-32" => Encoding.UTF32,
            "ASCII" => Encoding.ASCII,
            _ => Encoding.UTF8
        };
    }
} 