using AutoFixture;
using AutoFixture.Xunit3;

namespace PurchaseConversion.Api.Tests.Common.Attributes;

public sealed class AutoApiDataAttribute : AutoDataAttribute
{
    public AutoApiDataAttribute() : base(CreateFixture)
    {
    }

    private static IFixture CreateFixture()
    {
        return new Fixture();
    }
}
