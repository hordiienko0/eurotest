# Євро Лоджистікс — система обліку майданчика (пілот). Специфікація

- **Дата:** 2026-10-09
- **Джерело дизайну:** claude.ai/design, проєкт `19f5dd28-…`, файл `Логистика.dc.html`
  (копія: `docs/design/Логистика.dc.html`, стилі дизайн-системи: `docs/design/modernist-styles.css`)
- **Стек:** ASP.NET Core (.NET 10) + EF Core + PostgreSQL + SignalR · Angular 20+ (standalone, signals, zoneless) · Docker Compose

## 0. Обсяг ДЕМО (оновлено 2026-10-09, має пріоритет над рештою документа)

Перший етап — **демо, а не MVP**. Головне — дизайн: Angular-інтерфейс 1:1 з макетом, усі екрани та стани працюють
на мок-даних через простий C#-бекенд. Розділи нижче описують цільову архітектуру; для демо діють такі спрощення:

| Тема | Демо (етап 1) | Етап 2 |
|---|---|---|
| База | **SQLite** файл через EF Core, `dotnet run` без Docker | PostgreSQL, Docker Compose |
| Оновлення між пристроями | Опитування поточного екрана кожні 10 с | SignalR (розділ 6.1) |
| Outbox / IT-Enterprise | Немає; лише інтерфейси `IExternalStockProvider`, `IPhotoStorage`, `ICurrentOperator` | Outbox, адаптер (розділ 7) |
| Конкурентність | Лише унікальні індекси (місце, комірка) → 409 | `If-Match` / `xmin` |
| Пошук VIN | `LIKE` по SQLite | `pg_trgm` |
| Помилки | ProblemDetails з `code`, як у розділі 10 | + Serilog JSON, `/health` |
| Тести | Лише `ZoneLayout` (C# і TS) і переходи стану `Move` | Testcontainers |
| Мок-кнопки | Функції, що потребують інтеграції, — показові кнопки з повідомленням «Буде доступно після інтеграції»; Google Карти — просто посилання | Реальна реалізація |
| Журнал | Список як у макеті (останні 200), без фільтрів | Фільтри й пагінація |
| Фото | Стискає клієнт (1600 px, JPEG 0.8), сервер зберігає файл як є, без мініатюр | SkiaSharp-мініатюри |
| Статус зовнішніх складів | Поля `Zone.IsOnline`, `Zone.LastSyncedAt` замість окремої сутності `ExternalSyncStatus` | Окрема сутність |
| Схема БД | `EnsureCreated` без міграцій; скидання демо пересоздає базу | EF Core міграції |

Angular-фронтенд звертається до бекенду через `/api/v1` (proxy у `ng serve`), API та моделі даних — як у розділах 4–6.

## 1. Мета й контекст

Перетворити клікабельний прототип «Логістика авто · Соломоново» на робочий застосунок для **внутрішнього пілоту**:
оператори майданчика працюють з ПК, планшетів і телефонів, переміщують авто Škoda між зонами, ведуть
ключницю та журнал операцій.

**Критерії успіху пілоту**
1. Усі чотири вкладки дизайну (Зони, Переміщення, Ключі, Журнал) працюють з реальним бекендом і виглядають як у макеті.
2. Два оператори на різних пристроях бачать зміни один одного без перезавантаження і не можуть поставити два авто на одне місце чи видати одну комірку двом авто.
3. Демо можна показати «з коробки»: `docker compose up` → застосунок з мок-даними, як у прототипі; є скидання демо-стану.
4. Архітектура готова до наступних кроків без переписування: вхід через Microsoft (Entra ID) та інтеграція з IT-Enterprise.

**Прийняті рішення**
| Питання | Рішення |
|---|---|
| Користувачі | Внутрішній пілот, ПК / планшети / телефони |
| Автентифікація | Пілот без логіну: оператор обирає себе зі списку. Пізніше — Microsoft Entra ID |
| Зовнішні склади й дилери | Як у дизайні: залишки + статус зв'язку, джерело — симульований провайдер за інтерфейсом |
| Хостинг | Ще не визначено → Docker Compose, PostgreSQL, сховище фото за інтерфейсом |
| Офлайн | Лише онлайн; при втраті зв'язку — банер і блокування дій |
| Архітектура | Модульний моноліт, сервер — єдине джерело правди для бізнес-правил |
| IT-Enterprise | Пізніше. Зараз — Outbox і інтерфейси-провайдери, без адаптера |
| Тести | Мінімум: критичні доменні правила й конкурентність |

**Поза межами пілоту:** реальний вхід і ролі, адаптер IT-Enterprise, офлайн-режим, другий макет `Площадка.dc.html`,
звіти/аналітика, багатомовність (інтерфейс лише українською).

## 2. Що покращуємо порівняно з прототипом

| Прототип | Реалізація |
|---|---|
| Увесь стан у `localStorage` одного браузера | PostgreSQL, спільний стан для всіх пристроїв, SignalR-оновлення |
| Ніщо не заважає двом операторам зайняти одне місце | Унікальні індекси (зона, місце) і (комірка ключа) + оптимістична конкурентність |
| Оператор — довільний текст | Довідник операторів, `OperatorId` у кожній операції, шов під Entra ID |
| Журнал — масив рядків `HH:MM:SS`, VIN обрізаний | Журнал = завершені `Move` (лише додавання), час у UTC, повний VIN, номер із послідовності |
| Фото base64 у стані, лише в останніх 30 записів | Файли у сховищі + мініатюри, без обмеження кількості |
| Скасування мовчки перезаписує початкове місце | Якщо місце зайняте — 409 `OriginTaken` і вибір нового місця |
| Пошук перебирає всі авто в браузері | Серверний пошук з індексом `pg_trgm` |
| Клієнт завантажує всі авто одразу | Карта завантажує одну зону, список зон — агрегати |
| Налаштування — пропси компонента | `AppSettings` на сервері |

## 3. Архітектура

```
 Браузер (Angular SPA)  ──HTTP /api/v1──►  nginx  ──►  Eurocar.Api (ASP.NET Core)
        ▲                                                 │  EF Core
        └──────── WebSocket /hubs/yard (SignalR) ◄────────┤──────────► PostgreSQL
                                                          │──────────► /data/photos (IPhotoStorage)
                                                          └─ BackgroundServices:
                                                               OutboxDispatcher  → SignalR (зараз), IT-Enterprise (пізніше)
                                                               ExternalStockPoller → IExternalStockProvider (Simulated)
```

Один проєкт API з поділом на папки (feature folders). Бізнес-правила — у `Domain/`, ендпоінти тонкі.

## 4. Модель даних

| Сутність | Поля | Обмеження / примітки |
|---|---|---|
| `ZoneGroup` | `Code` (`3.1`…`3.8`), `Name`, `IsEditable` | Seed, лише читання. Редаговані: `3.2`, `3.3` |
| `Zone` | `Id`, `Code`, `GroupCode`, `Name`, `Kind` (`Mapped`/`External`), `Capacity`, `Rows?`, `PerRow?`, `AreaM2?`, `Priority` (`High`/`Normal`/`Low`), `StripLabel`, `IsDeleted`, `ExternalRef?`, версія | `Code` унікальний серед не видалених. `Rows`/`PerRow` лише для `Mapped` |
| `ExternalSyncStatus` | `ZoneId`, `IsOnline`, `LastSyncedAt` | 1:1 з External-зоною |
| `Car` | `Id`, `Vin` (17), `Model`, `Color`, `Status` (`Parked`/`InTransit`/`AtExternal`), `ZoneId?`, `SpotCode?`, `KeySlotId?`, `ExternalRef?`, версія | `Vin` унікальний; унікальний (`ZoneId`,`SpotCode`) де `SpotCode` не null; унікальний `KeySlotId` де не null; GIN `pg_trgm` по `Vin` |
| `KeySlot` | `Id`, `Cabinet` (1–12), `Number` (1–100), `Status` (`Empty`/`InBox`/`Issued`) | 1 200 рядків у seed; унікальний (`Cabinet`,`Number`) |
| `Move` | `Id`, `No?`, `CarId`, `OperatorId`, `FromZoneId`, `FromSpot?`, `ToZoneId?`, `ToSpot?`, `State` (`InTransit`/`Completed`/`Cancelled`), `StartedAt`, `CompletedAt?`, `PhotoId?`, `KeySlotLabel?`, `ExternalRef?`, версія | Частковий унікальний індекс: один `InTransit` на `CarId`. `No` — з послідовності PostgreSQL, присвоюється при завершенні |
| `Photo` | `Id`, `StorageKey`, `ThumbKey`, `TakenAt`, `ContentType`, `SizeBytes` | |
| `Operator` | `Id`, `DisplayName`, `IsActive`, `ExternalId?` | `ExternalId` — майбутній Entra object id |
| `AppSettings` | `RequirePhoto` (true), `CellLabelMode` (`Vin`/`Model`), `ExternalFeedEnabled` (true) | Один рядок |
| `OutboxMessage` | `Id`, `Type`, `PayloadJson`, `OccurredAt`, `ProcessedAt?`, `Attempts`, `Error?` | Пишеться в тій самій транзакції, що й зміна |

Версія рядка — системна колонка PostgreSQL `xmin` як concurrency token EF Core.

## 5. Доменні правила

### 5.1 Розкладка зони (`ZoneLayout`, однакова в C# і TypeScript)
- Літери рядів: `ABCDEFGHIJKLMNOPQRST` (до 20 рядів). Коди місць: `{літера}{номер}`, номер 1…`PerRow`, рахуються по рядах, доки не досягнуто `Capacity`.
- Місткість з площі: `cap = clamp(floor(area / 25), 1, 300)`.
- Розкладка для нової місткості: `per = clamp(ceil(sqrt(cap·0.7)), 5, 15)`, `rows = ceil(cap/per)`, округлити до парного, максимум 20.
- Геометрія (для карти та маршруту): ряд `r`, номер `n`, пара `p = r>>1`, сторона `col = r%2`;
  центр `cx = 1.6 + p·5.6 + col·2 + 1`, `cy = 3 + n + 0.5`; проїзд `ax = col ? (p+1)·5.6 + 0.8 : p·5.6 + 0.8`; дорога `y = 3.1`.
  Розмір полотна: `W = 1.6 + rows/2·5.6`, `H = per + 6`.
- Маршрут від КПП: `(0.8,1) → (0.8,3.1) → (ax,3.1) → (ax,cy) → (cx,cy)`. Між місцями: через спільний проїзд, інакше через дорогу.
  Довжина — манхеттенська × 2.5 м; час пішки 1.3 м/с, на авто 2.8 м/с.
- «Найближчі вільні»: 6 вільних місць з найменшою манхеттенською відстанню від початкового місця (або від КПП).

### 5.2 Життєвий цикл переміщення
`Вибір` (лише на клієнті) → **Start** → `InTransit` → (**SetTarget**, **Photo**)* → **Complete** → `Completed`; з `InTransit` можливий **Cancel** → `Cancelled`.

- **Start:** авто в статусі `Parked` (або `AtExternal` для зовнішньої зони). Створюється `Move(InTransit)`, авто → `InTransit`, `ZoneId/SpotCode` → null (місце звільняється), комірка ключа → `Issued`.
- **SetTarget:** ціль — вільне місце в `Mapped`-зоні, не рівне початковому; або `External`-зона онлайн (місце не вказується). Місце лише резервується в `Move`, авто не ставиться.
- **Photo:** до 10 МБ, `image/*`. Сервер зберігає оригінал, зменшений до 1600 px, і мініатюру 320 px (SkiaSharp). Повторне фото замінює попереднє.
- **Complete** (одна транзакція): потрібна ціль; якщо `RequirePhoto` — потрібне фото. Перевірка, що місце досі вільне.
  - Ціль `Mapped`: авто → `Parked` на місці; ключ зберігає свою комірку (→ `InBox`), якщо комірки немає — перша вільна за порядком шафа 1…12, комірка 1…100.
  - Ціль `External`: авто → `AtExternal`; комірка звільняється (`Empty`), у журналі ключ «передано з авто».
  - `Move.No` = `nextval`, `CompletedAt` = зараз, подія `MoveCompleted` в Outbox.
- **Cancel:** якщо початкове місце вільне — авто повертається туди, ключ → `InBox`. Інакше 409 `OriginTaken`; клієнт повторює з `fallbackSpot` у тій самій зоні.
- Активне переміщення належить оператору, що його почав; інші бачать авто як «у дорозі».

### 5.3 Зони
- Створювати / змінювати / видаляти можна лише зони груп `3.2` (код `T{n}`) і `3.3` (`X{n}`), `n` — найменший вільний.
- Зміна місткості не може бути меншою за кількість авто в зоні (`CapacityBelowOccupancy`). Авто на місцях, що зникли, переставляються на перші вільні місця нової розкладки в тій самій транзакції.
- Видалення — лише порожньої зони (м'яке, `IsDeleted`).

### 5.4 Ключниця
12 шаф × 100 комірок. Лічильники: на місці (`InBox`), видано (`Issued`), порожніх (`Empty`).

## 6. API (`/api/v1`, Minimal APIs, OpenAPI)

| Метод | Шлях | Опис |
|---|---|---|
| GET | `/zones` | Групи з зонами: заповненість, вільні, пріоритет, площа, статус синхронізації; підсумки (власні, зовнішні, онлайн, кількість операцій) |
| GET | `/zones/{code}` | Зона + місця (`code`, `car{id,vin,model,color}?`, `reservedByMoveId?`) або для `External` — список авто |
| POST / PUT / DELETE | `/zones`, `/zones/{code}` | Створення / зміна / видалення (правила 5.3) |
| GET | `/cars/search?vin=` | ≥ 3 символи, до 8 результатів, з місцем і назвою зони |
| GET | `/cars/{id}` | Деталі авто й ключа |
| POST | `/moves` | `{carId}` → Start |
| GET | `/moves/active` | Активне переміщення поточного оператора |
| PUT | `/moves/{id}/target` | `{zoneCode, spotCode?}` |
| POST | `/moves/{id}/photo` | multipart |
| POST | `/moves/{id}/complete` | |
| POST | `/moves/{id}/cancel` | `{fallbackSpot?}` |
| GET | `/moves?state=Completed&page=&size=&vin=&operatorId=&from=&to=` | Журнал |
| GET | `/keys/summary` | Лічильники + кількість по шафах |
| GET | `/keys/cabinets/{n}` | 100 комірок з авто |
| GET | `/photos/{id}`, `/photos/{id}/thumb` | Кешування `Cache-Control: immutable` |
| GET | `/operators` | Активні оператори |
| GET / PUT | `/settings` | `AppSettings` |
| POST | `/demo/reset` | Лише в режимі `Demo` |

- Поточний оператор — заголовок `X-Operator-Id`, читається через `ICurrentOperator` (пізніше — claims Entra ID).
- Зміни приймають `If-Match` з версією; невідповідність → 409 `Conflict`.
- Помилки — `ProblemDetails` з полем `code`: `SpotOccupied`, `SpotNotInZone`, `OriginTaken`, `PhotoRequired`, `TargetRequired`, `ZoneOffline`, `CapacityBelowOccupancy`, `ZoneNotEmpty`, `ZoneNotEditable`, `CarBusy`, `Conflict`, `Validation`.

### 6.1 Реальний час — SignalR `/hubs/yard`
- Групи: `zones`, `zone:{code}`, `keys:{cabinet}`, `journal`.
- Події (після коміту, через `OutboxDispatcher`): `SpotChanged{zone,spot,car?,reserved}`, `ZoneOccupancyChanged{zone,used,capacity}`, `ZoneChanged{zone}`, `KeySlotChanged{cabinet,number,status,car?}`, `MoveCompleted{no,…}`, `ExternalSyncChanged{zone,isOnline,lastSyncedAt}`.
- Після перепідключення клієнт перезавантажує поточний екран.

## 7. Інтеграції (шви для майбутнього)

- **`IExternalStockProvider`** — `PollAsync()` повертає зміни залишків і статус зв'язку зовнішніх зон. Пілот: `SimulatedExternalStockProvider` (кожні 9 с додає/прибирає авто в випадковій онлайн-зоні, оновлює `LastSyncedAt` ще для ~4 зон; P07 завжди офлайн). Вимикається `ExternalFeed__Enabled=false`.
- **`ICarMasterDataProvider`** — довідник авто. Пілот: демо-seed. Пізніше: адаптер IT-Enterprise (upsert за VIN).
- **Outbox** — усі доменні події пишуться в `OutboxMessage` у транзакції зміни. `OutboxDispatcher` (BackgroundService) обробляє записи по черзі з повторами; зараз єдиний споживач — SignalR. Адаптер IT-Enterprise буде другим споживачем (надсилання команд «переміщено», «передано на склад», «ключ видано»).
- **`ExternalRef`** у `Zone`, `Car`, `Move` — для зіставлення з IT-Enterprise.
- **`IPhotoStorage`** — `LocalDiskPhotoStorage` (`Photos__Path`). Пізніше — Azure Blob / S3.

## 8. Демо-дані (`Seed__Mode=Demo`)

Детермінований генератор (фіксований seed), відтворює прототип:
- Власні зони: S1, S2, S3 (300 місць, 20×15), T1 (2 500 м², 10×10), T2 (1 500 м², 60), X1 (1 000 м², 40), R1 (120, 8×15), SV (40, 4×10), QH (30, 2×15) з підписами смуг і пріоритетами з макета; заповненість 40–82 %.
- 14 партнерських складів `P01–P14` (міста з макета), 16 дилерів `D01–D16`; P07 офлайн 47 хв.
- ≈1 700 авто: моделі Fabia, Scala, Octavia, Kamiq, Karoq, Kodiaq, Superb, Enyaq, Elroq; кольори білий, чорний, сірий, сріблястий, синій, червоний; VIN `TMB` + 8 символів + 6 цифр.
- Ключі власних зон — у перемішаних комірках 12 шаф.
- 3 завершені операції в журналі (№ 1–3 з макета), оператори: Іваненко А., Петренко С., Коваль О.
- `POST /demo/reset` очищає й заново генерує дані.

## 9. Фронтенд

- Angular 20+, standalone, signals, zoneless; `@microsoft/signalr`; без UI-бібліотек — глобальні стилі з `modernist-styles.css` + перевизначення палітри з макета (`--color-accent:#0e3a2f`, `--color-accent-2:#78faae`, шкали OKLCH), шрифт Archivo.
- Маршрути: `/zones`, `/move/:zone`, `/keys/:cabinet`, `/log`. Активна вкладка — з маршруту.
- `core/`: згенерований API-клієнт (`openapi-typescript` + легка обгортка), `YardHubService`, `OperatorService` (вибір зі списку, `localStorage`, додає `X-Operator-Id`), інтерсептор помилок (код → український текст), `ConnectionService` + банер «Немає з'єднання».
- `shared/`: `zone-layout.ts`, форматери (`hh:mm:ss`, `mm:ss`, «N хв тому», множина «зона/зони/зон»), `photo-lightbox`, `stat-strip`, `tag`.
- `features/zones`: групи, рядки зон, діалог створення/редагування (як у макеті, з тими самими повідомленнями).
- `features/move`: `zone-map-aerial` (абсолютне позиціонування + SVG-маршрут, автопрокрутка до вибраного місця), `zone-map-scheme`, `external-stock-list`, `move-panel` (5 кроків, таймер, чипи зон, ввід місця, «Найближчі вільні», фото), `move-result`, кнопка Google Maps.
- `features/keys`: шафи, сітка комірок (10 колонок, на мобільному 5), панель комірки, «Показати авто на карті».
- `features/log`: список з пагінацією, фільтри VIN / оператор / дата, мініатюри з лайтбоксом.
- `layout/`: шапка (бренд, вкладки, пошук VIN з випадаючими результатами, годинник ≥1100 px), мобільна нижня навігація й панель дій (<760 px).
- Стан: сервіси-сховища на signals (`ZonesStore`, `ZoneMapStore`, `MoveStore`, `KeysStore`), точкові оновлення з SignalR.
- Фото: `<input type="file" accept="image/*" capture="environment">`, стискання на клієнті до 1600 px JPEG 0.8 перед завантаженням.
- Пошук у вкладці «Ключі» відкриває комірку; в інших — карту зони з вибраним авто (як у макеті).

## 10. Обробка помилок

| Ситуація | Поведінка UI |
|---|---|
| `SpotOccupied` | «Місце D7 вже зайняте», оновлення карти, скидання вибору |
| `Conflict` | «Хтось щойно змінив це», автоматичне перезавантаження даних |
| `OriginTaken` | Пропозиція найближчих вільних місць для повернення |
| `PhotoRequired` / `TargetRequired` | Кнопка «Завершити» неактивна (і перевірка на сервері) |
| `CapacityBelowOccupancy` | Текст помилки в діалозі зони |
| `ZoneOffline` | Кнопка «Відправити авто на цей склад» прихована |
| Немає мережі | Банер, дії заблоковані, автопідключення SignalR, перезавантаження екрана |
| 500 | Загальне повідомлення + `traceId` |

Сервер: Serilog (консоль, JSON), `traceId` у ProblemDetails, `/health` (база, сховище фото).

## 11. Тестування (мінімальне)

- **xUnit, домен:** переходи стану `Move`, `ZoneLayout` (коди, перерахунок місткості, геометрія), розподіл ключів.
- **xUnit + Testcontainers (PostgreSQL) + WebApplicationFactory:** паралельні `SetTarget/Complete` на одне місце (один успіх, один 409); `Complete` атомарний разом з Outbox; `Cancel` з зайнятим початковим місцем.
- **Angular:** тест `zone-layout.ts` на тих самих прикладах, що й C#.

## 12. Структура репозиторію й розгортання

```
backend/Eurocar.Api/{Features,Domain,Infrastructure,Integration,Seed}
backend/Eurocar.Tests/
frontend/                      Angular
docker-compose.yml             postgres:17, api, web (nginx: статика + проксі /api, /hubs з WebSocket)
docs/design/, docs/superpowers/specs/
```

- Міграції EF Core застосовуються при старті API; в режимі `Demo` порожня база заповнюється seed.
- Конфігурація через змінні середовища: `ConnectionStrings__Db`, `Seed__Mode` (`Demo`/`None`), `Photos__Path`, `ExternalFeed__Enabled`.
- Фото — Docker volume `/data/photos`; база — volume `pgdata`.
- Один origin (nginx), CORS не потрібен. Локальна розробка: `ng serve` з proxy-конфігом на API.

## 13. Наступні етапи (не в пілоті)

1. Вхід Microsoft Entra ID: `ICurrentOperator` читає claims, `Operator.ExternalId`, ролі operator / admin (зони, налаштування, скидання демо).
2. Адаптер IT-Enterprise: вхідний `ICarMasterDataProvider`, вихідний споживач Outbox.
3. Реальні джерела залишків зовнішніх складів (`IExternalStockProvider`).
4. Хмарне сховище фото, CI/CD, вибір хостингу.
