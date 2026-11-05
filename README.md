# TradeSystem

Фрагмент информационной системы для торгового предприятия: справочники товаров,
поставщиков и мест хранения, заказы поставщикам (вручную и автоматически по дефициту),
счета и оплата, поступления на склад, продажи и справочные отчёты.

Приложение настольное, однопользовательское, работает на Windows. Данные хранятся
в JSON-файлах папки `data`.

## Сборка

```
dotnet build TradeSystem.App.csproj
```

## Запуск

```
dotnet run --project TradeSystem.App.csproj
```

или готовый файл после сборки:

```
bin\Debug\net8.0-windows\TradeSystem.exe
```

## Тесты

Сборка тестов

```
TradeSystem.UnitTests\bin\Debug\net8.0-windows\TradeSystem.UnitTests.exe --output Detailed
```
## Структура

- `Domain` — сущности и перечисления предметной области
- `Application` — сервисы, модели передачи данных, проверки и маппинг
- `Infrastructure` — хранение данных в JSON-файлах
- `ViewModels`, `Views` — представление по шаблону MVVM
- `data` — рабочие JSON-файлы с данными
- `TradeSystem.UnitTests` — юнит-тесты