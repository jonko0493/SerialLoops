using HaruhiChokuretsuLib.Archive.Event;
using LiteDB;
using SerialLoops.Lib.Items;

namespace SerialLoops.Lib.Script.Parameters;

public class EpisodeHeaderScriptParameter(string name, short epHeaderIndex) : ScriptParameter(name, ParameterType.EPISODE_HEADER)
{
    public Episode EpisodeHeaderIndex { get; set; } = (Episode)epHeaderIndex;
    public override short[] GetValues(object obj = null) => [(short)EpisodeHeaderIndex];

    public override string GetValueString(Project project)
    {
        return project.Localize(EpisodeHeaderIndex.ToString());
    }

    public override EpisodeHeaderScriptParameter Clone(Project project, EventFile eventFile)
    {
        return new(Name, (short)EpisodeHeaderIndex);
    }

    public static SystemTextureItem GetTexture(Episode episode, LiteDatabase db)
    {
        var itemsCol = db.GetCollection<ItemDescription>(Project.ItemsCollectionName);
        // We use names here because display names can change but names cannot
        return episode switch
        {
            Episode.EPISODE_1 =>
                (SystemTextureItem)itemsCol.FindById("SYSTEX_SYS_CMN_T60"),
            Episode.EPISODE_2 =>
                (SystemTextureItem)itemsCol.FindById("SYSTEX_SYS_CMN_T61"),
            Episode.EPISODE_3 =>
                (SystemTextureItem)itemsCol.FindById("SYSTEX_SYS_CMN_T62"),
            Episode.EPISODE_4 =>
                (SystemTextureItem)itemsCol.FindById("SYSTEX_SYS_CMN_T63"),
            Episode.EPISODE_5 =>
                (SystemTextureItem)itemsCol.FindById("SYSTEX_SYS_CMN_T64"),
            Episode.EPILOGUE =>
                (SystemTextureItem)itemsCol.FindById("SYSTEX_SYS_CMN_T66"),
            _ => null,
        };
    }

    public enum Episode
    {
        None = 0,
        EPISODE_1 = 1,
        EPISODE_2 = 2,
        EPISODE_3 = 3,
        EPISODE_4 = 4,
        EPISODE_5 = 5,
        EPILOGUE = 6,
    }
}
