namespace FlashSale.Api;

public record CreateEventRequest(
    string Name,
    string Description,
    DateTimeOffset StartsAt,
    string TimeZoneId,
    string Location,
    decimal Price,
    string Currency);
