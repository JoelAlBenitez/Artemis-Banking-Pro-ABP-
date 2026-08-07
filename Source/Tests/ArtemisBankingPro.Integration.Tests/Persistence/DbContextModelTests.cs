using ArtemisBankingPro.Core.Domain.Entities.SavingsAccounts;
using ArtemisBankingPro.Core.Domain.Entities.Transactions;
using ArtemisBankingPro.Infraestructrue.Persistence.Context;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ArtemisBankingPro.Integration.Tests.Persistence
{
    public sealed class DbContextModelTests
    {
        private static DbContextArtemisBanking BuildContext()
        {
            var options = new DbContextOptionsBuilder<DbContextArtemisBanking>()
                .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ArtemisBankingProModelTests;")
                .Options;

            return new DbContextArtemisBanking(options);
        }

        [Fact]
        public void Model_ShouldBuildWithoutErrors()
        {
            using var context = BuildContext();

            var build = () => context.Model;

            build.Should().NotThrow();
        }

    
        [Fact]
        public void SavingsAccount_ShouldExposeAMaterializableTransactionsCollection()
        {
            using var context = BuildContext();

            var navigation = context.Model
                .FindEntityType(typeof(SavingsAccount))!
                .FindNavigation(nameof(SavingsAccount.Transactions));

            navigation.Should().NotBeNull();
            navigation!.IsCollection.Should().BeTrue();
            navigation.TargetEntityType.ClrType.Should().Be(typeof(Transaction));
        }

        [Fact]
        public void SavingsAccount_ShouldDeclareTheUniqueActivePrimaryAccountIndex()
        {
            using var context = BuildContext();

            var indexes = context.Model
                .FindEntityType(typeof(SavingsAccount))!
                .GetIndexes();

            indexes.Should().Contain(index =>
                index.IsUnique
                && index.Properties.Any(property => property.Name == nameof(SavingsAccount.CustomerId)));
        }

        [Fact]
        public void SavingsAccount_ShouldDeclareItsAccountNumberAsUnique()
        {
            using var context = BuildContext();

            var indexes = context.Model
                .FindEntityType(typeof(SavingsAccount))!
                .GetIndexes();

            indexes.Should().Contain(index =>
                index.IsUnique
                && index.Properties.Count == 1
                && index.Properties[0].Name == nameof(SavingsAccount.AccountNumber));
        }
    }
}
