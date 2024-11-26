using CopyFiles.Core.Const;
using System.Diagnostics;

namespace CopyFiles.Core.Models;

public class FileInformation
{
	public string FilePath { get; init; }
	public Version? FileVersion { get; set; }
	public DateTime? LastWriteTime { get; set; }
	public long FileSize { get; set; }
	public bool Exists { get; set; }

	public FileInformation( string filePath, bool isAbsolute )
	{
		FilePath = filePath;
		// 相対パスを設定する場合は、ファイル情報を取得する
		if( isAbsolute )
		{
			UpdateFileInfo();
		}
	}
	public void UpdateFileInfo()
	{
		var info = new FileInfo( FilePath );
		Exists = info.Exists;
		FileVersion = GetFileVersion( FilePath );
		if( Exists )
		{
			LastWriteTime = info.LastWriteTimeUtc;
			FileSize = info.Length;
		}
	}
	private static Version? GetFileVersion( string filePath )
	{
		var verInfo = FileVersionInfo.GetVersionInfo( filePath );
		if( !string.IsNullOrEmpty( verInfo.FileVersion ) )
		{
			return new Version( verInfo.FileMajorPart, verInfo.FileMinorPart, verInfo.FileBuildPart, verInfo.FilePrivatePart );
		}
		return null;
	}
}
public class TargetFileInformation
{
	/// <summary>
	/// コピー元ファイル情報
	/// </summary>
	public FileInformation BaseFileInfo { get; init; }
	/// <summary>
	/// コピー先ファイル情報
	/// </summary>
	public FileInformation ReferFileInfo { get; init; }
	/// <summary>
	/// ファイルの比較状態
	/// </summary>
	public CompareStatus CompareStatus { get; set; }
	/// <summary>
	/// コピーの必要性チェック
	/// </summary>
	public bool NeedCopy =>	CompareStatus switch
							{
								// 通常コピー
								CompareStatus.NewFile or
								CompareStatus.UnMatch or
								CompareStatus.UnMatchSameVersion or
								// 署名用コピー
								CompareStatus.NotExistSignature => true,
								// そのほかは処理不要
								CompareStatus.Unknown or
								CompareStatus.Match or
								CompareStatus.MatchWithoutSignature or
								CompareStatus.MatchWithoutDate or
								CompareStatus.ExistSignature => false,
								_ => throw new NotImplementedException(),
							};
	public TargetFileInformation( FileInformation baseInfo, FileInformation referInfo )
	{
		BaseFileInfo = baseInfo;
		ReferFileInfo = referInfo;
		CompareStatus = CompareStatus.Unknown;
	}
}
