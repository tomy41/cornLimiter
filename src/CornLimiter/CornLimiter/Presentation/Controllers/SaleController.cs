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
public class SaleController(ISellOneUseCase sellOneUseCase,
    IListSalesByFarmerUseCase listSellingsByFarmerUseCase,
    SellOneCommandValidator sellOneCommandValidator,
    ListSalesByFarmerQueryValidator salesByFarmerQueryValidator) : ControllerBase
{
    private readonly ISellOneUseCase _sellOneUseCase = sellOneUseCase ?? throw new ArgumentNullException(nameof(sellOneUseCase));
    private readonly IListSalesByFarmerUseCase _listSellingsByFarmerUseCase = listSellingsByFarmerUseCase ?? throw new ArgumentNullException(nameof(listSellingsByFarmerUseCase));
    private readonly SellOneCommandValidator _sellOneCommandValidator = sellOneCommandValidator ?? throw new ArgumentNullException(nameof(sellOneCommandValidator));
    private readonly ListSalesByFarmerQueryValidator _salesByFarmerQueryValidator = salesByFarmerQueryValidator ?? throw new ArgumentNullException(nameof(salesByFarmerQueryValidator));

    [MapToApiVersion(1)]
    [HttpPost("SellOne")]
    public async Task<IActionResult> SellOne(SellOneCommand command)
    {
        try
        {
            _sellOneCommandValidator.ValidateAndThrow(command);
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
    [HttpGet("ListByFarmer/{farmerCode}")]
    public async Task<IActionResult> ListSalesByFarmerAsync(Guid farmerCode)
    {
        var query = new ListSalesByFarmerQuery { FarmerCode = farmerCode };
        _salesByFarmerQueryValidator.ValidateAndThrow(query);

        var sales = await _listSellingsByFarmerUseCase.ExecuteAsync(query);

        var result = new
        {
            farmerCode,
            total = sales.Count(),
            latest = sales.Any() ? sales.Max(x => x.SoldOnUtc) : (DateTime?)null,
            sales
        };

        return Ok(result);
    }
}