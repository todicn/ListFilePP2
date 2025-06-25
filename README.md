# ListFile - File Line Reader Library

A high-performance .NET 8 library for reading the last N lines from files efficiently, with support for both small and large files.

## Features

- **Efficient File Reading**: Optimized algorithms for reading the last N lines from files
- **File Monitoring**: Real-time monitoring of file changes with automatic line reading
- **Performance Optimized**: Different strategies for small vs. large files using Strategy pattern
- **Thread-Safe**: Safe for concurrent access
- **Configurable**: Flexible options for file size thresholds and buffer sizes
- **Async Support**: Both synchronous and asynchronous APIs
- **Event-Driven**: File change notifications with detailed event information
- **Observer Pattern Diagnostics**: Flexible diagnostic system using observer pattern for performance monitoring and event tracking
- **Dependency Injection**: Built-in support for Microsoft.Extensions.DependencyInjection
- **Well-Tested**: Comprehensive unit tests with edge case coverage

## Quick Start

### Installation

Add the ListFile.Core package to your project:

```xml
<PackageReference Include="ListFile.Core" Version="1.0.0" />
```

### Basic Usage

```csharp
using ListFile.Core.Interfaces;
using ListFile.Core.Implementations;
using Microsoft.Extensions.Options;

// Create a file reader with default options
var options = Options.Create(new FileReaderOptions());
IFileReader fileReader = new FileReader(options);

// Read the last 10 lines from a file
IEnumerable<IFileLine> lines = await fileReader.ReadLastLinesAsync("myfile.txt");

foreach (var line in lines)
{
    Console.WriteLine($"Line {line.LineNumber}: {line.Content}");
}
```

### Dependency Injection

```csharp
using Microsoft.Extensions.DependencyInjection;
using ListFile.Core.Services;

var services = new ServiceCollection();

// Register file reading services with custom options
services.AddFileReading(options =>
{
    options.SmallFileThresholdBytes = 1024 * 1024; // 1MB
});

var serviceProvider = services.BuildServiceProvider();

// Configure diagnostic observers for performance monitoring and event tracking
serviceProvider.ConfigureDiagnosticObservers(
    enablePerformanceObserver: true,
    enableMonitoringObserver: true,
    enableConsoleOutput: true);

var fileReader = serviceProvider.GetRequiredService<IFileReader>();
var fileMonitor = serviceProvider.GetRequiredService<IFileMonitor>();

// Read last 5 lines
var result = await fileReader.ReadLastLinesAsync("largefile.txt", 5);
```

### File Monitoring

```csharp
using ListFile.Core.Interfaces;

// Get the file monitor from DI
var fileMonitor = serviceProvider.GetRequiredService<IFileMonitor>();

// Set up event handler
fileMonitor.FileChanged += (sender, args) =>
{
    Console.WriteLine($"File {args.FilePath} changed at {args.Timestamp}");
    Console.WriteLine($"Change type: {args.ChangeType}");
    Console.WriteLine($"Lines read: {args.Lines.Count()}");
    
    foreach (var line in args.Lines)
    {
        Console.WriteLine($"  {line.LineNumber}: {line.Content}");
    }
};

// Start monitoring a file (reads last 10 lines on changes)
fileMonitor.StartMonitoring("logfile.txt", 10);

// Stop monitoring
fileMonitor.StopMonitoring();

// Don't forget to dispose
fileMonitor.Dispose();
```

## Diagnostic Observers

The library uses the Observer pattern for diagnostics, allowing you to monitor file operations, performance metrics, and monitoring events in a flexible and extensible way.

### Available Observers

#### PerformanceDiagnosticObserver
Tracks performance metrics for file operations:
- File operation start/completion events
- Execution time measurements
- Strategy selection notifications
- Console output and structured logging

#### MonitoringDiagnosticObserver  
Tracks file monitoring statistics:
- Monitoring start/stop events
- File change detection events
- Statistics collection (change counts, durations)
- Error tracking

### Configuring Observers

```csharp
using Microsoft.Extensions.DependencyInjection;
using ListFile.Core.Services;

var services = new ServiceCollection();
services.AddFileReading();
var serviceProvider = services.BuildServiceProvider();

// Configure diagnostic observers
serviceProvider.ConfigureDiagnosticObservers(
    enablePerformanceObserver: true,    // Enable performance tracking
    enableMonitoringObserver: true,     // Enable monitoring statistics  
    enableConsoleOutput: true           // Enable console output for performance
);
```

### Custom Observers

You can create custom observers by implementing `IDiagnosticObserver`:

```csharp
public class CustomDiagnosticObserver : IDiagnosticObserver
{
    public void OnDiagnosticEvent(IDiagnosticEvent diagnosticEvent)
    {
        switch (diagnosticEvent.EventType)
        {
            case DiagnosticEventType.FileOperationCompleted:
                // Handle file operation completion
                var elapsedMs = diagnosticEvent.Data["ElapsedMilliseconds"];
                Console.WriteLine($"Operation took {elapsedMs}ms");
                break;
                
            case DiagnosticEventType.FileChangeDetected:
                // Handle file change detection
                var changeType = diagnosticEvent.Data["ChangeType"];
                Console.WriteLine($"File changed: {changeType}");
                break;
        }
    }
}

// Register and attach custom observer
services.AddScoped<CustomDiagnosticObserver>();
var customObserver = serviceProvider.GetService<CustomDiagnosticObserver>();
var diagnosticSubject = serviceProvider.GetService<IDiagnosticSubject>();
diagnosticSubject.Attach(customObserver);
```

