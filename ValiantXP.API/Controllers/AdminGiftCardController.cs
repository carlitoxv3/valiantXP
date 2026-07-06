using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ValiantXP.Domain.Entities;
using ValiantXP.Domain.Interfaces;

namespace ValiantXP.API.Controllers;

/// <summary>Admin endpoints for managing GiftCard providers and code pools.</summary>
[ApiController]
[Route("api/admin/giftcard")]
[Produces("application/json")]
[Authorize]
public class AdminGiftCardController : ControllerBase
{
    private readonly IUnitOfWork _uow;

    public AdminGiftCardController(IUnitOfWork uow) => _uow = uow;

    // ─── PROVIDERS ────────────────────────────────────────────────────────────

    /// <summary>List all active GiftCard providers.</summary>
    /// <returns>Collection of active providers (id, name, urls, campaignId).</returns>
    [HttpGet("providers")]
    [ProducesResponseType(typeof(IEnumerable<object>), 200)]
    public async Task<IActionResult> GetProviders(CancellationToken ct)
    {
        var providers = await _uow.GiftCardProviders.GetAllActiveAsync(ct);
        return Ok(providers.Select(p => new
        {
            id             = p.Id,
            name           = p.Name,
            instructiveUrl = p.InstructiveUrl,
            logoUrl        = p.LogoUrl,
            isActive       = p.IsActive,
            campaignId     = p.CampaignId
        }));
    }

