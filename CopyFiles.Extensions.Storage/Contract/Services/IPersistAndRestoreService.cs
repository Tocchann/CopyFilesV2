namespace CopyFiles.Extensions.Storage.Contract.Services;

public interface IPersistAndRestoreService
{
	Task PersistDataAsync<TValue>( TValue content, CancellationToken token = default ) where TValue : class;
	ValueTask<TResult?> RestoreDataAsync<TResult>( CancellationToken token = default ) where TResult : class;
	TResult? RestoreData<TResult>() where TResult : class;
	public string? PersistFilePath { get; set; }
}
