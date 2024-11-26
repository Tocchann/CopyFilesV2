using CommunityToolkit.Mvvm.ComponentModel;
using CopyFiles.Core.Const;
using CopyFiles.Core.Models;
using System.ComponentModel.DataAnnotations;

namespace CopyFiles.ViewModels;

public partial class TargetFileInformationItem : ObservableObject
{
	[ObservableProperty]
	bool isCopy;

	[ObservableProperty]
	CompareStatus status;

	[ObservableProperty]
	string sourceFilePath;

	[ObservableProperty]
	string sourceFileVersion;

	[ObservableProperty]
	string sourceLastWriteTime;

	[ObservableProperty]
	string destinationFilePath;

	[ObservableProperty]
	string destinationFileVersion;

	[ObservableProperty]
	string destinationLastWriteTime;

	[ObservableProperty]
	bool isSelected;

	public TargetFileInformationItem( TargetFileInformation targetInfo, bool copyMode )
	{
		IsCopy = targetInfo.NeedCopy;
		Status = targetInfo.CompareStatus;
		// コピーモードは両方とも表示する
		if( copyMode )
		{
			SourceFilePath = targetInfo.ReferFileInfo.FilePath;
			SourceFileVersion = targetInfo.ReferFileInfo.FileVersion?.ToString() ?? string.Empty;
			SourceLastWriteTime = targetInfo.ReferFileInfo.LastWriteTime?.ToString( "yyyy-MM-dd HH:mm:dd" ) ?? string.Empty;
			DestinationFilePath = targetInfo.BaseFileInfo.FilePath;
			DestinationFileVersion = targetInfo.BaseFileInfo.FileVersion?.ToString() ?? string.Empty;
			DestinationLastWriteTime = targetInfo.BaseFileInfo.LastWriteTime?.ToString( "yyyy-MM-dd HH:mm:dd" ) ?? string.Empty;
		}
		// 署名処理の場合はオリジナルファイルしか参照しない
		else
		{
			SourceFilePath = targetInfo.BaseFileInfo.FilePath;
			SourceFileVersion = targetInfo.BaseFileInfo.FileVersion?.ToString() ?? string.Empty;
			SourceLastWriteTime = targetInfo.BaseFileInfo.LastWriteTime?.ToString( "yyyy-MM-dd HH:mm:dd" ) ?? string.Empty;
			DestinationFilePath = string.Empty;
			DestinationFileVersion = string.Empty;
			DestinationLastWriteTime = string.Empty;
		}
		IsSelected = false;
	}
}
