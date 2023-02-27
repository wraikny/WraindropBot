# 使い方 - Wraindrop Bot

`!wd`は指定したprefixに読み替えてください。

- [使い方 - Wraindrop Bot](#使い方---wraindrop-bot)
  - [Commands](#commands)
    - [join](#join)
    - [leave](#leave)
    - [name-get](#name-get)
    - [name-set](#name-set)
    - [name-set-user](#name-set-user)
    - [name-delete](#name-delete)
    - [name-delete-user](#name-delete-user)
    - [speed-get](#speed-get)
    - [speed-set](#speed-set)
    - [dict-list](#dict-list)
    - [dict-get](#dict-get)
    - [dict-set](#dict-set)
    - [dict-delete](#dict-delete)
    - [dict-clear](#dict-clear)
    - [translate](#translate)

## Commands

### join

ボイスチャンネルに参加します。
このコマンドを実行したテキストチャンネルに投稿された文章が自動で読み上げられます。

```
!wd join
```

エイリアス
* `j`

### leave

ボイスチャンネルから切断します。

```
!wd leave
```

エイリアス
* `l`

### name-get

サーバーで読み上げる名前を取得します。

```
!wd name-get
```

エイリアス
* `ng`

### name-set

サーバーで読み上げる名前を設定します。

```
!wd name-set <読み上げる名前>
```

エイリアス
* `ns`

### name-set-user

サーバーで指定したユーザを読み上げる名前を設定します。

```
!wd name-set-user <@対象のユーザ> <読み上げる名前>
```

エイリアス
* `nsu`

### name-delete

サーバーで読み上げる名前を消去します。

```
!wd name-delete
```

エイリアス
* `nd`

### name-delete-user

サーバーで指定したユーザを読み上げる名前を消去します。

```
!wd name-delete-user <@対象のユーザ>
```

エイリアス
* `ndu`

### speed-get

サーバーでの発話速度を取得します。

```
!wd speed-get
```

エイリアス
* `sg`

### speed-set

サーバーでの発話速度を設定します。(50~300)

```
!wd speed-set <発話速度>
```

エイリアス
* `ss`

### dict-list

読み上げ時に置換されるワードの一覧を取得します。

```
!wd dict-list
```

エイリアス
* `dl`

### dict-get

読み上げ時に置換されるワードを取得します。

```
!wd dict-get <対象のワード>
```

エイリアス
* `dg`

### dict-set

読み上げ時に置換されるワードを追加・更新します。

```
!wd dict-set <対象のワード> <置換するワード>
```

エイリアス
* `ds`

### dict-delete

読み上げ時に置換されるワードを削除します。

```
!wd dict-delete <対象のワード>
```

エイリアス
* `dd`

### dict-clear

読み上げ時に置換されるワードをすべて削除します。

```
!wd dict-clear
```

エイリアスはありません。

### translate

文章を指定した言語に翻訳（Google翻訳）して返信します。

言語コード: https://cloud.google.com/translate/docs/languages

```
!wd traslate <言語コード> <文章>
```

エイリアス
* `t`
