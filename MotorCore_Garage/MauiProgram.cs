using Microsoft.Extensions.Logging;
using MauiIcons.FontAwesome.Brand;
using MauiIcons.Material;

namespace MotorCore_Garage
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMaterialMauiIcons()
                .UseFontAwesomeBrandMauiIcons()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("NotoSans-Regular.ttf", "NotoSansRegular");
                    fonts.AddFont("NotoSans-SemiBold.ttf", "NotoSansSemibold");
                    fonts.AddFont("NotoSansDisplay-SemiBold.ttf", "NotoSansDisplaySemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
