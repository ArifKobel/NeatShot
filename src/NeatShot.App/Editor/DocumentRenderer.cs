using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NeatShot.Core.Annotations;
using NeatShot.Imaging;

namespace NeatShot.Editor;

public static class DocumentRenderer
{
    private const double Dpi = 96;

    public static BitmapSource Render(AnnotationDocument document, Rgba background)
    {
        ArgumentNullException.ThrowIfNull(document);

        var fill = new SolidColorBrush(Color.FromArgb(background.A, background.R, background.G, background.B));

        var image = document.Image;
        var canvas = document.Canvas;
        var renderer = new AnnotationRenderer(image);
        var visual = new DrawingVisual();

        using (var context = visual.RenderOpen())
        {
            context.PushTransform(new TranslateTransform(-canvas.X, -canvas.Y));
            context.DrawRectangle(fill, null, new Rect(canvas.X, canvas.Y, canvas.Width, canvas.Height));
            context.DrawImage(image.ToBitmapSource(), new Rect(0, 0, image.Width, image.Height));
            renderer.Draw(context, document.Annotations);
            context.Pop();
        }

        var target = new RenderTargetBitmap((int)canvas.Width, (int)canvas.Height, Dpi, Dpi, PixelFormats.Pbgra32);
        target.Render(visual);
        target.Freeze();
        return target;
    }
}
