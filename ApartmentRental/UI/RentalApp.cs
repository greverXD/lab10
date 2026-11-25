using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using ApartmentRental.Data;
using ApartmentRental.Extensions;
using ApartmentRental.Models;
using ApartmentRental.Services;
using static ApartmentRental.UI.ConsoleHelper;

namespace ApartmentRental.UI
{
    /// <summary>Консольное меню программы: клиент, собственник, отчёты.</summary>
    public class RentalApp
    {
        private const int TopCount = 5;
        private const string DefaultSaveFolder = "Saved";

        private static readonly Regex NameRegex = new Regex(@"^\p{L}[\p{L}\s\-\.']{1,99}$");
        private static readonly Regex RegionRegex = new Regex(@"^\p{L}[\p{L}\s\-\.]{1,49}$");

        private readonly ApartmentRepository _repository;
        private readonly ReportService _reports;
        private readonly string _baseFolder;

        public RentalApp(ApartmentRepository repository, ReportService reports, string baseFolder)
        {
            _repository = repository;
            _reports = reports;
            _baseFolder = baseFolder;
        }

        private IReadOnlyList<Apartment> Apartments => _repository.Apartments;

        public void Run()
        {
            while (true)
            {
                int choice = Menu("АРЕНДА КВАРТИР",
                    $"Квартир в базе: {Apartments.Count}, регионов: {Apartments.Regions().Count}",
                    "Выход",
                    "Я клиент — найти квартиру",
                    "Я собственник — мои объявления",
                    "Отчёты и статистика");

                switch (choice)
                {
                    case 0: return;
                    case 1: ClientMenu(); break;
                    case 2: OwnerMenu(); break;
                    case 3: ReportsMenu(); break;
                }
            }
        }

        // =====================================================================
        //  Клиент
        // =====================================================================

        private void ClientMenu()
        {
            while (true)
            {
                int choice = Menu("КЛИЕНТ", null, "Назад",
                    "Выбрать регион и квартиру",
                    "Поиск по ключевому слову",
                    "Подбор по фильтрам",
                    "Все квартиры с сортировкой");

                switch (choice)
                {
                    case 0: return;
                    case 1: BrowseByRegion(); break;
                    case 2: SearchByKeyword(); break;
                    case 3: FilterApartments(); break;
                    case 4: ShowAllSorted(); break;
                }
            }
        }

        private void BrowseByRegion()
        {
            var regions = Apartments
                .GroupBy(a => a.Region, StringComparer.CurrentCultureIgnoreCase)
                .OrderBy(g => g.Key)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .ToList();

            if (regions.Count == 0)
            {
                Info("В базе пока нет квартир.");
                Pause();
                return;
            }

            while (true)
            {
                Header("ВЫБОР РЕГИОНА");
                int choice = Choose("Назад", regions.Select(r => $"{r.Name} (квартир: {r.Count})").ToArray());
                if (choice == 0) return;

                string region = regions[choice - 1].Name;
                var list = Apartments.InRegion(region).OrderBy(a => a.MonthlyPrice).ToList();
                ChooseAndView(list, $"РЕГИОН «{region.ToUpper()}»");
            }
        }

        private void SearchByKeyword()
        {
            Header("ПОИСК ПО КЛЮЧЕВОМУ СЛОВУ");
            Console.WriteLine("Поиск идёт по региону, адресу, описанию и имени собственника.");
            string keyword = ReadText("Введите слово для поиска: ", 100);

            var found = Apartments.Search(keyword).OrderBy(a => a.MonthlyPrice).ToList();
            ChooseAndView(found, $"ПОИСК «{keyword}» — НАЙДЕНО: {found.Count}");
        }

