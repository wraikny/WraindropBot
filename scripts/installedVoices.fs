#r "netstandard"
#r "nuget: System.Speech"

open System
open System.Speech.Synthesis

let synthesizer = new SpeechSynthesizer()
// synthesizer.SetOutputToDefaultAudioDevice()
// synthesizer.Speak("Hello")

for v in synthesizer.GetInstalledVoices() do
  let info = v.VoiceInfo
  
  printfn "Name: %s, Gender: %O, Age: %O, Enabled: %O" info.Description info.Gender info.Age v.Enabled
