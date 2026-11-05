// Application/Mappers/MappingProfile.cs

using AutoMapper;
using TradeSystem.Application.Dto;
using TradeSystem.Domain.Entities;

namespace TradeSystem.Application.Mappers;

// Настройка AutoMapper: копирует совпадающие по имени поля сущностей в DTO.
// Вычисляемые поля (остатки, имена, суммы) заполняются отдельно в DtoMapper.
public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Поставщик: адрес раскладывается на плоские поля, остальное — в DtoMapper.
        CreateMap<Supplier, SupplierDto>()
            .ForMember(d => d.Country, o => o.MapFrom(s => s.Address.Country))
            .ForMember(d => d.Region, o => o.MapFrom(s => s.Address.Region))
            .ForMember(d => d.City, o => o.MapFrom(s => s.Address.City))
            .ForMember(d => d.Street, o => o.MapFrom(s => s.Address.Street))
            .ForMember(d => d.Building, o => o.MapFrom(s => s.Address.Building))
            .ForMember(d => d.Apartment, o => o.MapFrom(s => s.Address.Apartment))
            .ForMember(d => d.PostalCode, o => o.MapFrom(s => s.Address.PostalCode))
            .ForMember(d => d.FullAddress, o => o.Ignore())
            .ForMember(d => d.ProductsCount, o => o.Ignore());

        // Полка: число товаров считает сервис.
        CreateMap<Shelf, ShelfDto>()
            .ForMember(d => d.ProductsCount, o => o.Ignore());

        // Товар: подписи и остатки заполняются в DtoMapper.
        CreateMap<Product, ProductDto>()
            .ForMember(d => d.UnitLabel, o => o.MapFrom(s => s.Unit.Of()))
            .ForMember(d => d.DefaultShelfName, o => o.Ignore())
            .ForMember(d => d.CurrentStock, o => o.Ignore())
            .ForMember(d => d.SuppliersCount, o => o.Ignore())
            .ForMember(d => d.NeedsReplenish, o => o.Ignore())
            .ForMember(d => d.StorageShelves, o => o.Ignore());

        // Связь «товар — поставщик»: названия подставляются в DtoMapper.
        CreateMap<SupplierProduct, SupplierProductDto>()
            .ForMember(d => d.ProductName, o => o.Ignore())
            .ForMember(d => d.SupplierName, o => o.Ignore());

        // Позиция заказа: остаток к получению вычисляется в DtoMapper.
        CreateMap<PurchaseOrderLine, PurchaseOrderLineDto>()
            .ForMember(d => d.UnitLabel, o => o.MapFrom(s => s.Unit.Of()))
            .ForMember(d => d.RemainingQuantity, o => o.Ignore());

        // Заказ: поставщик, оплата и позиции заполняются в DtoMapper.
        CreateMap<PurchaseOrder, PurchaseOrderDto>()
            .ForMember(d => d.SupplierName, o => o.Ignore())
            .ForMember(d => d.StatusLabel, o => o.MapFrom(s => s.Status.Of()))
            .ForMember(d => d.PaymentStatus, o => o.Ignore())
            .ForMember(d => d.PaymentStatusLabel, o => o.Ignore())
            .ForMember(d => d.Lines, o => o.Ignore());

        // Счёт: номер заказа и подпись статуса заполняются в DtoMapper.
        CreateMap<Invoice, InvoiceDto>()
            .ForMember(d => d.OrderNumber, o => o.Ignore())
            .ForMember(d => d.PaymentStatusLabel, o => o.MapFrom(s => s.PaymentStatus.Of()));

        // Позиция поступления: названия товара и полки подставляются в DtoMapper.
        CreateMap<GoodsReceiptLine, GoodsReceiptLineDto>()
            .ForMember(d => d.ProductName, o => o.Ignore())
            .ForMember(d => d.ShelfName, o => o.Ignore());

        // Поступление: поставщик, заказ и позиции заполняются в DtoMapper.
        CreateMap<GoodsReceipt, GoodsReceiptDto>()
            .ForMember(d => d.SupplierName, o => o.Ignore())
            .ForMember(d => d.OrderNumber, o => o.Ignore())
            .ForMember(d => d.StatusLabel, o => o.MapFrom(s => s.Status.Of()))
            .ForMember(d => d.Lines, o => o.Ignore());

        // Позиция продажи: названия товара и полки подставляются в DtoMapper.
        CreateMap<SaleLine, SaleLineDto>()
            .ForMember(d => d.ProductName, o => o.Ignore())
            .ForMember(d => d.ShelfName, o => o.Ignore());

        // Продажа: сумма и позиции вычисляются в DtoMapper.
        CreateMap<Sale, SaleDto>()
            .ForMember(d => d.StatusLabel, o => o.MapFrom(s => s.Status.Of()))
            .ForMember(d => d.TotalAmount, o => o.Ignore())
            .ForMember(d => d.Lines, o => o.Ignore());

        // Остаток: названия товара и полки подставляются в DtoMapper.
        CreateMap<StockBalance, StockBalanceDto>()
            .ForMember(d => d.ProductName, o => o.Ignore())
            .ForMember(d => d.ShelfCode, o => o.Ignore())
            .ForMember(d => d.ShelfName, o => o.Ignore());
    }
}
