using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using ApartmentRental.Data;
using ApartmentRental.Services;
using ApartmentRental.UI;

namespace ApartmentRental
{
    internal static class Program
    {
        private const string DataFileName = "apartments.xml";

        private static int Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            if (OperatingSystem.IsWindows())
                Console.InputEncoding = Encoding.Unicode; // корректный ввод кириллицы в консоли Windows

            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("ru-RU");
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ru-RU");

            // Папка, из которой запущена программа (при «dotnet run» — папка проекта).
            string baseFolder = Directory.GetCurrentDirectory();
            string dataPath = FindDataFile(baseFolder);

            var repository = new ApartmentRepository(dataPath);
            try
            {
                repository.Load();
            }
            catch (FileNotFoundException)
            {
                ConsoleHelper.Error($"Не найден файл с данными: {dataPath}");
                return 1;
            }
            catch (XmlException ex)
            {
                ConsoleHelper.Error($"Ошибка в XML-файле (строка {ex.LineNumber}): {ex.Message}");
                return 1;
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is IOException || ex is UnauthorizedAccessException)
            {
                ConsoleHelper.Error("Не удалось загрузить данные: " + ex.Message);
                return 1;
            }

            ConsoleHelper.Info($"Данные загружены из файла: {dataPath}");
            foreach (string warning in repository.LoadWarnings)
                ConsoleHelper.Warning(warning);

            var reports = new ReportService(Path.Combine(baseFolder, "Reports"));
            var app = new RentalApp(repository, reports, baseFolder);

            try
            {
                app.Run();
            }
            catch (EndOfStreamException)
            {
                // Ввод закрыт (Ctrl+Z в Windows, Ctrl+D в Linux) — просто выходим.
            }

            Console.WriteLine("\nДо свидания!");
            return 0;
        }

        /// <summary>Ищет XML сначала в папке Data рядом с проектом, затем рядом с exe.</summary>
        private static string FindDataFile(string baseFolder)
        {
            string projectData = Path.Combine(baseFolder, "Data", DataFileName);
            if (File.Exists(projectData))
                return projectData;

            return Path.Combine(AppContext.BaseDirectory, "Data", DataFileName);
        }
    }
}
