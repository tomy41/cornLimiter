using AutoMapper;
using CornLimiter.Application.Queries;
using CornLimiter.Application.UseCases;
using CornLimiter.Domain;
using CornLimiter.Domain.Models;
using CornLimiter.Domain.ValueObjects;
using Moq;


namespace CornLmiterTests.Application.UseCases;

/// <summary>
/// Unit tests for the <see cref="ListSellingsByFarmerUseCase"/> class.
/// </summary>
public class ListSellingsByFarmerUseCaseTests
{
    /// <summary>
    /// Tests that ExecuteAsync throws ArgumentNullException when the query parameter is null.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NullQuery_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<ISaleRepository>();
        var mockMapper = new Mock<IMapper>();
        var useCase = new ListSellingsByFarmerUseCase(mockRepository.Object, mockMapper.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            useCase.ExecuteAsync(null!, CancellationToken.None));
    }

    /// <summary>
    /// Tests that ExecuteAsync returns an empty collection when the repository returns an empty list.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_EmptyRepositoryResult_ReturnsEmptyCollection()
    {
        // Arrange
        var mockRepository = new Mock<ISaleRepository>();
        var mockMapper = new Mock<IMapper>();
        var farmerCode = Guid.NewGuid();
        var query = new SalesByFarmerQuery { FarmerCode = farmerCode };
        var emptySalesList = new List<Sale>();

        mockRepository
            .Setup(r => r.ListByFarmerCodeAsync(farmerCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptySalesList);

        var useCase = new ListSellingsByFarmerUseCase(mockRepository.Object, mockMapper.Object);

        // Act
        var result = await useCase.ExecuteAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        mockRepository.Verify(r => r.ListByFarmerCodeAsync(farmerCode, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync correctly maps and returns a single sale from the repository.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_SingleSaleResult_ReturnsCorrectlyMappedDto()
    {
        // Arrange
        var mockRepository = new Mock<ISaleRepository>();
        var mockMapper = new Mock<IMapper>();
        var farmerCode = Guid.NewGuid();
        var query = new SalesByFarmerQuery { FarmerCode = farmerCode };

        var sale = new Sale();
        var saleDto = new SaleDto { Id = 1, SoldOnUtc = DateTime.UtcNow };
        var salesList = new List<Sale> { sale };

        mockRepository
            .Setup(r => r.ListByFarmerCodeAsync(farmerCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(salesList);

        mockMapper
            .Setup(m => m.Map<SaleDto>(sale))
            .Returns(saleDto);

        var useCase = new ListSellingsByFarmerUseCase(mockRepository.Object, mockMapper.Object);

        // Act
        var result = await useCase.ExecuteAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Equal(saleDto, resultList[0]);
        mockMapper.Verify(m => m.Map<SaleDto>(sale), Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync correctly maps and returns multiple sales from the repository.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_MultipleSalesResult_ReturnsAllMappedDtos()
    {
        // Arrange
        var mockRepository = new Mock<ISaleRepository>();
        var mockMapper = new Mock<IMapper>();
        var farmerCode = Guid.NewGuid();
        var query = new SalesByFarmerQuery { FarmerCode = farmerCode };

        var sale1 = new Sale();
        var sale2 = new Sale();
        var sale3 = new Sale();
        var saleDto1 = new SaleDto { Id = 1, SoldOnUtc = DateTime.UtcNow };
        var saleDto2 = new SaleDto { Id = 2, SoldOnUtc = DateTime.UtcNow.AddDays(-1) };
        var saleDto3 = new SaleDto { Id = 3, SoldOnUtc = DateTime.UtcNow.AddDays(-2) };
        var salesList = new List<Sale> { sale1, sale2, sale3 };

        mockRepository
            .Setup(r => r.ListByFarmerCodeAsync(farmerCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(salesList);

        mockMapper.Setup(m => m.Map<SaleDto>(sale1)).Returns(saleDto1);
        mockMapper.Setup(m => m.Map<SaleDto>(sale2)).Returns(saleDto2);
        mockMapper.Setup(m => m.Map<SaleDto>(sale3)).Returns(saleDto3);

        var useCase = new ListSellingsByFarmerUseCase(mockRepository.Object, mockMapper.Object);

        // Act
        var result = await useCase.ExecuteAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(3, resultList.Count);
        Assert.Contains(saleDto1, resultList);
        Assert.Contains(saleDto2, resultList);
        Assert.Contains(saleDto3, resultList);
        mockMapper.Verify(m => m.Map<SaleDto>(It.IsAny<Sale>()), Times.Exactly(3));
    }

    /// <summary>
    /// Tests that ExecuteAsync passes the cancellation token to the repository method.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ValidQuery_PassesCancellationTokenToRepository()
    {
        // Arrange
        var mockRepository = new Mock<ISaleRepository>();
        var mockMapper = new Mock<IMapper>();
        var farmerCode = Guid.NewGuid();
        var query = new SalesByFarmerQuery { FarmerCode = farmerCode };
        var cancellationToken = new CancellationToken(false);
        var salesList = new List<Sale>();

        mockRepository
            .Setup(r => r.ListByFarmerCodeAsync(farmerCode, cancellationToken))
            .ReturnsAsync(salesList);

        var useCase = new ListSellingsByFarmerUseCase(mockRepository.Object, mockMapper.Object);

        // Act
        await useCase.ExecuteAsync(query, cancellationToken);

        // Assert
        mockRepository.Verify(r => r.ListByFarmerCodeAsync(farmerCode, cancellationToken), Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync calls the repository with the correct farmer code from the query.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ValidQuery_CallsRepositoryWithCorrectFarmerCode()
    {
        // Arrange
        var mockRepository = new Mock<ISaleRepository>();
        var mockMapper = new Mock<IMapper>();
        var farmerCode = Guid.NewGuid();
        var query = new SalesByFarmerQuery { FarmerCode = farmerCode };
        var salesList = new List<Sale>();

        mockRepository
            .Setup(r => r.ListByFarmerCodeAsync(farmerCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(salesList);

        var useCase = new ListSellingsByFarmerUseCase(mockRepository.Object, mockMapper.Object);

        // Act
        await useCase.ExecuteAsync(query, CancellationToken.None);

        // Assert
        mockRepository.Verify(r => r.ListByFarmerCodeAsync(farmerCode, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync works correctly with an empty Guid farmer code.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_EmptyGuidFarmerCode_CallsRepositoryWithEmptyGuid()
    {
        // Arrange
        var mockRepository = new Mock<ISaleRepository>();
        var mockMapper = new Mock<IMapper>();
        var emptyGuid = Guid.Empty;
        var query = new SalesByFarmerQuery { FarmerCode = emptyGuid };
        var salesList = new List<Sale>();

        mockRepository
            .Setup(r => r.ListByFarmerCodeAsync(emptyGuid, It.IsAny<CancellationToken>()))
            .ReturnsAsync(salesList);

        var useCase = new ListSellingsByFarmerUseCase(mockRepository.Object, mockMapper.Object);

        // Act
        var result = await useCase.ExecuteAsync(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        mockRepository.Verify(r => r.ListByFarmerCodeAsync(emptyGuid, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync uses default cancellation token when not specified.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_DefaultCancellationToken_UsesDefaultToken()
    {
        // Arrange
        var mockRepository = new Mock<ISaleRepository>();
        var mockMapper = new Mock<IMapper>();
        var farmerCode = Guid.NewGuid();
        var query = new SalesByFarmerQuery { FarmerCode = farmerCode };
        var salesList = new List<Sale>();

        mockRepository
            .Setup(r => r.ListByFarmerCodeAsync(farmerCode, default))
            .ReturnsAsync(salesList);

        var useCase = new ListSellingsByFarmerUseCase(mockRepository.Object, mockMapper.Object);

        // Act
        await useCase.ExecuteAsync(query);

        // Assert
        mockRepository.Verify(r => r.ListByFarmerCodeAsync(farmerCode, default), Times.Once);
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when the salesRepository parameter is null.
    /// </summary>
    [Fact]
    public void Constructor_NullSalesRepository_ThrowsArgumentNullException()
    {
        // Arrange
        var mockMapper = new Mock<IMapper>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ListSellingsByFarmerUseCase(null!, mockMapper.Object));
        
        Assert.Equal("salesRepository", exception.ParamName);
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when the mapper parameter is null.
    /// </summary>
    [Fact]
    public void Constructor_NullMapper_ThrowsArgumentNullException()
    {
        // Arrange
        var mockRepository = new Mock<ISaleRepository>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ListSellingsByFarmerUseCase(mockRepository.Object, null!));
    
        Assert.Equal("mapper", exception.ParamName);
    }

    /// <summary>
    /// Tests that the constructor succeeds when all parameters are valid.
    /// </summary>
    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Arrange
        var mockRepository = new Mock<ISaleRepository>();
        var mockMapper = new Mock<IMapper>();

        // Act
        var useCase = new ListSellingsByFarmerUseCase(mockRepository.Object, mockMapper.Object);

        // Assert
        Assert.NotNull(useCase);
    }
}