using FoldersSynchronizer;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 4)
        {
            Console.WriteLine("Usage: FolderSynchronizer <sourcePath> <replicaPath> <interval> <logFilePath>");
            return;
        }

        string sourcePath = args[0];
        string replicaPath = args[1];
        if (!int.TryParse(args[2], out int interval))
        {
            Console.WriteLine("Invalid interval.");
            return;
        }
        string logFilePath = args[3];

        var synchronizer = new FolderSynchronizer(sourcePath, replicaPath, interval, logFilePath);

        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();

        synchronizer.Stop();
    }
}