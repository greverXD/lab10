using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ApartmentRental.UI
{
    /// <summary>Ввод с проверкой и цветной вывод в консоль.</summary>
    public static class ConsoleHelper
    {
        private static readonly string[] YesAnswers = { "д", "да", "y", "yes" };
        private static readonly string[] NoAnswers = { "н", "нет", "n", "no" };

        // ---------- Вывод ----------

        public static void Header(string title)
        {
            string line = new string('=', Math.Max(50, title.Length + 4));
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(line);
            Console.WriteLine("  " + title);
            Console.WriteLine(line);
            Console.ResetColor();
        }

        public static void Error(string message) => WriteColored(message, ConsoleColor.Red);

        public static void Warning(string message) => WriteColored(message, ConsoleColor.Yellow);

        public static void Success(string message) => WriteColored(message, ConsoleColor.Green);

        public static void Info(string message) => WriteColored(message, ConsoleColor.DarkCyan);

        private static void WriteColored(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public static void Pause()
        {
            Console.Write("\nНажмите Enter, чтобы продолжить...");
            ReadLine();
        }

        // ---------- Меню ----------

        /// <summary>Заголовок + пункты меню. Возвращает номер выбранного пункта (0 — выход/назад).</summary>
        public static int Menu(string title, string? subtitle, string zeroItem, params string[] items)
        {
            Header(title);
            if (!string.IsNullOrEmpty(subtitle))
                Info(subtitle + Environment.NewLine);
            return Choose(zeroItem, items);
        }

        /// <summary>Пункты меню без заголовка.</summary>
        public static int Choose(string zeroItem, params string[] items)
        {
            for (int i = 0; i < items.Length; i++)
                Console.WriteLine($"{i + 1}. {items[i]}");
            Console.WriteLine($"0. {zeroItem}");
            return ReadInt("Ваш выбор: ", 0, items.Length);
        }

        // ---------- Ввод ----------

        /// <summary>Читает строку. Если ввод закрыт (Ctrl+Z / Ctrl+D) — EndOfStreamException.</summary>
        public static string ReadLine()
        {
            string? line = Console.ReadLine();
            if (line == null)
                throw new EndOfStreamException();
            return line.Trim();
        }

        public static string ReadText(string prompt, int maxLength, bool allowEmpty = false)
        {
            while (true)
            {
                Console.Write(prompt);
                string value = ReadLine();

                if (value.Length == 0 && allowEmpty)
                    return value;
                if (value.Length == 0)
                    Error("Значение не может быть пустым.");
                else if (value.Length > maxLength)
                    Error($"Слишком длинное значение (не больше {maxLength} символов).");
                else
                    return value;
            }
        }

        public static int ReadInt(string prompt, int min, int max) =>
            ReadIntCore(prompt, min, max, optional: false) ?? min;

        /// <summary>Пустой ввод — null (фильтр не задан).</summary>
        public static int? ReadOptionalInt(string prompt, int min, int max) =>
            ReadIntCore(prompt, min, max, optional: true);

        public static decimal ReadDecimal(string prompt, decimal min, decimal max) =>
            ReadDecimalCore(prompt, min, max, optional: false) ?? min;

        public static decimal? ReadOptionalDecimal(string prompt, decimal min, decimal max) =>
            ReadDecimalCore(prompt, min, max, optional: true);

        public static double ReadDouble(string prompt, double min, double max) =>
            (double)ReadDecimal(prompt, (decimal)min, (decimal)max);

        public static double? ReadOptionalDouble(string prompt, double min, double max) =>
            (double?)ReadOptionalDecimal(prompt, (decimal)min, (decimal)max);

        public static bool ReadYesNo(string prompt) =>
            ReadYesNoCore(prompt, optional: false) ?? false;

        public static bool? ReadOptionalYesNo(string prompt) =>
            ReadYesNoCore(prompt, optional: true);

        private static int? ReadIntCore(string prompt, int min, int max, bool optional)
        {
            while (true)
            {
                Console.Write(prompt);
                string text = ReadLine();

                if (text.Length == 0 && optional)
                    return null;
                if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                    && value >= min && value <= max)
                    return value;

                Error($"Введите целое число от {min} до {max}.");
            }
        }

        private static decimal? ReadDecimalCore(string prompt, decimal min, decimal max, bool optional)
        {
            while (true)
            {
                Console.Write(prompt);
                string text = ReadLine();

                if (text.Length == 0 && optional)
                    return null;
                if (TryParseNumber(text, out decimal value) && value >= min && value <= max)
                    return value;

                Error($"Введите число от {min:N0} до {max:N0}.");
            }
        }

        private static bool? ReadYesNoCore(string prompt, bool optional)
        {
            while (true)
            {
                Console.Write(prompt);
                string text = ReadLine().ToLowerInvariant();

                if (text.Length == 0 && optional)
                    return null;
                if (YesAnswers.Contains(text))
                    return true;
                if (NoAnswers.Contains(text))
                    return false;

                Error("Введите «д» (да) или «н» (нет).");
            }
        }

        /// <summary>Принимает и запятую, и точку: «54,5» и «54.5»; пробелы между разрядами игнорируются.</summary>
        private static bool TryParseNumber(string text, out decimal value)
        {
            string normalized = text.Replace(" ", "").Replace(" ", "").Replace(',', '.');
            return decimal.TryParse(normalized, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                                    CultureInfo.InvariantCulture, out value);
        }
    }
}
