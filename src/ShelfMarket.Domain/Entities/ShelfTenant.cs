namespace ShelfMarket.Domain.Entities;

public class ShelfTenant
{
    public Guid? Id { get; private set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";

    private ShelfTenant() { }

    public ShelfTenant(string firstName, string lastName, string address, string postalCode, string city, string email, string phoneNumber)
    {
        UpdateContact(firstName, lastName, email, phoneNumber);
        UpdateAddress(address, postalCode, city);
    }

    public void UpdateContact(string first, string last, string email, string phoneNumber)
    {
        FirstName = string.IsNullOrWhiteSpace(first) ? throw new ArgumentException("Indtast fornavn") : first.Trim();
        LastName = string.IsNullOrWhiteSpace(last) ? throw new ArgumentException("Indtast efternavn") : last.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? throw new ArgumentException("Indtast email") : email.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? throw new ArgumentException("Indtast telefon nummer") : phoneNumber.Trim();
    }

    public void UpdateAddress(string address, string postalCode, string city)
    {
        Address = string.IsNullOrWhiteSpace(address) ? throw new ArgumentException("Indtast adresse") : address.Trim();
        PostalCode = string.IsNullOrWhiteSpace(postalCode) ? throw new ArgumentException("Indtast post Nummer") : postalCode.Trim();
        City = string.IsNullOrWhiteSpace(city) ? throw new ArgumentException("Indtast by navn") : city.Trim();
    }
}
