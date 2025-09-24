using ShelfMarket.Domain.Entities;

namespace ShelfMarket.Domain.Tests;

[TestClass]
public class UseCase002_ShelfTenant_UnitTest
{
    private static (string first, string last, string addr, string zip, string city, string email, string phone) Valid()
        => ("Mikkel", "Jensen", "Kokhaven", "4736", "Karrebæksminde", "test@test.dk", "12345678");

    [TestMethod]
    public void UpdateContact_Trims_Values()
    {
        var v = Valid();
        var t = new ShelfTenant(v.first, v.last, v.addr, v.zip, v.city, v.email, v.phone);

        t.UpdateContact("  Mikkel  ", "  Jensen ", "  test@test.dk ", "  12345678 ");

        Assert.AreEqual("Mikkel", t.FirstName);
        Assert.AreEqual("Jensen", t.LastName);
        Assert.AreEqual("test@test.dk", t.Email);
        Assert.AreEqual("12345678", t.PhoneNumber);
    }

    [TestMethod]
    public void UpdateAddress_Trims_Values()
    {
        var v = Valid();
        var t = new ShelfTenant(v.first, v.last, v.addr, v.zip, v.city, v.email, v.phone);

        t.UpdateAddress("  Kokhaven  ", " 4736 ", "  Karrebæksminde ");

        Assert.AreEqual("Kokhaven", t.Address);
        Assert.AreEqual("4736", t.PostalCode);
        Assert.AreEqual("Karrebæksminde", t.City);
    }
}
