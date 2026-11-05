// Application/Mappers/MappingProfile.cs

using AutoMapper;
using TradeSystem.Application.Dto;
using TradeSystem.Domain.Entities;

namespace TradeSystem.Application.Mappers;

/// <summary>
/// Настройка AutoMapper: копирование совпадающих по имени полей сущности в DTO.
/// Поля, которые требуют данных из других источников (остатки, имена, суммы),
/// намеренно игнорируются и заполняются в DtoMapper.
/// </summary>
public sealed class MappingProfile : Profile
{
    public MappingProfile()
    {
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

        CreateMap<Shelf, ShelfDto>()
            .ForMember(d => d.ProductsCount, o => o.Ignore());

        CreateMap<Product, ProductDto>()
            .ForMember(d => d.UnitLabel, o => o.MapFrom(s => s.Unit.Of()))
            .ForMember(d => d.DefaultShelfName, o => o.Ignore())
            .ForMember(d => d.CurrentStock, o => o.Ignore())
            .ForMember(d => d.SuppliersCount, o => o.Ignore())
            .ForMember(d => d.NeedsReplenish, o => o.Ignore())
            .ForMember(d => d.StorageShelves, o => o.Ignore());

        CreateMap<SupplierProduct, SupplierProductDto>()
            .ForMember(d => d.ProductName, o => o.Ignore())
            .ForMember(d => d.SupplierName, o => o.Ignore());

        CreateMap<PurchaseOrderLine, PurchaseOrderLineDto>()
            .ForMember(d => d.UnitLabel, o => o.MapFrom(s => s.Unit.Of()))
            .ForMember(d => d.RemainingQuantity, o => o.Ignore());

        CreateMap<PurchaseOrder, PurchaseOrderDto>()
            .ForMember(d => d.SupplierName, o => o.Ignore())
            .ForMember(d => d.StatusLabel, o => o.MapFrom(s => s.Status.Of()))
            .ForMember(d => d.PaymentStatus, o => o.Ignore())
            .ForMember(d => d.PaymentStatusLabel, o => o.Ignore())
            .ForMember(d => d.Lines, o => o.Ignore());

        CreateMap<Invoice, InvoiceDto>()
            .ForMember(d => d.OrderNumber, o => o.Ignore())
            .ForMember(d => d.PaymentStatusLabel, o => o.MapFrom(s => s.PaymentStatus.Of()));

        CreateMap<GoodsReceiptLine, GoodsReceiptLineDto>()
            .ForMember(d => d.ProductName, o => o.Ignore())
            .ForMember(d => d.ShelfName, o => o.Ignore());

        CreateMap<GoodsReceipt, GoodsReceiptDto>()
            .ForMember(d => d.SupplierName, o => o.Ignore())
            .ForMember(d => d.OrderNumber, o => o.Ignore())
            .ForMember(d => d.StatusLabel, o => o.MapFrom(s => s.Status.Of()))
            .ForMember(d => d.Lines, o => o.Ignore());

        CreateMap<SaleLine, SaleLineDto>()
            .ForMember(d => d.ProductName, o => o.Ignore())
            .ForMember(d => d.ShelfName, o => o.Ignore());

        CreateMap<Sale, SaleDto>()
            .ForMember(d => d.StatusLabel, o => o.MapFrom(s => s.Status.Of()))
            .ForMember(d => d.TotalAmount, o => o.Ignore())
            .ForMember(d => d.Lines, o => o.Ignore());

        CreateMap<StockBalance, StockBalanceDto>()
            .ForMember(d => d.ProductName, o => o.Ignore())
            .ForMember(d => d.ShelfCode, o => o.Ignore())
            .ForMember(d => d.ShelfName, o => o.Ignore());
    }
}
