namespace NeatShot.Core.Capture;

public sealed record Capture(
    CapturedImage Image,
    PixelRect SourceBounds,
    CaptureMode Mode,
    DateTimeOffset CapturedAt);
