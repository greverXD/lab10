using System;
using System.IO;
using System.Linq;
using System.Text;
using ApartmentRental.Extensions;
using ApartmentRental.Models;

namespace ApartmentRental.Services
{
    /// <summary>Сохранение понравившейся квартиры в текстовый файл.</summary>
    public static class FileService
    {
        private const string InvalidPathChars = "<>\"|?*";

        /// <summary>
        /// Проверяет путь к папке, введённый пользователем, и создаёт папку.
        /// Относительный путь считается от папки программы.
        /// </summary>
        public static bool TryPrepareFolder(string input, string baseFolder, out string fullPath, out string error)
        {
            fullPath = "";
            error = "";

            string folder = input.Trim().Trim('"');
            if (folder.Length == 0)
            {
                error = "Путь к папке не может быть пустым.";
                return false;
            }

            if (folder.Any(c => char.IsControl(c) || InvalidPathChars.Contains(c)))
            {
                error = "Путь содержит недопустимые символы: < > \" | ? *";
                return false;
            }

            try
            {
                fullPath = Path.GetFullPath(Path.IsPathRooted(folder) ? folder : Path.Combine(baseFolder, folder));
                Directory.CreateDirectory(fullPath);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                error = "Нет прав на создание или запись в эту папку.";
            }
            catch (PathTooLongException)
            {
                error = "Слишком длинный путь.";
            }
            catch (IOException ex)
            {
                error = "Не удалось создать папку: " + ex.Message;
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException)
            {
                error = "Неверный путь к папке.";
            }

            return false;
        }

        /// <summary>Записывает карточку квартиры в файл (перезапись или дозапись).</summary>
        public static void SaveApartment(Apartment apartment, string path, bool append)
        {
            string text = apartment.ToFileText();
            if (append)
                File.AppendAllText(path, text, Encoding.UTF8);
            else
                File.WriteAllText(path, text, Encoding.UTF8);
        }
    }
}
