# 使い方 - Wraindrop Bot

`<prefix>`は`!wd`等の設定したプレフィックスに読み替えてください。

## Commands

### join

ボイスチャンネルに参加します。
このコマンドを実行したテキストチャンネルに投稿された文章が自動で読み上げられます。

```
<prefix> join
```

エイリアス
* `j`

### leave

ボイスチャンネルから切断します。

```
<prefix> leave
```

エイリアス
* `l`

### name-get

サーバー毎の読み上げる名前を取得します。

```
<prefix> name-get
```

エイリアス
* `ng`

### name-set

サーバーで読み上げる名前を設定します。

```
<prefix> name-set <読み上げる名前>
```

エイリアス
* `ns`

### name-set-user

サーバーで指定したユーザを読み上げる名前を設定します。

```
<prefix> name-set-user <@対象のユーザ> <読み上げる名前>
```

エイリアス
* `nsu`

### name-delete

サーバーで読み上げる名前を消去します。

```
<prefix> name-delete
```

エイリアス
* `nd`

### name-delete-user

サーバーで指定したユーザを読み上げる名前を消去します。

```
<prefix> name-delete-user <@対象のユーザ>
```

エイリアス
* `ndu`

### speed-get

サーバーでの発話速度を取得します。

```
<prefix> speed-get
```

エイリアス
* `sg`

### speed-set

サーバーでの発話速度を設定します。(50~300)

```
<prefix> speed-set <発話速度>
```

エイリアス
* `ss`

### dict-list

読み上げ時に置換されるワードの一覧を取得します。

```
<prefix> dict-list
```

エイリアス
* `dl`

### dict-get

読み上げ時に置換されるワードを取得します。

```
<prefix> dict-get <対象のワード>
```

エイリアス
* `dg`

### dict-set

読み上げ時に置換されるワードを追加・更新します。

```
<prefix> dict-set <対象のワード> <置換するワード>
```

### dict-delete

読み上げ時に置換されるワードを削除します。

```
<prefix> dict-delete <対象のワード>
```

エイリアス
* `dd`

### dict-clear

読み上げ時に置換されるワードをすべて削除します。

```
<prefix> dict-clear
```
