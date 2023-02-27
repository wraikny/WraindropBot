# 開発 - Wraindrop Bot

## 開発環境（Windows）

### .NET6

以下からダウンロード・インストールできます。

[.NET6](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)

`dotnet`コマンドが実行できることを確認してください。

```sh
dotnet --version
6.0.101
```

リポジトリで次のコマンドを実行して、開発ツールをインストールします。

```sh
dotnet tool restore
```

### ffmpeg

ffmpegのインストールが必要です。

Scoopを利用している場合は以下のコマンドでインストール可能です。

```sh
scoop install ffmpeg
```

### デバッグ

開発用botのトークンを入力した`config_debug.json`を用意して、以下のコマンドを実行します。

```sh
dotnet run --project src/WraindropBot config_debug.json
```

### ビルド

Raspbian用のバイナリをビルドします。

```sh
dotnet fake build
dotnet fake build -t publish.raspbian
```

`publish/WraindropBot.linux-arm`以下にファイルが出力されます。

```
WraindropBot.linux-arm
|--libe_sqlite3.so
|--WraindropBot
```

テンプレートを元に設定ファイルを作成します。

```sh
cp config_template.json publish/config.json
```

`config.json`の`token`にDiscord botのトークンを入力しておきます。
また、`translationUrl`に、Google App ScriptのURLを指定します。作成方法は[Google翻訳APIを無料で作る方法](https://qiita.com/satto_sann/items/be4177360a0bc3691fdf)と[google_translate_api.js](/scripts/google_translate_api.js)を参考にしてください。

### ラズパイにファイルを移す

SSH (`scp`コマンド)を利用します。

例えば以下のような`scripts/deploy.cmd`を用意して実行すると便利です。

```cmd
cd /d %~dp0
cd ..
scp -r publish/config.json publish/WraindropBot.linux-arm/* pi@wkpi.local:~/MyPrograms/WraindropBot/.
```

`pi@wkpi.local`と`~/MyPrograms/WraindropBot`は自分の環境で読み替えてください。

## 本番環境（Raspbian）の準備

### Raspberry Pi

Raspbian OSをインストールしてSSHの設定を済ませておきます。

### 依存ライブラリのインストール

#### ffmpeg

```sh
sudo apt-get install ffmpeg
```

#### libsodium, Opus
`DSharpPlus.VoiceNext`に必要なライブラリをインストールします。

バージョン共通
```sh
sudo apt-get install libopus0 libopus-dev
```

Raspbian 10の場合 ※詳しくは以下のURLを参考にしてください。

```sh
sudo apt-get install libsodium23 libsodium-dev
```

[ArticlesAudioVoiceNextPrerequisites - DSharpPlus](https://dsharpplus.github.io/articles/audio/voicenext/prerequisites.html)

#### AquesTalk Pi

AquestTalk Pi は個人の非営利でのみ無償で使用できます。
それ以外の場合はライセンスを購入する必要があります。

無償版は以下のコマンドでインストールできます。

```sh
cd /home/pi
wget https://www.a-quest.com/archive/package/aquestalkpi-20201010.tgz
zcat aquestalkpi-20201010.tgz | tar xv
rm aquestalkpi-20201010.tgz
chmod +x aquestalkpi/AquesTalkPi
```

### 実行

tmuxなどを利用すると良いです。

```sh
sudo apt-get install tmux
tmux new -s wraindrop
```

```sh
cd ~/MyPrograms/WraindropBot
chmod +x WraindropBot
./WraindropBot config.json
```

`Ctrl + B`を押したあと`D`を押してデタッチ。

セッションを再開するときは

```sh
tmux a [ -t wraindrop ]
```
