using Microsoft.Extensions.DependencyInjection;
using CopyFiles.Extensions.UI.Abstractions;

namespace CopyFiles.Extensions.UI.WPF;

public class ConfigureService
{
	public static IServiceCollection AddDispAlert( IServiceCollection services )
	{
		services.AddSingleton<IDispAlert, DispAlert>();
		return services;
	}
	public static IServiceCollection AddSelectFolderDialog( IServiceCollection services )
	{
		services.AddTransient<ISelectFolderDialog, SelectFolderDialog>();
		return services;
	}
}
