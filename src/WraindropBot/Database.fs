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
      let! _ = conn.ExecuteAsync(sql, param, trans)

      do! trans.CommitAsync()
    with
    | e ->
      do! trans.RollbackAsync()
      raise e
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
    """create table if not exists users (
  guildId integer not null,
  userId integer not null,
  name text default null,
  speakingSpeed integer not null default 100,
  primary key (guildId, userId)
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
      do!
        execute
          connStr
          sql
          {| guildId = guildId
             userId = userId
             speakingSpeed = speed |}

      return speed
    }
