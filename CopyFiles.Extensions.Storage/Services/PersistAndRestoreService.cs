using Microsoft.Extensions.Logging;
using CopyFiles.Extensions.Storage.Contract.Services;

namespace CopyFiles.Extensions.Storage.Services;

public class PersistAndRestoreService( ILogger<PersistAndRestoreService> m_logger, IFileService m_fileService ) : IPersistAndRestoreService
{
	public async Task PersistDataAsync<TValue>( TValue content, CancellationToken token = default ) where TValue : class
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		if( ConfigureServices.PersistFilePath is null )
		{
			throw new InvalidOperationException( "ConfigureServices.PersistFilePath is not set" );
		}
		await m_fileService.SaveAsync( ConfigureServices.PersistFilePath, content, token );
	}
	public async ValueTask<TResult?> RestoreDataAsync<TResult>( CancellationToken token = default ) where TResult : class
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		if( ConfigureServices.PersistFilePath is null )
		{
			throw new InvalidOperationException( "ConfigureServices.PersistFilePath is not set" );
		}
		try
		{
			return await m_fileService.ReadAsync<TResult>( ConfigureServices.PersistFilePath, token );
		}
		catch( Exception ex )
		{
			m_logger.LogError( ex, ex.Message );
			return default;
		}
	}
	public TResult? RestoreData<TResult>() where TResult : class
	{
		m_logger.LogInformation( System.Reflection.MethodBase.GetCurrentMethod()?.Name );
		if( ConfigureServices.PersistFilePath is null )
		{
			throw new InvalidOperationException( "ConfigureServices.PersistFilePath is not set" );
		}
		try
		{
			return m_fileService.Read<TResult>( ConfigureServices.PersistFilePath );
		}
		catch( Exception ex )
		{
			m_logger.LogError( ex, ex.Message );
			return default;
		}
	}
}