        private void FilterApartments()
        {
            Header("ПОДБОР ПО ФИЛЬТРАМ");
            Console.WriteLine("Нажмите Enter, чтобы пропустить фильтр.\n");

            var filters = new List<ApartmentFilter>();
            var applied = new List<string>();

            // Регион
            List<string> regions = Apartments.Regions();
            for (int i = 0; i < regions.Count; i++)
                Console.WriteLine($"  {i + 1}. {regions[i]}");
            int? regionNumber = ReadOptionalInt("Регион (номер): ", 1, Math.Max(1, regions.Count));
            if (regionNumber is int rn && regions.Count > 0)
            {
                string region = regions[rn - 1];
                filters.Add(a => string.Equals(a.Region, region, StringComparison.CurrentCultureIgnoreCase));
                applied.Add(region);
            }

            // Цена
            decimal? minPrice = ReadOptionalDecimal("Цена от, руб.: ", 0, 100_000_000);
            if (minPrice is decimal min)
            {
                filters.Add(a => a.MonthlyPrice >= min);
                applied.Add($"цена от {min:N0}");
            }

            decimal? maxPrice = ReadOptionalDecimal("Цена до, руб.: ", minPrice ?? 0, 100_000_000);
            if (maxPrice is decimal max)
            {
                filters.Add(a => a.MonthlyPrice <= max);
                applied.Add($"цена до {max:N0}");
            }

            // Комнаты
            int? minRooms = ReadOptionalInt("Комнат от: ", Apartment.MinRooms, Apartment.MaxRooms);
            if (minRooms is int roomsFrom)
            {
                filters.Add(a => a.Rooms >= roomsFrom);
                applied.Add($"комнат от {roomsFrom}");
            }

            int? maxRooms = ReadOptionalInt("Комнат до: ", minRooms ?? Apartment.MinRooms, Apartment.MaxRooms);
            if (maxRooms is int roomsTo)
            {
                filters.Add(a => a.Rooms <= roomsTo);
                applied.Add($"комнат до {roomsTo}");
            }

            // Площадь
            double? minArea = ReadOptionalDouble("Площадь от, м²: ", 0, Apartment.MaxArea);
            if (minArea is double area)
            {
                filters.Add(a => a.Area >= area);
                applied.Add($"площадь от {area:0.#} м²");
            }

            // Мебель, животные, этаж
            bool? furniture = ReadOptionalYesNo("С мебелью? (д/н): ");
            if (furniture is bool needFurniture)
            {
                filters.Add(a => a.HasFurniture == needFurniture);
                applied.Add(needFurniture ? "с мебелью" : "без мебели");
            }

            bool? pets = ReadOptionalYesNo("Нужно, чтобы можно было с животными? (д/н): ");
            if (pets == true)
            {
                filters.Add(a => a.PetsAllowed);
                applied.Add("можно с животными");
            }

            bool? middleFloor = ReadOptionalYesNo("Только не первый и не последний этаж? (д/н): ");
            if (middleFloor == true)
            {
                filters.Add(a => a.IsMiddleFloor);
                applied.Add("не крайний этаж");
            }

            var result = Apartments.ApplyFilters(filters).OrderBy(a => a.MonthlyPrice).ToList();

            string title = filters.Count == 0
                ? $"ВСЕ КВАРТИРЫ (ФИЛЬТРЫ НЕ ЗАДАНЫ) — {result.Count}"
                : $"ПОДБОР: {string.Join(", ", applied)} — НАЙДЕНО: {result.Count}";
            ChooseAndView(result, title);
        }

        private void ShowAllSorted()
        {
            Header("ВСЕ КВАРТИРЫ");
            int choice = Choose("Назад",
                "По цене (сначала дешёвые)",
                "По цене (сначала дорогие)",
                "По площади (сначала большие)",
                "По популярности (просмотры)",
                "Сначала новые объявления");
            if (choice == 0) return;

            IEnumerable<Apartment> sorted = choice switch
            {
                1 => Apartments.OrderBy(a => a.MonthlyPrice),
                2 => Apartments.OrderByDescending(a => a.MonthlyPrice),
                3 => Apartments.OrderByDescending(a => a.Area),
                4 => Apartments.OrderByDescending(a => a.Views),
                _ => Apartments.OrderByDescending(a => a.PublishedAt)
            };

            ChooseAndView(sorted.ToList(), "ВСЕ КВАРТИРЫ");
        }

        /// <summary>Показывает список квартир и даёт выбрать одну для просмотра.</summary>
        private void ChooseAndView(List<Apartment> list, string title)
        {
            while (true)
            {
                Header(title);
                if (list.Count == 0)
                {
                    Info("Квартиры не найдены.");
                    Pause();
                    return;
                }

                for (int i = 0; i < list.Count; i++)
                    Console.WriteLine($"{i + 1,2}. {list[i].GetShortInfo()}");
                Console.WriteLine(" 0. Назад");

                int number = ReadInt("Номер квартиры для просмотра: ", 0, list.Count);
                if (number == 0) return;

                ViewApartment(list[number - 1]);
            }
        }

        private void ViewApartment(Apartment apartment)
        {
            try
            {
                // Событие Viewed → репозиторий сразу сохраняет новый счётчик в XML.
                apartment.RegisterView();
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Error("Не удалось сохранить счётчик просмотров: " + ex.Message);
            }

            while (true)
            {
                Header($"КВАРТИРА №{apartment.Id}");
                Console.Write(apartment.GetFullInfo());
                Console.WriteLine();

                int choice = Choose("Назад к списку",
                    "Сохранить квартиру в текстовый файл",
                    "Рассчитать стоимость аренды за период");

                switch (choice)
                {
                    case 0: return;
                    case 1: SaveApartmentToFile(apartment); break;
                    case 2: CalculateRent(apartment); break;
                }
            }
        }

