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

	public static ProjectSettingManager ProjectSettingManager { get; set; }
	public static IDispAlert DispAlert { get; set; }
	static App()
	{
		host_ = Host.CreateDefaultBuilder()
			.ConfigureLogging( ( context, logging ) => logging.AddDebug() )
			.ConfigureServices( OnConfigureServices )
			.Build();
		ProjectSettingManager = new();
		// メッセージボックスは全体で一つでいいでしょう
		DispAlert = GetService<IDispAlert>();
		DispAlert.Title = "インストーラビルドサポートツール";
		DispAlert.UseTaskDialog = true; // ちょっと色気を出してみる
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
		services.AddTransient<Contract.Views.ISelectWorkView, Views.SelectWorkView>();

		services.AddTransient<ViewModels.AddSolutionViewModel>();
		services.AddTransient<Contract.Views.IAddSolutionView,Views.AddSolutionView>();

		services.AddTransient<ViewModels.CollectTargetFilesViewModel>();
		services.AddTransient<Contract.Views.ICollectTargetFilesView, Views.CollectTargetFilesView>();

		services.AddTransient<ViewModels.EditReferFolderViewModel>();
		services.AddTransient<Contract.Views.IEditReferFolderView, Views.EditReferFolderView>();

		services.AddTransient<ViewModels.ArchiveNonSignedFilesViewModel>();
		services.AddTransient<Contract.Views.IArchiveNonSignedFilesView, Views.ArchiveNonSignedFilesView>();
	}
	private static IHost host_;

	private async void OnStartupAsync( object sender, StartupEventArgs e )
	{
		await host_.StartAsync();
	}
	private async void OnExitAsync( object sender, ExitEventArgs e )
	{
		await host_.StopAsync();
	}

	private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
	{
		DispAlert.Show(e.Exception.ToString());
		e.Handled = true; // 例外を処理済みにする終了させない
	}
}
