using KazanlakEvents.Application.Services.Interfaces;
using KazanlakEvents.Domain.Entities;
using KazanlakEvents.Domain.Interfaces;
using KazanlakEvents.Web.Controllers.Api;
using KazanlakEvents.Web.ViewModels.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KazanlakEvents.Web.Controllers.Api.v1;

/// <summary>
/// Manage tickets: view, purchase, transfer, and QR check-in.
/// </summary>
[Route("api/v1/tickets")]
[Authorize(AuthenticationSchemes = "ApiJwt")]
public class TicketsApiController(
    ITicketService ticketService,
    ICurrentUserService currentUser) : BaseApiController
{
    /// <summary>Get all tickets belonging to the current user.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(List<TicketApiDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyTickets(CancellationToken ct = default)
    {
        var tickets = await ticketService.GetUserTicketsAsync(currentUser.UserId!.Value, ct);
        return Ok(tickets.Select(MapTicket).ToList());
    }

    /// <summary>Purchase tickets for an event.</summary>
    [HttpPost("purchase")]
    [ProducesResponseType(typeof(PurchaseResultApiDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> PurchaseTickets(
        [FromBody] PurchaseTicketsApiRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var tickets = await ticketService.RegisterForEventAsync(
                request.EventId, currentUser.UserId!.Value,
                request.TicketTypeId, request.Quantity, ct);

            return CreatedAtAction(nameof(GetMyTickets), null, new PurchaseResultApiDto
            {
                OrderNumber = null,
                Tickets     = tickets.Select(MapTicket).ToList()
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Get a specific ticket by its ticket number / QR code.</summary>
    [HttpGet("{ticketNumber}")]
    [ProducesResponseType(typeof(TicketApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicket(string ticketNumber, CancellationToken ct = default)
    {
        var ticket = await ticketService.GetTicketByQrCodeAsync(ticketNumber, ct);
        if (ticket is null || ticket.HolderId != currentUser.UserId)
            return NotFound();

        return Ok(MapTicket(ticket));
    }

    /// <summary>Transfer a ticket to another user by email address.</summary>
    [HttpPost("{id:guid}/transfer")]
    [ProducesResponseType(typeof(TicketApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> TransferTicket(
        Guid id,
        [FromBody] TransferTicketApiRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var tickets = await ticketService.GetUserTicketsAsync(currentUser.UserId!.Value, ct);
            if (tickets.All(t => t.Id != id))
                return Forbid();

            var transferred = await ticketService.TransferTicketToEmailAsync(id, request.RecipientEmail, ct);
            return Ok(MapTicket(transferred));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }

    /// <summary>Check in a ticket by QR code. Requires Organizer, Admin, or SuperAdmin role.</summary>
    [HttpPost("checkin")]
    [Authorize(AuthenticationSchemes = "ApiJwt", Roles = "Organizer,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(TicketApiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckIn(
        [FromBody] CheckinApiRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var ticket = await ticketService.CheckInTicketAsync(
                request.QrCode, currentUser.UserId!.Value, ct);
            return Ok(MapTicket(ticket));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Ticket not found" });
        }
        catch (Exception ex)
        {
            return Problem(ex.Message);
        }
    }


    private static TicketApiDto MapTicket(Ticket t) => new()
    {
        TicketNumber   = t.TicketNumber,
        QrCode         = t.QrCode,
        QrCodeImageUrl = t.QrCodeImageUrl,
        EventTitle     = t.TicketType?.Event?.Title ?? string.Empty,
        TicketTypeName = t.TicketType?.Name ?? string.Empty,
        Price          = t.TicketType?.Price ?? 0m,
        Status         = t.Status.ToString(),
        IssuedAt       = t.IssuedAt,
        CheckedInAt    = t.CheckedInAt
    };
}
