namespace FlashSale.Api;

public class ReservationService
{
    private readonly TimeProvider clock;
    private readonly List<SeatHold> holds = new();
    private readonly List<Reservation> reservations = new();
    private readonly object holdLock = new();

    public ReservationService(TimeProvider clock)
    {
        this.clock = clock;
    }

    public Seat[] GetSeatsForEvent(IReadOnlyList<Seat> seats)
    {
        lock (holdLock)
        {
            var now = clock.GetUtcNow();
            return seats.Select(s => new Seat
            {
                Id = s.Id,
                EventId = s.EventId,
                Label = s.Label,
                Status = reservations.Any(r => r.SeatId == s.Id) ? SeatStatus.Sold
                    : holds.Any(h => h.SeatId == s.Id && h.ExpiresAt > now) ? SeatStatus.Held
                    : s.Status
            }).ToArray();
        }
    }

    public SeatHold? TryHold(Seat seat, Guid userId)
    {
        lock (holdLock)
        {
            if (seat.Status != SeatStatus.Available || reservations.Any(r => r.SeatId == seat.Id))
                return null;

            var now = clock.GetUtcNow();
            holds.RemoveAll(h => h.ExpiresAt <= now);

            var existingHold = holds.FirstOrDefault(h => h.SeatId == seat.Id);
            if (existingHold is not null)
                return existingHold.UserId == userId ? existingHold : null;

            var hold = new SeatHold
            {
                Id = Guid.NewGuid(),
                SeatId = seat.Id,
                UserId = userId,
                ExpiresAt = now.AddMinutes(5)
            };
            holds.Add(hold);
            return hold;
        }
    }

    public Reservation? TryConfirmHold(Guid holdId, Guid userId)
    {
        lock (holdLock)
        {
            var existingReservation = reservations.FirstOrDefault(r => r.Id == holdId);
            if (existingReservation is not null)
                return existingReservation.UserId == userId ? existingReservation : null;

            var now = clock.GetUtcNow();
            holds.RemoveAll(h => h.ExpiresAt <= now);

            var hold = holds.FirstOrDefault(h => h.Id == holdId && h.UserId == userId);
            if (hold is null || reservations.Any(r => r.SeatId == hold.SeatId))
                return null;

            var reservation = new Reservation
            {
                Id = hold.Id,
                SeatId = hold.SeatId,
                UserId = hold.UserId,
                ConfirmedAt = now
            };
            reservations.Add(reservation);
            holds.Remove(hold);
            return reservation;
        }
    }
}
