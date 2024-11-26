using CopyFiles.Core.Models;
using System.Diagnostics;

namespace CopyFiles.Core.DataflowBlock;

public class GenerateFileInformation
{
	public static TargetFileInformation Generate( IEnumerable<ReferFolder> settings, string filePath, bool isAbsolute )
	{
		foreach( var setting in settings )
		{
			if( filePath.StartsWith( setting.BaseFolder ) )
			{
				int splitPos = setting.BaseFolder.Length;
				if( filePath[splitPos] == Path.DirectorySeparatorChar )
				{
					splitPos++;
				}
				var refPath = filePath.Substring( splitPos );
				if( isAbsolute )
				{
					refPath = Path.Combine( setting.ReferenceFolder, refPath );
					// 絶対パスの場合は、参照先から集めてくるので、存在するところにマッチさせる(複数設定されてるはずなので)
					if( !File.Exists( refPath ) )
					{
						continue;
					}
				}
				return new( new( filePath, true ), new( refPath, isAbsolute ) );
			}
		}
		throw new InvalidDataException( $"{filePath} をコピーするための設定がありません。" );
	}
}
