namespace FlashSale.Api;

public class SeatHold
{
    public required Guid Id { get; init; }
    public required int SeatId { get; init; }
    public required Guid UserId { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}
