using Asp.Versioning;
using CornLimiter.Application.Commands;
using CornLimiter.Application.Exceptions;
using CornLimiter.Application.UseCases;
using Microsoft.AspNetCore.Mvc;

namespace CornLimiter.Presentation.Controllers;

[ApiVersion(1)]
[ApiController]
[Route("v{version:apiVersion}/[controller]")]
public class SaleController : ControllerBase
{
    private readonly SellOneUseCase _sellOneUseCase;

    public SaleController(SellOneUseCase sellOneUseCase)
    {
        _sellOneUseCase = sellOneUseCase ?? throw new ArgumentNullException(nameof(sellOneUseCase));
    }

    /// <summary>
    /// Sells one unit for the specified farmer.
    /// Receives a GUID named farmerCode in the request body.
    /// </summary>
    [MapToApiVersion(1)]
    [HttpPost("SellOne")]
    public async Task<IActionResult> SellOne(SellOneCommand command)
    {
        try
        {
            var sale = await _sellOneUseCase.ExecuteAsync(command);

            var result = new
            {
                success = true,
                sale
            };

        return Ok(result);
        }
        catch(RateLimitException rex)
        {
            return StatusCode(429, rex.Message);
        }
        catch
        {
            throw;
        }
    }
}