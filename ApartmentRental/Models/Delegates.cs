namespace ApartmentRental.Models
{
    public delegate bool ApartmentFilter(Apartment apartment);

    public delegate void ApartmentEventHandler(Apartment apartment, string message);
}
