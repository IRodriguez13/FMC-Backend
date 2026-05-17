using Fmc.Api.Branding;

namespace Fmc.Api.Tests;

public class FmcAppTests
{
    [Fact]
    public void ApiTitle_IncludesProductAndInternalCode()
    {
        Assert.Contains(FmcApp.ProductName, FmcApp.ApiTitle);
        Assert.Contains(FmcApp.InternalCode, FmcApp.ApiTitle);
    }
}
