# Wraindrop bot

## Environment
* .NET6 （開発環境）
* ffmpeg
  * 例えば `sudo apt install ffmpeg` や `scoop install ffmpeg` などとする。
* libsodium and Opus
  * [ArticlesAudioVoiceNextPrerequisites - DSharpPlus](https://dsharpplus.github.io/articles/audio/voicenext/prerequisites.html)
* AquesTalk (Raspberry Pi)

## ビルド

### setup

```sh
dotnet tool restore
dotnet fake build
```

### Format

```sh
dotnet fake build -t format
```

### Publish for Raspbian

```sh
dotnet fake build -t publish.raspbian
```

## RaspberryPiでAquesTalk Piをインストールする

AquestTalk Pi は個人の非営利でのみ無償で使用できます。

```sh
cd /home/pi
wget https://www.a-quest.com/archive/package/aquestalkpi-20201010.tgz
zcat aquestalkpi-20201010.tgz | tar xv
rm aquestalkpi-20201010.tgz
chmod +x aquestalkpi/AquesTalkPi
```

# Reference
* [DSharpPlus](https://dsharpplus.github.io/)
* [Google Cloud TTSとDSharpPlusを使って代読DiscordボットをF#で作る](https://anqou.net/teqblog/2020/12/make-tts-discord-bot-in-fstar-using-google-cloud-tts-and-dsharp-plus/)
