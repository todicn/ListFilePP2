namespace ListFile.Core.Configuration;

/// <summary>
/// Configuration options for the file reader.
/// </summary>
public class FileReaderOptions
{
    /// <summary>
    /// The configuration section name for file reader options.
    /// </summary>
    public const string SectionName = "FileReader";

    /// <summary>
    /// Gets or sets a value indicating whether to enable diagnostic observers.
    /// This is now handled through the observer pattern instead of direct logging.
    /// </summary>
    [Obsolete("Use diagnostic observers instead. Configure observers using ConfigureDiagnosticObservers extension method.")]
    public bool EnablePerformanceLogging { get; set; } = false;

    /// <summary>
    /// Gets or sets the threshold for small files (read all lines vs. read backwards).
    /// Small files use File.ReadAllLines() which loads the entire file into memory.
    /// Large files use buffered backward reading which is memory efficient for any size.
    /// </summary>
    public long SmallFileThresholdBytes { get; set; } = 1024 * 1024; // 1MB

    /// <summary>
    /// Gets or sets the buffer size for reading large files backwards.
    /// </summary>
    public int BufferSize { get; set; } = 4096; // 4KB

    /// <summary>
    /// Gets or sets the default encoding for reading files.
    /// </summary>
    public string DefaultEncoding { get; set; } = "UTF-8";
} 