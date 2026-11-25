namespace ApartmentRental.Models
{
    /// <summary>Делегат-фильтр: возвращает true, если квартира подходит под условие.</summary>
    public delegate bool ApartmentFilter(Apartment apartment);

    /// <summary>Делегат для событий квартиры (например, просмотра).</summary>
    public delegate void ApartmentEventHandler(Apartment apartment, string message);
}
