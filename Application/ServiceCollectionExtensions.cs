// Application/ServiceCollectionExtensions.cs

using System;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using TradeSystem.Application.Contracts.Services;
using TradeSystem.Application.Mappers;
using TradeSystem.Application.Services;

namespace TradeSystem.Application;

// Регистрация сервисов прикладного слоя в DI.
public static class ServiceCollectionExtensions
{
    // Добавляет AutoMapper, DtoMapper и сервисы приложения.
    public static IServiceCollection AddTradeSystemApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IMapper>(_ => new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance).CreateMapper());
        services.AddSingleton<DtoMapper>();

        services.AddSingleton<IStockService, StockService>();
        services.AddSingleton<ICatalogService, CatalogService>();
        services.AddSingleton<IPurchaseOrderService, PurchaseOrderService>();
        services.AddSingleton<IInvoiceService, InvoiceService>();
        services.AddSingleton<IGoodsReceiptService, GoodsReceiptService>();
        services.AddSingleton<ISaleService, SaleService>();
        services.AddSingleton<IReportService, ReportService>();

        return services;
    }
}
