using CopyFiles.Core.Models;
using System.Diagnostics;

namespace CopyFiles.Core.DataflowBlock;

public class GenerateFileInformation
{
	public static TargetFileInformation Generate( IEnumerable<ReferFolder> settings, string filePath, bool isAbsolute )
	{
		foreach( var setting in settings.Where( s => filePath.StartsWith( s.BaseFolder ) ) )
		{
			var refPath = Path.GetRelativePath( setting.BaseFolder, filePath );
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
		foreach( var setting in settings.Where( s => filePath.StartsWith( s.ReferenceFolder ) ) )
		{
			var refPath = Path.GetRelativePath( setting.ReferenceFolder, filePath );
			if( isAbsolute )
			{
				refPath = Path.Combine( setting.BaseFolder, refPath );
				// 絶対パスの場合は、参照先から集めてくるので、存在するところにマッチさせる(複数設定されてるはずなので)
				if( !File.Exists( filePath ) )
				{
					continue;
				}
			}
			return new( new( refPath, isAbsolute ), new( filePath, true ) );
		}
		throw new InvalidDataException( $"{filePath} をコピーするための設定がありません。" );
	}
}
