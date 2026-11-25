using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ApartmentRental.Models;

namespace ApartmentRental.Extensions
{
    /// <summary>Методы расширения для коллекций квартир и отдельной квартиры.</summary>
    public static class ApartmentExtensions
    {
        /// <summary>Квартиры указанного региона.</summary>
        public static IEnumerable<Apartment> InRegion(this IEnumerable<Apartment> source, string region) =>
            source.Where(a => string.Equals(a.Region, region, StringComparison.CurrentCultureIgnoreCase));

        /// <summary>Поиск по ключевому слову.</summary>
        public static IEnumerable<Apartment> Search(this IEnumerable<Apartment> source, string keyword) =>
            source.Where(a => a.Matches(keyword));

        /// <summary>Оставляет квартиры, которые проходят ВСЕ фильтры (делегаты ApartmentFilter).</summary>
        public static IEnumerable<Apartment> ApplyFilters(this IEnumerable<Apartment> source,
                                                          IReadOnlyCollection<ApartmentFilter> filters) =>
            source.Where(a => filters.All(filter => filter(a)));

        /// <summary>Список регионов без повторов, по алфавиту.</summary>
        public static List<string> Regions(this IEnumerable<Apartment> source) =>
            source.Select(a => a.Region)
                  .Distinct(StringComparer.CurrentCultureIgnoreCase)
                  .OrderBy(r => r, StringComparer.CurrentCulture)
                  .ToList();

        /// <summary>Текст карточки квартиры для сохранения в файл.</summary>
        public static string ToFileText(this Apartment apartment)
        {
            string line = new string('=', 60);
            var sb = new StringBuilder();
            sb.AppendLine(line);
            sb.AppendLine($"Квартира сохранена {DateTime.Now:dd.MM.yyyy HH:mm}");
            sb.AppendLine(line);
            sb.Append(apartment.GetFullInfo());
            sb.AppendLine();
            return sb.ToString();
        }
    }
}
