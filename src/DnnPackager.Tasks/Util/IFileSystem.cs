using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace DnnPackager.Util
{

    public interface IFileSystem
    {
        bool FileExists(string path);
        bool DirectoryExists(string path);
        void DeleteFile(string path);
        void DeleteFile(string path, DeletionOptions options);
        void DeleteDirectory(string path);
        IEnumerable<string> EnumerateDirectories(string parentDirectoryPath);
        IEnumerable<string> EnumerateDirectoriesRecursively(string parentDirectoryPath);
        IEnumerable<string> EnumerateFiles(string parentDirectoryPath, params string[] searchPatterns);
        IEnumerable<string> EnumerateFilesRecursively(string parentDirectoryPath, params string[] searchPatterns);
        long GetFileSize(string path);
        string ReadFile(string path);
        void AppendToFile(string path, string contents);
        void OverwriteFile(string path, string contents);
        Stream OpenFile(string path, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.Read);
        Stream OpenFile(string path, FileMode mode = FileMode.OpenOrCreate, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.Read);
        Stream CreateTemporaryFile(string extension, out string path);
        void CopyDirectory(string sourceDirectory, string targetDirectory, int overwriteFileRetryAttempts = 3);
        void CopyFile(string sourceFile, string targetFile, int overwriteFileRetryAttempts = 3);
        void PurgeDirectory(string targetDirectory, DeletionOptions options);
        void PurgeDirectory(string targetDirectory, Predicate<IFileInfo> filter, DeletionOptions options);
        void EnsureDirectoryExists(string directoryPath);
        void EnsureDiskHasEnoughFreeSpace(string directoryPath);
        void EnsureDiskHasEnoughFreeSpace(string directoryPath, long requiredSpaceInBytes);
        string GetFullPath(string relativeOrAbsoluteFilePath);
        void OverwriteAndDelete(string originalFile, string temporaryReplacement);
        string GetPathRelativeTo(string fullPath, string relativeTo);
    }

    public class PhysicalFileSystem : IFileSystem
    {
        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public void DeleteFile(string path)
        {
            DeleteFile(path, null);
        }

        public void DeleteFile(string path, DeletionOptions options)
        {
            options = options ?? DeletionOptions.TryThreeTimes;

            if (string.IsNullOrWhiteSpace(path))
                return;

            for (var i = 0; i < options.RetryAttempts; i++)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                catch
                {
                    Thread.Sleep(options.SleepBetweenAttemptsMilliseconds);

                    if (i == options.RetryAttempts - 1)
                    {
                        if (options.ThrowOnFailure)
                        {
                            throw;
                        }

                        break;
                    }
                }
            }
        }

        public void DeleteDirectory(string path)
        {
            Directory.Delete(path, true);
        }

        public IEnumerable<string> EnumerateFiles(string parentDirectoryPath, params string[] searchPatterns)
        {
            return searchPatterns.Length == 0
                ? Directory.EnumerateFiles(parentDirectoryPath, "*", SearchOption.TopDirectoryOnly)
                : searchPatterns.SelectMany(pattern => Directory.EnumerateFiles(parentDirectoryPath, pattern, SearchOption.TopDirectoryOnly));
        }

        public IEnumerable<string> EnumerateFilesRecursively(string parentDirectoryPath, params string[] searchPatterns)
        {
            return searchPatterns.Length == 0
                ? Directory.EnumerateFiles(parentDirectoryPath, "*", SearchOption.AllDirectories)
                : searchPatterns.SelectMany(pattern => Directory.EnumerateFiles(parentDirectoryPath, pattern, SearchOption.AllDirectories));
        }

        public IEnumerable<string> EnumerateDirectories(string parentDirectoryPath)
        {
            return Directory.EnumerateDirectories(parentDirectoryPath);
        }

        public IEnumerable<string> EnumerateDirectoriesRecursively(string parentDirectoryPath)
        {
            return Directory.EnumerateDirectories(parentDirectoryPath, "*", SearchOption.AllDirectories);
        }

        public long GetFileSize(string path)
        {
            return new FileInfo(path).Length;
        }

        public string ReadFile(string path)
        {
            return File.ReadAllText(path);
        }

        public void AppendToFile(string path, string contents)
        {
            File.AppendAllText(path, contents);
        }

        public void OverwriteFile(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }

        public Stream OpenFile(string path, FileAccess access, FileShare share)
        {
            return OpenFile(path, FileMode.OpenOrCreate, access, share);
        }

        public Stream OpenFile(string path, FileMode mode, FileAccess access, FileShare share)
        {
            return new FileStream(path, mode, access, share);
        }

        public Stream CreateTemporaryFile(string extension, out string path)
        {
            if (!extension.StartsWith("."))
                extension = "." + extension;

            path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            path = Path.Combine(path, Assembly.GetEntryAssembly().GetName().Name);
            path = Path.Combine(path, "Temp");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = Path.Combine(path, Guid.NewGuid() + extension);

            return OpenFile(path, FileAccess.ReadWrite, FileShare.Read);
        }

        public void CopyFile(string sourceFile, string targetFile, int overwriteFileRetryAttempts = 3)
        {
            for (var i = 0; i < overwriteFileRetryAttempts; i++)
            {
                try
                {
                    if (File.Exists(targetFile))
                    {
                        // Ensure not read-only, since File.Copy fails when the file is read-only
                        File.SetAttributes(targetFile, FileAttributes.Normal);
                    }

                    File.Copy(sourceFile, targetFile, true);
                    File.SetAttributes(targetFile, FileAttributes.Normal);
                }
                catch
                {
                    Thread.Sleep(i == 0 ? 100 : 1000 * i);

                    if (i == overwriteFileRetryAttempts - 1)
                    {
                        throw;
                    }
                }
            }
        }

        public void PurgeDirectory(string targetDirectory, DeletionOptions options)
        {
            PurgeDirectory(targetDirectory, (fi) => true, options);
        }

        public void PurgeDirectory(string targetDirectory, Predicate<IFileInfo> include, DeletionOptions options)
        {
            if (!DirectoryExists(targetDirectory))
            {
                return;
            }

            foreach (var file in EnumerateFilesRecursively(targetDirectory))
            {
                if (include != null)
                {
                    var info = new FileInfoAdapter(new FileInfo(file));
                    if (!include(info))
                    {
                        continue;
                    }
                }

                DeleteFile(file, options);
            }
        }

        public void OverwriteAndDelete(string originalFile, string temporaryReplacement)
        {
            var backup = originalFile + ".backup" + Guid.NewGuid();

            if (!File.Exists(originalFile))
                File.Copy(temporaryReplacement, originalFile, true);
            else
                File.Replace(temporaryReplacement, originalFile, backup);

            File.Delete(temporaryReplacement);
            if (File.Exists(backup))
                File.Delete(backup);
        }

        public string GetPathRelativeTo(string fullPath, string relativeTo)
        {
            // http://stackoverflow.com/questions/703281/getting-path-relative-to-the-current-working-directory
            var file = new Uri(fullPath);
            var folder = new Uri(relativeTo + (relativeTo.EndsWith("\\") ? "" : "\\"));
            var relativePath =
                Uri.UnescapeDataString(
                    folder.MakeRelativeUri(file)
                        .ToString()
                        .Replace('/', Path.DirectorySeparatorChar)
                    );
            return RemovePathTraversal(relativePath);
        }

        public string RemovePathTraversal(string path)
        {
            var pathTraversalChars = ".." + Path.DirectorySeparatorChar;
            if (path.StartsWith(pathTraversalChars))
            {
                path = path.Replace(pathTraversalChars, string.Empty);
                return RemovePathTraversal(path);
            }
            return path;
        }

        public void EnsureDirectoryExists(string directoryPath)
        {
            if (!DirectoryExists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
        }

        public void CopyDirectory(string sourceDirectory, string targetDirectory, int overwriteFileRetryAttempts = 3)
        {
            if (!DirectoryExists(sourceDirectory))
                return;

            if (!DirectoryExists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            var files = Directory.GetFiles(sourceDirectory, "*");
            foreach (var sourceFile in files)
            {
                var targetFile = Path.Combine(targetDirectory, Path.GetFileName(sourceFile));

                CopyFile(sourceFile, targetFile, overwriteFileRetryAttempts);
            }

            foreach (var childSourceDirectory in Directory.GetDirectories(sourceDirectory))
            {
                var name = Path.GetFileName(childSourceDirectory);
                var childTargetDirectory = Path.Combine(targetDirectory, name);
                CopyDirectory(childSourceDirectory, childTargetDirectory);
            }
        }

        public void EnsureDiskHasEnoughFreeSpace(string directoryPath)
        {
            EnsureDiskHasEnoughFreeSpace(directoryPath, 500 * 1024 * 1024);
        }

        public void EnsureDiskHasEnoughFreeSpace(string directoryPath, long requiredSpaceInBytes)
        {
            ulong freeBytesAvailable;
            ulong totalNumberOfBytes;
            ulong totalNumberOfFreeBytes;

            var success = GetDiskFreeSpaceEx(directoryPath, out freeBytesAvailable, out totalNumberOfBytes, out totalNumberOfFreeBytes);
            if (!success)
                return;

            // Always make sure at least 500MB are available regardless of what we need 
            var required = requiredSpaceInBytes < 0 ? 0 : (ulong)requiredSpaceInBytes;
            required = Math.Max(required, 500L * 1024 * 1024);
            if (totalNumberOfFreeBytes < required)
            {
                throw new IOException(string.Format("The drive containing the directory '{0}' does not have enough free disk space available for this operation to proceed. The disk only has {1} available; please free up at least {2}.", directoryPath, totalNumberOfFreeBytes.ToFileSizeString(), required.ToFileSizeString()));
            }
        }

        public string GetFullPath(string relativeOrAbsoluteFilePath)
        {
            if (!Path.IsPathRooted(relativeOrAbsoluteFilePath))
            {
                relativeOrAbsoluteFilePath = Path.Combine(Environment.CurrentDirectory, relativeOrAbsoluteFilePath);
            }

            relativeOrAbsoluteFilePath = Path.GetFullPath(relativeOrAbsoluteFilePath);
            return relativeOrAbsoluteFilePath;
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetDiskFreeSpaceEx(string lpDirectoryName,
            out ulong lpFreeBytesAvailable,
            out ulong lpTotalNumberOfBytes,
            out ulong lpTotalNumberOfFreeBytes);
    }

    public interface IFileInfo
    {
        string Extension { get; }
        DateTime LastAccessTimeUtc { get; }
        DateTime LastWriteTimeUtc { get; }
    }

    public class FileInfoAdapter : IFileInfo
    {
        readonly FileInfo info;

        public FileInfoAdapter(FileInfo info)
        {
            this.info = info;
        }

        public string Extension { get { return info.Extension; } }
        public DateTime LastAccessTimeUtc { get { return info.LastAccessTimeUtc; } }
        public DateTime LastWriteTimeUtc { get { return info.LastWriteTimeUtc; } }
    }

    public class DeletionOptions : IEquatable<DeletionOptions>
    {
        public static DeletionOptions TryThreeTimes { get { return new DeletionOptions { RetryAttempts = 3, ThrowOnFailure = true }; } }
        public static DeletionOptions TryThreeTimesIgnoreFailure { get { return new DeletionOptions { RetryAttempts = 3, ThrowOnFailure = false }; } }

        DeletionOptions()
        {
            SleepBetweenAttemptsMilliseconds = 100;
        }

        public int RetryAttempts { get; private set; }
        public int SleepBetweenAttemptsMilliseconds { get; private set; }
        public bool ThrowOnFailure { get; private set; }

        public bool Equals(DeletionOptions other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return RetryAttempts == other.RetryAttempts && SleepBetweenAttemptsMilliseconds == other.SleepBetweenAttemptsMilliseconds && ThrowOnFailure.Equals(other.ThrowOnFailure);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DeletionOptions)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = RetryAttempts;
                hashCode = (hashCode * 397) ^ SleepBetweenAttemptsMilliseconds;
                hashCode = (hashCode * 397) ^ ThrowOnFailure.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(DeletionOptions left, DeletionOptions right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(DeletionOptions left, DeletionOptions right)
        {
            return !Equals(left, right);
        }
    }

    public static class NumericExtensions
    {
        private const long Kilobyte = 1024;
        private const long Megabyte = 1024 * Kilobyte;
        private const long Gigabyte = 1024 * Megabyte;
        private const long Terabyte = 1024 * Gigabyte;

        public static string ToFileSizeString(this long bytes)
        {
            return ToFileSizeString(bytes <= 0 ? 0 : (ulong)bytes);
        }

        public static string ToFileSizeString(this ulong bytes)
        {
            if (bytes > Terabyte) return (bytes / Terabyte).ToString("0 TB");
            if (bytes > Gigabyte) return (bytes / Gigabyte).ToString("0 GB");
            if (bytes > Megabyte) return (bytes / Megabyte).ToString("0 MB");
            if (bytes > Kilobyte) return (bytes / Kilobyte).ToString("0 KB");
            return bytes + " bytes";
        }
    }
}
