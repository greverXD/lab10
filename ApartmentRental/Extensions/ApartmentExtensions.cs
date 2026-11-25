using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ApartmentRental.Models;

namespace ApartmentRental.Extensions
{
    public static class ApartmentExtensions
    {
        public static IEnumerable<Apartment> InRegion(this IEnumerable<Apartment> source, string region) =>
            source.Where(a => string.Equals(a.Region, region, StringComparison.CurrentCultureIgnoreCase));

        public static IEnumerable<Apartment> Search(this IEnumerable<Apartment> source, string keyword) =>
            source.Where(a => a.Matches(keyword));

        public static IEnumerable<Apartment> ApplyFilters(this IEnumerable<Apartment> source,
                                                          IReadOnlyCollection<ApartmentFilter> filters) =>
            source.Where(a => filters.All(filter => filter(a)));

        public static List<string> Regions(this IEnumerable<Apartment> source) =>
            source.Select(a => a.Region)
                  .Distinct(StringComparer.CurrentCultureIgnoreCase)
                  .OrderBy(r => r, StringComparer.CurrentCulture)
                  .ToList();

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
