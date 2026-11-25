using System;
using System.IO;
using System.Linq;

namespace ApartmentRental.Extensions
{
    /// <summary>Методы расширения для строк и простых типов.</summary>
    public static class StringExtensions
    {
        private const string InvalidFileNameChars = "<>:\"/\\|?*";

        // Имена, которые Windows не разрешает использовать для файлов.
        private static readonly string[] ReservedNames =
            new[] { "CON", "PRN", "AUX", "NUL" }
                .Concat(Enumerable.Range(1, 9).SelectMany(i => new[] { "COM" + i, "LPT" + i }))
                .ToArray();

        public static string ToYesNo(this bool value) => value ? "да" : "нет";

        /// <summary>Проверяет имя файла (одинаково строго для Windows и Linux).</summary>
        public static bool IsValidFileName(this string? name)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
                return false;

            if (name.Any(c => char.IsControl(c) || InvalidFileNameChars.Contains(c)))
                return false;

            if (name.EndsWith(".") || name.EndsWith(" ") || name.Trim('.').Length == 0)
                return false;

            string baseName = Path.GetFileNameWithoutExtension(name).ToUpperInvariant();
            return !ReservedNames.Contains(baseName);
        }

        /// <summary>Добавляет расширение .txt, если его нет.</summary>
        public static string WithTxtExtension(this string fileName) =>
            fileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ? fileName : fileName + ".txt";
    }
}
