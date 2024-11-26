using CopyFiles.Extensions.Storage.Contract.Services;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace CopyFiles.Extensions.Storage.Services;

public class FileService : IFileService
{
	public async ValueTask<TResult?> ReadAsync<TResult>( string filePath, CancellationToken token = default )
	{
		// 指定されたファイルから読み取る
		if( File.Exists( filePath ) )
		{
			using( var stream = File.OpenRead( filePath ) )
			{
				return await ReadAsync<TResult>( stream );
			}
		}
		return default;
	}
	public async ValueTask<TResult?> ReadAsync<TResult>( Stream stream, CancellationToken token = default )
	{
		return await JsonSerializer.DeserializeAsync<TResult>( stream, cancellationToken: token );
	}

	public TResult? Read<TResult>( string filePath )
	{
		// 指定されたファイルから読み取る
		if( File.Exists( filePath ) )
		{
			using( var stream = File.OpenRead( filePath ) )
			{
				JsonSerializerOptions? options = null;
				return JsonSerializer.Deserialize<TResult>( stream, options );
			}
		}
		return default;
	}

	public async Task SaveAsync<TValue>( string filePath, TValue content, CancellationToken token = default )
	{
		var folder = Path.GetDirectoryName( filePath );
		if( !string.IsNullOrEmpty( folder ) )
		{
			Directory.CreateDirectory( folder );
		}
		using( var stream = File.Create( filePath ) )
		{
			// UNICODE文字をエスケープしない、インデントはつけない(無駄な空白はあっても影響しないがカットしておく)
			var options = new JsonSerializerOptions
			{
				Encoder = JavaScriptEncoder.Create( UnicodeRanges.All ),
				WriteIndented = true,
			};
			await JsonSerializer.SerializeAsync<TValue>( stream, content, options, token );
		}
	}
}
