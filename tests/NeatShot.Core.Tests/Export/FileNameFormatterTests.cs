using NeatShot.Core.Export;

namespace NeatShot.Core.Tests.Export;

public class FileNameFormatterTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 8, 7, 21, 5, 9, TimeSpan.FromHours(2));

    [Theory]
    [InlineData("NeatShot {date} at {time}", "NeatShot 2026-08-07 at 21.05.09")]
    [InlineData("{year}{month}{day}-{hour}{minute}{second}", "20260807-210509")]
    [InlineData("plain", "plain")]
    public void Format_ExpandsKnownTokens(string pattern, string expected)
    {
        Assert.Equal(expected, FileNameFormatter.Format(pattern, Timestamp));
    }

    [Fact]
    public void Format_LeavesUnknownTokensUntouched()
    {
        Assert.Equal("shot {foo}", FileNameFormatter.Format("shot {foo}", Timestamp));
    }

    [Fact]
    public void Format_LeavesUnclosedBraceUntouched()
    {
        Assert.Equal("shot {date", FileNameFormatter.Format("shot {date", Timestamp));
    }

    [Fact]
    public void Format_ReplacesInvalidFileNameCharacters()
    {
        Assert.Equal("a-b-c", FileNameFormatter.Format("a:b/c", Timestamp));
    }

    [Fact]
    public void Format_FallsBackToTimestampWhenResultIsBlank()
    {
        Assert.Equal("2026-08-07 21.05.09", FileNameFormatter.Format("   ", Timestamp));
    }
}
