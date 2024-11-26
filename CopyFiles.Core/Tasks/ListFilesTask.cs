using CopyFiles.Core.DataflowBlock;
using CopyFiles.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace CopyFiles.Core.Tasks;

public static class ListFilesTask
{
	/// <summary>
	/// プロジェクトで参照しているファイルのリストアップ
	/// </summary>
	/// <param name="setting"></param>
	/// <param name="progress"></param>
	/// <param name="token"></param>
	/// <returns></returns>
	public static async Task<IEnumerable<string>> ListSourceFilesAsync( ProjectSetting setting, IProgress<string> progress, CancellationToken token )
	{
		// プロジェクトが異なると同じファイルを参照することがあるので一意になっている必要がある
		var baseFiles = new HashSet<string>();
		if( setting.ProjectFiles == null )
		{
			return baseFiles;
		}
		var blockOptions = new ExecutionDataflowBlockOptions
		{
			CancellationToken = token,
			EnsureOrdered = false,
			MaxDegreeOfParallelism = -1,    //	制限なしでよいと思うのよ
		};
		var singleBlockOptions = new ExecutionDataflowBlockOptions
		{
			CancellationToken = token,
			EnsureOrdered = false,
			MaxDegreeOfParallelism = 1, //	直列で処理する
		};
		var linkOptions = new DataflowLinkOptions
		{
			PropagateCompletion = true,
		};
		// 実際にチェックするファイル数をカウントする(先に処理する。プログレス的には IsIndeterminate = true の状態)
		var readFilesBlock = new TransformManyBlock<string, string>(
			projectFilePath => ProjectFileReader.Read( projectFilePath, token ), blockOptions );

		// 同じファイルが複数含まれないように列挙しないとダメ
		var addBaseFilesBlock = new ActionBlock<string>( file => baseFiles.Add( file ), singleBlockOptions );
		readFilesBlock.LinkTo( addBaseFilesBlock, linkOptions );

		foreach( var projectFile in setting.ProjectFiles )
		{
			progress.Report( projectFile );
			await readFilesBlock.SendAsync( projectFile, token );
		}
		readFilesBlock.Complete();
		await addBaseFilesBlock.Completion;

		return baseFiles;
	}

	/// <summary>
	/// ファイル収集処理用のリストアップ
	/// </summary>
	/// <param name="setting">プロジェクト設定情報</param>
	/// <param name="progress">インジケータオブジェクト</param>
	/// <param name="token">キャンセルトークン</param>
	/// <returns>列挙したファイル情報リスト</returns>
	public static async Task<IEnumerable<TargetFileInformation>> ListCopyFilesAsync( ProjectSetting setting, IEnumerable<string> baseFiles, IProgress<int> progress, CancellationToken token )
	{
		var targetFiles = new List<TargetFileInformation>();
		if( setting.CopySettings == null )
		{
			return targetFiles;
		}

		var blockOptions = new ExecutionDataflowBlockOptions
		{
			CancellationToken = token,
			EnsureOrdered = false,
			MaxDegreeOfParallelism = -1,    //	制限なしでよいと思うのよ
		};
		var singleBlockOptions = new ExecutionDataflowBlockOptions
		{
			CancellationToken = token,
			EnsureOrdered = false,
			MaxDegreeOfParallelism = 1, //	直列で処理する(ロックするのは面倒だからね)
		};
		var linkOptions = new DataflowLinkOptions
		{
			PropagateCompletion = true,
		};
		var generateFilesBlock = new TransformBlock<string, TargetFileInformation>(
			filePath => GenerateFileInformation.Generate( setting.CopySettings, filePath, true ), blockOptions );

		var compareFilesBlock = new TransformBlock<TargetFileInformation, TargetFileInformation>(
			targetInfo => CompareFileInfo.CompareCopy( targetInfo, token ), blockOptions );

		int progressCount = 0;
		var addTargetFilesBlock = new ActionBlock<TargetFileInformation>(
			targetInfo =>
			{
				progress.Report( progressCount );
				progressCount++;
				targetFiles.Add( targetInfo );
			}, singleBlockOptions );

		generateFilesBlock.LinkTo( compareFilesBlock, linkOptions );
		compareFilesBlock.LinkTo( addTargetFilesBlock, linkOptions );

		foreach( var projectFile in baseFiles )
		{
			await generateFilesBlock.SendAsync( projectFile, token );
		}
		generateFilesBlock.Complete();
		await addTargetFilesBlock.Completion;

		return targetFiles;
	}


	public static async Task<List<TargetFileInformation>> ListNotSignedFilesAsync( ProjectSetting setting, List<string> baseFiles, IProgress<int> progress, CancellationToken token )
	{
		var targetFiles = new List<TargetFileInformation>();
		if( setting.SignerFileSetting == null )
		{
			return targetFiles;
		}
		var blockOptions = new ExecutionDataflowBlockOptions
		{
			CancellationToken = token,
			EnsureOrdered = false,
			MaxDegreeOfParallelism = -1,    //	制限なしでよいと思うのよ
		};
		var singleBlockOptions = new ExecutionDataflowBlockOptions
		{
			CancellationToken = token,
			EnsureOrdered = false,
			MaxDegreeOfParallelism = 1, //	直列で処理する
		};
		var linkOptions = new DataflowLinkOptions
		{
			PropagateCompletion = true,
		};
		var generateFilesBlock = new TransformBlock<string, TargetFileInformation>(
			filePath => GenerateFileInformation.Generate( [setting.SignerFileSetting], filePath, false ), blockOptions );

		var compareFilesBlock = new TransformBlock<TargetFileInformation, TargetFileInformation>(
			targetInfo => CompareFileInfo.CheckSignature( targetInfo, token ), blockOptions );

		int progressCount = 0;
		var addTargetFilesBlock = new ActionBlock<TargetFileInformation>(
			targetInfo =>
			{
				progress.Report( progressCount );
				progressCount++;
				targetFiles.Add( targetInfo );
			}, singleBlockOptions );

		generateFilesBlock.LinkTo( compareFilesBlock, linkOptions );
		compareFilesBlock.LinkTo( addTargetFilesBlock, linkOptions );

		foreach( var projectFile in baseFiles )
		{
			await generateFilesBlock.SendAsync( projectFile, token );
		}
		generateFilesBlock.Complete();
		await addTargetFilesBlock.Completion;

		return targetFiles;
	}
}