        private static void CalculateRent(Apartment apartment)
        {
            int months = ReadInt("Срок аренды в месяцах (1–36): ", 1, 36);
            decimal total = apartment.CalculateRentCost(months);
            Success($"Аренда на {months} мес.: {total:N0} руб. ({apartment.MonthlyPrice:N0} руб. × {months}).");
            Pause();
        }

        private void SaveApartmentToFile(Apartment apartment)
        {
            Console.WriteLine();
            Console.Write($"Папка для сохранения (Enter — «{DefaultSaveFolder}»): ");
            string folderInput = ReadLine();
            if (folderInput.Length == 0)
                folderInput = DefaultSaveFolder;

            if (!FileService.TryPrepareFolder(folderInput, _baseFolder, out string folder, out string error))
            {
                Error(error);
                Pause();
                return;
            }

            string fileName;
            while (true)
            {
                fileName = ReadText("Имя файла (например, moya_kvartira): ", 100);
                if (fileName.IsValidFileName())
                    break;
                Error("Недопустимое имя файла: нельзя использовать < > : \" / \\ | ? * и имена CON, NUL, COM1 и т. п.");
            }

            string path = Path.Combine(folder, fileName.WithTxtExtension());
            bool append = false;

            if (File.Exists(path))
            {
                Warning($"Файл уже существует: {path}");
                int choice = Choose("Отмена", "Перезаписать файл", "Дописать квартиру в конец файла");
                if (choice == 0) return;
                append = choice == 2;
            }

            try
            {
                FileService.SaveApartment(apartment, path, append);
                Success($"Квартира сохранена в файл: {path}");
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is NotSupportedException)
            {
                Error("Не удалось сохранить файл: " + ex.Message);
            }

            Pause();
        }

        // =====================================================================
        //  Собственник
        // =====================================================================

        private void OwnerMenu()
        {
            while (true)
            {
                int choice = Menu("СОБСТВЕННИК", null, "Назад",
                    "Добавить объявление",
                    "Мои объявления (просмотры, удаление)");

                switch (choice)
                {
                    case 0: return;
                    case 1: AddListing(); break;
                    case 2: MyListings(); break;
                }
            }
        }

        private void AddListing()
        {
            Header("НОВОЕ ОБЪЯВЛЕНИЕ");

            Console.WriteLine("Шаг 1. Регион");
            List<string> regions = Apartments.Regions();
            int regionChoice = Choose("Новый регион", regions.ToArray());
            string region = regionChoice == 0 ? ReadRegionName() : regions[regionChoice - 1];

            Console.WriteLine("\nШаг 2. Квартира");
            string address = ReadText("Адрес (улица, дом, квартира): ", 150);
            int rooms = ReadInt($"Количество комнат ({Apartment.MinRooms}–{Apartment.MaxRooms}): ",
                                Apartment.MinRooms, Apartment.MaxRooms);
            double area = ReadDouble($"Площадь, м² ({Apartment.MinArea}–{Apartment.MaxArea}): ",
                                     Apartment.MinArea, Apartment.MaxArea);
            int totalFloors = ReadInt($"Этажей в доме (1–{Apartment.MaxFloor}): ", 1, Apartment.MaxFloor);
            int floor = ReadInt($"Этаж (1–{totalFloors}): ", 1, totalFloors);
            decimal price = ReadDecimal("Цена аренды в месяц, руб.: ", 1_000, 10_000_000);
            bool furniture = ReadYesNo("Есть мебель? (д/н): ");
            bool pets = ReadYesNo("Можно с животными? (д/н): ");
            string description = ReadText("Описание (Enter — без описания): ", 300, allowEmpty: true);

            Console.WriteLine("\nШаг 3. Контакты");
            string ownerName = ReadName("ФИО собственника: ");
            string phone = ReadPhone("Телефон: ");

            Apartment apartment;
            try
            {
                apartment = new Apartment(_repository.NextId, region, address, area, price, rooms, floor,
                                          totalFloors, furniture, pets, ownerName, phone, description);
            }
            catch (ArgumentException ex)
            {
                Error(ex.Message);
                Pause();
                return;
            }

            Header("ПРОВЕРЬТЕ ОБЪЯВЛЕНИЕ");
            Console.Write(apartment.GetFullInfo());
            Console.WriteLine();

            if (!ReadYesNo("Опубликовать объявление? (д/н): "))
            {
                Info("Объявление не сохранено.");
                Pause();
                return;
            }

            try
            {
                _repository.Add(apartment);
                Success($"Объявление №{apartment.Id} опубликовано в регионе «{apartment.Region}».");
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException
                                       || ex is InvalidOperationException)
            {
                Error("Не удалось сохранить объявление: " + ex.Message);
            }

            Pause();
        }

