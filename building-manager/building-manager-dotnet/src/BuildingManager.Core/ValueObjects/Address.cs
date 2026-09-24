namespace BuildingManager.Core.ValueObjects;

public record Address
{
    public string Street { get; init; } = string.Empty;
    public string StreetNumber { get; init; } = string.Empty;
    public string? ApartmentNumber { get; init; }
    public string City { get; init; } = string.Empty;
    public string? PostalCode { get; init; }
    public string Country { get; init; } = "Serbia";

    public Address() { }

    public Address(string street, string streetNumber, string city, string? postalCode = null, string? apartmentNumber = null)
    {
        Street = street ?? throw new ArgumentNullException(nameof(street));
        StreetNumber = streetNumber ?? throw new ArgumentNullException(nameof(streetNumber));
        City = city ?? throw new ArgumentNullException(nameof(city));
        PostalCode = postalCode;
        ApartmentNumber = apartmentNumber;
    }

    public string FullAddress
    {
        get
        {
            var cityPart = string.IsNullOrEmpty(PostalCode) ? City : $"{PostalCode} {City}";
            return string.IsNullOrEmpty(ApartmentNumber)
                ? $"{Street} {StreetNumber}, {cityPart}, {Country}"
                : $"{Street} {StreetNumber}/{ApartmentNumber}, {cityPart}, {Country}";
        }
    }

    public string ShortAddress => $"{Street} {StreetNumber}, {City}";
}
