using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapShotsLK.API.Data;
using SnapShotsLK.API.Models;
using System.Security.Claims;

namespace SnapShotsLK.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BookingController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BookingController(AppDbContext context)
        {
            _context = context;
        }

        private User? GetCurrentUser()
        {
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            if (string.IsNullOrEmpty(email)) return null;
            return _context.Users.FirstOrDefault(u => u.Email == email);
        }

        // ── GET /api/booking/slots/{professionalId}
        // Public — returns all booked/blocked slots for a professional (for calendar display)
        [HttpGet("slots/{professionalId}")]
        public async Task<IActionResult> GetSlots(int professionalId)
        {
            var slots = await _context.Bookings
                .Where(b => b.ProfessionalUserId == professionalId
                         && b.Status != "cancelled"
                         && b.Status != "rejected")
                .Select(b => new { b.BookingDate, b.TimeSlot, b.Status, b.Type })
                .ToListAsync();

            return Ok(slots);
        }

        // ── POST /api/booking
        // Authenticated client submits a booking
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateBooking([FromBody] CreateBookingDto dto)
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            // Verify the slot is still free
            var conflict = await _context.Bookings.AnyAsync(b =>
                b.ProfessionalUserId == dto.ProfessionalId &&
                b.BookingDate == dto.BookingDate &&
                b.TimeSlot == dto.TimeSlot &&
                b.Status != "cancelled" &&
                b.Status != "rejected");

            if (conflict)
                return BadRequest(new { message = "This slot is already taken. Please choose another time." });

            var booking = new Booking
            {
                ClientUserId = user.UserId,
                ProfessionalUserId = dto.ProfessionalId,
                PackageName = dto.PackageName,
                PackagePrice = dto.PackagePrice,
                BookingDate = dto.BookingDate,
                TimeSlot = dto.TimeSlot,
                Notes = dto.Notes,
                Status = "pending",
                Type = "booking",
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Booking submitted successfully!", bookingId = booking.BookingId });
        }

        // ── GET /api/booking/my
        // Client — see their own submitted bookings
        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyBookings()
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            var bookings = await _context.Bookings
                .Include(b => b.Professional)
                .Where(b => b.ClientUserId == user.UserId && b.Type == "booking")
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new
                {
                    b.BookingId,
                    b.BookingDate,
                    b.TimeSlot,
                    b.Status,
                    b.PackageName,
                    b.PackagePrice,
                    b.Notes,
                    b.CreatedAt,
                    professional = new
                    {
                        id = b.Professional!.UserId,
                        name = b.Professional.BusinessName ?? b.Professional.Name ?? b.Professional.Email,
                        serviceType = b.Professional.ServiceType,
                    }
                })
                .ToListAsync();

            return Ok(bookings);
        }

        // ── GET /api/booking/incoming
        // Admin — see incoming bookings from clients
        [Authorize]
        [HttpGet("incoming")]
        public async Task<IActionResult> GetIncomingBookings()
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            var bookings = await _context.Bookings
                .Include(b => b.Client)
                .Where(b => b.ProfessionalUserId == user.UserId && b.Type == "booking")
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new
                {
                    b.BookingId,
                    b.BookingDate,
                    b.TimeSlot,
                    b.Status,
                    b.PackageName,
                    b.PackagePrice,
                    b.Notes,
                    b.CreatedAt,
                    client = new
                    {
                        id = b.Client!.UserId,
                        name = b.Client.Name ?? b.Client.Email,
                        email = b.Client.Email,
                        phone = b.Client.Phone,
                    }
                })
                .ToListAsync();

            return Ok(bookings);
        }

        // ── PATCH /api/booking/{id}/status
        // Admin — confirm or cancel a booking
        [Authorize]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();
            if (booking.ProfessionalUserId != user.UserId) return Forbid();

            booking.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Booking {dto.Status}." });
        }

        // ── POST /api/booking/block
        // Admin — blocks their own time slot (e.g. they have another job)
        [Authorize]
        [HttpPost("block")]
        public async Task<IActionResult> BlockSlot([FromBody] BlockSlotDto dto)
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            // Don't allow blocking if a confirmed booking exists
            var conflict = await _context.Bookings.AnyAsync(b =>
                b.ProfessionalUserId == user.UserId &&
                b.BookingDate == dto.BookingDate &&
                b.TimeSlot == dto.TimeSlot &&
                b.Status == "confirmed");

            if (conflict)
                return BadRequest(new { message = "A confirmed booking exists for this slot. Cancel it first." });

            // Toggle: if already blocked, remove the block
            var existing = await _context.Bookings.FirstOrDefaultAsync(b =>
                b.ProfessionalUserId == user.UserId &&
                b.BookingDate == dto.BookingDate &&
                b.TimeSlot == dto.TimeSlot &&
                b.Type == "blocked");

            if (existing != null)
            {
                _context.Bookings.Remove(existing);
                await _context.SaveChangesAsync();
                return Ok(new { message = "Slot unblocked.", unblocked = true });
            }

            var block = new Booking
            {
                ClientUserId = user.UserId,       // self-blocked
                ProfessionalUserId = user.UserId,
                BookingDate = dto.BookingDate,
                TimeSlot = dto.TimeSlot,
                Notes = dto.Reason ?? "Unavailable",
                Status = "blocked",
                Type = "blocked",
                CreatedAt = DateTime.UtcNow
            };

            _context.Bookings.Add(block);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Slot blocked.", blocked = true });
        }

        // ── DELETE /api/booking/{id}
        // Client — cancel their own pending booking
        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var user = GetCurrentUser();
            if (user == null) return Unauthorized();

            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();
            if (booking.ClientUserId != user.UserId) return Forbid();
            if (booking.Status == "confirmed")
                return BadRequest(new { message = "Cannot cancel a confirmed booking. Contact the professional." });

            booking.Status = "cancelled";
            await _context.SaveChangesAsync();
            return Ok(new { message = "Booking cancelled." });
        }
    }

    // ── DTOs ────────────────────────────────────────────────────────
    public class CreateBookingDto
    {
        public int ProfessionalId { get; set; }
        public string BookingDate { get; set; } = string.Empty;
        public string TimeSlot { get; set; } = string.Empty;
        public string? PackageName { get; set; }
        public string? PackagePrice { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }

    public class BlockSlotDto
    {
        public string BookingDate { get; set; } = string.Empty;
        public string TimeSlot { get; set; } = string.Empty;
        public string? Reason { get; set; }
    }
}
