using System;

namespace ApartmentRental.Models
{
    /// <summary>
    /// Квартира для аренды. Часть 1 из 2: защищённые поля и свойства доступа к ним.
    /// Константы, read-only и статические члены, конструктор, методы и событие —
    /// в файле Apartment.Methods.cs.
    /// </summary>
    public partial class Apartment : Housing
    {
        // ---------- Защищённые поля ----------
        protected int _rooms;
        protected int _floor;
        protected int _totalFloors;
        protected bool _hasFurniture;
        protected bool _petsAllowed;
        protected string _ownerName = "";
        protected string _ownerPhone = "";
        protected string _description = "";
        protected int _views;
        protected DateTime _publishedAt;

        // ---------- Свойства ----------
        public int Rooms
        {
            get => _rooms;
            set
            {
                if (value < MinRooms || value > MaxRooms)
                    throw new ArgumentException($"Количество комнат должно быть от {MinRooms} до {MaxRooms}.");
                _rooms = value;
            }
        }

        public int Floor
        {
            get => _floor;
            set
            {
                if (value < 1 || value > MaxFloor)
                    throw new ArgumentException($"Этаж должен быть от 1 до {MaxFloor}.");
                _floor = value;
            }
        }

        public int TotalFloors
        {
            get => _totalFloors;
            set
            {
                if (value < 1 || value > MaxFloor)
                    throw new ArgumentException($"Этажность дома должна быть от 1 до {MaxFloor}.");
                _totalFloors = value;
            }
        }

        public bool HasFurniture
        {
            get => _hasFurniture;
            set => _hasFurniture = value;
        }

        public bool PetsAllowed
        {
            get => _petsAllowed;
            set => _petsAllowed = value;
        }

        public string OwnerName
        {
            get => _ownerName;
            set => _ownerName = RequireText(value, "Имя собственника");
        }

        public string OwnerPhone
        {
            get => _ownerPhone;
            set
            {
                if (!IsValidPhone(value))
                    throw new ArgumentException("Неверный номер телефона. Пример: +79161234567 или 8 916 123-45-67.");
                _ownerPhone = NormalizePhone(value);
            }
        }

        public string Description
        {
            get => _description;
            set => _description = value?.Trim() ?? "";
        }

        /// <summary>Сколько раз квартиру открывали клиенты (хранится в XML).</summary>
        public int Views
        {
            get => _views;
            protected set => _views = Math.Max(0, value);
        }

        public DateTime PublishedAt
        {
            get => _publishedAt;
            set => _publishedAt = value;
        }
    }
}
