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
	public static async Task<List<string>> ListSourceFilesAsync( ProjectSetting setting, IProgress<string> progress, CancellationToken token )
	{
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
		var baseFiles = new List<string>();
		var addBaseFilesBlock = new ActionBlock<string>( filePath =>
		{
			progress.Report( filePath );
			baseFiles.Add( filePath );
		}, singleBlockOptions );
		readFilesBlock.LinkTo( addBaseFilesBlock, linkOptions );
		foreach( var projectFile in setting.ProjectFiles )
		{
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
	public static async Task<List<TargetFileInformation>> ListCopyFilesAsync( ProjectSetting setting, List<string> baseFiles, IProgress<int> progress, CancellationToken token )
	{
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
		var targetFiles = new List<TargetFileInformation>();
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
		var targetFiles = new List<TargetFileInformation>();
		var generateFilesBlock = new TransformBlock<string, TargetFileInformation>(
			filePath => GenerateFileInformation.Generate( setting.CopySettings, filePath, false ), blockOptions );

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
