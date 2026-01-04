using System.Reflection;
using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Core;
using DocumentOrganizer.Services;
using DocumentOrganizer.ViewModel;
using DocumentOrganizer.Views;
using DocumentTaggerCore.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using Syncfusion.Maui.Core.Hosting;

namespace DocumentOrganizer;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseMauiCommunityToolkitCore()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		var config = GetConfiguration();
		WorkerOptions? options = config.GetSection("DT").Get<WorkerOptions>();
		builder.Configuration.AddConfiguration(config);
#if DEBUG
		builder.Logging.AddDebug();
#endif

		builder.ConfigureSyncfusionCore();

		if (options != null)
			builder.Services.AddSingleton(options);
		
		builder.Services.AddSingleton<IAlertService, AlertService>();
		builder.Services.AddSingleton<IRuleService, RuleService>();
		builder.Services.AddSingleton<RuleEditorViewModel>();
		builder.Services.AddSingleton<FolderViewModel>();
		builder.Services.AddSingleton<RuleEditorView>();
		builder.Services.AddSingleton<MainViewModel>();
		builder.Services.AddSingleton<FolderView>();
		builder.Services.AddSingleton<MainPage>();

		var app = builder.Build();

		ServiceHelper.Initialize(app.Services);

		return app;
	}

	private static IConfiguration GetConfiguration()
	{
		var a = Assembly.GetExecutingAssembly();
		using var stream = a.GetManifestResourceStream("DocumentOrganizer.appsettings.json");

		if (stream != null)
		{
			var config = new ConfigurationBuilder()
						.AddJsonStream(stream)
						.Build();

			return config;
		}

		return new ConfigurationBuilder().Build();
	}
}
