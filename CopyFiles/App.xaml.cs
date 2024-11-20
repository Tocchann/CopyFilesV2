using CopyFiles.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CopyFiles.Extensions.UI.Abstractions;
using System.Windows;

namespace CopyFiles;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	public static T GetService<T>() where T : class => host_.Services.GetService<T>()!;

	static App()
	{
		host_ = Host.CreateDefaultBuilder()
			.ConfigureLogging( ( context, logging ) => logging.AddDebug() )
			.ConfigureServices( OnConfigureServices )
			.Build();
	}
	private static void OnConfigureServices( HostBuilderContext context, IServiceCollection services )
	{
		// 拡張ライブラリのDI登録
		CopyFiles.Extensions.UI.WPF.ConfigureService.AddDispAlert( services );
		CopyFiles.Extensions.UI.WPF.ConfigureService.AddSelectFolderDialog( services );

		CopyFiles.Extensions.Storage.ConfigureServices.AddServices( services );

		services.AddHostedService<Services.ApplicationHostService>();

		// 起動時の選択ウィンドウ(そのままメインウィンドウとして稼働する)
		services.AddTransient<ViewModels.SelectWorkViewModel>();
		services.AddTransient<Views.SelectWorkView>();

		services.AddSingleton<ProjectSettingModel>();
	}
	private static IHost host_;

	private async void OnStartupAsync( object sender, StartupEventArgs e )
	{
		// メッセージボックスのタイトル設定を行う(ちょっと色気を出してタスクダイアログを使う)
		var dispAlert = GetService<IDispAlert>();
		dispAlert.Title = "インストーラビルドサポートツール";
		dispAlert.UseTaskDialog = true;	// ちょっと色気を出してみる
		await host_.StartAsync();
	}
	private async void OnExitAsync( object sender, ExitEventArgs e )
	{
		await host_.StopAsync();
	}
}
