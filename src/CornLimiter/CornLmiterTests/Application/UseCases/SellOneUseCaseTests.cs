using AutoMapper;
using CornLimiter.Application.Commands;
using CornLimiter.Application.Exceptions;
using CornLimiter.Application.UseCases;
using CornLimiter.Configuration;
using CornLimiter.Domain;
using CornLimiter.Domain.Models;
using CornLimiter.Domain.ValueObjects;
using CornLimiter.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;

namespace CornLmiterTests.Application.UseCases;


public class SellOneUseCaseTests
{
    private Mock<IMemoryCache> mockMemoryCache;
    private Mock<ISaleRepository> mockRepository;
    private Mock<IUnitOfWork> mockUnitOfWork;
    private Mock<IMapper> mockMapper;
    private Mock<IOptions<CornLimiterOptions>> mockOptions;
    private Mock<IFeatureFlagsService> mockFeatureFlagService;
    public SellOneUseCaseTests()
    {
        mockMemoryCache = new Mock<IMemoryCache>();
        mockRepository = new Mock<ISaleRepository>();
        mockUnitOfWork = new Mock<IUnitOfWork>();
        mockMapper = new Mock<IMapper>();
        mockOptions = new Mock<IOptions<CornLimiterOptions>>();
        mockFeatureFlagService = new Mock<IFeatureFlagsService>();
    }
    /// <summary>
    /// Tests that ExecuteAsync throws ArgumentNullException when command parameter is null.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NullCommand_ThrowsArgumentNullException()
    {
        // Arrange
        mockOptions.Setup(o => o.Value).Returns(new CornLimiterOptions());
        mockFeatureFlagService.Setup(f => f.IsFeatureFlagEnabled(It.IsAny<string>())).Returns(false);

        var useCase = new SellOneUseCase(
            mockMemoryCache.Object,
            mockRepository.Object,
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockOptions.Object,
            mockFeatureFlagService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            useCase.ExecuteAsync(null!, CancellationToken.None));
    }