### Diagnostic Event Types

- `FileOperationStarted` - File operation begins
- `FileOperationCompleted` - File operation completes successfully  
- `FileOperationFailed` - File operation fails
- `PerformanceMetrics` - Performance metrics collected
- `MonitoringStarted` - File monitoring begins
- `MonitoringStopped` - File monitoring ends
- `FileChangeDetected` - File change detected during monitoring
- `StrategySelected` - Reading strategy selected
- `Error` - Error occurred during operation

## Configuration Options

The `FileReaderOptions` class provides several configuration options:

```csharp
public class FileReaderOptions
{
    // Threshold for small vs. large file processing (default: 1MB)
    // Small files use File.ReadAllLines() which loads the entire file into memory.
    // Large files use buffered backward reading which is memory efficient for any size.
    public long SmallFileThresholdBytes { get; set; } = 1024 * 1024;
    
    // Buffer size for reading large files (default: 4KB)
    public int BufferSize { get; set; } = 4096;
    
    // Default encoding for reading files (default: UTF-8)
    public string DefaultEncoding { get; set; } = "UTF-8";
}
```

## Performance Characteristics

The library uses different strategies based on file size:

- **Small Files** (≤ threshold): Reads all lines and returns the last N
- **Large Files** (> threshold): Uses backward reading algorithm for efficiency

### Benchmarks

For a 10,000-line file (≈1MB):
- Reading last 10 lines: ~5-15ms
- Memory usage: Minimal (only stores requested lines)

## API Reference

### IFileReader Interface

```csharp
public interface IFileReader
{
    // Asynchronous method to read last N lines
    Task<IEnumerable<IFileLine>> ReadLastLinesAsync(string filePath, int lineCount = 10);
    
    // Synchronous method to read last N lines  
    IEnumerable<IFileLine> ReadLastLines(string filePath, int lineCount = 10);
}
```

### IFileMonitor Interface

```csharp
public interface IFileMonitor : IDisposable
{
    // Event raised when monitored file changes
    event EventHandler<FileChangedEventArgs> FileChanged;
    
    // Start monitoring a file for changes
    void StartMonitoring(string filePath, int lineCount = 10);
    
    // Stop monitoring the current file
    void StopMonitoring();
    
    // Check if currently monitoring
    bool IsMonitoring { get; }
    
    // Get the path of the monitored file
    string? MonitoredFilePath { get; }
}
```

### FileChangedEventArgs Class

```csharp
public class FileChangedEventArgs : EventArgs
{
    public string FilePath { get; }           // Path of the changed file
    public IEnumerable<IFileLine> Lines { get; } // Lines read after change
    public FileChangeType ChangeType { get; }    // Type of change (Modified, Created, Deleted)
    public DateTime Timestamp { get; }           // When the change was detected
}

public enum FileChangeType
{
    Modified,  // File content was modified
    Created,   // File was created
    Deleted    // File was deleted
}
```

### IFileLine Interface

```csharp
public interface IFileLine
{
    int LineNumber { get; }    // 1-based line number
    string Content { get; }    // Line content
}
```

## Error Handling

The library throws appropriate exceptions for various error conditions:

- `ArgumentNullException`: When file path is null or empty
- `ArgumentException`: When line count is less than 1
- `FileNotFoundException`: When the specified file doesn't exist

## Thread Safety

The FileReader class is thread-safe and can be used concurrently from multiple threads. The implementation uses appropriate locking mechanisms to ensure data integrity.

## Examples

### Reading Different Numbers of Lines

```csharp
// Read last 5 lines
var last5 = await fileReader.ReadLastLinesAsync("file.txt", 5);

// Read last 20 lines  
var last20 = await fileReader.ReadLastLinesAsync("file.txt", 20);

// Default behavior (last 10 lines)
var defaultLines = await fileReader.ReadLastLinesAsync("file.txt");
```

### Real-time Log Monitoring

```csharp
using var fileMonitor = serviceProvider.GetRequiredService<IFileMonitor>();

fileMonitor.FileChanged += (sender, args) =>
{
    if (args.ChangeType == FileChangeType.Modified)
    {
        Console.WriteLine($"[{args.Timestamp:HH:mm:ss}] Log file updated:");
        foreach (var line in args.Lines.TakeLast(3)) // Show last 3 lines
        {
            Console.WriteLine($"  {line.Content}");
        }
    }
};

// Monitor application log file
fileMonitor.StartMonitoring("app.log", 10);

// Keep monitoring until application stops
Console.WriteLine("Monitoring app.log for changes. Press any key to stop...");
Console.ReadKey();
```

### Configuration from appsettings.json

```json
{
  "FileReader": {
    "SmallFileThresholdBytes": 1048576,
    "BufferSize": 8192
  }
}
```

```csharp
services.AddFileReading(configuration);
```

## Contributing

Contributions are welcome! Please ensure your code follows the established patterns:

- Use async/await for all public APIs
- Include comprehensive unit tests
- Follow C# naming conventions
- Add XML documentation for public APIs
- Ensure thread safety

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Architecture

The library follows clean architecture principles:

- **Interfaces**: Define contracts (`IFileReader`, `IFileLine`)
- **Implementations**: Core logic (`FileReader`, `FileLine`)
- **Configuration**: Options pattern (`FileReaderOptions`)
- **Services**: Dependency injection extensions (`ServiceCollectionExtensions`)

Built with .NET 8 and modern C# features including nullable reference types and performance optimizations. 