using Syncfusion.Maui.Toolkit.Hosting;
using Microsoft.Extensions.Logging;

namespace BasicTools;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>().ConfigureSyncfusionToolkit()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
				fonts.AddFont("appicons.ttf", "appicons");
			});

#if WINDOWS
		Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler.Mapper.AppendToMapping("DisableMultiselectCheckbox",
(handler, view) =>
{
    handler.PlatformView.IsMultiSelectCheckBoxEnabled = false;
});
#endif

#if DEBUG
        builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
