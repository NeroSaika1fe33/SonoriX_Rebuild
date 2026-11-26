# SonoriX 開発手順

作品の概要と新しい譜面解析器については、[リポジトリの README](../README.md) をご覧ください。このページでは、Unity と FMOD の構成、プロジェクトの開き方をまとめています。

## コードの構成

| 名前空間 | ディレクトリ | 役割 |
| --- | --- | --- |
| `FFrameWork.*` | `Assets/Scripts/FFrameWork/` | 既存のゲームと演出を動かす旧実装 |
| `SonoriX.*` | `Assets/Scripts/SonoriX/` | 譜面解析、演奏処理、入力、表示を分けて再設計している新実装 |

両方のコードが Unity のコンパイル対象になっています。新実装は Domain、Runtime、Infrastructure、App、Presentation に分けています。一部の開発用スクリプトはグローバル名前空間にあり、`LiveRuntimeTestDriver` が新実装のテスト入口です。

## プロジェクトを開く

Unity **6000.4.5f1** を使用します。バージョンは [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt) で確認できます。

Git LFS をインストールしてから、次の手順で取得します。

```powershell
git lfs install
git clone https://github.com/NeroSaika1fe33/SonoriX_Rebuild.git
Set-Location SonoriX_Rebuild
git lfs pull
```

Unity と FMOD Studio のプロジェクトは、次の位置関係で管理しています。

```text
SonoriX_Rebuild/
├── SonoriX/                 Unity プロジェクト
└── SonoriX_Fmod_Project/     FMOD Studio プロジェクト
    ├── SonoriX.fspro
    ├── Metadata/
    ├── Assets/
    └── Build/
```

Unity Hub から内側の `SonoriX/` を開き、パッケージの復元とアセットの読み込みが終わるまで待ちます。

## シーンの使い分け

- `Assets/Scenes/xxAxxLive.unity`：旧実装のゲームシーン。現在のビルド設定で有効になっています。
- `Assets/Scenes/SonoriX.unity`：新実装のテストシーン。`LiveRuntimeTestDriver` で譜面や入力を設定し、`ManualLiveClock` を使って演奏処理を進めます。

新実装のテストでは、`LiveRuntimeTestDriver` の `Chart Text` に解析対象のテキストを設定します。必要な参照を確認して `Auto Boot` を有効にし、Play モードで起動します。Play モード中にやり直す場合は、コンポーネントのコンテキストメニューから `Restart Test Runtime` を実行します。

譜面解析の確認処理は、Unity Editor の `SonoriX → Checks → Chart Compiler` から実行できます。

## FMOD の設定

リポジトリには **FMOD for Unity 2.03.14**、設定、編集用音声プロジェクト、Bank を含めています。音声を編集する場合は、別途 [FMOD Studio 2.03.14](https://www.fmod.com/download) をインストールします。

1. FMOD Studio で `SonoriX_Fmod_Project/SonoriX.fspro` を開きます。`Metadata/` と音声素材の `Assets/` も一緒に保管してください。
2. Unity の `FMOD → Edit Settings` で、プロジェクトのパスを `../SonoriX_Fmod_Project/SonoriX.fspro` に設定します。Bank フォルダーを指定する場合は `../SonoriX_Fmod_Project/Build` を使います。どちらも Unity プロジェクトからの相対パスです。
3. 音声を変更したら、Studio の `File → Build All Platforms` で Bank を生成し、Unity 側でも更新します。
4. 編集用プロジェクトと、更新した Bank を一緒に管理します。`Assets/StreamingAssets/` の再生用 Bank も確認してください。

Bank は再生用のデータです。あとから音声を編集できるように、元の FMOD Studio プロジェクトも残しています。

音声が読み込めない場合は、まず `git lfs pull` が完了しているかを確認します。Unity から Studio が起動しない場合は、本機に Studio がインストールされているかを確認し、必要に応じて `.fspro` を Studio から直接開いてください。

## ファイルの管理

Unity と FMOD は、外側の一つの Git リポジトリで管理しています。旧実装の修正と新実装の開発は、変更内容ごとにコミットを分けています。

音声、動画、Bank、一部のネイティブライブラリは Git LFS で管理します。リポジトリ直下の `.gitattributes` を保持し、アセットと対応する `.meta` は一緒に扱ってください。Unity のキャッシュやソフトウェアのインストール先は管理対象に含めません。

長期間安定版を保ちながら作業する場合は、用途に応じてブランチを分けます。ブランチの切り替えは、Unity と FMOD の両方に反映されます。
