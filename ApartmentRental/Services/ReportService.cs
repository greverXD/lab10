using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ApartmentRental.Models;

namespace ApartmentRental.Services
{
    public class ReportService
    {
        private readonly string _folder;

        public ReportService(string folder)
        {
            _folder = folder;
        }

        public string Folder => _folder;

        public List<string> BuildRegionSummary(IReadOnlyCollection<Apartment> apartments)
        {
            var rows = from a in apartments
                       group a by a.Region into g
                       orderby g.Count() descending, g.Key
                       select new
                       {
                           Region = g.Key,
                           Count = g.Count(),
                           MinPrice = g.Min(a => a.MonthlyPrice),
                           AvgPrice = g.Average(a => a.MonthlyPrice),
                           MaxPrice = g.Max(a => a.MonthlyPrice),
                           Views = g.Sum(a => a.Views)
                       };

            string separator = new string('-', 84);
            var lines = new List<string>
            {
                "СВОДКА ПО РЕГИОНАМ",
                $"Сформирована: {DateTime.Now:dd.MM.yyyy HH:mm}",
                "",
                $"{"Регион",-20}{"Квартир",9}{"Мин. цена",14}{"Сред. цена",14}{"Макс. цена",14}{"Просмотров",13}",
                separator
            };

            lines.AddRange(rows.Select(r =>
                $"{r.Region,-20}{r.Count,9}{r.MinPrice,14:N0}{r.AvgPrice,14:N0}{r.MaxPrice,14:N0}{r.Views,13}"));

            lines.Add(separator);
            lines.Add($"Всего квартир: {apartments.Count}, всего просмотров: {apartments.Sum(a => a.Views)}");
            return lines;
        }

        public List<string> BuildTopViewed(IReadOnlyCollection<Apartment> apartments, int count)
        {
            var lines = new List<string>
            {
                $"ТОП-{count} КВАРТИР ПО ПРОСМОТРАМ",
                $"Сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}",
                ""
            };

            lines.AddRange(apartments
                .OrderByDescending(a => a.Views)
                .ThenBy(a => a.MonthlyPrice)
                .Take(count)
                .Select((a, index) =>
                    $"{index + 1}. [{a.Views} просм.] {a.Region}, {a.Address} — {a.Rooms}-комн., {a.MonthlyPrice:N0} руб./мес."));

            return lines;
        }

        public List<string> BuildCatalog(IReadOnlyCollection<Apartment> apartments)
        {
            var lines = new List<string>
            {
                "КАТАЛОГ КВАРТИР",
                $"Сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}"
            };

            lines.AddRange(apartments
                .OrderBy(a => a.Region)
                .ThenBy(a => a.MonthlyPrice)
                .GroupBy(a => a.Region)
                .SelectMany(g => new[] { "", $"=== {g.Key} (квартир: {g.Count()}) ===" }
                    .Concat(g.Select(a => "  " + a.GetShortInfo()))));

            return lines;
        }

        public string Save(string name, IEnumerable<string> lines)
        {
            Directory.CreateDirectory(_folder);
            string path = Path.Combine(_folder, $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
            File.WriteAllLines(path, lines, Encoding.UTF8);
            return path;
        }
    }
}
