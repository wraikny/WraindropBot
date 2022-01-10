namespace WraindropBot

open DSharpPlus.Entities
open DSharpPlus.CommandsNext.Converters

[<Sealed>]
type WDHelpFormatter(ctx) =
  inherit DefaultHelpFormatter(ctx)

  override this.Build() =
    this.EmbedBuilder.Color <- DiscordColor.SpringGreen
    base.Build()
