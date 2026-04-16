using CornLimiter.Application.Commands;
using CornLimiter.Application.Exceptions;
using CornLimiter.Application.Queries;
using CornLimiter.Application.UseCases;
using CornLimiter.Application.Validators;
using CornLimiter.Domain.ValueObjects;
using CornLimiter.Presentation.Controllers;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CornLmiterTests.Presentation.Controllers;

/// <summary>
/// Unit tests for the <see cref="SaleController"/> class.
/// </summary>
public class SaleControllerTests
{
    private readonly Mock<ISellOneUseCase> _mockSellOneUseCase;
    private readonly Mock<IListSellingsByFarmerUseCase> _mockListSellingsByFarmerUseCase;
    private readonly SellOneCommandValidator _sellOneCommandValidator;
    private readonly SalesByFarmerQueryValidator _salesByFarmerQueryValidator;

    public SaleControllerTests()
    {
        // Crear mocks de las interfaces
        _mockSellOneUseCase = new Mock<ISellOneUseCase>();
        _mockListSellingsByFarmerUseCase = new Mock<IListSellingsByFarmerUseCase>();

        // Crear validadores
        _sellOneCommandValidator = new SellOneCommandValidator();
        _salesByFarmerQueryValidator = new SalesByFarmerQueryValidator();
    }

