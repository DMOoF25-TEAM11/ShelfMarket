namespace ShelfMarket.Domain.Entities;

public class ShelfTenant
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Address { get; set; }
    public string PostalCode { get; set; }
    public string City { get; set; }
    public string Email { get; set; }
    public string PhoneNumber { get; set; }
    public string Status { get; set; }

    private ShelfTenant() { }

    public ShelfTenant(string firstName, string lastName, string address, string postalCode, string city, string email, string phoneNumber)
    {
        Id = Guid.NewGuid();
        UpdateContact(firstName, lastName, email, phoneNumber);
        UpdateAddress(address, postalCode, city);
    }

    public ShelfTenant(Guid tenantId, string firstName, string lastName, string address, string postalCode, string city, string email, string phoneNumber)
    {
        Id = tenantId == Guid.Empty ? Guid.NewGuid() : tenantId;
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
