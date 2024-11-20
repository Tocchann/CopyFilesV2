using Microsoft.Extensions.DependencyInjection;
using CopyFiles.Extensions.Storage.Contract.Services;
using CopyFiles.Extensions.Storage.Services;

namespace CopyFiles.Extensions.Storage;

public static class ConfigureServices
{
	public static IServiceCollection AddServices( IServiceCollection services )
	{
		services.AddTransient<IFileService, FileService>();
		services.AddTransient<IPersistAndRestoreService, PersistAndRestoreService>();
		return services;
	}
	public static string? PersistFilePath { get; set; }
}
