namespace CopyFiles.Extensions.Storage.Contract.Services;

public interface IFileService
{
	ValueTask<TResult?> ReadAsync<TResult>( string filePath, CancellationToken token = default );
	ValueTask<TResult?> ReadAsync<TResult>( Stream stream, CancellationToken token = default );
	TResult? Read<TResult>( string filePath );
	Task SaveAsync<TValue>( string filePath, TValue content, CancellationToken token = default );
}
