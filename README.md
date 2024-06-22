# FolderSynchronizer

FolderSynchronizer is a C# application that synchronizes a source directory with a replica directory at regular intervals. It ensures that the replica directory is an exact copy of the source directory, including subdirectories and files.

## Test Task Requirements

This project was implemented as part of a test task for Veeam Software. The task requirements were as follows:

- **Synchronization Direction**: Synchronization is one-way. After synchronization, the content of the replica folder should exactly match the content of the source folder.
- **Periodicity**: Synchronization should be performed periodically.
- **Logging**: File creation, copying, and removal operations should be logged to a file and the console output.
- **Configuration**: Folder paths, synchronization interval, and log file path should be provided using command line arguments.
- **Libraries**: Usage of third-party libraries for folder synchronization is undesirable. However, using external libraries for well-known algorithms (e.g., MD5 calculation) is acceptable.

## Usage
Run the application with the following arguments:
```sh
FolderSynchronizer.exe <sourcePath> <replicaPath> <interval> <logFilePath>
```
- `sourcePath`: The path to the source directory.
- `replicaPath`: The path to the replica directory.
- `interval`: The synchronization interval in milliseconds.
- `logFilePath`: The path to the log file.

## How It Works

1. **Initialization**: The `FolderSynchronizer` class is initialized with the source path, replica path, synchronization interval, and log file path. A timer is set up to run the synchronization process at the specified interval.

2. **Logging**: Logs are written to a log file asynchronously using a `ConcurrentQueue` to ensure thread safety and non-blocking behavior.

3. **Synchronization**:
    - The `Synchronize` method checks if the source and replica directories exist. If the replica directory does not exist, it is created.
    - The `SynchronizeDirectories` method performs the actual synchronization:
        - **Copy and Update Files**: Copies new and updated files from the source to the replica directory.
        - **Recursively Synchronize Subdirectories**: Ensures subdirectories are synchronized.
        - **Delete Files and Directories**: Deletes files and directories in the replica that do not exist in the source.

4. **File Comparison**: Files are compared using their length, last write time, and MD5 hash to determine if they are identical.

5. **Stopping**: The `Stop` method stops the synchronization process and ensures all log entries are written to the log file before the application exits.
