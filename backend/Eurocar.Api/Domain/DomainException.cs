namespace Eurocar.Api.Domain;

public sealed class DomainException(string code, string message, int status = 400) : Exception(message)
{
    public string Code { get; } = code;
    public int Status { get; } = status;
}

/// <summary>All business errors. Messages are shown to operators verbatim.</summary>
public static class Errors
{
    public static DomainException NotFound(string what) => new("NotFound", $"{what} не знайдено.", 404);
    public static DomainException Validation(string message) => new("Validation", message);
    public static DomainException OperatorRequired() => new("OperatorRequired", "Оберіть оператора.");
    public static DomainException CarBusy() => new("CarBusy", "Авто вже в дорозі.", 409);
    public static DomainException ActiveMoveExists() => new("ActiveMoveExists", "У вас уже є незавершене переміщення. Завершіть або скасуйте його.", 409);
    public static DomainException NotYourMove() => new("NotYourMove", "Цю операцію почав інший оператор.", 403);
    public static DomainException SpotRequired() => new("SpotRequired", "Вкажіть номер місця.");
    public static DomainException SpotNotInZone(string zone, string spot) => new("SpotNotInZone", $"У зоні {zone} немає місця {spot}.");
    public static DomainException SpotIsOrigin() => new("SpotIsOrigin", "Це початкове місце авто.");
    public static DomainException SpotOccupied(string spot) => new("SpotOccupied", $"Місце {spot} зайняте.", 409);
    public static DomainException OriginTaken(string spot) => new("OriginTaken", $"Початкове місце {spot} вже зайняте. Оберіть інше місце для повернення.", 409);
    public static DomainException OriginGone(string spot) => new("OriginTaken", $"Початкового місця {spot} більше немає — зону зменшили. Оберіть інше місце для повернення.", 409);
    public static DomainException ZoneFull() => new("ZoneFull", "Склад заповнений, вільних місць немає.", 409);
    public static DomainException KeySlotTaken() => new("KeySlotTaken", "Комірку ключниці щойно зайняли. Спробуйте ще раз.", 409);
    public static DomainException ZoneOffline()=> new("ZoneOffline", "Склад зараз не на зв’язку.", 409);
    public static DomainException TargetRequired() => new("TargetRequired", "Оберіть зону й місце призначення.");
    public static DomainException PhotoRequired() => new("PhotoRequired", "Сфотографуйте авто на новому місці.");
    public static DomainException PhotoInvalid() => new("PhotoInvalid", "Потрібне зображення до 10 МБ.");
    public static DomainException KeyBoxFull() => new("KeyBoxFull", "У ключниці немає вільних комірок.", 409);
    public static DomainException ZoneNotEditable() => new("ZoneNotEditable", "Цю зону не можна змінювати.", 403);
    public static DomainException AreaTooSmall() => new("AreaTooSmall", "Площа має бути не меншою за 25 м².");
    public static DomainException CapacityBelowOccupancy(int used) => new("CapacityBelowOccupancy", $"На майданчику {used} авто, місткість не може бути меншою.");
    public static DomainException ZoneNotEmpty() => new("ZoneNotEmpty", "Видалити можна лише порожню зону.", 409);
    public static DomainException Conflict() => new("Conflict", "Хтось щойно змінив це. Дані оновлено.", 409);
}
