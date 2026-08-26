using NeatShot.Core.Annotations;

namespace NeatShot.Core.Tests.Annotations;

public class ObscureAnnotationTests
{
    [Fact]
    public void DefaultsToMediumStrength()
    {
        var blur = new ObscureAnnotation(new ImageRect(0, 0, 10, 10), ObscureKind.Blur);

        Assert.Equal(ObscureAnnotation.DefaultStrength, blur.Strength);
    }

    [Theory]
    [InlineData(-3, ObscureAnnotation.MinStrength)]
    [InlineData(7, 7)]
    [InlineData(99, ObscureAnnotation.MaxStrength)]
    public void WithStrength_ClampsToRange(int requested, int expected)
    {
        var blur = new ObscureAnnotation(new ImageRect(0, 0, 10, 10), ObscureKind.Pixelate);

        var changed = blur.WithStrength(requested);

        Assert.Equal(expected, changed.Strength);
        Assert.Equal(blur.Id, changed.Id);
    }
}
