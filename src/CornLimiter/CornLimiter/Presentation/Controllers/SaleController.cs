using Asp.Versioning;
using CornLimiter.Application.Commands;
using CornLimiter.Application.Exceptions;
using CornLimiter.Application.Queries;
using CornLimiter.Application.UseCases;
using CornLimiter.Application.Validators;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CornLimiter.Presentation.Controllers;

//[Authorize]
[ApiVersion(1)]
[ApiController]
[Route("v{version:apiVersion}/[controller]")]
public class SaleController(SellOneUseCase sellOneUseCase, 
    ListSellingsByFarmerUseCase listSellingsByFarmerUseCase, 
    SellOneCommandValidator sellOneCommandValidator, 
    SalesByFarmerQueryValidator salesByFarmerQueryValidator) : ControllerBase
{
    private readonly SellOneUseCase _sellOneUseCase = sellOneUseCase ?? throw new ArgumentNullException(nameof(sellOneUseCase));
    private readonly ListSellingsByFarmerUseCase _listSellingsByFarmerUseCase = listSellingsByFarmerUseCase ?? throw new ArgumentNullException(nameof(listSellingsByFarmerUseCase));

    [MapToApiVersion(1)]
    [HttpPost("SellOne")]
    public async Task<IActionResult> SellOne(SellOneCommand command)
    {
        try
        {
            sellOneCommandValidator.ValidateAndThrow(command);
            var sale = await _sellOneUseCase.ExecuteAsync(command);

            var result = new
            {
                sale
            };

            return Ok(result);
        }
        catch (RateLimitException rex)
        {
            return StatusCode(429, rex.Message);
        }
        catch
        {
            throw;
        }
    }

    [MapToApiVersion(1)]
    [HttpGet("SalesByFarmer/{farmerCode}")]
    public async Task<IActionResult> ListSalesByFarmerAsync(Guid farmerCode)
    {
        var query = new SalesByFarmerQuery { FarmerCode = farmerCode };
        salesByFarmerQueryValidator.ValidateAndThrow(query);

        var sales = await _listSellingsByFarmerUseCase.ExecuteAsync(query);

        var result = new
        {
            farmerCode,
            total = sales.Count(),
            latest = sales.Max(x => x.SoldOnUtc),
            sales
        };

        return Ok(result);
    }
}