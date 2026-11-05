// Domain/ValueObjects/Address.cs

namespace TradeSystem.Domain.ValueObjects;

public sealed class Address
{
    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public string? Street { get; set; }
    public string? Building { get; set; }
    public string? Apartment { get; set; }
    public string? PostalCode { get; set; }
}
