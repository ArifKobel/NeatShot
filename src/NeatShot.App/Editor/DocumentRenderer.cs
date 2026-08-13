using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NeatShot.Core.Annotations;
using NeatShot.Imaging;

namespace NeatShot.Editor;

public static class DocumentRenderer
{
    private const double Dpi = 96;

    public static BitmapSource Render(AnnotationDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var image = document.Image;
        var renderer = new AnnotationRenderer(image);
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            context.DrawImage(image.ToBitmapSource(), new Rect(0, 0, image.Width, image.Height));
            renderer.Draw(context, document.Annotations);
        }

        var target = new RenderTargetBitmap(image.Width, image.Height, Dpi, Dpi, PixelFormats.Pbgra32);
        target.Render(visual);
        target.Freeze();
        return target;
    }
}
