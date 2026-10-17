namespace FlashSale.Api;

public class Event
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required DateTimeOffset StartsAt { get; init; }
    public required string TimeZoneId { get; init; }
    public required string Location { get; init; }
    public required decimal Price { get; init; }
    public required string Currency { get; init; }
    public required Guid CreatedByUserId { get; init; }
}
