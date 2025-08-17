using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace decode
{
    public class FileGenerationProgress
    {
        public int FilesGenerated { get; set; }
        public int TotalFiles { get; set; }
        public long BytesWritten { get; set; }
        public long TotalBytes { get; set; }
        public string CurrentFileName { get; set; }
        public double PercentComplete => TotalFiles > 0 ? (double)FilesGenerated / TotalFiles * 100 : 0;
    }

    public class FileGenerator : IDisposable
    {
        private readonly int _fileCount;
        private readonly long _fileSizeBytes;
        private readonly string _prefix;
        private readonly string _outputDirectory;
        private readonly List<string> _generatedFiles;
        private readonly RNGCryptoServiceProvider _rng;

        public FileGenerator(int fileCount, long fileSizeBytes, string prefix, string outputDirectory)
        {
            ValidateParameters(fileCount, fileSizeBytes, prefix, outputDirectory);
            
            _fileCount = fileCount;
            _fileSizeBytes = fileSizeBytes;
            _prefix = prefix;
            _outputDirectory = outputDirectory;
            _generatedFiles = new List<string>();
            _rng = new RNGCryptoServiceProvider();
        }

        private void ValidateParameters(int fileCount, long fileSizeBytes, string prefix, string outputDirectory)
        {
            if (fileCount <= 0)
                throw new ArgumentException("File count must be positive", nameof(fileCount));
            
            if (fileSizeBytes <= 0)
                throw new ArgumentException("File size must be positive", nameof(fileSizeBytes));
            
            if (string.IsNullOrWhiteSpace(prefix))
                throw new ArgumentException("Prefix cannot be null or empty", nameof(prefix));
            
            if (prefix.Length != 3)
                throw new ArgumentException("Prefix must be exactly 3 characters", nameof(prefix));
            
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("Output directory cannot be null or empty", nameof(outputDirectory));
        }

        public long CalculateTotalDiskSpace()
        {
            return (long)_fileCount * _fileSizeBytes;
        }

        public void GenerateFiles(IProgress<FileGenerationProgress> progress = null)
        {
            try
            {
                // Ensure output directory exists
                Directory.CreateDirectory(_outputDirectory);

                // Check available disk space
                var driveInfo = new DriveInfo(Path.GetPathRoot(_outputDirectory));
                long totalSpaceNeeded = CalculateTotalDiskSpace();
                
                if (driveInfo.AvailableFreeSpace < totalSpaceNeeded)
                {
                    throw new InvalidOperationException(
                        $"Insufficient disk space. Need {totalSpaceNeeded:N0} bytes, but only {driveInfo.AvailableFreeSpace:N0} bytes available.");
                }

                Console.WriteLine($"Generating {_fileCount} files of {_fileSizeBytes:N0} bytes each...");
                Console.WriteLine($"Total disk space required: {totalSpaceNeeded:N0} bytes");
                Console.WriteLine($"Output directory: {_outputDirectory}");

                for (int i = 0; i < _fileCount; i++)
                {
                    string fileName = GenerateUniqueFileName();
                    string filePath = Path.Combine(_outputDirectory, fileName);
                    
                    GenerateFile(filePath);
                    MakeFileImmutable(filePath);
                    _generatedFiles.Add(filePath);

                    // Report progress
                    progress?.Report(new FileGenerationProgress
                    {
                        FilesGenerated = i + 1,
                        TotalFiles = _fileCount,
                        BytesWritten = (long)(i + 1) * _fileSizeBytes,
                        TotalBytes = totalSpaceNeeded,
                        CurrentFileName = fileName
                    });
                }

                Console.WriteLine($"Successfully generated {_fileCount} files.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during file generation: {ex.Message}");
                throw;
            }
        }

        private string GenerateUniqueFileName()
        {
            string fileName;
            string filePath;
            int attempts = 0;
            const int maxAttempts = 1000;

            do
            {
                if (attempts >= maxAttempts)
                    throw new InvalidOperationException("Unable to generate unique filename after maximum attempts");

                string randomString = GenerateRandomString(61);
                fileName = $"{_prefix}{randomString}.dat";
                filePath = Path.Combine(_outputDirectory, fileName);
                attempts++;
            }
            while (File.Exists(filePath) || _generatedFiles.Contains(filePath));

            return fileName;
        }

        private string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var result = new StringBuilder(length);
            var buffer = new byte[4];

            for (int i = 0; i < length; i++)
            {
                _rng.GetBytes(buffer);
                uint randomValue = BitConverter.ToUInt32(buffer, 0);
                result.Append(chars[(int)(randomValue % chars.Length)]);
            }

            return result.ToString();
        }

        private void GenerateFile(string filePath)
        {
            const int bufferSize = 64 * 1024; // 64KB buffer for efficient I/O
            long remainingBytes = _fileSizeBytes;

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                while (remainingBytes > 0)
                {
                    int bytesToWrite = (int)Math.Min(bufferSize, remainingBytes);
                    byte[] buffer = new byte[bytesToWrite];
                    
                    _rng.GetBytes(buffer);
                    fileStream.Write(buffer, 0, bytesToWrite);
                    
                    remainingBytes -= bytesToWrite;
                }
            }
        }

        private void MakeFileImmutable(string filePath)
        {
            try
            {
                File.SetAttributes(filePath, File.GetAttributes(filePath) | FileAttributes.ReadOnly);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not make file immutable: {ex.Message}");
            }
        }

        public bool VerifyFiles()
        {
            Console.WriteLine("Verifying generated files...");
            bool allValid = true;

            foreach (string filePath in _generatedFiles)
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"ERROR: File not found: {filePath}");
                    allValid = false;
                    continue;
                }

                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length != _fileSizeBytes)
                {
                    Console.WriteLine($"ERROR: File size mismatch for {filePath}. Expected: {_fileSizeBytes}, Actual: {fileInfo.Length}");
                    allValid = false;
                }

                var attributes = File.GetAttributes(filePath);
                if ((attributes & FileAttributes.ReadOnly) == 0)
                {
                    Console.WriteLine($"WARNING: File is not read-only: {filePath}");
                }
            }

            Console.WriteLine(allValid ? "All files verified successfully." : "Some files failed verification.");
            return allValid;
        }

        public void CleanupFiles()
        {
            Console.WriteLine("Cleaning up generated files...");
            int deletedCount = 0;

            foreach (string filePath in _generatedFiles)
            {
                try
                {
                    if (File.Exists(filePath))
                    {
                        // Remove read-only attribute before deletion
                        File.SetAttributes(filePath, FileAttributes.Normal);
                        File.Delete(filePath);
                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file {filePath}: {ex.Message}");
                }
            }

            _generatedFiles.Clear();
            Console.WriteLine($"Deleted {deletedCount} files.");
        }

        public void Dispose()
        {
            _rng?.Dispose();
        }

        public List<string> GetGeneratedFiles()
        {
            return new List<string>(_generatedFiles);
        }
    }
}
