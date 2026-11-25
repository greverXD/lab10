using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using ApartmentRental.Models;

namespace ApartmentRental.Data
{
    public class ApartmentRepository
    {
        private readonly string _filePath;
        private readonly List<Apartment> _apartments = new List<Apartment>();
        private readonly List<string> _loadWarnings = new List<string>();
        private XDocument _document = new XDocument();

        public ApartmentRepository(string filePath)
        {
            _filePath = filePath;
        }

        public string FilePath => _filePath;

        public IReadOnlyList<Apartment> Apartments => _apartments;
        public IReadOnlyList<string> LoadWarnings => _loadWarnings;

        public int NextId => _apartments.Count == 0 ? 1 : _apartments.Max(a => a.Id) + 1;


        public void Load()
        {
            if (!File.Exists(_filePath))
                throw new FileNotFoundException("Файл с данными о квартирах не найден.", _filePath);

            _document = XDocument.Load(_filePath, LoadOptions.SetLineInfo);

            XElement root = _document.Root ?? throw new InvalidDataException("XML-файл пуст.");
            if (root.Name != "apartments")
                throw new InvalidDataException("Корневой элемент XML должен называться <apartments>.");

            var records = from region in root.Elements("region")
                          let regionName = (string?)region.Attribute("name")
                          from element in region.Elements("apartment")
                          select new { Region = regionName, Element = element };

            _apartments.Clear();
            _loadWarnings.Clear();

            foreach (var record in records)
            {
                int line = ((IXmlLineInfo)record.Element).LineNumber;
                try
                {
                    Apartment apartment = ParseApartment(record.Element, record.Region);

                    if (_apartments.Any(a => a.Id == apartment.Id))
                    {
                        _loadWarnings.Add($"Строка {line}: объявление №{apartment.Id} повторяется и пропущено.");
                        continue;
                    }

                    Attach(apartment);
                }
                catch (Exception ex) when (ex is ArgumentException || ex is InvalidDataException)
                {
                    _loadWarnings.Add($"Строка {line}: запись пропущена — {ex.Message}");
                }
            }
        }

        private static Apartment ParseApartment(XElement element, string? region)
        {
            if (string.IsNullOrWhiteSpace(region))
                throw new InvalidDataException("у элемента <region> нет атрибута name");

            XElement owner = element.Element("owner")
                             ?? throw new InvalidDataException("нет элемента <owner>");

            return new Apartment(
                id: ReadValue(element.Attribute("id")?.Value, "id", XmlConvert.ToInt32),
                region: region,
                address: ReadText(element, "address"),
                area: ReadValue(element.Element("area")?.Value, "area", XmlConvert.ToDouble),
                monthlyPrice: ReadValue(element.Element("price")?.Value, "price", XmlConvert.ToDecimal),
                rooms: ReadValue(element.Element("rooms")?.Value, "rooms", XmlConvert.ToInt32),
                floor: ReadValue(element.Element("floor")?.Value, "floor", XmlConvert.ToInt32),
                totalFloors: ReadValue(element.Element("totalFloors")?.Value, "totalFloors", XmlConvert.ToInt32),
                hasFurniture: ReadValue(element.Element("furniture")?.Value, "furniture", XmlConvert.ToBoolean),
                petsAllowed: ReadValue(element.Element("pets")?.Value, "pets", XmlConvert.ToBoolean),
                ownerName: ReadText(owner, "name"),
                ownerPhone: ReadText(owner, "phone"),
                description: (string?)element.Element("description") ?? "",
                views: element.Attribute("views") is XAttribute views
                    ? ReadValue(views.Value, "views", XmlConvert.ToInt32)
                    : 0,
                publishedAt: element.Element("published") is XElement published
                    ? ReadValue(published.Value, "published", ParseDate)
                    : DateTime.Today);
        }

        private static string ReadText(XElement parent, string name)
        {
            string? value = (string?)parent.Element(name);
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidDataException($"нет значения <{name}>");
            return value.Trim();
        }

        private static T ReadValue<T>(string? raw, string name, Func<string, T> convert)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new InvalidDataException($"нет значения «{name}»");
            try
            {
                return convert(raw.Trim());
            }
            catch (Exception ex) when (ex is FormatException || ex is OverflowException)
            {
                throw new InvalidDataException($"неверное значение «{name}»: {raw.Trim()}");
            }
        }

        private static DateTime ParseDate(string raw) =>
            XmlConvert.ToDateTime(raw, XmlDateTimeSerializationMode.Unspecified);

        public void Add(Apartment apartment)
        {
            if (_apartments.Any(a => a.Id == apartment.Id))
                throw new InvalidOperationException($"Объявление №{apartment.Id} уже существует.");

            XElement root = _document.Root ?? throw new InvalidOperationException("Данные не загружены.");

            XElement? regionElement = root.Elements("region")
                .FirstOrDefault(r => string.Equals((string?)r.Attribute("name"), apartment.Region,
                                                   StringComparison.CurrentCultureIgnoreCase));

            bool newRegion = regionElement == null;
            if (regionElement == null)
            {
                regionElement = new XElement("region", new XAttribute("name", apartment.Region));
                root.Add(regionElement);
            }
            else
            {
                apartment.Region = (string)regionElement.Attribute("name")!;
            }

            XElement element = ToXElement(apartment);
            regionElement.Add(element);

            try
            {
                Save();
            }
            catch
            {
                if (newRegion) regionElement.Remove();
                else element.Remove();
                throw;
            }

            Attach(apartment);
        }

        public void Remove(Apartment apartment)
        {
            XElement? element = FindElement(apartment.Id);
            XElement? region = element?.Parent;

            element?.Remove();
            if (region != null && !region.Elements("apartment").Any())
                region.Remove();

            Save();

            apartment.Viewed -= OnApartmentViewed;
            _apartments.Remove(apartment);
        }

        private void Attach(Apartment apartment)
        {
            apartment.Viewed += OnApartmentViewed;
            _apartments.Add(apartment);
        }

        private void OnApartmentViewed(Apartment apartment, string message)
        {
            FindElement(apartment.Id)?.SetAttributeValue("views", apartment.Views);
            Save();
        }

        private XElement? FindElement(int id)
        {
            string idText = id.ToString();
            return _document.Descendants("apartment")
                            .FirstOrDefault(e => (string?)e.Attribute("id") == idText);
        }

        private static XElement ToXElement(Apartment a) =>
            new XElement("apartment",
                new XAttribute("id", a.Id),
                new XAttribute("views", a.Views),
                new XElement("address", a.Address),
                new XElement("rooms", a.Rooms),
                new XElement("area", a.Area),
                new XElement("floor", a.Floor),
                new XElement("totalFloors", a.TotalFloors),
                new XElement("price", a.MonthlyPrice),
                new XElement("furniture", a.HasFurniture),
                new XElement("pets", a.PetsAllowed),
                new XElement("owner",
                    new XElement("name", a.OwnerName),
                    new XElement("phone", a.OwnerPhone)),
                new XElement("description", a.Description),
                new XElement("published", a.PublishedAt.ToString("yyyy-MM-dd")));

        private void Save()
        {
            string tempPath = _filePath + ".tmp";
            _document.Save(tempPath);
            File.Move(tempPath, _filePath, true);
        }
    }
}
