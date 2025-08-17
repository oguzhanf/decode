using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace decode
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Usage();
                return;
            }

            try
            {
                ParameterHandler(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static void ParameterHandler(string[] args)
        {
            if (args.Length < 2)
            {
                Usage();
                return;
            }

            switch (args[0].ToLower())
            {
                case "--md5":
                    if (args.Length < 3 || args[1].ToLower() != "ile")
                    {
                        Console.WriteLine("Error: MD5 requires 'ile' parameter and filename");
                        Usage();
                        return;
                    }
                    Md5Calculate(args[2]);
                    break;
                case "--base64":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Error: Base64 requires operation (encode/decode) and input");
                        Usage();
                        return;
                    }
                    switch (args[1].ToLower())
                    {
                        case "--decode":
                            if (args.Length < 3)
                            {
                                Console.WriteLine("Error: Base64 decode requires input");
                                Usage();
                                return;
                            }
                            Base64Decode(args[2]);
                            break;
                        case "--encode":
                            if (args.Length < 3)
                            {
                                Console.WriteLine("Error: Base64 encode requires input");
                                Usage();
                                return;
                            }
                            Base64Encode(args[2]);
                            break;
                        default:
                            Usage();
                            break;
                    }
                    break;
                case "--generate":
                    HandleFileGeneration(args);
                    break;
                default:
                    Usage();
                    break;
            }
        }

        private static void Base64Encode(string input)
        {
            try
            {
                if (File.Exists(input))
                {
                    // Encode file content
                    byte[] fileBytes = File.ReadAllBytes(input);
                    string encoded = Convert.ToBase64String(fileBytes);
                    Console.WriteLine(encoded);
                }
                else
                {
                    // Encode string directly
                    byte[] bytes = Encoding.UTF8.GetBytes(input);
                    string encoded = Convert.ToBase64String(bytes);
                    Console.WriteLine(encoded);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error encoding: {ex.Message}");
            }
        }

        private static void Base64Decode(string input)
        {
            try
            {
                if (File.Exists(input))
                {
                    // Decode file content
                    string fileContent = File.ReadAllText(input);
                    byte[] decoded = Convert.FromBase64String(fileContent.Trim());
                    string result = Encoding.UTF8.GetString(decoded);
                    Console.WriteLine(result);
                }
                else
                {
                    // Decode string directly
                    byte[] decoded = Convert.FromBase64String(input);
                    string result = Encoding.UTF8.GetString(decoded);
                    Console.WriteLine(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error decoding: {ex.Message}");
            }
        }

        private static void Md5Calculate(string filename)
        {
            try
            {
                if (!File.Exists(filename))
                {
                    Console.WriteLine($"Error: File '{filename}' not found.");
                    return;
                }

                using (var md5 = MD5.Create())
                using (var stream = File.OpenRead(filename))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    string hashString = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    Console.WriteLine($"MD5 hash of '{filename}': {hashString}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error calculating MD5: {ex.Message}");
            }
        }

        private static void HandleFileGeneration(string[] args)
        {
            if (args.Length < 6)
            {
                Console.WriteLine("Error: File generation requires parameters");
                Console.WriteLine("Usage: --generate --count <number> --size <bytes> --prefix <3chars> --output <directory>");
                Console.WriteLine("Optional: --verify --cleanup");
                return;
            }

            try
            {
                int fileCount = 0;
                long fileSizeBytes = 0;
                string prefix = "";
                string outputDirectory = "";
                bool verify = false;
                bool cleanup = false;

                // Parse arguments
                for (int i = 1; i < args.Length; i++)
                {
                    switch (args[i].ToLower())
                    {
                        case "--count":
                            if (i + 1 < args.Length && int.TryParse(args[i + 1], out fileCount))
                                i++;
                            else
                            {
                                Console.WriteLine("Error: Invalid file count");
                                return;
                            }
                            break;
                        case "--size":
                            if (i + 1 < args.Length && long.TryParse(args[i + 1], out fileSizeBytes))
                                i++;
                            else
                            {
                                Console.WriteLine("Error: Invalid file size");
                                return;
                            }
                            break;
                        case "--prefix":
                            if (i + 1 < args.Length)
                            {
                                prefix = args[i + 1];
                                i++;
                            }
                            else
                            {
                                Console.WriteLine("Error: Prefix required");
                                return;
                            }
                            break;
                        case "--output":
                            if (i + 1 < args.Length)
                            {
                                outputDirectory = args[i + 1];
                                i++;
                            }
                            else
                            {
                                Console.WriteLine("Error: Output directory required");
                                return;
                            }
                            break;
                        case "--verify":
                            verify = true;
                            break;
                        case "--cleanup":
                            cleanup = true;
                            break;
                    }
                }

                // Validate required parameters
                if (fileCount <= 0 || fileSizeBytes <= 0 || string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(outputDirectory))
                {
                    Console.WriteLine("Error: Missing required parameters");
                    Console.WriteLine("Required: --count <number> --size <bytes> --prefix <3chars> --output <directory>");
                    return;
                }

                // Create and use file generator
                using (var generator = new FileGenerator(fileCount, fileSizeBytes, prefix, outputDirectory))
                {
                    Console.WriteLine($"Total disk space required: {generator.CalculateTotalDiskSpace():N0} bytes");

                    // Progress reporting
                    var progress = new Progress<FileGenerationProgress>(p =>
                    {
                        Console.WriteLine($"Progress: {p.FilesGenerated}/{p.TotalFiles} files ({p.PercentComplete:F1}%) - Current: {p.CurrentFileName}");
                    });

                    generator.GenerateFiles(progress);

                    if (verify)
                    {
                        generator.VerifyFiles();
                    }

                    if (cleanup)
                    {
                        Console.WriteLine("Do you want to cleanup generated files? (y/N)");
                        string response = Console.ReadLine();
                        if (response?.ToLower() == "y" || response?.ToLower() == "yes")
                        {
                            generator.CleanupFiles();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        static void Usage()
        {
            Console.WriteLine("decode - A .NET Core command line tool for MD5, Base64, and file generation");
            Console.WriteLine("================================================================================");
            Console.WriteLine();
            Console.WriteLine("USAGE:");
            Console.WriteLine("  decode [command] [options]");
            Console.WriteLine();
            Console.WriteLine("COMMANDS:");
            Console.WriteLine("  --md5 ile [filename]");
            Console.WriteLine("    Generate MD5 hash from file");
            Console.WriteLine("    Example: decode --md5 ile myfile.txt");
            Console.WriteLine();
            Console.WriteLine("  --base64 --encode [input]");
            Console.WriteLine("    Encode file or string as base64");
            Console.WriteLine("    Example: decode --base64 --encode myfile.txt");
            Console.WriteLine("    Example: decode --base64 --encode \"Hello World\"");
            Console.WriteLine();
            Console.WriteLine("  --base64 --decode [input]");
            Console.WriteLine("    Decode base64 encoded file or string");
            Console.WriteLine("    Example: decode --base64 --decode encoded.txt");
            Console.WriteLine("    Example: decode --base64 --decode \"SGVsbG8gV29ybGQ=\"");
            Console.WriteLine();
            Console.WriteLine("  --generate --count <number> --size <bytes> --prefix <3chars> --output <directory> [options]");
            Console.WriteLine("    Generate test files with random, incompressible content");
            Console.WriteLine("    Required parameters:");
            Console.WriteLine("      --count <number>     Number of files to generate");
            Console.WriteLine("      --size <bytes>       Size of each file in bytes");
            Console.WriteLine("      --prefix <3chars>    Exactly 3 characters for filename prefix");
            Console.WriteLine("      --output <directory> Target directory for file creation");
            Console.WriteLine("    Optional parameters:");
            Console.WriteLine("      --verify             Verify generated files after creation");
            Console.WriteLine("      --cleanup            Prompt to cleanup generated files");
            Console.WriteLine();
            Console.WriteLine("    Example: decode --generate --count 10 --size 1048576 --prefix ABC --output C:\\temp --verify");
            Console.WriteLine("    This creates 10 files of 1MB each with names like ABC{random}.dat");
            Console.WriteLine();
            Console.WriteLine("FILE GENERATION FEATURES:");
            Console.WriteLine("  - Cryptographically secure random content (incompressible)");
            Console.WriteLine("  - Unique 64-character filenames: {prefix}{61-char-random}.dat");
            Console.WriteLine("  - Files are made immutable (read-only) after creation");
            Console.WriteLine("  - Progress reporting during generation");
            Console.WriteLine("  - Disk space validation before generation");
            Console.WriteLine("  - File verification and cleanup capabilities");
            Console.WriteLine();
        }
    }
}
