module Update

open Elmish
open Fable.Core
open Fable.Core.JsInterop
open Fable.Import
open Common
open Model
open Model.Enums
open View

let init _ = Game.Fresh(), Cmd.none

module Sounds =
    open Browser.Types
    [<Emit("new Audio($0)")>]
    let sound file : HTMLAudioElement = jsNative
    let private cheers = [|
                    sound("London1.mp3")
                    sound("London10.mp3")
                    sound("London6.mp3")
                    sound("London7.mp3")
                    |]
    let play (sound: HTMLAudioElement) =
        if sound.ended then sound.play()
        else
            sound.currentTime <- 0.
            sound.play()
    let cheer() =
        chooseRandom cheers |> play
    let private bombs = [|
        sound("London8.mp3")
        sound("London9.mp3")
        sound("London13.m4a") |]
    let bomb() =
        let s = chooseRandom bombs
        play s
open Sounds

let update msg model =
    match msg with
    | UserMessage msg ->
        { model with messageToUser = msg }, Cmd.none
    | ToggleOptions ->
        let showOptions = not model.showOptions
        if not showOptions then // done button was hit, so persist settings
            let encode = Thoth.Json.Encode.Auto.toString(1, model.settings)
            Browser.WebStorage.localStorage.["settings"] <- encode
        { model with showOptions = showOptions }, Cmd.none
    | Reset -> { Game.Fresh() with showOptions = model.showOptions }, Cmd.none
    | AnswerKey k ->
        if model.showOptions && k = Enter then
            model, Cmd.ofMsg ToggleOptions
        elif model.showOptions || not (Model.Enums.keysOf model.settings.mathBase |> Array.exists ((=) k)) then
            model, Cmd.none
        else
            match k with
            | Number key ->
                let model = { model with currentAnswer = model.currentAnswer + key }
                model, if model.settings.autoEnter && model.currentAnswer.Length = model.problem.answer.Length then Cmd.ofMsg (AnswerKey Enter) else Cmd.none
            | Backspace ->
                { model with currentAnswer = model.currentAnswer.Substring(0, max 0 <| model.currentAnswer.Length - 1) }, Cmd.none
            | Enter ->
                let onCorrect =
                    match model.settings.sound with
                    | On | CheerOnly -> cheer
                    | _ -> ignore
                let onIncorrect =
                    match model.settings.sound with
                    | On | BombOnly -> bomb
                    | _ -> ignore
                Game.TryAdvance model onCorrect onIncorrect, Cmd.none
            | HintKey ->
                { model with showHints = not model.showHints }, Cmd.none
    | Setting msg ->
        let settings = model.settings
        let settings' =
            match msg with
            | SettingChange.Sound v -> { settings with sound = v }
            | SettingChange.AutoEnter v -> { settings with autoEnter = v }
            | SettingChange.ProgressiveDifficulty v -> { settings with progressiveDifficulty = v }
            | SettingChange.MathBase v -> { settings with mathBase = v }
            | SettingChange.Operation v -> { settings with mathType = v }
            | SettingChange.Maximum v -> { settings with size = v }
        match msg with
        | Operation _ | Maximum _ | MathBase _ ->
            { model with settings = settings'; cells = Model.ComputeHints settings'; reviewList = [] } |> Game.nextProblem, Cmd.none
        | _ ->
            { model with settings = settings'; }, Cmd.none
