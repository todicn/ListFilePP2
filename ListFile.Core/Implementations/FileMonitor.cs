using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ListFile.Core.Interfaces;

namespace ListFile.Core.Implementations;

/// <summary>
/// Monitors a file for changes and reads the last N lines when the file is modified.
/// Uses FileSystemWatcher for efficient file monitoring.
/// </summary>
public class FileMonitor : IFileMonitor
{
    private readonly IFileReader fileReader;
    private readonly ILogger<FileMonitor>? logger;
    private FileSystemWatcher? fileWatcher;
    private string? monitoredFilePath;
    private int lineCount;
    private bool isDisposed;
    private readonly object lockObject = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="FileMonitor"/> class.
    /// </summary>
    /// <param name="fileReader">The file reader to use for reading file content.</param>
    /// <param name="logger">Optional logger for monitoring events.</param>
    public FileMonitor(IFileReader fileReader, ILogger<FileMonitor>? logger = null)
    {
        this.fileReader = fileReader ?? throw new ArgumentNullException(nameof(fileReader));
        this.logger = logger;
    }

    /// <inheritdoc />
    public event EventHandler<FileChangedEventArgs>? FileChanged;

    /// <inheritdoc />
    public bool IsMonitoring { get; private set; }

    /// <inheritdoc />
    public string? MonitoredFilePath => monitoredFilePath;

    /// <inheritdoc />
    public void StartMonitoring(string filePath, int lineCount = 10)
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
            if (IsMonitoring)
            {
                StopMonitoring();
            }

            var fileInfo = new FileInfo(filePath);
            var directoryPath = fileInfo.DirectoryName ?? throw new ArgumentException("Invalid file path", nameof(filePath));
            var fileName = fileInfo.Name;

            this.monitoredFilePath = filePath;
            this.lineCount = lineCount;

            // Create and configure FileSystemWatcher
            fileWatcher = new FileSystemWatcher(directoryPath, fileName)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            // Subscribe to events
            fileWatcher.Changed += OnFileChanged;
            fileWatcher.Created += OnFileCreated;
            fileWatcher.Deleted += OnFileDeleted;
            fileWatcher.Error += OnWatcherError;

            IsMonitoring = true;

            logger?.LogInformation("Started monitoring file: {FilePath}", filePath);

            // Read initial content
            try
            {
                var initialLines = fileReader.ReadLastLines(filePath, lineCount);
                OnFileChanged(FileChangeType.Modified, initialLines);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to read initial content from file: {FilePath}", filePath);
            }
        }
    }

    /// <inheritdoc />
    public void StopMonitoring()
    {
        lock (lockObject)
        {
            if (fileWatcher != null)
            {
                fileWatcher.EnableRaisingEvents = false;
                fileWatcher.Changed -= OnFileChanged;
                fileWatcher.Created -= OnFileCreated;
                fileWatcher.Deleted -= OnFileDeleted;
                fileWatcher.Error -= OnWatcherError;
                fileWatcher.Dispose();
                fileWatcher = null;
            }

            IsMonitoring = false;
            var previousPath = monitoredFilePath;
            monitoredFilePath = null;

            if (previousPath != null)
            {
                logger?.LogInformation("Stopped monitoring file: {FilePath}", previousPath);
            }
        }
    }

    /// <summary>
    /// Handles file change events from FileSystemWatcher.
    /// </summary>
    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Add a small delay to ensure the file write is complete
            Thread.Sleep(50);

            if (File.Exists(e.FullPath))
            {
                var lines = fileReader.ReadLastLines(e.FullPath, lineCount);
                OnFileChanged(FileChangeType.Modified, lines);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error reading file after change: {FilePath}", e.FullPath);
        }
    }

    /// <summary>
    /// Handles file creation events from FileSystemWatcher.
    /// </summary>
    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Add a small delay to ensure the file creation is complete
            Thread.Sleep(100);

            if (File.Exists(e.FullPath))
            {
                var lines = fileReader.ReadLastLines(e.FullPath, lineCount);
                OnFileChanged(FileChangeType.Created, lines);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error reading file after creation: {FilePath}", e.FullPath);
        }
    }

    /// <summary>
    /// Handles file deletion events from FileSystemWatcher.
    /// </summary>
    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        OnFileChanged(FileChangeType.Deleted, Array.Empty<IFileLine>());
    }

    /// <summary>
    /// Handles FileSystemWatcher error events.
    /// </summary>
    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        logger?.LogError(e.GetException(), "FileSystemWatcher error occurred");
        
        // Try to restart monitoring if possible
        if (IsMonitoring && !string.IsNullOrEmpty(monitoredFilePath))
        {
            try
            {
                var currentPath = monitoredFilePath;
                var currentLineCount = lineCount;
                StopMonitoring();
                Thread.Sleep(1000); // Wait before restarting
                StartMonitoring(currentPath, currentLineCount);
                logger?.LogInformation("Successfully restarted file monitoring after error");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to restart file monitoring after error");
            }
        }
    }

    /// <summary>
    /// Raises the FileChanged event with the specified change type and lines.
    /// </summary>
    private void OnFileChanged(FileChangeType changeType, IEnumerable<IFileLine> lines)
    {
        if (monitoredFilePath != null)
        {
            var eventArgs = new FileChangedEventArgs(monitoredFilePath, lines, changeType);
            
            try
            {
                FileChanged?.Invoke(this, eventArgs);
                logger?.LogDebug("File change event raised: {ChangeType} for {FilePath}", changeType, monitoredFilePath);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error in FileChanged event handler");
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!isDisposed)
        {
            StopMonitoring();
            isDisposed = true;
        }
    }
} 