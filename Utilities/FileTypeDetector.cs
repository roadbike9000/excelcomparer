using System;
using System.IO;

namespace ExcelComparer.Utilities
{
    /// <summary>
    /// Utility for detecting file types based on extension and content.
    /// </summary>
    public static class FileTypeDetector
    {
        public enum FileType
        {
            Excel,
            Csv,
            Unknown
        }

        /// <summary>
        /// Detects the file type based on extension.
        /// </summary>
        public static FileType DetectFileType(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return FileType.Unknown;

            try
            {
                var extension = Path.GetExtension(filePath).ToLowerInvariant();

                return extension switch
                {
                    ".xlsx" => FileType.Excel,
                    ".xlsm" => FileType.Excel,
                    ".xlsb" => FileType.Excel,
                    ".xls" => FileType.Excel,
                    ".csv" => FileType.Csv,
                    ".txt" => FileType.Csv, // Treat .txt as potential CSV
                    _ => FileType.Unknown
                };
            }
            catch (ArgumentException)
            {
                // Path contains invalid characters
                return FileType.Unknown;
            }
        }

        /// <summary>
        /// Checks if both files are the same type.
        /// </summary>
        public static bool AreSameType(string file1, string file2)
        {
            var type1 = DetectFileType(file1);
            var type2 = DetectFileType(file2);

            return type1 == type2 && type1 != FileType.Unknown;
        }

        /// <summary>
        /// Checks if both files are Excel files.
        /// </summary>
        public static bool AreExcelFiles(string file1, string file2)
        {
            return DetectFileType(file1) == FileType.Excel &&
                   DetectFileType(file2) == FileType.Excel;
        }

        /// <summary>
        /// Checks if both files are CSV files.
        /// </summary>
        public static bool AreCsvFiles(string file1, string file2)
        {
            return DetectFileType(file1) == FileType.Csv &&
                   DetectFileType(file2) == FileType.Csv;
        }
    }
}