    /// <summary>Get provider by ID with available stock count.</summary>
    /// <param name="id">Provider GUID.</param>
    /// <returns>Provider detail including available code count.</returns>
    [HttpGet("providers/{id:guid}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProvider(Guid id, CancellationToken ct)
    {
        var provider = await _uow.GiftCardProviders.GetByIdAsync(id, ct);
        if (provider is null) return NotFound();

        var stock = await _uow.GiftCards.GetAvailableCountAsync(id, ct);
        return Ok(new
        {
            id             = provider.Id,
            name           = provider.Name,
            instructiveUrl = provider.InstructiveUrl,
            logoUrl        = provider.LogoUrl,
            isActive       = provider.IsActive,
            campaignId     = provider.CampaignId,
            availableStock = stock
        });
    }

    /// <summary>Create a new GiftCard provider.</summary>
    /// <param name="request">Provider creation payload.</param>
    /// <returns>Created provider id and name.</returns>
    [HttpPost("providers")]
    [ProducesResponseType(typeof(object), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateProvider(
        [FromBody] CreateGiftCardProviderRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { error = "Provider name is required" });

        var provider = new GiftCardProvider
        {
            Id             = Guid.NewGuid(),
            Name           = request.Name.Trim(),
            InstructiveUrl = request.InstructiveUrl,
            LogoUrl        = request.LogoUrl,
            IsActive       = true,
            CampaignId     = request.CampaignId
        };

        await _uow.GiftCardProviders.AddAsync(provider, ct);
        await _uow.SaveChangesAsync(ct);

        return CreatedAtAction(
            nameof(GetProvider),
            new { id = provider.Id },
            new { id = provider.Id, name = provider.Name });
    }

    /// <summary>Update an existing GiftCard provider.</summary>
    /// <param name="id">Provider GUID.</param>
    /// <param name="request">Fields to update.</param>
    /// <returns>Success message.</returns>
    [HttpPut("providers/{id:guid}")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateProvider(
        Guid id,
        [FromBody] UpdateGiftCardProviderRequest request,
        CancellationToken ct)
    {
        var provider = await _uow.GiftCardProviders.GetByIdAsync(id, ct);
        if (provider is null) return NotFound();

        provider.Name           = request.Name.Trim();
        provider.InstructiveUrl = request.InstructiveUrl;
        provider.LogoUrl        = request.LogoUrl;
        provider.IsActive       = request.IsActive;

        await _uow.GiftCardProviders.UpdateAsync(provider, ct);
        await _uow.SaveChangesAsync(ct);

        return Ok(new { message = "Provider updated successfully" });
    }

    // ─── STOCK ────────────────────────────────────────────────────────────────

    /// <summary>Get available code count for a provider pool.</summary>
    /// <param name="id">Provider GUID.</param>
    /// <returns>Provider name and available stock count.</returns>
    [HttpGet("providers/{id:guid}/stock")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetStock(Guid id, CancellationToken ct)
    {
        var provider = await _uow.GiftCardProviders.GetByIdAsync(id, ct);
        if (provider is null) return NotFound();

        var count = await _uow.GiftCards.GetAvailableCountAsync(id, ct);
        return Ok(new
        {
            providerId     = id,
            providerName   = provider.Name,
            availableStock = count
        });
    }

    // ─── CODE IMPORT ──────────────────────────────────────────────────────────

    /// <summary>Validate codes before import — returns list of duplicates without committing.</summary>
    /// <param name="request">Provider ID and list of code rows to validate.</param>
    /// <returns>Validation summary with duplicate codes and canImport flag.</returns>
    [HttpPost("codes/validate")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ValidateImport(
        [FromBody] ValidateImportRequest request,
        CancellationToken ct)
    {
        if (request.Rows.Count == 0)
            return BadRequest(new { error = "No codes provided" });

        var provider = await _uow.GiftCardProviders.GetByIdAsync(request.ProviderId, ct);
        if (provider is null)
            return NotFound(new { error = $"Provider {request.ProviderId} not found" });

        var duplicates = new List<string>();
        foreach (var row in request.Rows)
            if (await _uow.GiftCards.CodeExistsAsync(request.ProviderId, row.Code.Trim(), ct))
                duplicates.Add(row.Code);

        return Ok(new
        {
            total          = request.Rows.Count,
            duplicates     = duplicates.Count,
            duplicateCodes = duplicates,
            canImport      = duplicates.Count == 0,
            message        = duplicates.Count == 0
                ? $"All {request.Rows.Count} codes are valid."
                : $"{duplicates.Count} duplicate(s) found."
        });
    }

    /// <summary>Bulk import codes into a provider pool. Validates for duplicates before committing.</summary>
    /// <param name="request">Provider ID and list of code rows to import.</param>
    /// <returns>Count of imported codes and provider info.</returns>
    [HttpPost("codes/import")]
    [ProducesResponseType(typeof(object), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Import(
        [FromBody] ImportRequest request,
        CancellationToken ct)
    {
        if (request.Rows.Count == 0)
            return BadRequest(new { error = "No codes provided" });

        var provider = await _uow.GiftCardProviders.GetByIdAsync(request.ProviderId, ct);
        if (provider is null)
            return NotFound(new { error = $"Provider {request.ProviderId} not found" });

        var duplicates = new List<string>();
        foreach (var row in request.Rows)
            if (await _uow.GiftCards.CodeExistsAsync(request.ProviderId, row.Code.Trim(), ct))
                duplicates.Add(row.Code);

        if (duplicates.Count > 0)
            return BadRequest(new
            {
                error      = "Duplicate codes detected. Use /codes/validate first.",
                duplicates
            });

        var cards = request.Rows.Select(row => new GiftCard
        {
            Id          = Guid.NewGuid(),
            ProviderId  = request.ProviderId,
            Code        = row.Code.Trim(),
            RedeemUrl   = row.RedeemUrl,
            Pin         = row.Pin,
            Description = row.Description
        }).ToList();

        await _uow.GiftCards.BulkInsertAsync(cards, ct);
        await _uow.SaveChangesAsync(ct);

        return Ok(new
        {
            imported     = cards.Count,
            providerId   = request.ProviderId,
            providerName = provider.Name
        });
    }

    /// <summary>Download CSV template for bulk code import.</summary>
    /// <returns>CSV file with headers and one example row.</returns>
    [HttpGet("codes/template")]
    [ProducesResponseType(typeof(FileContentResult), 200)]
    public IActionResult GetTemplate()
    {
        var csv = new StringBuilder();
        csv.AppendLine("Code,RedeemUrl,Pin,Description");
        csv.AppendLine("EXAMPLE-001,https://redeem.example.com/?c=EXAMPLE-001,1234,Amazon $50 voucher");

        var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        return File(bytes, "text/csv", "giftcard_import_template.csv");
    }
}

// ─── DTOs ─────────────────────────────────────────────────────────────────────

/// <summary>Payload for creating a new GiftCard provider.</summary>
public record CreateGiftCardProviderRequest(
    string   Name,
    string?  InstructiveUrl,
    string?  LogoUrl,
    Guid?    CampaignId);

/// <summary>Payload for updating an existing GiftCard provider.</summary>
public record UpdateGiftCardProviderRequest(
    string   Name,
    string?  InstructiveUrl,
    string?  LogoUrl,
    bool     IsActive);

/// <summary>A single row in the code import CSV / JSON payload.</summary>
public record ImportGiftCardRow(
    string   Code,
    string?  RedeemUrl,
    string?  Pin,
    string?  Description);

/// <summary>Payload for the pre-import validation endpoint.</summary>
public record ValidateImportRequest(
    Guid                   ProviderId,
    List<ImportGiftCardRow> Rows);

/// <summary>Payload for the bulk code import endpoint.</summary>
public record ImportRequest(
    Guid                   ProviderId,
    List<ImportGiftCardRow> Rows);
