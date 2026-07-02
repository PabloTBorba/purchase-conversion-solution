using AutoFixture;
using AutoFixture.Xunit3;
using Moq;
using PurchaseConversion.Domain.Entities;
using PurchaseConversion.Domain.Interfaces;
using PurchaseConversion.Domain.Models;
using PurchaseConversion.Domain.Services;
using PurchaseConversion.Domain.Tests.Common.Attributes;

namespace PurchaseConversion.Domain.Tests.Services;

public sealed class PurchaseServiceTests
{
    [Theory, AutoDomainData]
    internal async Task StorePurchaseAsync_WhenValidInput_StoresAndReturnsId(
        [Frozen] Mock<IPurchaseRepository> purchaseRepository,
        PurchaseService purchaseService,
        IFixture fixture)
    {
        //arrange
        var expectedId = fixture.Create<Guid>();

        purchaseRepository
            .Setup(r => r.AddAsync(It.IsAny<PurchaseTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedId);

        //act
        var id = await purchaseService.StorePurchaseAsync(
            "Test",
            DateOnly.FromDateTime(DateTime.UtcNow),
            10.123m,
            CancellationToken.None);

        //assert
        Assert.Equal(expectedId, id);

        purchaseRepository.Verify(
            r => r.AddAsync(It.IsAny<PurchaseTransaction>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory, AutoDomainData]
    internal async Task ConvertAsync_WhenValidInput_UsesRateAndRounds(
        [Frozen] Mock<IPurchaseRepository> purchaseRepository,
        [Frozen] Mock<IExchangeRateProvider> exchangeRateProvider,
        PurchaseService purchaseService,
        IFixture fixture)
    {
        //arrange
        var purchaseId = fixture.Create<Guid>();
        var purchaseDate = DateOnly.FromDateTime(DateTime.UtcNow);

        purchaseRepository
            .Setup(r => r.GetByIdAsync(purchaseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PurchaseTransaction(purchaseId, "Test", purchaseDate, 10m));

        exchangeRateProvider
            .Setup(r => r.GetRateAsync("EUR", purchaseDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExchangeRateResult(true, 1.23456m, null));

        //act
        var converted = await purchaseService.ConvertAsync(purchaseId, "EUR", CancellationToken.None);

        //assert
        Assert.Equal(12.35m, converted.ConvertedAmount);
        Assert.Equal(1.23456m, converted.ExchangeRate);
    }

    [Theory, AutoDomainData]
    internal async Task StorePurchaseAsync_WhenCanceled_ThrowsOperationCanceledException(
        [Frozen] Mock<IPurchaseRepository> purchaseRepository,
        PurchaseService purchaseService)
    {
        //arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        purchaseRepository
            .Setup(r => r.AddAsync(It.IsAny<PurchaseTransaction>(), It.IsAny<CancellationToken>()))
            .Returns((PurchaseTransaction _, CancellationToken ct) => Task.FromCanceled<Guid>(ct));

        //act
        Task<Guid> act() => purchaseService.StorePurchaseAsync(
            "Test",
            DateOnly.FromDateTime(DateTime.UtcNow),
            10m,
            cts.Token);

        //assert
        await Assert.ThrowsAsync<OperationCanceledException>((Func<Task<Guid>>)act);

        purchaseRepository.Verify(
            r => r.AddAsync(
                It.IsAny<PurchaseTransaction>(),
                It.Is<CancellationToken>(t => t == cts.Token)),
            Times.Never);
    }

    [Theory, AutoDomainData]
    internal async Task ConvertAsync_WhenRepositoryIsCanceled_ThrowsOperationCanceledException(
        [Frozen] Mock<IPurchaseRepository> purchaseRepository,
        [Frozen] Mock<IExchangeRateProvider> exchangeRateProvider,
        PurchaseService purchaseService,
        IFixture fixture)
    {
        //arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var purchaseId = fixture.Create<Guid>();

        purchaseRepository
            .Setup(r => r.GetByIdAsync(purchaseId, It.IsAny<CancellationToken>()))
            .Returns((Guid _, CancellationToken ct) => Task.FromCanceled<PurchaseTransaction?>(ct));

        //act
        Task<ConvertedPurchaseTransaction> act() => purchaseService.ConvertAsync(purchaseId, "EUR", cts.Token);

        //assert
        await Assert.ThrowsAsync<OperationCanceledException>((Func<Task<ConvertedPurchaseTransaction>>)act);

        exchangeRateProvider.Verify(
            r => r.GetRateAsync(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}