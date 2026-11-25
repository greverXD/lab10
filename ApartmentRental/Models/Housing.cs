using System;
using System.Text;

namespace ApartmentRental.Models
{
    /// <summary>
    /// Базовый класс «Жильё»: общие данные любого объекта, который сдаётся в аренду.
    /// </summary>
    public class Housing
    {
        private int _id;
        private string _region = "";
        private string _address = "";
        private double _area;
        private decimal _monthlyPrice;

        public Housing(int id, string region, string address, double area, decimal monthlyPrice)
        {
            Id = id;
            Region = region;
            Address = address;
            Area = area;
            MonthlyPrice = monthlyPrice;
        }

        public int Id
        {
            get => _id;
            set => _id = value > 0 ? value : throw new ArgumentException("Номер объявления должен быть положительным.");
        }

        public string Region
        {
            get => _region;
            set => _region = RequireText(value, "Регион");
        }

        public string Address
        {
            get => _address;
            set => _address = RequireText(value, "Адрес");
        }

        public double Area
        {
            get => _area;
            set => _area = value > 0 ? value : throw new ArgumentException("Площадь должна быть больше нуля.");
        }

        public decimal MonthlyPrice
        {
            get => _monthlyPrice;
            set => _monthlyPrice = value > 0 ? value : throw new ArgumentException("Цена должна быть больше нуля.");
        }

        /// <summary>Метод 1 (виртуальный): краткая информация в одну строку.</summary>
        public virtual string GetShortInfo() =>
            $"№{Id} {Address}, {Area:0.#} м², {MonthlyPrice:N0} руб./мес.";

        /// <summary>Метод 2 (виртуальный): полная информация.</summary>
        public virtual string GetFullInfo()
        {
            var sb = new StringBuilder();
            AppendField(sb, "Объявление", $"№{Id}");
            AppendField(sb, "Регион", Region);
            AppendField(sb, "Адрес", Address);
            AppendField(sb, "Площадь", $"{Area:0.#} м²");
            AppendField(sb, "Цена", $"{MonthlyPrice:N0} руб. в месяц");
            AppendField(sb, "Цена за м²", $"{GetPricePerSquareMeter():N0} руб.");
            return sb.ToString();
        }

        /// <summary>Метод 3: стоимость аренды за указанное число месяцев.</summary>
        public decimal CalculateRentCost(int months)
        {
            if (months <= 0)
                throw new ArgumentException("Срок аренды должен быть не меньше 1 месяца.");
            return MonthlyPrice * months;
        }

        /// <summary>Метод 4: цена аренды одного квадратного метра.</summary>
        public decimal GetPricePerSquareMeter() => Math.Round(MonthlyPrice / (decimal)Area, 2);

        public override string ToString() => GetShortInfo();

        // ----- вспомогательные методы для наследников -----

        protected static string RequireText(string? value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"Поле «{fieldName}» не может быть пустым.");
            return value.Trim();
        }

        protected static void AppendField(StringBuilder sb, string label, string value)
        {
            sb.AppendLine($"{label + ":",-22}{value}");
        }
    }
}
