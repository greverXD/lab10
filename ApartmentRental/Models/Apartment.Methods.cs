using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ApartmentRental.Extensions;

namespace ApartmentRental.Models
{
    
    public partial class Apartment
    {
        public const int MinRooms = 1;
        public const int MaxRooms = 10;
        public const int MaxFloor = 100;
        public const double MinArea = 8;
        public const double MaxArea = 1000;
        private const int PhoneCompareDigits = 10;

        private static int _instancesCreated;  
        private static int _viewsInSession;  

        private static readonly Regex PhoneDigitsRegex = new Regex(@"^\+?[0-9]{10,13}$");
        private static readonly Regex PhoneCharsRegex = new Regex(@"^\+?[0-9\s\-\(\)]+$");
        private readonly List<DateTime> _sessionViews = new List<DateTime>();

        public event ApartmentEventHandler? Viewed;

        public Apartment(int id, string region, string address, double area, decimal monthlyPrice,
                         int rooms, int floor, int totalFloors, bool hasFurniture, bool petsAllowed,
                         string ownerName, string ownerPhone, string description = "",
                         int views = 0, DateTime? publishedAt = null)
            : base(id, region, address, area, monthlyPrice)
        {
            if (area < MinArea || area > MaxArea)
                throw new ArgumentException($"Площадь квартиры должна быть от {MinArea} до {MaxArea} м².");

            Rooms = rooms;
            TotalFloors = totalFloors;
            Floor = floor;
            if (floor > totalFloors)
                throw new ArgumentException("Этаж не может быть больше этажности дома.");

            HasFurniture = hasFurniture;
            PetsAllowed = petsAllowed;
            OwnerName = ownerName;
            OwnerPhone = ownerPhone;
            Description = description;
            Views = views;
            PublishedAt = publishedAt ?? DateTime.Today;

            _instancesCreated++;
        }

        public static int InstancesCreated => _instancesCreated;

        public static int ViewsInSession => _viewsInSession;

        public static bool IsValidPhone(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            string trimmed = phone.Trim();
            return PhoneCharsRegex.IsMatch(trimmed) && PhoneDigitsRegex.IsMatch(NormalizePhone(trimmed));
        }

        public static string NormalizePhone(string phone)
        {
            string trimmed = phone.Trim();
            string digits = new string(trimmed.Where(c => c >= '0' && c <= '9').ToArray());
            return trimmed.StartsWith("+") ? "+" + digits : digits;
        }

        public static bool IsSamePhone(string first, string second)
        {
            string a = NormalizePhone(first).TrimStart('+');
            string b = NormalizePhone(second).TrimStart('+');
            if (a.Length < PhoneCompareDigits || b.Length < PhoneCompareDigits)
                return false;
            return a[^PhoneCompareDigits..] == b[^PhoneCompareDigits..];
        }

        public IReadOnlyList<DateTime> SessionViews => _sessionViews;

        public bool IsMiddleFloor => Floor > 1 && Floor < TotalFloors;

        public void RegisterView()
        {
            Views++;
            _sessionViews.Add(DateTime.Now);
            _viewsInSession++;
            Viewed?.Invoke(this, $"Квартира №{Id} просмотрена. Всего просмотров: {Views}");
        }

       
        public bool Matches(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return true;

            string word = keyword.Trim();
            return new[] { Region, Address, Description, OwnerName }
                .Any(field => field.Contains(word, StringComparison.CurrentCultureIgnoreCase));
        }

        public override string GetShortInfo() =>
            $"{base.GetShortInfo()}, {Rooms}-комн., эт. {Floor}/{TotalFloors}, просмотров: {Views}";

        public override string GetFullInfo()
        {
            var sb = new StringBuilder(base.GetFullInfo());
            AppendField(sb, "Комнат", Rooms.ToString());
            AppendField(sb, "Этаж", $"{Floor} из {TotalFloors}");
            AppendField(sb, "Мебель", HasFurniture.ToYesNo());
            AppendField(sb, "Можно с животными", PetsAllowed.ToYesNo());
            AppendField(sb, "Собственник", OwnerName);
            AppendField(sb, "Телефон", OwnerPhone);
            AppendField(sb, "Опубликовано", PublishedAt.ToString("dd.MM.yyyy"));
            AppendField(sb, "Просмотров", Views.ToString());
            if (Description.Length > 0)
                AppendField(sb, "Описание", Description);
            return sb.ToString();
        }
    }
}
