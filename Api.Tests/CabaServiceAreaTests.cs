using Fmc.Application.Services;
using Fmc.Domain.Constants;

namespace Fmc.Api.Tests;

public class CabaServiceAreaTests
{
    [Theory]
    [InlineData(-34.6037, -58.3816)] // centro
    [InlineData(-34.5875, -58.4250)] // Palermo
    [InlineData(-34.6210, -58.3730)] // San Telmo
    public void Contains_CabaPoints_ReturnsTrue(double lat, double lng)
    {
        Assert.True(CabaServiceArea.Contains(lat, lng));
        Assert.True(LocationValidation.IsWithinCabaServiceArea(lat, lng));
    }

    [Theory]
    [InlineData(40.4168, -3.7038)] // Madrid
    [InlineData(-34.90, -58.40)] // fuera sur
    public void Contains_OutsideCaba_ReturnsFalse(double lat, double lng)
    {
        Assert.False(CabaServiceArea.Contains(lat, lng));
    }

    [Fact]
    public void EnsureContains_OutsideCaba_Throws()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            CabaServiceArea.EnsureContains(40.4168, -3.7038));
        Assert.Contains("CABA", ex.Message);
    }
}
