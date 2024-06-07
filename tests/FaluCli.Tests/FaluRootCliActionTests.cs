using Xunit;

namespace Falu.Tests;

public class FaluRootCliActionTests
{
    [Theory]
    [InlineData("1.0.0", "1.0.0", false)] // same version
    [InlineData("1.0.0", "0.1.0", false)] // older version
    [InlineData("1.0.0", "1.0.1", true)] // new patch version
    [InlineData("1.0.0", "1.1.0", true)] // new minor version
    [InlineData("1.0.0", "2.0.0", true)] // new major version
    [InlineData("1.0.0", "1.0.0-beta", false)] // pre-release version
    [InlineData("1.0.0", "1.12.1-pr0271-0047", false)] // pre-release version
    public void IsNewerVersionAvailable_Works(string version, string latestTagName, bool expected)
        => Assert.Equal(expected, FaluRootCliAction.IsNewerVersionAvailable(version, latestTagName, out _, out _));
}
