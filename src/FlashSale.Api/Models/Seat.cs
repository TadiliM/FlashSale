using System.Text.Json.Serialization;

namespace FlashSale.Api;

public class Seat
{
    public required int Id { get; init; }
    public required int EventId { get; init; }
    public required string Label { get; init; }
    public required SeatStatus Status { get; init; }
}

[JsonConverter(typeof(JsonStringEnumConverter<SeatStatus>))]
public enum SeatStatus
{
    Available,
    Held,
    Sold
}
