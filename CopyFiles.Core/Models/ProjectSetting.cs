namespace CopyFiles.Core.Models;

public class ReferFolder
{
	public required string BaseFolder { get; set; }
	public required string ReferenceFolder { get; set; }
}
public class ProjectSetting
{
	public required string Name { get; set; }
	// 実際のコピー対象を決めるためのプロジェクトファイル一覧
	public required List<string> ProjectFiles { get; init; }

	// ビルド結果とインストーラ参照パス間でコピーするための設定
	public required List<ReferFolder> CopySettings { get; init; }
	// コピー対象のみ表示の絞り込みフラグ
	public bool IsNeedCopy { get; set; }

	// 署名用ファイル設定(コピー元は圧縮ファイル対象ベースパス、コピー先は圧縮ファイルを作成するフォルダ)
	public required ReferFolder SignerFileSetting { get; init; }
	// 圧縮ファイルにつけるプレフィックス
	public required string ZipFileNamePrefix { get; init; }
	// 圧縮対象のみ表示の絞り込みフラグ
	public bool IsNeedArchive { get; set; }
}
