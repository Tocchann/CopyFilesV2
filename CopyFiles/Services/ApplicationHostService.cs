using CopyFiles.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CopyFiles.Extensions.Storage.Contract.Services;
using System;
using System.IO;

namespace CopyFiles.Services;

internal class ApplicationHostService( ILogger<ApplicationHostService> m_logger, IServiceProvider m_serviceProvider, IHostEnvironment m_environment, IPersistAndRestoreService m_persistAndRestoreService ) : IHostedService
{
	public async Task StartAsync( CancellationToken cancellationToken )
	{
		m_logger.LogInformation( "ApplicationHostService.StartAsync()" );
		cancellationToken.ThrowIfCancellationRequested();
		CopyFiles.Extensions.Storage.ConfigureServices.PersistFilePath = 
			Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.LocalApplicationData ), "Morrin", m_environment.ApplicationName, "ApplicationSettings.json" );
		var mainWindow = m_serviceProvider.GetService<Views.SelectWorkView>();
		mainWindow?.Show();
		await Task.CompletedTask;
	}
	public async Task StopAsync( CancellationToken cancellationToken )
	{
		m_logger.LogInformation( "ApplicationHostService.StopAsync()" );
		cancellationToken.ThrowIfCancellationRequested();
		var settings = m_serviceProvider.GetService<ProjectSettingModel>()!;
		await m_persistAndRestoreService.PersistDataAsync( settings, cancellationToken );
	}
}
