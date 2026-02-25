namespace GhostVPN.Desktop.Common;

public static class AppBuilderExtension
{
    public static AppBuilder WithFontByDefault(this AppBuilder appBuilder)
    {
        var fallbacks = new List<FontFallback>();

        try
        {
            var notoSansSc = new FontFamily($"{Global.AvaAssets}Fonts/NotoSansSC-Regular.ttf#Noto Sans SC");
            fallbacks.Add(new FontFallback { FontFamily = notoSansSc });
        }
        catch (Exception ex)
        {
            Logging.SaveLog("WithFontByDefault", ex);
        }

        // Always keep Windows-safe fallback fonts to avoid startup failure.
        fallbacks.Add(new FontFallback { FontFamily = new FontFamily("Segoe UI") });
        fallbacks.Add(new FontFallback { FontFamily = new FontFamily("Arial") });

        if (OperatingSystem.IsLinux())
        {
            fallbacks.Add(new FontFallback
            {
                FontFamily = new FontFamily("Noto Color Emoji")
            });
        }

        return appBuilder.With(new FontManagerOptions
        {
            FontFallbacks = fallbacks.ToArray()
        });
    }
}
