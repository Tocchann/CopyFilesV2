
using System.Diagnostics;
using System.Xml;

namespace CopyFiles.Core.DataflowBlock;

public class ProjectFileReader
{
	// TransformManyBlock を利用して呼び出すことで非同期化できるようになっている
	public static IEnumerable<string> Read( string projectFilePath, CancellationToken token )
	{
		return Path.GetExtension( projectFilePath ).ToLower() switch
		{
			".ism" => ReadIsmFile( projectFilePath, token ),
			// ".wixproj" => ReadWixProjectFile( projectFilePath ),
			// プロジェクトとして読み取らないものはそのままターゲットファイルとして渡す
			_ => [projectFilePath],
		};
	}
	private static IEnumerable<string> ReadIsmFile( string projectFilePath, CancellationToken token )
	{
		HashSet<string> targetFiles = new();
		XmlDocument ism = new();
		ism.Load( projectFilePath );
		token.ThrowIfCancellationRequested();
		string projectFolder = Path.GetDirectoryName( projectFilePath )!;
		var pathVariable = ReadPathVariable( projectFolder, ism );
		var nodes = ism.SelectNodes( "//col[text()='ISBuildSourcePath']" );
		token.ThrowIfCancellationRequested();
		if( nodes != null )
		{
			foreach( XmlElement node in nodes )
			{
				token.ThrowIfCancellationRequested();
				var tableName = node.ParentNode?.Attributes?["name"]?.Value;
				int index = GetISBuildSourcePathIndex( ism, tableName );
				if( index != -1 )
				{
					Trace.WriteLine( $"//table[@name='{tableName}']" );
					var rows = ism.SelectNodes( $"//table[@name='{tableName}']/row" );
					if( rows != null )
					{
						foreach( XmlElement row in rows )
						{
							var sourcePath = row.ChildNodes[index]?.InnerText;
							if( !string.IsNullOrEmpty( sourcePath ) )
							{
								// パスは変換してから格納する(無駄なループは極力避けましょう)
								if( sourcePath.Contains( '<' ) )
								{
									foreach( var kv in pathVariable )
									{
										sourcePath = sourcePath.Replace( kv.Key, kv.Value );
										if( !sourcePath.Contains( '<' ) )
										{
											break;
										}
									}
								}
								targetFiles.Add( sourcePath );
								Trace.WriteLine( $"Add:{sourcePath}" );
							}
						}
					}
				}
			}
		}
		return targetFiles;
	}
	/// <summary>
	/// パス変換テーブル情報を読み取る(変換しやすいようにキーも調整しておく)
	/// </summary>
	/// <param name="projectFolder"></param>
	/// <param name="ism"></param>
	/// <returns></returns>
	private static Dictionary<string, string> ReadPathVariable( string projectFolder, XmlDocument ism )
	{
		var pathVariable = new Dictionary<string, string>();
		var isPathVariables = ism.SelectNodes( "//table[@name='ISPathVariable']/row" );
		if( isPathVariables != null )
		{
			foreach( XmlElement row in isPathVariables )
			{
				if( row.ChildNodes.Count >= 2 )
				{
					var key = row.ChildNodes[0]?.InnerText;
					var value = row.ChildNodes[1]?.InnerText;
					if( key == "ISProjectFolder" )
					{
						value = projectFolder;
					}
					if( string.IsNullOrEmpty( key ) == false && string.IsNullOrEmpty( value ) == false )
					{
						// キーはあとで単純変換できるようにするために<>をつけておく
						pathVariable["<" + key + ">"] = value;
					}
				}
			}
		}
		return pathVariable;
	}
	/// <summary>
	/// テーブルの ISBuildSourcePath のインデックスを取得する
	/// </summary>
	/// <param name="ism"></param>
	/// <param name="tableName"></param>
	/// <returns></returns>
	private static int GetISBuildSourcePathIndex( XmlDocument ism, string? tableName )
	{
		if( string.IsNullOrEmpty( tableName ) )
		{
			return -1;
		}
		var cols = ism.SelectNodes( $"//table[@name='{tableName}']/col" );
		if( cols == null )
		{
			return -1;
		}
		for( int index = 0 ; index < cols.Count ; index++ )
		{
			if( cols[index]?.InnerText == "ISBuildSourcePath" )
			{
				return index;
			}
		}
		return -1;
	}
}
