﻿using HaruhiChokuretsuLib.Archive.Event;

namespace SerialLoops.Lib.Script.Parameters;

public class ChibiEmoteScriptParameter(string name, short emote) : ScriptParameter(name, ParameterType.CHIBI_EMOTE)
{
    public ChibiEmote Emote { get; set; } = (ChibiEmote)emote;
    public override short[] GetValues(object? obj = null) => [(short)Emote];

    public override ChibiEmoteScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)Emote);
    }

    public enum ChibiEmote : short
    {
        EXCLAMATION_POINT = 1,
        LIGHT_BULB = 2,
        ANGER_MARK = 3,
        MUSIC_NOTE = 4,
        SWEAT_DROP = 5,
    }
}
