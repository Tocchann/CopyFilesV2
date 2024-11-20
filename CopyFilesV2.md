# CopyFiles Ver.2 に向けて

## 現行かかえる不便な点

- 選択コピー機能がない
    - 初期につけたチェックマークはやっぱりあったほうが便利
    - WPF の複数選択問題があるがそれはこのツールではあまり効果はない気がする。
- 署名処理をコピーではなく圧縮にしたい
    - 署名対象ファイルをコピーだと別途圧縮処理がいるので直接ZIPに圧縮してしまいたい
- 筆まめのインストールプロジェクトでも使えるようにしたい
    - .ism を対象とするだけでなく、.wixproj も対象にできると嬉しい
    - .wixproj も v4 以上を対象にできるか？
    - そのためには筆まめの WiX 依存バージョンを最新化しないとダメではないか？
- 見た目の改善
    - SuperDCopy みたいな階層構造で表示ができるとよい？
        - ヘッダーコントロールがなくてもいいなら Grid の IsSharedSizeScope 
    - コピー元・コピー先の日付も表示したほうが良いでしょう
    - アイコンがあるとよい？(無くてもいいと思うけどどうなんだろう？)
- CLI 版が有ったら便利か？
    - 自動化という点では便利だろうけど、ノーチェックで動かせるのは、署名処理くらいなのであまり意味はない

## 処理の流れを考える

処理ごとにメソッドを用意してそれらをまとめて Dataflow で流せるようにした

クラスはここにあるものとは異なる形で実装した

### コピー処理

必要ファイルの収集
- .ism を解析して取込(xml形式のみ対応)
- .exe を指定(直接パス指定)
- .dll を指定(直接パス指定)
- .sln(or .wixproj)を指定(VSのプロジェクト情報を解析するのだが当面未対応)
    - これは最終的にもやらないかもしれない(わからんが…)

必要ファイルのコピー元の算出
- コピー先、コピー元のセットから、コピー元のパスを探す

コピー条件の算出
- コピー元がない場合は、指定漏れとしてエラーにしたほうが良い
- コピー先にない場合は無条件コピー
- コピー先と完全一致(ハッシュの完全一致)はコピーしない(同一バイナリのため)
    - 日付が違った場合は、日付だけ変わっていることを告知？(しなくていい？)
    - 署名がある場合は、署名用のエリアのハッシュを比較(一致したらコピー不要)
    - 日付が違うだけの場合はコピー対象外(ビルドはされててもコピーしない)
```C#
// コピー条件フラグ より細かくつけるかどうかが悩ましい
enum TargetStatus
{
    // 未調査
    Unknown,
    // 完全一致
    Match,
    // 署名エリアのハッシュが一致(サイズは異なる)
    MatchWithSignArea,
    // 日付が違うだけ
    MatchWithoutDate,
    // 内容が違うがバージョンが一致
    UnMatchSameVersion,
    // 内容が違う
    UnMatch,
    // コピー先にない
    NewFile,
    // 署名無し(署名付け用)
    NotSigned,
}
```
## データクラスをちゃんと作る

モデル的な形でデータクラスを定義する
保存対象クラス
``` C#
class TargetFolder{
    public string Source { get; set; }
    public string Destination { get; set; }
    public bool ExitWithOpenFolder { get; set; }
}
```
``` C#
class TargetFolderWithArchiveFlag : TargetFolder
{
    public bool RequireArchive { get; set; }
    public string FilenamePrefix { get; set; }
    // ファイル名は自動生成できるようにしておく
    public ZipArchive CreateArvchive()
    {
        var toDay = DateTime.Today.ToString("yyyyMMdd");
        var fileNode = $"{FilenamePrefix}_{toDay}";
        var filePath = Path.Combine( Destination, fileNode + ".zip" );
        if( File.Exists( filePath ) )
        {
            for( int dupCount = 1 ; dupCount < int.Max ; dupCount++ )
            {
                filePath = Path.Combine( Destination, fileNode + $"_{dupCount}.zip" );
                if( !File.Exists( filePath ) )
                {
                    break;
                }
            }
        }
        if( File.Exists( filePath ) )
        {
            // HRESULT をどうするか？を考えないといけないよねぇ…
            throw new IOException( "重複ファイル名が多すぎます" );
        }
        // 作成モードで開く
        return ZipFile.Open( filePath, ZipArchiveMode.Create );
    }
}
```
新たにジェネレータークラスをきちんと分離して用意するべきだろう
``` C#
// TargetFolder を受け取って、対象ファイル一覧を生成するクラス
class CopyFilesGenerator
{
    public TargetFolder TargetFoler { get; set; }
    public IProgress<string> Progress { get; set; }
    public CancellationToken Token { get; set; }

    public CopyFilesList TargetFiles{ get; }

    public async void GenerateAsync( CancellationToken token );
}
```
``` C#
// 署名の必要なファイルの一覧を生成するクラス
class SignFilesGenerator
{
    public TargetFolder TargetFoler { get; set; }
    public IProgress<string> Progress { get; set; }
    public CancellationToken Token { get; set; }

    public CopyFilesList TargetFiles{ get; }

    public async void GenerateAsync( CancellationToken token );
}
```
ジェネレータで生成するデータ一覧(VMに表示させるためのデータモデル)
``` C#
class RelativeFileInformation
{
    [Required]
    public string Folder { get; }
    [Required]
    public string FileName { get; }
    [Required]
    public TargetStatus TargetStatus{ get; }
    
    public string GetFullPath( string baseFolder )
    {
        return Path.Combine( baseFolder, Folder, FileName );
    }
}
```
``` C#
// 操作対象一覧リスト・署名用の同じものを生成する。表示リストはここから絞り込んで作る
class CopyFilesList
{
    public string SourceBase { get; }
    public string DestinationBase { get; }
    public List<RelativeFileInformation> TargetFiles { get; } // コピー先がユニークになっていればよい(コピー元の重複はあり)
}
```
これらのほかに実際のMVVMのモデルになるクラスを用意する。  
もしかしたら、ジェネレータをそのクラスのメンバーにしてしまうかもしれないが現状不明

モデルクラスとは別にデータ保存クラスも用意する。  
こちらは、現行版同様バージョンごとにデータ管理できるようにしておくこと。  
基本的には現行版と同じ構造でよいと思う
TargetFiles はコピー先がユニークになる必要がある(重複は破綻)
TargetFiles をツリー的に表現する場合は、Destination を基準に処理することになる
