# SonoriX_Rebuild

Unity と FMOD を使ったリズムゲーム「SonoriX」の再設計プロジェクトです。以前の制作で学んだ譜面解析や演奏処理を、自分で理解し、変更しやすい構成に組み直しています。現在は、自作の譜面解析器と演奏ロジックの実装を中心に開発しています。

## 取り組んでいること

### 1. 自作の譜面解析器

OpenSekai の譜面解析から学んだ考え方を参考に、ゲームの仕様に合わせた SNX 形式の解析処理を設計しています。

テキストを読み取る段階と、ゲームで使うデータへ変換する段階を分けています。小節・Tick で表した位置から BPM や拍子をもとに秒数を求め、単発ノーツ、ロングノーツ、演出イベントへ変換します。ロングノーツでは始点・中継点・終点の対応も確認し、不正な記述はエラーとして検出します。

### 2. 処理の役割を分けた構成

譜面データとルールを扱う Domain、演奏進行を扱う Runtime、入力やテキスト読み込みを扱う Infrastructure、シーンを制御する App、画面表示を扱う Presentation に分けています。

入力や時刻の取得を演奏処理の外から渡す構成にし、特定のシーンや表示に依存せずにロジックを確認できるようにしています。

### 3. 解析結果を確認する仕組み

Unity Editor の `SonoriX → Checks → Chart Compiler` に、テキスト解析、譜面データへの変換、不正入力の検出を確認する処理を用意しています。対応する記述を増やしながら、解析結果を確認できるようにしています。

## 主なコード

| クラス | 主なメソッド | 役割 |
| --- | --- | --- |
| [SnxChartParser](SonoriX/Assets/Scripts/SonoriX/Infrastructure/Charts/SnxChartParser.cs) | `Parse()` | テキストからノーツやイベントの元データを読み取る |
| [ChartSourceCompiler](SonoriX/Assets/Scripts/SonoriX/Domain/Charts/ChartSourceCompiler.cs) | `Compile()` | 元データを演奏用の譜面データへ変換する |
| [ChartTimingMap](SonoriX/Assets/Scripts/SonoriX/Domain/Charts/ChartTimingMap.cs) | `ToSeconds()` | BPM・拍子の変化を反映して譜面位置を秒数へ変換する |
| [LiveRuntime](SonoriX/Assets/Scripts/SonoriX/Runtime/LiveRuntime.cs) | `Tick()` | 譜面進行、判定、スコア更新をまとめる |
| [ChartCompilerCheck](SonoriX/Assets/Scripts/SonoriX/Editor/ChartCompilerCheck.cs) | `Run()` | 解析と変換の確認処理を実行する |

## 現在の開発状況

旧実装の `FFrameWork.*` と、新実装の `SonoriX.*` が同じ Unity プロジェクト内にあります。新実装への移行は進行中です。

- 旧実装のシーン：`SonoriX/Assets/Scenes/xxAxxLive.unity`。現在のビルド設定はこちらを使用しています。
- 新実装のテストシーン：`SonoriX/Assets/Scenes/SonoriX.unity`。`LiveRuntimeTestDriver` を入口に、開発用の `ManualLiveClock` で動作を確認する構成です。

新実装の音楽再生との接続や、既存のゲーム全体を置き換える作業は、引き続き進めています。

## 開発環境と起動手順

Unity **6000.4.5f1**、C#、FMOD for Unity **2.03.14** を使用しています。

Git LFS で管理しているファイルを取得し、Unity Hub から内側の `SonoriX/` フォルダーを開いてください。FMOD Studio の編集用プロジェクトは `SonoriX_Fmod_Project/` にあります。

詳しい構成とセットアップは [Unity プロジェクトの開発手順](SonoriX/README.md) に記載しています。

## 参照元と開発方法

旧実装では、[OpenSekai](https://github.com/cubeww/OpenSekai) の [LiveLogic.cs](https://github.com/cubeww/OpenSekai/blob/32c91d90c7e18135fef1a214144cce3fb756b3e5/Assets/Scripts/Assembly-CSharp/Sekai/Core/Live/LiveLogic.cs) と [Converter.cs](https://github.com/cubeww/OpenSekai/blob/32c91d90c7e18135fef1a214144cce3fb756b3e5/Assets/Scripts/Assembly-CSharp/Sekai/SUS/Converter.cs) を主な基盤として利用・改修しています。表示や入力などの周辺実装にも、参考にした部分や改修して利用した部分があります。

新しい解析器は、そこで学んだ処理の考え方を参考にしながら、自分の仕様とデータ構造に合わせて設計しています。設計の検討や実装の補助には AI も活用しています。

FMOD、音楽、画像、フォントなどには、それぞれの利用条件が適用されます。

FMOD by Firelight Technologies Pty Ltd.
