// Domain/ValueObjects/Address.cs

namespace TradeSystem.Domain.ValueObjects;

// Адрес контрагента: хранится по частям, чтобы формы и отчёты могли собирать его гибко.
public sealed class Address
{
    public string? Country { get; set; } // Страна
    public string? Region { get; set; } // Регион/область
    public string? City { get; set; } // Город
    public string? Street { get; set; } // Улица
    public string? Building { get; set; } // Дом
    public string? Apartment { get; set; } // Квартира/офис
    public string? PostalCode { get; set; } // Почтовый индекс
}
