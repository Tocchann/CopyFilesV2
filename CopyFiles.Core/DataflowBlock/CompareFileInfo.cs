using CopyFiles.Core.Const;
using CopyFiles.Core.Models;
using PeFileAccessor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CopyFiles.Core.DataflowBlock;

public static class CompareFileInfo
{
	// コピーのための比較を行う
	public static TargetFileInformation CompareCopy( TargetFileInformation targetInfo, CancellationToken token )
	{
		// ベースを最新にするために参照先からコピーしてくるので参照先は絶対にあるはず
		if( !targetInfo.ReferFileInfo.Exists )
		{
			throw new FileNotFoundException( targetInfo.ReferFileInfo.FilePath );
		}
		// ベースにファイルが有る場合は更新する必要があるかをチェックするのでハッシュ値なども駆使して比較する
		if( targetInfo.BaseFileInfo.Exists )
		{
			// ファイルサイズと日付が同じかどうかは調査対象としない
			using( var hashAlgorithm = SHA256.Create() )
			{
				var baseHash = GetFileHash( hashAlgorithm, targetInfo.BaseFileInfo.FilePath, token );
				var referHash = GetFileHash( hashAlgorithm, targetInfo.ReferFileInfo.FilePath, token );
				// 内容的に一致とみなせる場合
				if( baseHash == referHash )
				{
					// サイズが一致してハッシュが同じ場合はファイル全体が一致している
					if( targetInfo.BaseFileInfo.FileSize == targetInfo.ReferFileInfo.FileSize )
					{
						// 日付が同じなら完全一致
						if( targetInfo.BaseFileInfo.LastWriteTime == targetInfo.ReferFileInfo.LastWriteTime )
						{
							targetInfo.CompareStatus = CompareStatus.Match;
						}
						// 内容は同じで日付が異なる(ビルドしたけど、中身変わらずなパターン)
						else
						{
							targetInfo.CompareStatus = CompareStatus.MatchWithoutDate;
						}
					}
					// サイズが違う場合は、署名の有無が異なると判断する(署名があるかは考慮しない)
					else
					{
						targetInfo.CompareStatus = CompareStatus.MatchWithoutSignature;
					}
				}
				// 内容が一致していない
				else
				{
					// 内容は違っているがバージョンが同じ == ビルドしたら中身が変わった
					if( targetInfo.ReferFileInfo.FileVersion != null &&
						targetInfo.ReferFileInfo.FileVersion == targetInfo.BaseFileInfo.FileVersion )
					{
						targetInfo.CompareStatus = CompareStatus.UnMatchSameVersion;
					}
					// バージョンがないか一致していないので、普通に異なるファイルという認識でよい
					else
					{
						targetInfo.CompareStatus = CompareStatus.UnMatch;
					}
				}
			}
		}
		// ベースにない場合は新規ファイルなので比較不要
		else
		{
			targetInfo.CompareStatus = CompareStatus.NewFile;
		}
		token.ThrowIfCancellationRequested();
		return targetInfo;
	}
	public static TargetFileInformation CheckSignature( TargetFileInformation targetInfo, CancellationToken token )
	{
		token.ThrowIfCancellationRequested();
		if( targetInfo.BaseFileInfo.Exists )
		{
			var fileImage = File.ReadAllBytes( targetInfo.BaseFileInfo.FilePath );
			token.ThrowIfCancellationRequested();
			if( PeFile.IsValidPE( fileImage ) )
			{
				targetInfo.CompareStatus = PeFile.IsSetSignatgure( fileImage ) ? CompareStatus.ExistSignature : CompareStatus.NotExistSignature;
			}
			else
			{
				// 署名していないので何もしない
				targetInfo.CompareStatus = CompareStatus.NoAction;
			}
		}
		return targetInfo;
	}
	private static string GetFileHash( HashAlgorithm hashAlgorithm, string filePath, CancellationToken token )
	{
		// 計算処理はオンメモリで行う(いろいろ面倒なのでね)
		var fileImage = File.ReadAllBytes( filePath );
		token.ThrowIfCancellationRequested();
		var hashImage = PeFile.GetHashSourceBytes( fileImage );
		token.ThrowIfCancellationRequested();
		var hashBytes = hashAlgorithm.ComputeHash( hashImage );
		token.ThrowIfCancellationRequested();
		//	比較を単純な文字列の比較にするため、16進数文字列に変換する
		var result = Convert.ToHexString( hashBytes ).ToLower();
		return result;
	}
}
