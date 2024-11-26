using CopyFiles.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CopyFiles.Extensions.Storage.Contract.Services;
using System.IO;

namespace CopyFiles.Services;

public class ApplicationHostService( ILogger<ApplicationHostService> m_logger, IServiceProvider m_serviceProvider, IHostEnvironment m_environment, IPersistAndRestoreService m_persistAndRestoreService ) : IHostedService
{
	public async Task StartAsync( CancellationToken cancellationToken )
	{
		m_logger.LogInformation( "ApplicationHostService.StartAsync()" );
		cancellationToken.ThrowIfCancellationRequested();
		m_persistAndRestoreService.PersistFilePath =
			Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Morrin", m_environment.ApplicationName, "ApplicationSettings.json" );
		var restoredData = await m_persistAndRestoreService.RestoreDataAsync<ProjectSettingManager>( cancellationToken );
		if( restoredData != null )
		{
			App.ProjectSettingManager = restoredData;
		}
		var mainWindow = m_serviceProvider.GetService<Contract.Views.ISelectWorkView>();
		mainWindow?.ShowWindow();
		await Task.CompletedTask;
	}
	public async Task StopAsync( CancellationToken cancellationToken )
	{
		m_logger.LogInformation( "ApplicationHostService.StopAsync()" );
		cancellationToken.ThrowIfCancellationRequested();
		await m_persistAndRestoreService.PersistDataAsync( App.ProjectSettingManager, cancellationToken );
	}
}