        private void MyListings()
        {
            Header("МОИ ОБЪЯВЛЕНИЯ");
            string phone = ReadPhone("Ваш номер телефона: ");

            while (true)
            {
                var mine = Apartments
                    .Where(a => Apartment.IsSamePhone(a.OwnerPhone, phone))
                    .OrderBy(a => a.Id)
                    .ToList();

                if (mine.Count == 0)
                {
                    Info("Объявлений с этим номером телефона нет.");
                    Pause();
                    return;
                }

                Header($"МОИ ОБЪЯВЛЕНИЯ: {mine.Count}, ВСЕГО ПРОСМОТРОВ: {mine.Sum(a => a.Views)}");
                for (int i = 0; i < mine.Count; i++)
                    Console.WriteLine($"{i + 1,2}. [{mine[i].Region}] {mine[i].GetShortInfo()}");

                Console.WriteLine("\nВыберите объявление, чтобы удалить его, или 0 — назад.");
                int number = ReadInt("Номер: ", 0, mine.Count);
                if (number == 0) return;

                Apartment apartment = mine[number - 1];
                if (!ReadYesNo($"Удалить объявление №{apartment.Id} ({apartment.Address})? (д/н): "))
                    continue;

                try
                {
                    _repository.Remove(apartment);
                    Success("Объявление удалено.");
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                {
                    Error("Не удалось удалить объявление: " + ex.Message);
                }
                Pause();
            }
        }

        private static string ReadRegionName()
        {
            while (true)
            {
                string name = ReadText("Название нового региона: ", 50);
                if (RegionRegex.IsMatch(name))
                    return name;
                Error("Название региона должно состоять из букв (допускаются пробел, дефис и точка).");
            }
        }

        private static string ReadName(string prompt)
        {
            while (true)
            {
                string name = ReadText(prompt, 100);
                if (NameRegex.IsMatch(name))
                    return name;
                Error("Имя должно состоять из букв (допускаются пробел, дефис, точка и апостроф).");
            }
        }

        private static string ReadPhone(string prompt)
        {
            while (true)
            {
                string phone = ReadText(prompt, 25);
                if (Apartment.IsValidPhone(phone))
                    return Apartment.NormalizePhone(phone);
                Error("Неверный номер. Пример: +79161234567, 8 916 123-45-67 или +375 29 123-45-67.");
            }
        }

        // =====================================================================
        //  Отчёты
        // =====================================================================

        private void ReportsMenu()
        {
            while (true)
            {
                int choice = Menu("ОТЧЁТЫ И СТАТИСТИКА", $"Отчёты сохраняются в папку: {_reports.Folder}", "Назад",
                    "Сводка по регионам",
                    $"Топ-{TopCount} квартир по просмотрам",
                    "Каталог всех квартир",
                    "Статистика текущего сеанса");

                switch (choice)
                {
                    case 0: return;
                    case 1: ShowAndSave("regions", _reports.BuildRegionSummary(Apartments.ToList())); break;
                    case 2: ShowAndSave("top_views", _reports.BuildTopViewed(Apartments.ToList(), TopCount)); break;
                    case 3: ShowAndSave("catalog", _reports.BuildCatalog(Apartments.ToList())); break;
                    case 4: ShowSessionStatistics(); break;
                }
            }
        }

        private void ShowAndSave(string reportName, List<string> lines)
        {
            Console.WriteLine();
            lines.ForEach(Console.WriteLine);
            Console.WriteLine();

            try
            {
                string path = _reports.Save(reportName, lines);
                Success($"Отчёт сохранён: {path}");
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Error("Не удалось сохранить отчёт: " + ex.Message);
            }

            Pause();
        }

        private void ShowSessionStatistics()
        {
            Header("СТАТИСТИКА СЕАНСА");
            Console.WriteLine($"Создано объектов «Квартира»: {Apartment.InstancesCreated}");
            Console.WriteLine($"Просмотров за сеанс:         {Apartment.ViewsInSession}");

            var viewed = Apartments
                .Where(a => a.SessionViews.Count > 0)
                .OrderByDescending(a => a.SessionViews.Count)
                .ToList();

            if (viewed.Count > 0)
            {
                Console.WriteLine("\nПросмотренные квартиры:");
                foreach (Apartment a in viewed)
                    Console.WriteLine($"  №{a.Id} {a.Address}: {a.SessionViews.Count} раз(а), " +
                                      $"последний — в {a.SessionViews[^1]:HH:mm:ss}");
            }

            Pause();
        }
    }
}
