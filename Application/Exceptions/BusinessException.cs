// Application/Exceptions/BusinessException.cs

using System;

namespace TradeSystem.Application.Exceptions;

// Нарушение бизнес-правила: недостаточный остаток, чужой товар, неверный статус и т.п.
public sealed class BusinessException : Exception
{
    public BusinessException(string message) : base(message)
    {
    }

    public BusinessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
