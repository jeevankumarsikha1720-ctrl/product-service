using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Api.Idempotency;
using ProductService.Application.Inventory.Commands.CommitStock;
using ProductService.Application.Inventory.Commands.ReceiveStock;
using ProductService.Application.Inventory.Commands.ReleaseStock;
using ProductService.Application.Inventory.Commands.ReserveStock;
using ProductService.Application.Inventory.Queries.GetInventoryByProductId;
using ProductService.Application.Inventory.Queries.GetMovementHistory;

namespace ProductService.Api.Controllers;

[ApiController]
[Route("api/inventory")]
public sealed class InventoryController(IMediator mediator, IIdempotencyStore idempotency) : ControllerBase
{
    private const string IdempotencyHeader = "Idempotency-Key";
    private static readonly TimeSpan IdempotencyTtl = TimeSpan.FromHours(24);

    [HttpGet("products/{productId:guid}")]
    public async Task<IActionResult> GetByProductId(Guid productId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetInventoryByProductIdQuery(productId), ct);
        return Ok(result);
    }

    [HttpGet("{inventoryItemId:guid}/movements")]
    public async Task<IActionResult> GetMovements(
        Guid inventoryItemId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetMovementHistoryQuery(inventoryItemId, page, pageSize),
            ct);

        return Ok(result);
    }
    [HttpPost("products/{productId:guid}/receive")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Receive(
    Guid productId,
    [FromBody] ReceiveStockRequest request,
    CancellationToken ct)
    {
        var result = await mediator.Send(
            new ReceiveStockCommand(
                productId,
                request.Quantity,
                request.Note),
            ct);

        return Ok(result);
    }
    [HttpPost("products/{productId:guid}/reserve")]
    public async Task<IActionResult> Reserve(
    Guid productId,
    [FromBody] ReserveStockRequest request,
    CancellationToken ct)
    {
        var result = await mediator.Send(
            new ReserveStockCommand(
                productId,
                request.Quantity,
                request.ReferenceId,
                request.Note),
            ct);

        return Ok(result);
    }
    [HttpPost("products/{productId:guid}/release")]
    public async Task<IActionResult> Release(
    Guid productId,
    [FromBody] ReleaseStockRequest request,
    CancellationToken ct)
    {
        var result = await mediator.Send(
            new ReleaseStockCommand(
                productId,
                request.Quantity,
                request.ReferenceId,
                request.Note),
            ct);

        return Ok(result);
    }
    /// <summary>
    /// Commit a reservation: Reserved → Sold. Idempotent via the Idempotency-Key header.
    /// Send the SAME key on retry; the server replays the cached response instead of
    /// double-committing stock. Recommended: one fresh UUID per checkout attempt.
    /// </summary>
    [HttpPost("products/{productId:guid}/commit")]
    public async Task<IActionResult> Commit(
        Guid productId,
        [FromBody] CommitStockRequest request,
        CancellationToken ct)
    {
        var command = new CommitStockCommand(
            productId,
            request.Quantity,
            request.OrderId,
            request.Note);

        if (Request.Headers.TryGetValue(IdempotencyHeader, out var keyHeader) &&
            !string.IsNullOrWhiteSpace(keyHeader))
        {
            var key = $"commit:{keyHeader}:{productId}:{request.OrderId}:{request.Quantity}";
            var cached = await idempotency.TryGetAsync(key, ct);
            if (cached is not null)
            {
                return Content(cached, "application/json");
            }

            var result = await mediator.Send(command, ct);
            var body = JsonSerializer.Serialize(result);
            await idempotency.SetAsync(key, body, IdempotencyTtl, ct);
            return Content(body, "application/json");
        }

        return Ok(await mediator.Send(command, ct));
    }

    public sealed record CommitStockRequest(int Quantity, Guid OrderId, string? Note);
    public sealed record ReceiveStockRequest(int Quantity, string? Note);
    public sealed record ReserveStockRequest(int Quantity, Guid ReferenceId, string? Note);
    public sealed record ReleaseStockRequest(int Quantity, Guid ReferenceId, string? Note);
}
