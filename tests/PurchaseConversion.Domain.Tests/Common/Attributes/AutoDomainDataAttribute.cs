using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Xunit3;

namespace PurchaseConversion.Domain.Tests.Common.Attributes;

public sealed class AutoDomainDataAttribute : AutoDataAttribute
{
    public AutoDomainDataAttribute()
        : base(CreateFixture)
    {
    }

    private static IFixture CreateFixture()
    {
        var fixture = new Fixture();

        fixture.Customize(new AutoMoqCustomization
        {
            ConfigureMembers = true
        });

        return fixture;
    }
}
