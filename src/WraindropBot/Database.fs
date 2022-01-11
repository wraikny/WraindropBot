module WraindropBot.Database

open System
open System.Data
open System.Data.SQLite
open System.Text
open System.Threading.Tasks
open Dapper

type ConnStr = ConnStr of string

let createConnectionStr dataSource =
  SQLiteConnectionStringBuilder(DataSource = dataSource)
    .ToString()
  |> ConnStr

let private execute (ConnStr connStr) (sql: string) (param: obj) =
  task {
    use conn = new SQLiteConnection(connStr)
    do! conn.OpenAsync()
    use! trans = conn.BeginTransactionAsync()

    try
      let! count = conn.ExecuteAsync(sql, param, trans)

      do! trans.CommitAsync()
      return count
    with
    | e ->
      do! trans.RollbackAsync()
      raise e
      return 0
  }

let private query<'a> (ConnStr connStr) (sql: string) (param: obj) =
  task {
    use conn = new SQLiteConnection(connStr)
    do! conn.OpenAsync()
    return! conn.QueryAsync<'a>(sql, param)
  }

let setupDatabase (connStr) =
  execute
    connStr
    """
create table if not exists users (
  guildId integer not null,
  userId integer not null,
  name text default null,
  speakingSpeed integer not null default 100,
  primary key (guildId, userId)
);

create table if not exists words (
  guildId integer not null,
  word text not null,
  replaced text not null,
  primary key (guildId, word)
);"""
    null
  :> Task

[<CLIMutable>]
type User =
  { guildId: uint64
    userId: uint64
    name: string
    speakingSpeed: int }

module User =
  let init guildId userId name speakingSpeed =
    { guildId = guildId
      userId = userId
      name = name
      speakingSpeed = speakingSpeed }

[<CLIMutable>]
type Word =
  { guildId: uint64
    word: string
    replaced: string }

module Word =
  let init guildId word replaced =
    { guildId = guildId
      word = word
      replaced = replaced }


[<AllowNullLiteral>]
type DatabaseHandler(connStr: ConnStr) =
  member _.GetUser(guildId: uint64, userId: uint64) =
    task {
      let! user =
        query<User>
          connStr
          "select * from users where guildId = @guildId and userId = @userId;"
          {| guildId = guildId; userId = userId |}

      return Seq.tryHead user
    }

  member _.SetUserName(guildId: uint64, userId: uint64, name: string) =
    let sql =
      """
insert into users (userId, guildId, name)
values (@userId, @guildId, @name) 
on conflict (userId, guildId)
do update
  set name = @name
;"""

    execute
      connStr
      sql
      {| guildId = guildId
         userId = userId
         name = name |}
    :> Task

  member _.SetUserSpeed(guildId: uint64, userId: uint64, speed: int) =
    let speed = speed |> WDConfig.validateSpeed

    let sql =
      """
insert into users (userId, guildId, speakingSpeed)
values (@userId, @guildId, @speakingSpeed)
on conflict (userId, guildId)
do update
  set speakingSpeed = @speakingSpeed
;"""

    task {
      let! _ =
        execute
          connStr
          sql
          {| guildId = guildId
             userId = userId
             speakingSpeed = speed |}

      return speed
    }

  member _.GetWords(guildId: uint64) =
    task {
      let! words = query<Word> connStr "select * from words where guildId = @guildId;" {| guildId = guildId |}

      return words
    }

  member _.GetWord(guildId: uint64, word: string) =
    task {
      let! words =
        query<Word>
          connStr
          "select * from words where guildId = @guildId and word = @word;"
          {| guildId = guildId; word = word |}

      return words |> Seq.tryHead
    }

  member _.SetWord(guildId: uint64, word: string, replaced: string) =
    let sql =
      """
insert into words (guildId, word, replaced)
values (@guildId, @word, @replaced)
on conflict (guildId, word)
do update
  set replaced = @replaced
;"""

    execute
      connStr
      sql
      {| guildId = guildId
         word = word
         replaced = replaced |}
    :> Task

  member _.DeleteWord(guildId: uint64, word: string) =
    task {
      let! count =
        execute
          connStr
          "delete from words where guildId = @guildId and word = @word"
          {| guildId = guildId; word = word |}

      return count <> 0
    }

  member _.DeleteWords(guildId: uint64) =
    execute connStr "delete from words where guildId = @guildId" {| guildId = guildId |}
