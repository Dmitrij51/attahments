using NUnit.Framework;
using System.Collections.Concurrent;
using async_tests;
using System.Linq;

namespace PrimesAmount.Tests;

public class PrimesAmountTests
{
    private class FakePrimeService : PrimeService
    {
        public bool WasClearCacheCalled { get; private set; }
        public ConcurrentDictionary<int, int> CheckedNumbers { get; } = new();

        public override bool CheckIsPrime(int p)
        {
            CheckedNumbers.AddOrUpdate(p, 1, (_, count) => count + 1);

            return p == 2 || p == 3 || p == 5 || p == 7;
        }

        public override void ClearCache()
        {
            WasClearCacheCalled = true;
        }
    }

    private class ThrowingPrimeService : PrimeService
    {
        public bool WasClearCacheCalled { get; private set; }

        public override bool CheckIsPrime(int p)
        {

            if (p == 3)
                throw new InvalidOperationException("worker failed");

            return true;
        }

        public override void ClearCache()
        {
            WasClearCacheCalled = true;
        }
    }

    [Test]
    public void CountPrimesAmount_ReturnsPrimeNumbers()
    {
        // Arrange
        var service = new FakePrimeService();
        var primesAmount = new async_tests.PrimesAmount(10);

        // Act
        var result = primesAmount.CountPrimesAmount(service);

        // Assert
        Assert.That(result, Is.EquivalentTo(new[] { 2, 3, 5, 7 }));
    }

    [Test]
    public void CountPrimesAmount_ReturnsEmptyCollection_WhenNoPrimes()
    {
        // Arrange
        var service = new FakePrimeService();
        var primesAmount = new async_tests.PrimesAmount(2);

        // Act
        var result = primesAmount.CountPrimesAmount(service);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void CountPrimesAmount_CallsClearCache()
    {
        // Arrange
        var service = new FakePrimeService();
        var primesAmount = new async_tests.PrimesAmount(10);

        // Act
        primesAmount.CountPrimesAmount(service);

        // Assert
        Assert.That(service.WasClearCacheCalled, Is.True);
    }

    [Test]
    public void CountPrimesAmount_ChecksEveryNumberExactlyOnce()
    {
        // Arrange
        var service = new FakePrimeService();
        var primesAmount = new async_tests.PrimesAmount(10);

        // Act
        primesAmount.CountPrimesAmount(service);

        // Assert
        Assert.That(service.CheckedNumbers.Keys, Is.EquivalentTo(
            Enumerable.Range(2, 8)
        ));

        Assert.That(
            service.CheckedNumbers.Values,
            Is.All.EqualTo(1)
        );
    }

    [Test]
    public void CountPrimesAmount_WorkerThrows_ReturnsEmptyCollectionAndStillClearsCache()
    {
        // Arrange
        var service = new ThrowingPrimeService();
        var primesAmount = new async_tests.PrimesAmount(10);

        // Act
        var result = primesAmount.CountPrimesAmount(service);

        // Assert — catch очищает очередь, finally освобождает кэш
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Empty);
            Assert.That(service.WasClearCacheCalled, Is.True);
        });
    }

    [Test]
    public void CountPrimesAmount_BorderOne_ReturnsEmptyCollectionWithoutTouchingService()
    {
        var service = new FakePrimeService();
        var primesAmount = new async_tests.PrimesAmount(1);

        // Act
        var result = primesAmount.CountPrimesAmount(service);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Empty);
            Assert.That(service.CheckedNumbers, Is.Empty);
            Assert.That(service.WasClearCacheCalled, Is.False);
        });
    }
}