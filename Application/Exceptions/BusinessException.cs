// Application/Exceptions/BusinessException.cs

using System;

namespace TradeSystem.Application.Exceptions;

/// <summary>
/// Ошибка, нарушающая бизнес-правило предметной области
/// (недостаточный остаток, товар не у этого поставщика и т.п.).
/// </summary>
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
