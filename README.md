# Євро Лоджистікс — демо обліку майданчика

Демо: Angular-клієнт 1:1 з макетом `docs/design/Логистика.dc.html` + C# API з мок-даними.

- Специфікація: `docs/superpowers/specs/2026-10-09-logistics-yard-design.md`
- План: `docs/superpowers/plans/2026-10-09-logistics-yard-demo.md`

## Запуск

Потрібно: .NET 10 SDK, Node.js 20+.

```bash
# 1. API → http://localhost:5080 (база data/eurocar.db створюється з демо-даними)
cd backend/Eurocar.Api
dotnet run

# 2. Клієнт → http://localhost:4200
cd frontend
npm install
npm start
```

Телефон або планшет у тій самій мережі: `npm start -- --host 0.0.0.0` і відкрити `http://<IP-комп’ютера>:4200`.

Скинути демо-дані: кнопка «Скинути демо-дані» внизу сторінки «Зони» або `curl -X POST http://localhost:5080/api/v1/demo/reset`.

## Тести

```bash
dotnet test backend
cd frontend && npx ng test --watch=false
```

## Що в демо є моком

- Залишки зовнішніх складів і дилерів генерує симулятор (`backend/Eurocar.Api/Integration/ExternalStock.cs`; вимкнути: `ExternalFeed:Enabled=false`).
- Оператор обирається зі списку, входу немає (далі — Microsoft Entra ID).
- «Google Карти» — просте посилання.
- Інтеграція з IT-Enterprise — наступний етап (інтерфейси вже є: `IExternalStockProvider`, `IPhotoStorage`, `ICurrentOperator`).