    /// <summary>
    /// Tests that ExecuteAsync throws RateLimitException when ValidateRateLimitAsync detects a rate limit violation
    /// with cache boost enabled and farmer is in cache.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_CacheBoostEnabled_FarmerInCache_ThrowsRateLimitException()
    {
        // Arrange
        var farmerCode = Guid.NewGuid();
        var command = new SellOneCommand { FarmerCode = farmerCode };

        mockOptions.Setup(o => o.Value).Returns(new CornLimiterOptions { MinutesLimitToAllowNewSale = 5 });
        mockFeatureFlagService.Setup(f => f.IsFeatureFlagEnabled("cacheBoostEnabledFeatureFlag")).Returns(true);

        object? cacheValue = new object();
        mockMemoryCache.Setup(m => m.TryGetValue(farmerCode, out cacheValue)).Returns(true);

        var useCase = new SellOneUseCase(
            mockMemoryCache.Object,
            mockRepository.Object,
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockOptions.Object,
            mockFeatureFlagService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RateLimitException>(() =>
            useCase.ExecuteAsync(command, CancellationToken.None));
    }

    /// <summary>
    /// Tests that ExecuteAsync throws RateLimitException when ValidateRateLimitAsync detects
    /// a recent sale within the rate limit window.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_RecentSaleWithinWindow_ThrowsRateLimitException()
    {
        // Arrange
        var farmerCode = Guid.NewGuid();
        var command = new SellOneCommand { FarmerCode = farmerCode };
        var recentSale = Sale.Create(farmerCode);

        mockOptions.Setup(o => o.Value).Returns(new CornLimiterOptions { MinutesLimitToAllowNewSale = 60 });
        mockFeatureFlagService.Setup(f => f.IsFeatureFlagEnabled("cacheBoostEnabledFeatureFlag")).Returns(false);

        object? cacheValue = null;
        mockMemoryCache.Setup(m => m.TryGetValue(farmerCode, out cacheValue)).Returns(false);
        mockRepository.Setup(r => r.GetLastAsync(farmerCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(recentSale);

        var useCase = new SellOneUseCase(
            mockMemoryCache.Object,
            mockRepository.Object,
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockOptions.Object,
            mockFeatureFlagService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<RateLimitException>(() =>
            useCase.ExecuteAsync(command, CancellationToken.None));
    }

    /// <summary>
    /// Tests that ExecuteAsync successfully creates and caches a sale when cache boost is enabled
    /// and there are no rate limit violations.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_CacheBoostEnabled_ValidCommand_ReturnsSaleDto()
    {
        // Arrange
        var farmerCode = Guid.NewGuid();
        var command = new SellOneCommand { FarmerCode = farmerCode };
        var sale = Sale.Create(farmerCode);
        var saleDto = new SaleDto { Id = 1, SoldOnUtc = DateTime.UtcNow };

        var mockCacheEntry = new Mock<ICacheEntry>();

        mockOptions.Setup(o => o.Value).Returns(new CornLimiterOptions { MinutesLimitToAllowNewSale = 5 });
        mockFeatureFlagService.Setup(f => f.IsFeatureFlagEnabled("cacheBoostEnabledFeatureFlag")).Returns(true);

        object? cacheValue = null;
        mockMemoryCache.Setup(m => m.TryGetValue(farmerCode, out cacheValue)).Returns(false);
        mockRepository.Setup(r => r.GetLastAsync(farmerCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sale?)null);
        mockRepository.Setup(r => r.AddAsync(It.IsAny<Sale>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        mockMemoryCache.Setup(m => m.CreateEntry(farmerCode))
            .Returns(mockCacheEntry.Object);

        mockMapper.Setup(m => m.Map<SaleDto>(It.IsAny<Sale>()))
            .Returns(saleDto);

        var useCase = new SellOneUseCase(
            mockMemoryCache.Object,
            mockRepository.Object,
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockOptions.Object,
            mockFeatureFlagService.Object);

        // Act
        var result = await useCase.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(saleDto.Id, result.Id);
        mockRepository.Verify(r => r.AddAsync(It.IsAny<Sale>(), It.IsAny<CancellationToken>()), Times.Once);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockMapper.Verify(m => m.Map<SaleDto>(It.IsAny<Sale>()), Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync successfully creates a sale when cache boost is disabled
    /// and there are no rate limit violations.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_CacheBoostDisabled_ValidCommand_ReturnsSaleDto()
    {
        // Arrange
        var farmerCode = Guid.NewGuid();
        var command = new SellOneCommand { FarmerCode = farmerCode };
        var saleDto = new SaleDto { Id = 2, SoldOnUtc = DateTime.UtcNow };

        mockOptions.Setup(o => o.Value).Returns(new CornLimiterOptions { MinutesLimitToAllowNewSale = 5 });
        mockFeatureFlagService.Setup(f => f.IsFeatureFlagEnabled("cacheBoostEnabledFeatureFlag")).Returns(false);

        object? cacheValue = null;
        mockMemoryCache.Setup(m => m.TryGetValue(farmerCode, out cacheValue)).Returns(false);
        mockRepository.Setup(r => r.GetLastAsync(farmerCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sale?)null);
        mockRepository.Setup(r => r.AddAsync(It.IsAny<Sale>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        mockMapper.Setup(m => m.Map<SaleDto>(It.IsAny<Sale>()))
            .Returns(saleDto);

        var useCase = new SellOneUseCase(
            mockMemoryCache.Object,
            mockRepository.Object,
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockOptions.Object,
            mockFeatureFlagService.Object);

        // Act
        var result = await useCase.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(saleDto.Id, result.Id);
        mockRepository.Verify(r => r.AddAsync(It.IsAny<Sale>(), It.IsAny<CancellationToken>()), Times.Once);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockMapper.Verify(m => m.Map<SaleDto>(It.IsAny<Sale>()), Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync correctly applies minimum cache expiration of 1 minute
    /// when MinutesLimitToAllowNewSale is set to 0.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_CacheBoostEnabled_ZeroMinutesLimit_AppliesMinimumOneMinute()
    {
        // Arrange
        var farmerCode = Guid.NewGuid();
        var command = new SellOneCommand { FarmerCode = farmerCode };
        var saleDto = new SaleDto { Id = 3, SoldOnUtc = DateTime.UtcNow };
        var mockCacheEntry = new Mock<ICacheEntry>();

        mockOptions.Setup(o => o.Value).Returns(new CornLimiterOptions { MinutesLimitToAllowNewSale = 0 });
        mockFeatureFlagService.Setup(f => f.IsFeatureFlagEnabled("cacheBoostEnabledFeatureFlag")).Returns(true);

        object? cacheValue = null;
        mockMemoryCache.Setup(m => m.TryGetValue(farmerCode, out cacheValue)).Returns(false);
        mockRepository.Setup(r => r.GetLastAsync(farmerCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sale?)null);
        mockRepository.Setup(r => r.AddAsync(It.IsAny<Sale>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        mockMemoryCache.Setup(m => m.CreateEntry(farmerCode))
            .Returns(mockCacheEntry.Object);

        mockMapper.Setup(m => m.Map<SaleDto>(It.IsAny<Sale>()))
            .Returns(saleDto);

        var useCase = new SellOneUseCase(
            mockMemoryCache.Object,
            mockRepository.Object,
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockOptions.Object,
            mockFeatureFlagService.Object);

        // Act
        var result = await useCase.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        mockCacheEntry.VerifySet(e => e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1), Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync correctly applies minimum cache expiration of 1 minute
    /// when MinutesLimitToAllowNewSale is set to a negative value.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_CacheBoostEnabled_NegativeMinutesLimit_AppliesMinimumOneMinute()
    {
        // Arrange
        var farmerCode = Guid.NewGuid();
        var command = new SellOneCommand { FarmerCode = farmerCode };
        var saleDto = new SaleDto { Id = 4, SoldOnUtc = DateTime.UtcNow };
        var mockCacheEntry = new Mock<ICacheEntry>();

        mockOptions.Setup(o => o.Value).Returns(new CornLimiterOptions { MinutesLimitToAllowNewSale = -10 });
        mockFeatureFlagService.Setup(f => f.IsFeatureFlagEnabled("cacheBoostEnabledFeatureFlag")).Returns(true);

        object? cacheValue = null;
        mockMemoryCache.Setup(m => m.TryGetValue(farmerCode, out cacheValue)).Returns(false);
        mockRepository.Setup(r => r.GetLastAsync(farmerCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sale?)null);
        mockRepository.Setup(r => r.AddAsync(It.IsAny<Sale>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        mockMemoryCache.Setup(m => m.CreateEntry(farmerCode))
            .Returns(mockCacheEntry.Object);

        mockMapper.Setup(m => m.Map<SaleDto>(It.IsAny<Sale>()))
            .Returns(saleDto);

        var useCase = new SellOneUseCase(
            mockMemoryCache.Object,
            mockRepository.Object,
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockOptions.Object,
            mockFeatureFlagService.Object);

        // Act
        var result = await useCase.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        mockCacheEntry.VerifySet(e => e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1), Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync correctly applies cache expiration when MinutesLimitToAllowNewSale
    /// is set to a large valid value.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_CacheBoostEnabled_LargeMinutesLimit_AppliesCorrectExpiration()
    {
        // Arrange
        var farmerCode = Guid.NewGuid();
        var command = new SellOneCommand { FarmerCode = farmerCode };
        var saleDto = new SaleDto { Id = 5, SoldOnUtc = DateTime.UtcNow };
        var mockCacheEntry = new Mock<ICacheEntry>();

        mockOptions.Setup(o => o.Value).Returns(new CornLimiterOptions { MinutesLimitToAllowNewSale = 1000 });
        mockFeatureFlagService.Setup(f => f.IsFeatureFlagEnabled("cacheBoostEnabledFeatureFlag")).Returns(true);

        object? cacheValue = null;
        mockMemoryCache.Setup(m => m.TryGetValue(farmerCode, out cacheValue)).Returns(false);
        mockRepository.Setup(r => r.GetLastAsync(farmerCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sale?)null);
        mockRepository.Setup(r => r.AddAsync(It.IsAny<Sale>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        mockMemoryCache.Setup(m => m.CreateEntry(farmerCode))
            .Returns(mockCacheEntry.Object);

        mockMapper.Setup(m => m.Map<SaleDto>(It.IsAny<Sale>()))
            .Returns(saleDto);

        var useCase = new SellOneUseCase(
            mockMemoryCache.Object,
            mockRepository.Object,
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockOptions.Object,
            mockFeatureFlagService.Object);

        // Act
        var result = await useCase.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        mockCacheEntry.VerifySet(e => e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(1000), Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync properly passes the cancellation token through to repository and unit of work operations.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_ValidCommand_PassesCancellationTokenCorrectly()
    {
        // Arrange
        var farmerCode = Guid.NewGuid();
        var command = new SellOneCommand { FarmerCode = farmerCode };
        var saleDto = new SaleDto { Id = 6, SoldOnUtc = DateTime.UtcNow };
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        mockOptions.Setup(o => o.Value).Returns(new CornLimiterOptions { MinutesLimitToAllowNewSale = 5 });
        mockFeatureFlagService.Setup(f => f.IsFeatureFlagEnabled("cacheBoostEnabledFeatureFlag")).Returns(false);

        object? cacheValue = null;
        mockMemoryCache.Setup(m => m.TryGetValue(farmerCode, out cacheValue)).Returns(false);
        mockRepository.Setup(r => r.GetLastAsync(farmerCode, cancellationToken))
            .ReturnsAsync((Sale?)null);
        mockRepository.Setup(r => r.AddAsync(It.IsAny<Sale>(), cancellationToken))
            .Returns(Task.CompletedTask);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(cancellationToken))
            .ReturnsAsync(1);

        mockMapper.Setup(m => m.Map<SaleDto>(It.IsAny<Sale>()))
            .Returns(saleDto);

        var useCase = new SellOneUseCase(
            mockMemoryCache.Object,
            mockRepository.Object,
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockOptions.Object,
            mockFeatureFlagService.Object);

        // Act
        var result = await useCase.ExecuteAsync(command, cancellationToken);

        // Assert
        Assert.NotNull(result);
        mockRepository.Verify(r => r.GetLastAsync(farmerCode, cancellationToken), Times.Once);
        mockRepository.Verify(r => r.AddAsync(It.IsAny<Sale>(), cancellationToken), Times.Once);
        mockUnitOfWork.Verify(u => u.SaveChangesAsync(cancellationToken), Times.Once);
    }

    /// <summary>
    /// Tests that ExecuteAsync successfully executes when command has an empty Guid (Guid.Empty) as FarmerCode.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_EmptyGuidFarmerCode_ExecutesSuccessfully()
    {
        // Arrange
        var command = new SellOneCommand { FarmerCode = Guid.Empty };
        var saleDto = new SaleDto { Id = 7, SoldOnUtc = DateTime.UtcNow };

        mockOptions.Setup(o => o.Value).Returns(new CornLimiterOptions { MinutesLimitToAllowNewSale = 5 });
        mockFeatureFlagService.Setup(f => f.IsFeatureFlagEnabled("cacheBoostEnabledFeatureFlag")).Returns(false);

        object? cacheValue = null;
        mockMemoryCache.Setup(m => m.TryGetValue(Guid.Empty, out cacheValue)).Returns(false);
        mockRepository.Setup(r => r.GetLastAsync(Guid.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sale?)null);
        mockRepository.Setup(r => r.AddAsync(It.IsAny<Sale>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        mockUnitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        mockMapper.Setup(m => m.Map<SaleDto>(It.IsAny<Sale>()))
            .Returns(saleDto);

        var useCase = new SellOneUseCase(
            mockMemoryCache.Object,
            mockRepository.Object,
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockOptions.Object,
            mockFeatureFlagService.Object);

        // Act
        var result = await useCase.ExecuteAsync(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(saleDto.Id, result.Id);
    }

    /// <summary>
    /// Tests that the constructor successfully creates an instance with all valid dependencies
    /// and calls the feature flags service with the correct feature flag key.
    /// </summary>
    /// <param name="featureFlagValue">The value returned by the feature flag service.</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Constructor_ValidDependencies_SuccessfullyCreatesInstanceAndCallsFeatureFlag(bool featureFlagValue)
    {
        // Arrange

        mockFeatureFlagService
            .Setup(f => f.IsFeatureFlagEnabled("cacheBoostEnabledFeatureFlag"))
            .Returns(featureFlagValue);

        // Act
        var useCase = new SellOneUseCase(
            mockMemoryCache.Object,
            mockRepository.Object,
            mockUnitOfWork.Object,
            mockMapper.Object,
            mockOptions.Object,
            mockFeatureFlagService.Object);

        // Assert
        Assert.NotNull(useCase);
        mockFeatureFlagService.Verify(
            f => f.IsFeatureFlagEnabled("cacheBoostEnabledFeatureFlag"),
            Times.Once);
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when memoryCache parameter is null.
    /// </summary>
    [Fact]
    public void Constructor_NullMemoryCache_ThrowsArgumentNullException()
    {
        // Arrange

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SellOneUseCase(
                null!,
                mockRepository.Object,
                mockUnitOfWork.Object,
                mockMapper.Object,
                mockOptions.Object,
                mockFeatureFlagService.Object));
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when repository parameter is null.
    /// </summary>
    [Fact]
    //[Trait("Category", "ProductionBugSuspected")]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SellOneUseCase(
                mockMemoryCache.Object,
                null!,
                mockUnitOfWork.Object,
                mockMapper.Object,
                mockOptions.Object,
                mockFeatureFlagService.Object));
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when unitOfWork parameter is null.
    /// </summary>
    [Fact]
    public void Constructor_NullUnitOfWork_ThrowsArgumentNullException()
    {
        // Arrange

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SellOneUseCase(
                mockMemoryCache.Object,
                mockRepository.Object,
                null!,
                mockMapper.Object,
                mockOptions.Object,
                mockFeatureFlagService.Object));
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when mapper parameter is null.
    /// </summary>
    [Fact]
    public void Constructor_NullMapper_ThrowsArgumentNullException()
    {
        // Arrange

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SellOneUseCase(
                mockMemoryCache.Object,
                mockRepository.Object,
                mockUnitOfWork.Object,
                null!,
                mockOptions.Object,
                mockFeatureFlagService.Object));
    }

    /// <summary>
    /// Tests that the constructor throws ArgumentNullException when options parameter is null.
    /// </summary>
    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SellOneUseCase(
                mockMemoryCache.Object,
                mockRepository.Object,
                mockUnitOfWork.Object,
                mockMapper.Object,
                null!,
                mockFeatureFlagService.Object));
    }

    /// <summary>
    /// Tests that the constructor throws NullReferenceException when featureFlagsService parameter is null,
    /// as it attempts to call IsFeatureFlagEnabled on a null reference.
    /// </summary>
    [Fact]
    public void Constructor_NullFeatureFlagsService_ThrowsArgumentNullException()
    {
        // Arrange

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new SellOneUseCase(
                mockMemoryCache.Object,
                mockRepository.Object,
                mockUnitOfWork.Object,
                mockMapper.Object,
                mockOptions.Object,
                null!));
    }
}