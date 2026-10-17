namespace FlashSale.Api;

public class Reservation
{
    public required Guid Id { get; init; }
    public required int SeatId { get; init; }
    public required Guid UserId { get; init; }
    public required DateTimeOffset ConfirmedAt { get; init; }
}
