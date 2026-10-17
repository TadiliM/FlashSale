using System.Security.Claims;
using FlashSale.Api;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddDbContext<FlashSaleDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("FlashSale")
        ?? throw new InvalidOperationException("Connection string 'FlashSale' is missing.")));
builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<FlashSaleDbContext>();
builder.Services.AddAuthorization();

builder.Services.AddSingleton<ReservationService>(provider =>
    new ReservationService(provider.GetRequiredService<TimeProvider>()));
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityApi<ApplicationUser>();

app.MapGet("/", () => "Hello World!");

app.MapGet("/events", async (FlashSaleDbContext db) =>
    await db.Events.AsNoTracking().ToArrayAsync());

app.MapPost("/events", async (CreateEventRequest request, ClaimsPrincipal user, FlashSaleDbContext db) =>
{
    if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
        || !await db.Users.AnyAsync(u => u.Id == userId))
        return Results.Unauthorized();

    if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200
        || request.Description is null || request.Description.Length > 2000
        || string.IsNullOrWhiteSpace(request.Location) || request.Location.Length > 200
        || string.IsNullOrWhiteSpace(request.TimeZoneId) || request.TimeZoneId.Length > 100
        || !TimeZoneInfo.TryFindSystemTimeZoneById(request.TimeZoneId, out _)
        || string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Length != 3
        || request.Price < 0 || request.Price > 99_999_999.99m
        || request.Price != decimal.Round(request.Price, 2))
        return Results.BadRequest("Invalid event data.");

    var created = new Event
    {
        Id = 0,
        Name = request.Name.Trim(),
        Description = request.Description.Trim(),
        StartsAt = request.StartsAt.ToUniversalTime(),
        TimeZoneId = request.TimeZoneId,
        Location = request.Location.Trim(),
        Price = request.Price,
        Currency = request.Currency.ToUpperInvariant(),
        CreatedByUserId = userId
    };

    db.Events.Add(created);
    await db.SaveChangesAsync();
    return Results.Created($"/events/{created.Id}", created);
}).RequireAuthorization();

app.MapGet("/events/{id:int}", async (int id, FlashSaleDbContext db) =>
{
    var selectedEvent = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
    return selectedEvent is null ? Results.NotFound() : Results.Ok(selectedEvent);
});

app.MapGet("/events/{id:int}/seats", async (int id, ReservationService reservations, FlashSaleDbContext db) =>
{
    if (!await db.Events.AnyAsync(e => e.Id == id))
        return Results.NotFound();

    var seats = await db.Seats.AsNoTracking().Where(s => s.EventId == id).ToArrayAsync();
    return Results.Ok(reservations.GetSeatsForEvent(seats));
});

app.MapGet("/events/{eventId:int}/seats/{seatId:int}", async (int eventId, int seatId, ReservationService reservations, FlashSaleDbContext db) =>
{
    var seat = await db.Seats.AsNoTracking().FirstOrDefaultAsync(s => s.Id == seatId && s.EventId == eventId);
    return seat is null ? Results.NotFound() : Results.Ok(reservations.GetSeatsForEvent([seat])[0]);
});

app.MapPost("/events/{eventId:int}/seats", async (int eventId, CreateSeatRequest request, ClaimsPrincipal user, FlashSaleDbContext db) =>
{
    if (!Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();

    var selectedEvent = await db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId);
    if (selectedEvent is null)
        return Results.NotFound();
    if (selectedEvent.CreatedByUserId != userId)
        return Results.Forbid();
    if (string.IsNullOrWhiteSpace(request.Label) || request.Label.Length > 20)
        return Results.BadRequest("Seat label must contain 1 to 20 characters.");

    var seat = new Seat
    {
        Id = 0,
        EventId = eventId,
        Label = request.Label.Trim().ToUpperInvariant(),
        Status = SeatStatus.Available
    };

    db.Seats.Add(seat);
    try
    {
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
    {
        return Results.Conflict("A seat with this label already exists for this event.");
    }

    return Results.Created($"/events/{eventId}/seats/{seat.Id}", seat);
}).RequireAuthorization();

app.MapPost("/events/{eventId:int}/seats/{seatId:int}/holds", async (int eventId, int seatId, CreateSeatHoldRequest request, ReservationService reservations, FlashSaleDbContext db) =>
{
    if (request.UserId == Guid.Empty)
        return Results.BadRequest("UserId must not be empty.");

    var seat = await db.Seats.AsNoTracking().FirstOrDefaultAsync(s => s.Id == seatId && s.EventId == eventId);
    if (seat is null)
        return Results.NotFound();

    var hold = reservations.TryHold(seat, request.UserId);
    return hold is null ? Results.Conflict() : Results.Ok(hold);
});

app.MapPost("/holds/{holdId:guid}/confirm", (Guid holdId, ConfirmHoldRequest request, ReservationService reservations) =>
{
    if (request.UserId == Guid.Empty)
        return Results.BadRequest("UserId must not be empty.");

    var reservation = reservations.TryConfirmHold(holdId, request.UserId);
    return reservation is null ? Results.Conflict() : Results.Ok(reservation);
});

app.Run();
