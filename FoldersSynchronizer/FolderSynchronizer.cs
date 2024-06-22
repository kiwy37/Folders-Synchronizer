using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FoldersSynchronizer;

class FolderSynchronizer
{
    private readonly string sourcePath;
    private readonly string replicaPath;
    private readonly int interval;
    private readonly string logFilePath;
    private readonly Timer syncTimer;
    private readonly ConcurrentQueue<string> logQueue;
    private readonly Task logTask;
    private readonly CancellationTokenSource logCancellationTokenSource;

    public FolderSynchronizer(string sourcePath, string replicaPath, int interval, string logFilePath)
    {
        this.sourcePath = sourcePath;
        this.replicaPath = replicaPath;
        this.interval = interval;
        this.logFilePath = logFilePath;

        logQueue = new ConcurrentQueue<string>();
        logCancellationTokenSource = new CancellationTokenSource();
        logTask = Task.Run(() => ProcessLogQueue(logCancellationTokenSource.Token));

        syncTimer = new Timer(Synchronize, null, 0, interval);
    }

    private void Log(string message)
    {
        string logMessage = $"{DateTime.Now}: {message}";
        Console.WriteLine(logMessage);
        logQueue.Enqueue(logMessage);
    }

    private async Task ProcessLogQueue(CancellationToken cancellationToken)
    {
        using (StreamWriter writer = new StreamWriter(logFilePath, true, Encoding.UTF8))
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (logQueue.TryDequeue(out string logMessage))
                {
                    await writer.WriteLineAsync(logMessage);
                }
                else
                {
                    await Task.Delay(100);
                }
            }
        }
    }

    private void Synchronize(object state)
    {
        try
        {
            if (!Directory.Exists(sourcePath))
            {
                Log($"Source path '{sourcePath}' does not exist.");
                return;
            }

            if (!Directory.Exists(replicaPath))
            {
                Directory.CreateDirectory(replicaPath);
                Log($"Replica path '{replicaPath}' created.");
            }

            SynchronizeDirectories(new DirectoryInfo(sourcePath), new DirectoryInfo(replicaPath));
        }
        catch (Exception ex)
        {
            Log($"Error during synchronization: {ex.Message}");
        }
    }

    private void SynchronizeDirectories(DirectoryInfo sourceDir, DirectoryInfo replicaDir)
    {
        var sourceFiles = sourceDir.GetFiles().ToDictionary(f => f.Name);
        var replicaFiles = replicaDir.GetFiles().ToDictionary(f => f.Name);

        // Copy and update files
        Parallel.ForEach(sourceFiles.Values, sourceFile =>
        {
            if (!replicaFiles.TryGetValue(sourceFile.Name, out FileInfo targetFile) || !FilesAreEqual(sourceFile, targetFile))
            {
                sourceFile.CopyTo(Path.Combine(replicaDir.FullName, sourceFile.Name), true);
                Log($"File '{sourceFile.FullName}' copied to '{targetFile?.FullName ?? Path.Combine(replicaDir.FullName, sourceFile.Name)}'.");
            }
        });

        // Recursively synchronize subdirectories
        var sourceSubDirs = sourceDir.GetDirectories().ToDictionary(d => d.Name);
        var replicaSubDirs = replicaDir.GetDirectories().ToDictionary(d => d.Name);

        Parallel.ForEach(sourceSubDirs.Values, sourceSubDir =>
        {
            if (!replicaSubDirs.TryGetValue(sourceSubDir.Name, out DirectoryInfo targetSubDir))
            {
                targetSubDir = Directory.CreateDirectory(Path.Combine(replicaDir.FullName, sourceSubDir.Name));
                Log($"Directory '{targetSubDir.FullName}' created.");
            }
            SynchronizeDirectories(sourceSubDir, targetSubDir);
        });

        // Delete files in replica that are not in source
        Parallel.ForEach(replicaFiles.Values, replicaFile =>
        {
            if (!sourceFiles.ContainsKey(replicaFile.Name))
            {
                replicaFile.Delete();
                Log($"File '{replicaFile.FullName}' deleted.");
            }
        });

        // Delete directories in replica that are not in source
        Parallel.ForEach(replicaSubDirs.Values, replicaSubDir =>
        {
            if (!sourceSubDirs.ContainsKey(replicaSubDir.Name))
            {
                replicaSubDir.Delete(true);
                Log($"Directory '{replicaSubDir.FullName}' deleted.");
            }
        });
    }

    private bool FilesAreEqual(FileInfo fileInfo1, FileInfo fileInfo2)
    {
        if (fileInfo1.Length != fileInfo2.Length || fileInfo1.LastWriteTimeUtc != fileInfo2.LastWriteTimeUtc)
        {
            return false;
        }

        using (var md5 = MD5.Create())
        {
            using (var stream1 = File.OpenRead(fileInfo1.FullName))
            using (var stream2 = File.OpenRead(fileInfo2.FullName))
            {
                var hash1 = md5.ComputeHash(stream1);
                var hash2 = md5.ComputeHash(stream2);
                return hash1.SequenceEqual(hash2);
            }
        }
    }

    public void Stop()
    {
        syncTimer.Change(Timeout.Infinite, Timeout.Infinite);
        logCancellationTokenSource.Cancel();
        logTask.Wait();
    }
}