    #region Constructor Tests

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when sellOneUseCase is null.
    /// </summary>
    [Fact]
    public void Constructor_NullSellOneUseCase_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SaleController(
                null!,
                _mockListSellingsByFarmerUseCase.Object,
                _sellOneCommandValidator,
                _salesByFarmerQueryValidator));

        Assert.Equal("sellOneUseCase", exception.ParamName);
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when listSellingsByFarmerUseCase is null.
    /// </summary>
    [Fact]
    public void Constructor_NullListSellingsByFarmerUseCase_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SaleController(
                _mockSellOneUseCase.Object,
                null!,
                _sellOneCommandValidator,
                _salesByFarmerQueryValidator));

        Assert.Equal("listSellingsByFarmerUseCase", exception.ParamName);
    }

    /// <summary>
    /// Tests that the constructor succeeds when all parameters are valid.
    /// </summary>
    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var controller = new SaleController(
            _mockSellOneUseCase.Object,
            _mockListSellingsByFarmerUseCase.Object,
            _sellOneCommandValidator,
            _salesByFarmerQueryValidator);

        // Assert
        Assert.NotNull(controller);
    }

    #endregion

    #region SellOne Tests

    /// <summary>
    /// Tests that SellOne returns Ok result when the command is valid.
    /// </summary>
    [Fact]
    public async Task SellOne_ValidCommand_ReturnsOkResult()
    {
        // Arrange
        var command = new SellOneCommand { FarmerCode = Guid.NewGuid() };
        var expectedSale = new SaleDto { Id = 1, SoldOnUtc = DateTime.UtcNow };

        _mockSellOneUseCase
            .Setup(x => x.ExecuteAsync(It.IsAny<SellOneCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSale);

        var controller = new SaleController(
            _mockSellOneUseCase.Object,
            _mockListSellingsByFarmerUseCase.Object,
            _sellOneCommandValidator,
            _salesByFarmerQueryValidator);

        // Act
        var result = await controller.SellOne(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Tests that SellOne returns the correct sale data in the response.
    /// </summary>
    [Fact]
    public async Task SellOne_ValidCommand_ReturnsCorrectSaleData()
    {
        // Arrange
        var command = new SellOneCommand { FarmerCode = Guid.NewGuid() };
        var expectedSale = new SaleDto
        {
            Id = 123,
            SoldOnUtc = DateTime.UtcNow
        };

        _mockSellOneUseCase
            .Setup(x => x.ExecuteAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSale);

        var controller = new SaleController(
            _mockSellOneUseCase.Object,
            _mockListSellingsByFarmerUseCase.Object,
            _sellOneCommandValidator,
            _salesByFarmerQueryValidator);

        // Act
        var result = await controller.SellOne(command);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        Assert.NotNull(value);

        var saleProperty = value.GetType().GetProperty("sale");
        Assert.NotNull(saleProperty);
        var sale = saleProperty.GetValue(value) as SaleDto;
        Assert.Equal(expectedSale, sale);
    }

    /// <summary>
    /// Tests that SellOne throws ValidationException when FarmerCode is empty.
    /// </summary>
    [Fact]
    public async Task SellOne_EmptyFarmerCode_ThrowsValidationException()
    {
        // Arrange
        var command = new SellOneCommand { FarmerCode = Guid.Empty };

        var controller = new SaleController(
            _mockSellOneUseCase.Object,
            _mockListSellingsByFarmerUseCase.Object,
            _sellOneCommandValidator,
            _salesByFarmerQueryValidator);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => controller.SellOne(command));
    }

    /// <summary>
    /// Tests that SellOne returns status code 429 when RateLimitException is thrown.
    /// </summary>
    [Fact]
    public async Task SellOne_RateLimitExceeded_ReturnsStatusCode429()
    {
        // Arrange
        var command = new SellOneCommand { FarmerCode = Guid.NewGuid() };
        var rateLimitMessage = RateLimitException.DefaultMessage;

        _mockSellOneUseCase
            .Setup(x => x.ExecuteAsync(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RateLimitException());

        var controller = new SaleController(
            _mockSellOneUseCase.Object,
            _mockListSellingsByFarmerUseCase.Object,
            _sellOneCommandValidator,
            _salesByFarmerQueryValidator);

        // Act
        var result = await controller.SellOne(command);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(429, statusCodeResult.StatusCode);
        Assert.Equal(rateLimitMessage, statusCodeResult.Value);
    }

    /// <summary>
    /// Tests that SellOne calls the use case with the correct command.
    /// </summary>
    [Fact]
    public async Task SellOne_ValidCommand_CallsUseCaseWithCorrectCommand()
    {
        // Arrange
        var command = new SellOneCommand { FarmerCode = Guid.NewGuid() };
        var expectedSale = new SaleDto { Id = 1, SoldOnUtc = DateTime.UtcNow };

        _mockSellOneUseCase
            .Setup(x => x.ExecuteAsync(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSale);

        var controller = new SaleController(
            _mockSellOneUseCase.Object,
            _mockListSellingsByFarmerUseCase.Object,
            _sellOneCommandValidator,
            _salesByFarmerQueryValidator);

        // Act
        await controller.SellOne(command);

        // Assert
        _mockSellOneUseCase.Verify(
            x => x.ExecuteAsync(command, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region ListSalesByFarmerAsync Tests

    /// <summary>
    /// Tests that ListSalesByFarmerAsync returns Ok result when the farmer code is valid.
    /// </summary>
    [Fact]
    public async Task ListSalesByFarmerAsync_ValidFarmerCode_ReturnsOkResult()
    {
        // Arrange
        var farmerCode = Guid.NewGuid();
        var sales = new List<SaleDto>
        {
            new SaleDto { Id = 1, SoldOnUtc = DateTime.UtcNow },
            new SaleDto { Id = 2, SoldOnUtc = DateTime.UtcNow.AddDays(-1) }
        };

        _mockListSellingsByFarmerUseCase
            .Setup(x => x.ExecuteAsync(It.IsAny<SalesByFarmerQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sales);

        var controller = new SaleController(
            _mockSellOneUseCase.Object,
            _mockListSellingsByFarmerUseCase.Object,
            _sellOneCommandValidator,
            _salesByFarmerQueryValidator);

        // Act
        var result = await controller.ListSalesByFarmerAsync(farmerCode);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    /// <summary>
    /// Tests that ListSalesByFarmerAsync returns correct response structure.
    /// </summary>
    [Fact]
    public async Task ListSalesByFarmerAsync_ValidFarmerCode_ReturnsCorrectResponseStructure()
    {
        // Arrange
        var farmerCode = Guid.NewGuid();
        var date1 = DateTime.UtcNow;
        var date2 = DateTime.UtcNow.AddDays(-1);
        var sales = new List<SaleDto>
        {
            new SaleDto { Id = 1, SoldOnUtc = date1 },
            new SaleDto { Id = 2, SoldOnUtc = date2 }
        };

        _mockListSellingsByFarmerUseCase
            .Setup(x => x.ExecuteAsync(It.IsAny<SalesByFarmerQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sales);

        var controller = new SaleController(
            _mockSellOneUseCase.Object,
            _mockListSellingsByFarmerUseCase.Object,
            _sellOneCommandValidator,
            _salesByFarmerQueryValidator);

        // Act
        var result = await controller.ListSalesByFarmerAsync(farmerCode);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        Assert.NotNull(value);

        var farmerCodeProperty = value.GetType().GetProperty("farmerCode");
        var totalProperty = value.GetType().GetProperty("total");
        var latestProperty = value.GetType().GetProperty("latest");
        var salesProperty = value.GetType().GetProperty("sales");

        Assert.NotNull(farmerCodeProperty);
        Assert.NotNull(totalProperty);
        Assert.NotNull(latestProperty);
        Assert.NotNull(salesProperty);

        Assert.Equal(farmerCode, farmerCodeProperty.GetValue(value));
        Assert.Equal(2, totalProperty.GetValue(value));
        Assert.Equal(date1, latestProperty.GetValue(value));
    }

    /// <summary>
    /// Tests that ListSalesByFarmerAsync returns empty sales list when no sales exist.
    /// </summary>
    [Fact]
    public async Task ListSalesByFarmerAsync_NoSales_ReturnsEmptyList()
    {
        // Arrange
        var farmerCode = Guid.NewGuid();
        var emptySales = new List<SaleDto>();

        _mockListSellingsByFarmerUseCase
            .Setup(x => x.ExecuteAsync(It.IsAny<SalesByFarmerQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptySales);

        var controller = new SaleController(
            _mockSellOneUseCase.Object,
            _mockListSellingsByFarmerUseCase.Object,
            _sellOneCommandValidator,
            _salesByFarmerQueryValidator);

        // Act
        var result = await controller.ListSalesByFarmerAsync(farmerCode);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var totalProperty = value!.GetType().GetProperty("total");
        Assert.Equal(0, totalProperty!.GetValue(value));
    }

    /// <summary>
    /// Tests that ListSalesByFarmerAsync throws ValidationException when FarmerCode is empty.
    /// </summary>
    [Fact]
    public async Task ListSalesByFarmerAsync_EmptyGuidFarmerCode_ThrowsValidationException()
    {
        // Arrange
        var farmerCode = Guid.Empty;

        var controller = new SaleController(
            _mockSellOneUseCase.Object,
            _mockListSellingsByFarmerUseCase.Object,
            _sellOneCommandValidator,
            _salesByFarmerQueryValidator);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => controller.ListSalesByFarmerAsync(farmerCode));
    }

    /// <summary>
    /// Tests that ListSalesByFarmerAsync calls the use case with the correct query.
    /// </summary>
    [Fact]
    public async Task ListSalesByFarmerAsync_ValidFarmerCode_CallsUseCaseWithCorrectQuery()
    {
        // Arrange
        var farmerCode = Guid.NewGuid();
        var sales = new List<SaleDto>();

        _mockListSellingsByFarmerUseCase
            .Setup(x => x.ExecuteAsync(It.IsAny<SalesByFarmerQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sales);

        var controller = new SaleController(
            _mockSellOneUseCase.Object,
            _mockListSellingsByFarmerUseCase.Object,
            _sellOneCommandValidator,
            _salesByFarmerQueryValidator);

        // Act
        await controller.ListSalesByFarmerAsync(farmerCode);

        // Assert
        _mockListSellingsByFarmerUseCase.Verify(
            x => x.ExecuteAsync(
                It.Is<SalesByFarmerQuery>(q => q.FarmerCode == farmerCode),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    /// <summary>
    /// Tests that ListSalesByFarmerAsync calculates the latest sale date correctly.
    /// </summary>
    [Fact]
    public async Task ListSalesByFarmerAsync_MultipleSales_CalculatesLatestDateCorrectly()
    {
        // Arrange
        var farmerCode = Guid.NewGuid();
        var oldestDate = DateTime.UtcNow.AddDays(-10);
        var middleDate = DateTime.UtcNow.AddDays(-5);
        var latestDate = DateTime.UtcNow;

        var sales = new List<SaleDto>
        {
            new SaleDto { Id = 1, SoldOnUtc = middleDate },
            new SaleDto { Id = 2, SoldOnUtc = oldestDate },
            new SaleDto { Id = 3, SoldOnUtc = latestDate }
        };

        _mockListSellingsByFarmerUseCase
            .Setup(x => x.ExecuteAsync(It.IsAny<SalesByFarmerQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sales);

        var controller = new SaleController(
            _mockSellOneUseCase.Object,
            _mockListSellingsByFarmerUseCase.Object,
            _sellOneCommandValidator,
            _salesByFarmerQueryValidator);

        // Act
        var result = await controller.ListSalesByFarmerAsync(farmerCode);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var latestProperty = value!.GetType().GetProperty("latest");
        Assert.Equal(latestDate, latestProperty!.GetValue(value));
    }

    /// <summary>
    /// Tests that ListSalesByFarmerAsync counts total sales correctly.
    /// </summary>
    [Fact]
    public async Task ListSalesByFarmerAsync_MultipleSales_CountsTotalCorrectly()
    {
        // Arrange
        var farmerCode = Guid.NewGuid();
        var sales = new List<SaleDto>
        {
            new SaleDto { Id = 1, SoldOnUtc = DateTime.UtcNow },
            new SaleDto { Id = 2, SoldOnUtc = DateTime.UtcNow.AddDays(-1) },
            new SaleDto { Id = 3, SoldOnUtc = DateTime.UtcNow.AddDays(-2) },
            new SaleDto { Id = 4, SoldOnUtc = DateTime.UtcNow.AddDays(-3) }
        };

        _mockListSellingsByFarmerUseCase
            .Setup(x => x.ExecuteAsync(It.IsAny<SalesByFarmerQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sales);

        var controller = new SaleController(
            _mockSellOneUseCase.Object,
            _mockListSellingsByFarmerUseCase.Object,
            _sellOneCommandValidator,
            _salesByFarmerQueryValidator);

        // Act
        var result = await controller.ListSalesByFarmerAsync(farmerCode);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var value = okResult.Value;
        var totalProperty = value!.GetType().GetProperty("total");
        Assert.Equal(4, totalProperty!.GetValue(value));
    }

    #endregion
}