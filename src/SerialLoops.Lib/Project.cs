using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using DynamicData;
using HaroohieClub.NitroPacker;
using HaruhiChokuretsuLib.Archive;
using HaruhiChokuretsuLib.Archive.Data;
using HaruhiChokuretsuLib.Archive.Event;
using HaruhiChokuretsuLib.Archive.Graphics;
using HaruhiChokuretsuLib.Audio.SDAT;
using HaruhiChokuretsuLib.Font;
using HaruhiChokuretsuLib.Util;
using HaruhiChokuretsuLib.Util.Exceptions;
using LiteDB;
using SerialLoops.Lib.Items;
using SerialLoops.Lib.Items.Shims;
using SerialLoops.Lib.Script.Parameters;
using SerialLoops.Lib.Util;
using SkiaSharp;
using static SerialLoops.Lib.Items.ItemDescription;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SerialLoops.Lib;

public partial class Project
{
    public const string PROJECT_FORMAT = "slproj";
    public const string EXPORT_FORMAT = "slzip";
    public static readonly JsonSerializerOptions SERIALIZER_OPTIONS = new() { Converters = { new SKColorJsonConverter() } };

    public const int DbVersion = 1;
    public const string ItemsCollectionName = "items";
    public const string ShimsCollectionName = "shims";

    public string Name { get; set; }
    public string LangCode { get; set; }
    public string BaseRomHash { get; set; }
    public Dictionary<string, string> ItemNames { get; set; }
    public Dictionary<int, NameplateProperties> Characters { get; set; }

    // SL settings
    [JsonIgnore]
    public string MainDirectory => Path.Combine(Config.ProjectsDirectory, Name);
    [JsonIgnore]
    public string BaseDirectory => Path.Combine(MainDirectory, "base");
    [JsonIgnore]
    public string IterativeDirectory => Path.Combine(MainDirectory, "iterative");
    [JsonIgnore]
    public string ProjectFile => Path.Combine(MainDirectory, $"{Name}.{PROJECT_FORMAT}");
    [JsonIgnore]
    public string DbFile => Path.Combine(MainDirectory, $"{Name}.db");
    [JsonIgnore]
    public Config Config { get; set; }
    [JsonIgnore]
    public ProjectSettings Settings { get; set; }
    [JsonIgnore]
    public List<ReactiveItemShim> ItemShims { get; set; } = [];

    // Archives
    [JsonIgnore]
    public ArchiveFile<DataFile> Dat { get; set; }
    [JsonIgnore]
    public ArchiveFile<GraphicsFile> Grp { get; set; }
    [JsonIgnore]
    public ArchiveFile<EventFile> Evt { get; set; }
    [JsonIgnore]
    public SoundArchive Snd { get; set; }

    // Common graphics
    [JsonIgnore]
    public FontReplacementDictionary FontReplacement { get; set; } = [];
    [JsonIgnore]
    public FontFile FontMap { get; set; } = new();
    [JsonIgnore]
    public SKBitmap SpeakerBitmap { get; set; }
    [JsonIgnore]
    public SKBitmap NameplateBitmap { get; set; }
    [JsonIgnore]
    public GraphicInfo NameplateInfo { get; set; }
    [JsonIgnore]
    public SKBitmap DialogueBitmap { get; set; }
    [JsonIgnore]
    public SKBitmap FontBitmap { get; set; }

    // Files shared between items
    [JsonIgnore]
    public CharacterDataFile ChrData { get; set; }
    [JsonIgnore]
    public EventFile EventTableFile { get; set; }
    [JsonIgnore]
    public ExtraFile Extra { get; set; }
    [JsonIgnore]
    public ScenarioStruct Scenario { get; set; }
    [JsonIgnore]
    public SoundDSFile SoundDS { get; set; }
    [JsonIgnore]
    public EventFile TopicFile { get; set; }
    [JsonIgnore]
    public EventFile TutorialFile { get; set; }
    [JsonIgnore]
    public MessageFile UiText { get; set; }
    [JsonIgnore]
    public MessageInfoFile MessInfo { get; set; }
    [JsonIgnore]
    public VoiceMapFile VoiceMap { get; set; }
    [JsonIgnore]
    public Dictionary<int, GraphicsFile> LayoutFiles { get; set; } = [];
    [JsonIgnore]
    public Dictionary<ChessFile.ChessPiece, SKBitmap> ChessPieceImages { get; private set; }
    public float AverageBgmMaxAmplitude { get; set; }

    // Localization function to make localizing accessible from the lib
    [JsonIgnore]
    public Func<string, string> Localize { get; set; }

    private static readonly string[] NON_SCRIPT_EVT_FILES = ["CHESSS", "EVTTBLS", "TOPICS", "SCENARIOS", "TUTORIALS", "VOICEMAPS"];
    private static readonly ItemType[] IGNORED_ORPHAN_TYPES = [ ItemType.Layout, ItemType.Scenario ];

    public Project()
    {
    }

    public Project(string name, string langCode, Config config, Func<string, string> localize, ILogger log)
    {
        Name = name;
        LangCode = langCode;
        Config = config;
        Localize = localize;
        log.Log("Creating project directories...");
        try
        {
            Directory.CreateDirectory(MainDirectory);
            Save(log);
            Directory.CreateDirectory(BaseDirectory);
            Directory.CreateDirectory(IterativeDirectory);
            Directory.CreateDirectory(Path.Combine(MainDirectory, "font"));
            File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "charset.json"), Path.Combine(MainDirectory, "font", "charset.json"), overwrite: true);
        }
        catch (Exception ex)
        {
            log.LogException("Exception occurred while attempting to create project directories.", ex);
        }
    }

    public enum LoadProjectState
    {
        SUCCESS,
        LOOSELEAF_FILES,
        CORRUPTED_FILE,
        NOT_FOUND,
        FAILED,
        DB_OUTDATED,
        SL_OUTDATED,
    }

    public struct LoadProjectResult
    {
        public LoadProjectState State { get; set; }
        public string BadArchive { get; set; }
        public int BadFileIndex { get; set; }

        public LoadProjectResult(LoadProjectState state, string badArchive, int badFileIndex)
        {
            State = state;
            BadArchive = badArchive;
            BadFileIndex = badFileIndex;
        }
        public LoadProjectResult(LoadProjectState state)
        {
            State = state;
            BadArchive = string.Empty;
            BadFileIndex = -1;
        }
    }

    public LoadProjectResult Load(Config config, ILogger log, IProgressTracker tracker, bool loadItems)
    {
        Config = config;
        LoadProjectSettings(log, tracker);
        ClearOrCreateCaches(config.CachesDirectory, log);
        string makefile = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Makefile_main"));
        if (!makefile.Equals(File.ReadAllText(Path.Combine(BaseDirectory, "src", "Makefile"))))
        {
            IO.CopyFileToDirectories(this, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Makefile_main"), Path.Combine("src", "Makefile"), log);
            IO.CopyFileToDirectories(this, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "Makefile_overlay"), Path.Combine("src", "overlays", "Makefile"), log);
        }

        if (!loadItems) // if we're loading items, we're obviously going to be making a new DB
        {
            using (LiteDatabase db = new(DbFile))
            {
                if (db.UserVersion < DbVersion)
                {
                    return new(LoadProjectState.DB_OUTDATED);
                }
                else if (db.UserVersion > DbVersion)
                {
                    return new(LoadProjectState.SL_OUTDATED);
                }
            }
        }

        return LoadArchives(log, tracker, loadItems);
    }

    public void LoadProjectSettings(ILogger log, IProgressTracker tracker)
    {
        tracker.Focus("Project Settings", 1);
        string projPath = Path.Combine(IterativeDirectory, "rom", $"{Name}.json");
        try
        {
            Settings = new(NdsProjectFile.Deserialize(projPath), log);
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load project from {projPath}", ex);
        }
        tracker.Finished++;
    }

    public LoadProjectResult LoadArchives(ILogger log, IProgressTracker tracker, bool loadItems)
    {
        if (loadItems && Directory.GetFiles(Path.Combine(IterativeDirectory, "assets"), "*", SearchOption.AllDirectories).Length > 0)
        {
            return new(LoadProjectState.LOOSELEAF_FILES);
        }
        tracker.Focus("dat.bin", 4);
        try
        {
            Dat = ArchiveFile<DataFile>.FromFile(Path.Combine(IterativeDirectory, "original", "archives", "dat.bin"), log, false);
        }
        catch (ArchiveLoadException ex)
        {
            if (Directory.GetFiles(Path.Combine(BaseDirectory, "assets", "data")).Any(f => Path.GetFileNameWithoutExtension(f) == $"{ex.Index:X3}"))
            {
                log.LogError($"File {ex.Index:4} (0x{ex.Index:X3}) '{ex.Filename}' in dat.bin was detected as corrupt.");
                return new(LoadProjectState.CORRUPTED_FILE, "dat.bin", ex.Index);
            }
            else
            {
                // If it's not a file they've modified, then they're using a bad base ROM
                log.LogError($"File {ex.Index:4} (0x{ex.Index:X3}) '{ex.Filename}' in dat.bin was detected as corrupt. " +
                             $"Please use a different base ROM as this one is corrupted.");
                return new(LoadProjectState.CORRUPTED_FILE, "dat.bin", -1);
            }
        }
        catch (Exception ex)
        {
            log.LogException("Error occurred while loading dat.bin", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;

        tracker.CurrentlyLoading = "grp.bin";
        try
        {
            Grp = ArchiveFile<GraphicsFile>.FromFile(Path.Combine(IterativeDirectory, "original", "archives", "grp.bin"), log);
        }
        catch (ArchiveLoadException ex)
        {
            if (Directory.GetFiles(Path.Combine(BaseDirectory, "assets", "graphics")).Any(f => Path.GetFileNameWithoutExtension(f) == $"{ex.Index:X3}"))
            {
                log.LogError($"File {ex.Index:4} (0x{ex.Index:X3}) '{ex.Filename}' in grp.bin was detected as corrupt.");
                return new(LoadProjectState.CORRUPTED_FILE, "grp.bin", ex.Index);
            }
            else
            {
                // If it's not a file they've modified, then they're using a bad base ROM
                log.LogError($"File {ex.Index:4} (0x{ex.Index:X3}) '{ex.Filename}' in grp.bin was detected as corrupt. " +
                             $"Please use a different base ROM as this one is corrupted.");
                return new(LoadProjectState.CORRUPTED_FILE, "grp.bin", -1);
            }
        }
        catch (Exception ex)
        {
            log.LogException("Error occurred while loading grp.bin", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;

        tracker.CurrentlyLoading = "evt.bin";
        try
        {
            Evt = ArchiveFile<EventFile>.FromFile(Path.Combine(IterativeDirectory, "original", "archives", "evt.bin"), log);
        }
        catch (ArchiveLoadException ex)
        {
            if (Directory.GetFiles(Path.Combine(BaseDirectory, "assets", "events")).Any(f => Path.GetFileNameWithoutExtension(f) == $"{ex.Index:X3}"))
            {
                log.LogError($"File {ex.Index:4} (0x{ex.Index:X3}) '{ex.Filename}' in evt.bin was detected as corrupt.");
                return new(LoadProjectState.CORRUPTED_FILE, "evt.bin", ex.Index);
            }
            else
            {
                // If it's not a file they've modified, then they're using a bad base ROM
                log.LogError($"File {ex.Index:4} (0x{ex.Index:X3}) '{ex.Filename}' in evt.bin was detected as corrupt. " +
                             $"Please use a different base ROM as this one is corrupted.");
                return new(LoadProjectState.CORRUPTED_FILE, "evt.bin", -1);
            }
        }
        catch (Exception ex)
        {
            log.LogException("Error occurred while loading evt.bin", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;

        tracker.CurrentlyLoading = "snd.bin";
        try
        {
            Snd = new(Path.Combine(IterativeDirectory, "original", "archives", "snd.bin"));
        }
        catch (Exception ex)
        {
            log.LogException("Error occurred while loading snd.bin", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;

        try
        {
            // Note that the nameplates are not localized by program locale but by selected project language
            string defaultCharactersJson = $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Defaults", "DefaultCharacters")}.{LangCode}.json";
            if (!File.Exists(defaultCharactersJson))
            {
                defaultCharactersJson = $"{Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Defaults", "DefaultCharacters")}.en.json";
            }
            Characters ??= JsonSerializer.Deserialize<Dictionary<int, NameplateProperties>>(File.ReadAllText(defaultCharactersJson), SERIALIZER_OPTIONS);
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load DefaultCharacters file", ex);
            return new(LoadProjectState.FAILED);
        }

        tracker.Focus("Font", 5);
        if (IO.TryReadStringFile(Path.Combine(MainDirectory, "font", "charset.json"), out string json, log))
        {
            FontReplacement.AddRange(JsonSerializer.Deserialize<List<FontReplacement>>(json));
        }
        else
        {
            log.LogError("Failed to load font replacement dictionary.");
        }
        tracker.Finished++;
        try
        {
            FontMap = Dat.GetFileByName("FONTS").CastTo<FontFile>();
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load font map", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;
        try
        {
            SpeakerBitmap = Grp.GetFileByName("SYS_CMN_B12DNX").GetImage(transparentIndex: 0);
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load speaker bitmap", ex);
            return new(LoadProjectState.FAILED);
        }
        try
        {
            GraphicsFile nameplate = Grp.GetFileByName("SYS_CMN_B12DNX");
            NameplateBitmap = nameplate.GetImage();
            NameplateInfo = new(nameplate);
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load nameplate bitmap", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;
        try
        {
            DialogueBitmap = Grp.GetFileByName("SYS_CMN_B02DNX").GetImage(transparentIndex: 0);
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load dialogue bitmap", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;
        try
        {
            GraphicsFile fontFile = Grp.GetFileByName("ZENFONTBNF");
            fontFile.InitializeFontFile();
            FontBitmap = Grp.GetFileByName("ZENFONTBNF").GetImage(transparentIndex: 0);
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load font bitmap", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;

        tracker.Focus("Chess Piece Images", 12);
        try
        {
            ChessPieceImages = [];
            ChessPieceImages.Add(ChessFile.ChessPiece.WhiteKing,
                ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.WhiteKing, Grp));
            tracker.Finished++;
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackKing,
                ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.BlackKing, Grp));
            tracker.Finished++;
            ChessPieceImages.Add(ChessFile.ChessPiece.WhiteQueen,
                ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.WhiteQueen, Grp));
            tracker.Finished++;
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackQueen,
                ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.BlackQueen, Grp));
            tracker.Finished++;
            SKBitmap whiteBishop = ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.WhiteBishopLeft, Grp);
            tracker.Finished++;
            SKBitmap blackBishop = ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.BlackBishopLeft, Grp);
            tracker.Finished++;
            ChessPieceImages.Add(ChessFile.ChessPiece.WhiteBishopLeft, whiteBishop);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhiteBishopRight, whiteBishop);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackBishopLeft, blackBishop);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackBishopRight, blackBishop);
            SKBitmap whiteKnight = ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.WhiteKnightLeft, Grp);
            tracker.Finished++;
            SKBitmap blackKnight = ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.BlackKnightLeft, Grp);
            tracker.Finished++;
            ChessPieceImages.Add(ChessFile.ChessPiece.WhiteKnightLeft, whiteKnight);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhiteKnightRight, whiteKnight);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackKnightLeft, blackKnight);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackKnightRight, blackKnight);
            SKBitmap whiteRook = ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.WhiteRookLeft, Grp);
            tracker.Finished++;
            SKBitmap blackRook = ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.BlackRookLeft, Grp);
            tracker.Finished++;
            ChessPieceImages.Add(ChessFile.ChessPiece.WhiteRookLeft, whiteRook);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhiteRookRight, whiteRook);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackRookLeft, blackRook);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackRookRight, blackRook);
            SKBitmap whitePawn = ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.WhitePawnA, Grp);
            tracker.Finished++;
            SKBitmap blackPawn = ChessPuzzleItem.GetChessPiece(ChessFile.ChessPiece.BlackPawnA, Grp);
            tracker.Finished++;
            ChessPieceImages.Add(ChessFile.ChessPiece.WhitePawnA, whitePawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhitePawnB, whitePawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhitePawnC, whitePawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhitePawnD, whitePawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhitePawnE, whitePawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhitePawnF, whitePawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhitePawnG, whitePawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.WhitePawnH, whitePawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackPawnA, blackPawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackPawnB, blackPawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackPawnC, blackPawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackPawnD, blackPawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackPawnE, blackPawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackPawnF, blackPawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackPawnG, blackPawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.BlackPawnH, blackPawn);
            ChessPieceImages.Add(ChessFile.ChessPiece.Empty, new(16, 32));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load chess pieces", ex);
            return new(LoadProjectState.FAILED);
        }

        tracker.Focus("Static Files", 6);
        try
        {
            EventTableFile = Evt.GetFileByName("EVTTBLS");
            EventTableFile.InitializeEventTableFile();
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load event table file", ex);
        }
        tracker.Finished++;
        try
        {
            ChrData = Dat.GetFileByName("CHRDATAS").CastTo<CharacterDataFile>();
        }
        catch (Exception ex)
        {
            log.LogException("Failed to load chrdata file", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;
        try
        {
            Extra = Dat.GetFileByName("EXTRAS").CastTo<ExtraFile>();
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load extra file", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;
        try
        {
            EventFile scenario = Evt.GetFileByName("SCENARIOS");
            scenario.InitializeScenarioFile();
            Scenario = scenario.Scenario;
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load scenario file", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;
        try
        {
            MessInfo = Dat.GetFileByName("MESSINFOS").CastTo<MessageInfoFile>();
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load message info file", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;
        try
        {
            UiText = Dat.GetFileByName("MESSS").CastTo<MessageFile>();
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load UI text file", ex);
            return new(LoadProjectState.FAILED);
        }
        tracker.Finished++;

        try
        {
            SoundDS = Dat.GetFileByName("SND_DSS").CastTo<SoundDSFile>();
        }
        catch (Exception ex)
        {
            log.LogException("Failed to load DS sound file.", ex);
        }
        try
        {
            if (VoiceMapIsV06OrHigher() || VoiceMapIsV08OrHigher())
            {
                VoiceMap = Evt.GetFileByName("VOICEMAPS").CastTo<VoiceMapFile>();
            }
        }
        catch (Exception ex)
        {
            log.LogException("Failed to load voice map", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            TutorialFile = Evt.GetFileByName("TUTORIALS");
            TutorialFile.InitializeTutorialFile();
        }
        catch (Exception ex)
        {
            log.LogException("Failed to load tutorials", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            TopicFile = Evt.GetFileByName("TOPICS");
            TopicFile.InitializeTopicFile();
        }
        catch (Exception ex)
        {
            log.LogException("Failed to load topic file", ex);
            return new(LoadProjectState.FAILED);
        }

        return loadItems ? LoadItems(tracker, log) : LoadShims(tracker, log);
    }

    private LoadProjectResult LoadShims(IProgressTracker tracker, ILogger log, bool renameItems = true)
    {
        try
        {
            using LiteDatabase db = new(DbFile);
            var shimsCol = db.GetCollection<ItemShim>(ShimsCollectionName);
            tracker.Focus("Loading Items", shimsCol.Count());
            ItemShims = shimsCol.FindAll().AsParallel().Select(s =>
            {
                tracker.Finished++;
                return s;
            }).OrderBy(s => s.DisplayName).Select(s => new ReactiveItemShim(s, this)).ToList();
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load items from database!", ex);
            return new(LoadProjectState.FAILED);
        }

        if (!renameItems)
        {
            return new(LoadProjectState.SUCCESS);
        }

        if (ItemNames is null)
        {
            try
            {
                ItemNames = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Extensions.GetLocalizedFilePath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Defaults", "DefaultNames"), "json")));
                foreach (ReactiveItemShim shim in ItemShims.Where(s => !ItemNames.ContainsKey(s.Name) && s.CanRename))
                {
                    ItemNames.Add(shim.Name, shim.DisplayName);
                }
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load item names", ex);
                return new(LoadProjectState.FAILED);
            }
        }

        foreach (ReactiveItemShim shim in ItemShims.Where(s => s.CanRename || s.Type == ItemType.Place))
        {
            if (ItemNames.TryGetValue(shim.Name, out string value))
            {
                shim.CommitRename = false;
                shim.DisplayName = value;
                shim.CommitRename = true;
            }
            else
            {
                ItemNames.Add(shim.Name, shim.DisplayName);
            }
        }

        return new(LoadProjectState.SUCCESS);
    }

    private LoadProjectResult LoadItems(IProgressTracker tracker, ILogger log)
    {
        LiteDatabase db = new(DbFile); // no using because we will manually dispose before calling LoadShims
        // Clear the database if we're upgrading it
        foreach (string collection in db.GetCollectionNames())
        {
            db.DropCollection(collection);
        }
        db.UserVersion = DbVersion;

        List<ItemDescription> items = [];

        try
        {
            BgTableFile bgTable = Dat.GetFileByName("BGTBLS").CastTo<BgTableFile>();
            tracker.Focus("Backgrounds", bgTable.BgTableEntries.Count);
            List<string> names = [];
            BackgroundItem[] bgs = bgTable.BgTableEntries.AsParallel().Select((entry, i) =>
            {
                if (entry.BgIndex1 > 0)
                {
                    GraphicsFile nameGraphic = Grp.GetFileByIndex(entry.BgIndex1);
                    string name = $"BG_{nameGraphic.Name[..nameGraphic.Name.LastIndexOf('_')]}";
                    string bgNameBackup = name;
                    for (int j = 1; names.Contains(name); j++)
                    {
                        name = $"{bgNameBackup}{j:D2}";
                    }
                    tracker.Finished++;
                    names.Add(name);
                    return new BackgroundItem(name, i, entry, this);
                }
                return null;
            }).Where(b => b is not null).ToArray();
            items.AddRange(bgs);
            var bgCol = db.GetCollection<BackgroundItemShim>(nameof(BackgroundItem));
            bgCol.InsertBulk(bgs.Select(bg => new BackgroundItemShim(bg)));
        }
        catch (Exception ex)
        {
            log.LogException("Failed to background items", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            string[] bgmFiles = SoundDS.BgmSection.AsParallel().Where(bgm => bgm is not null).Select(bgm => Path.Combine(IterativeDirectory, "rom", "data", bgm)).ToArray();
            tracker.Focus("BGM Tracks", bgmFiles.Length);
            List<float> maxes = [];
            BackgroundMusicItem[] bgms = bgmFiles.AsParallel().Select((bgm, i) =>
            {
                tracker.Finished++;
                BackgroundMusicItem bgmItem = new(bgm, i, this);
                if (AverageBgmMaxAmplitude == 0)
                {
                    maxes.Add(bgmItem.GetWaveProvider(log, false).GetMaxAmplitude(log));
                }
                return bgmItem;
            }).ToArray();

            AverageBgmMaxAmplitude = maxes.Average();

            items.AddRange(bgms);
            var bgmCol = db.GetCollection<BackgroundMusicItemShim>(nameof(BackgroundMusicItem));
            bgmCol.InsertBulk(bgms.Select(bgm => new BackgroundMusicItemShim(bgm)));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load BGM tracks", ex);
            return new(LoadProjectState.FAILED);
        }
        try
        {
            string[] voiceFiles = SoundDS.VoiceSection.AsParallel().Where(vce => vce is not null).Select(vce => Path.Combine(IterativeDirectory, "rom", "data", vce)).ToArray();
            tracker.Focus("Voiced Lines", voiceFiles.Length);
            VoicedLineItem[] voices = voiceFiles.AsParallel().Select((vce, i) =>
            {
                tracker.Finished++;
                return new VoicedLineItem(vce, i + 1, this);
            }).ToArray();
            items.AddRange(voices);
            var vceCol = db.GetCollection<VoicedLineItemShim>(nameof(VoicedLineItem));
            vceCol.InsertBulk(voices.Select(vce => new VoicedLineItemShim(vce)));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load voiced lines", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            tracker.Focus("Sound Effects", SoundDS.SfxSection.Count);
            var sfxCol = db.GetCollection<SfxItemShim>(nameof(SfxItem));
            for (short i = 0; i < SoundDS.SfxSection.Count; i++)
            {
                if (SoundDS.SfxSection[i].Index < Snd.SequenceArchives[SoundDS.SfxSection[i].SequenceArchive].File.Sequences.Count)
                {
                    string name = Snd.SequenceArchives[SoundDS.SfxSection[i].SequenceArchive].File.Sequences[SoundDS.SfxSection[i].Index].Name;
                    if (items.Any(s => s.Name == name))
                    {
                        name += "_2";
                    }
                    if (!name.Equals("SE_DUMMY"))
                    {
                        SfxItem sfx = new(SoundDS.SfxSection[i], name, i, this);
                        items.Add(sfx);
                        sfxCol.Insert(new SfxItemShim(sfx));
                    }
                }
                tracker.Finished++;
            }
        }
        catch (Exception ex)
        {
            log.LogException("Failed to load sound effects", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            tracker.Focus("Chess Puzzles", Dat.Files.Count(f => f.Name.StartsWith("CHS")));
            ChessPuzzleItem[] chessPuzzles = Dat.Files.AsParallel()
                .Where(f => f.Name.StartsWith("CHS"))
                .Select(f =>
                {
                    tracker.Finished++;
                    return new ChessPuzzleItem(f.CastTo<ChessFile>());
                }).ToArray();
            items.AddRange(chessPuzzles);
            var chessPuzzleCol = db.GetCollection<ChessPuzzleItemShim>(nameof(ChessPuzzleItem));
            chessPuzzleCol.InsertBulk(chessPuzzles.Select(cp => new ChessPuzzleItemShim(cp)));
        }
        catch (Exception ex)
        {
            log.LogException("Failed to load chess puzzles", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            ItemFile itemFile = Dat.GetFileByName("ITEMS").CastTo<ItemFile>();
            tracker.Focus("Items", itemFile.Items.Count);
            ItemItem[] itemItems = itemFile.Items.AsParallel().Where(i => i > 0).Select((i, idx) =>
            {
                tracker.Finished++;
                return new ItemItem(Grp.GetFileByIndex(i).Name, idx, i, this);
            }).ToArray();

            items.AddRange(itemItems);
            var itemCol = db.GetCollection<ItemItemShim>(nameof(ItemItem));
            itemCol.InsertBulk(itemItems.Select(i => new ItemItemShim(i)));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load item file", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            tracker.Focus("Characters", MessInfo.MessageInfos.Count);
            CharacterItem[] chars = MessInfo.MessageInfos.AsParallel().Where(m => (int)m.Character > 0).Select(m =>
            {
                tracker.Finished++;
                return new CharacterItem(m, Characters[(int)m.Character], this);
            }).ToArray();
            items.AddRange(chars);
            var charCol = db.GetCollection<CharacterItemShim>(nameof(CharacterItem));
            charCol.InsertBulk(chars.Select(c => new CharacterItemShim(c)));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load characters", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            tracker.Focus("Character Sprites", ChrData.Sprites.Count);
            CharacterSpriteItem[] sprites = ChrData.Sprites.AsParallel().Where(s => (int)s.Character > 0).Select(s =>
            {
                tracker.Finished++;
                return new CharacterSpriteItem(s, ChrData, this, log);
            }).ToArray();
            items.AddRange(sprites);
            var spriteCol = db.GetCollection<CharacterSpriteItemShim>(nameof(CharacterSpriteItem));
            spriteCol.InsertBulk(sprites.Select(s => new CharacterSpriteItemShim(s)));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load character sprites", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            ChibiFile chibiFile = Dat.GetFileByName("CHIBIS").CastTo<ChibiFile>();
            tracker.Focus("Chibis", chibiFile.Chibis.Count);
            ChibiItem[] chibis = chibiFile.Chibis.AsParallel().Select((c, i) =>
            {
                tracker.Finished++;
                return new ChibiItem(c, i, this);
            }).ToArray();
            items.AddRange(chibis);
            var chibiCol = db.GetCollection<ChibiItemShim>(nameof(ChibiItem));
            chibiCol.InsertBulk(chibis.Select(c => new ChibiItemShim(c)));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load chibis", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            tracker.Focus("Scripts", Evt.Files.Count - NON_SCRIPT_EVT_FILES.Length);
            ScriptItem[] scripts = Evt.Files.AsParallel()
                .Where(e => !NON_SCRIPT_EVT_FILES.Contains(e?.Name))
                .Select(e =>
                {
                    tracker.Finished++;
                    return new ScriptItem(e, EventTableFile.EvtTbl, log);
                }).ToArray();
            items.AddRange(scripts);
            var scriptCol = db.GetCollection<ScriptItemShim>(nameof(ScriptItem));
            scriptCol.InsertBulk(scripts.Select(scr => new ScriptItemShim(scr, this, log)));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load scripts", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            QMapFile qmap = Dat.GetFileByName("QMAPS").CastTo<QMapFile>();
            tracker.Focus("Maps", qmap.QMaps.Count);
            MapItem[] maps = Dat.Files.AsParallel()
                .Where(d => qmap.QMaps.Select(q => q.Name.Replace(".", "")).Contains(d.Name))
                .Select(m =>
                {
                    tracker.Finished++;
                    return new MapItem(m.CastTo<MapFile>(), qmap.QMaps.FindIndex(q => q.Name.Replace(".", "") == m.Name), this);
                }).ToArray();
            items.AddRange(maps);
            var mapCol = db.GetCollection<MapItemShim>(nameof(MapItem));
            mapCol.InsertBulk(maps.Select(m => new MapItemShim(m)));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load maps", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            PlaceFile placeFile = Dat.GetFileByName("PLACES").CastTo<PlaceFile>();
            tracker.Focus("Places", placeFile.PlaceGraphicIndices.Count);
            PlaceItem[] places = placeFile.PlaceGraphicIndices.Select((pidx, i) =>
            {
                tracker.Finished++;
                return new PlaceItem(i, Grp.GetFileByIndex(pidx));
            }).ToArray();
            tracker.Finished++;
            items.AddRange(places);
            var placeCol = db.GetCollection<PlaceItemShim>(nameof(PlaceItem));
            placeCol.InsertBulk(places.Select(p => new PlaceItemShim(p)));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load place items", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            DataFile[] puzzleDats = Dat.Files.AsParallel().Where(d => d.Name.StartsWith("SLG")).ToArray();
            tracker.Focus("Puzzles", puzzleDats.Length);
            PuzzleItem[] puzzles = puzzleDats.Select(d =>
            {
                tracker.Finished++;
                return new PuzzleItem(d.CastTo<PuzzleFile>(), this, log);
            }).ToArray();
            items.AddRange(puzzles);
            var puzzleCol = db.GetCollection<PuzzleItemShim>(nameof(PuzzleItem));
            puzzleCol.InsertBulk(puzzles.Select(p => new PuzzleItemShim(p)));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load puzzle items", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            tracker.Focus("Topics", TopicFile.Topics.Count);
            var topicCol = db.GetCollection<TopicItemShim>(nameof(TopicItem));
            foreach (Topic topic in TopicFile.Topics)
            {
                // Main topics have shadow topics that are located at ID + 40 (this is actually how the game finds them)
                // So if we're a main topic and we see another topic 40 back, we know we're one of these shadow topics and should really be
                // rolled into the original main topic
                if (topic.Type == TopicType.Main && items.AsParallel().Any(i => i.Type == ItemType.Topic && ((TopicItem)i).TopicEntry.Id == topic.Id - 40))
                {
                    TopicItem updateTopic = (TopicItem)items.AsParallel().First(i => i.Type == ItemType.Topic && ((TopicItem)i).TopicEntry.Id == topic.Id - 40);
                    updateTopic.HiddenMainTopic = topic;
                    topicCol.Update(updateTopic.Name, new(updateTopic));
                }
                else
                {
                    TopicItem addTopic = new(topic, this);
                    items.Add(addTopic);
                    topicCol.Insert(new TopicItemShim(addTopic));
                }
                tracker.Finished++;
            }
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load topics", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            SystemTextureFile systemTextureFile = Dat.GetFileByName("SYSTEXS").CastTo<SystemTextureFile>();
            var sysTexCol = db.GetCollection<SystemTextureItemShim>(nameof(SystemTextureItem));
            tracker.Focus("System Textures",
                5 + systemTextureFile.SystemTextures.Count(s => Grp.Files.AsParallel().Where(g => g.Name.StartsWith("XTR") || g.Name.StartsWith("SYS") && !g.Name.Contains("_SPC_") && g.Name != "SYS_CMN_B12DNX" && g.Name != "SYS_PPT_001DNX").Select(g => g.Index).Distinct().Contains(s.GrpIndex)));
            SystemTextureItem segaLogo = new(systemTextureFile.SystemTextures.First(s => s.GrpIndex == Grp.GetFileByName("LOGO_CO_SEGDNX").Index), this, "SYSTEX_LOGO_SEGA", height: 192);
            items.Add(segaLogo);
            sysTexCol.Insert(new SystemTextureItemShim(segaLogo));
            tracker.Finished++;

            SystemTextureItem aqiLogo = new(systemTextureFile.SystemTextures.First(s => s.GrpIndex == Grp.GetFileByName("LOGO_CO_AQIDNX").Index), this, "SYSTEX_LOGO_AQI", height: 192);
            items.Add(aqiLogo);
            sysTexCol.Insert(new SystemTextureItemShim(aqiLogo));
            tracker.Finished++;

            SystemTextureItem mobiLogo = new(systemTextureFile.SystemTextures.First(s => s.GrpIndex == Grp.GetFileByName("LOGO_MW_ACTDNX").Index), this, "SYSTEX_LOGO_MOBICLIP", height: 192);
            items.Add(mobiLogo);
            sysTexCol.Insert(new SystemTextureItemShim(mobiLogo));
            tracker.Finished++;

            string criLogoName = Grp.Files.AsParallel().Any(f => f.Name == "CREDITS") ? "SYSTEX_LOGO_HAROOHIE" : "SYSTEX_LOGO_CRIWARE";
            SystemTextureItem criLogo = new(systemTextureFile.SystemTextures.First(s => s.GrpIndex == Grp.GetFileByName("LOGO_MW_CRIDNX").Index), this, criLogoName, height: 192);
            items.Add(criLogo);
            sysTexCol.Insert(new SystemTextureItemShim(criLogo));
            tracker.Finished++;

            if (Grp.Files.Any(f => f.Name == "CREDITS"))
            {
                SystemTextureItem credits = new(systemTextureFile.SystemTextures.First(s => s.GrpIndex == Grp.GetFileByName("CREDITS").Index), this, "SYSTEX_LOGO_CREDITS", height: 192);
                items.Add(credits);
                sysTexCol.Insert(new SystemTextureItemShim(credits));
            }
            tracker.Finished++;

            foreach (SystemTexture extraSysTex in systemTextureFile.SystemTextures.Where(s => Grp.Files.AsParallel().Where(g => g.Name.StartsWith("XTR")).Distinct().Select(g => g.Index).Contains(s.GrpIndex)))
            {
                SystemTextureItem sysTex = new(extraSysTex, this, $"SYSTEX_{Grp.GetFileByIndex(extraSysTex.GrpIndex).Name[..^3]}");
                items.Add(sysTex);
                sysTexCol.Insert(new SystemTextureItemShim(sysTex));
                tracker.Finished++;
            }
            // Exclude B12 as that's the nameplates we replace in the character items and PPT_001 as that's the puzzle phase singularity we'll be replacing in the puzzle items
            // We also exclude the "special" graphics as they do not include all of them in the SYSTEX file (should be made to be edited manually)
            foreach (SystemTexture sysSysTex in systemTextureFile.SystemTextures.Where(s => Grp.Files.AsParallel().Where(g => g.Name.StartsWith("SYS") && !g.Name.Contains("_SPC_") && g.Name != "SYS_CMN_B12DNX" && g.Name != "SYS_PPT_001DNX").Select(g => g.Index).Contains(s.GrpIndex)).DistinctBy(s => s.GrpIndex))
            {
                // special case the ep headers
                SystemTextureItem sysTex = Grp.GetFileByIndex(sysSysTex.GrpIndex).Name[..^4].EndsWith("T6")
                    ? new SystemTextureItem(sysSysTex, this,
                        $"SYSTEX_{Grp.GetFileByIndex(sysSysTex.GrpIndex).Name[..^3]}", height: 192)
                    : new SystemTextureItem(sysSysTex, this,
                        $"SYSTEX_{Grp.GetFileByIndex(sysSysTex.GrpIndex).Name[..^3]}");
                items.Add(sysTex);
                sysTexCol.Insert(new SystemTextureItemShim(sysTex));
                tracker.Finished++;
            }
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load system textures", ex);
            return new(LoadProjectState.FAILED);
        }

        // We're gonna try to do more research on this later but for now we're going to hardcode these values
        try
        {
            LayoutFiles.Clear();
            tracker.Focus("Layouts", 22);

            var layoutCol = db.GetCollection<LayoutItemShim>(nameof(LayoutItem));

            // Puzzle phase layouts
            LayoutFiles.Add(0xC45, Grp.GetFileByIndex(0xC45));
            List <GraphicsFile> puzzlePhaseGraphics =
            [
                Grp.GetFileByIndex(0xC48),
                Grp.GetFileByIndex(0xC4A),
                Grp.GetFileByIndex(0xC4C),
                Grp.GetFileByIndex(0xC4D),
                Grp.GetFileByIndex(0xC4F),
                Grp.GetFileByIndex(0xC50),
                Grp.GetFileByIndex(0xC52),
                Grp.GetFileByIndex(0xC54),
                Grp.GetFileByIndex(0xC55),
                Grp.GetFileByIndex(0xC57),
                Grp.GetFileByIndex(0xC59),
                Grp.GetFileByIndex(0xC5B),
                Grp.GetFileByIndex(0xC5D),
                Grp.GetFileByIndex(0xC60),
                Grp.GetFileByIndex(0xC61),
                Grp.GetFileByIndex(0xC62),
                Grp.GetFileByIndex(0xC63),
                Grp.GetFileByIndex(0xC64),
                Grp.GetFileByIndex(0xC65),
                Grp.GetFileByIndex(0xC66),
                Grp.GetFileByIndex(0xC67),
            ];

            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 54, 13, "LYT_ACCIDENT_OUTBREAK", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 67, 5, "LYT_MAIN_TOPIC_DELAYED", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 72, 12, "LYT_DELAY_CHANCE", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 84, 2, "LYT_TOPIC_CHOOSE", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 122, 8, "LYT_READY", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 130, 3, "LYT_GO", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 134, 4, "LYT_TIME_RESULT", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 138, 2, "LYT_ACCIDENT_RESULT", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 140, 2, "LYT_POWER_UP_RESULT", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 142, 2, "LYT_BASE_TIME_LIMIT", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 148, 2, "LYT_HRH_DISTRACTION_BONUS", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 154, 5, "LYT_TOTAL_SCORE", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 163, 3, "LYT_MAIN_TOPICS_OBTAINED", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 163, 3, "LYT_ACCIDENT_BUTTON", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 175, 2, "LYT_MAIN_TOPIC", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 177, 1, "LYT_COUNTER", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 199, 27, "LYT_CHARACTER_TOPICS_OBTAINED", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 226, 4, "LYT_TIME_LIMIT", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 235, 2, "LYT_ACCIDENT_AVOIDED", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 286, 2, "LYT_SEARCH_BUTTON", this));
            tracker.Finished++;
            items.Add(new LayoutItem(0xC45, puzzlePhaseGraphics, 307, 1, "LYT_MIN_ERASED_GOAL", this));
            tracker.Finished++;

            layoutCol.InsertBulk(items.Where(i => i.Type == ItemType.Layout).Cast<LayoutItem>()
                .Select(l => new LayoutItemShim(l)));
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load layouts", ex);
            return new(LoadProjectState.FAILED);
        }

        EventFile scenarioFile;
        try
        {
            // Scenario item must be created after script and puzzle items are constructed
            tracker.Focus("Scenario", 1);
            scenarioFile = Evt.GetFileByName("SCENARIOS");
            scenarioFile.InitializeScenarioFile();
            items.Add(new ScenarioItem(scenarioFile.Scenario, this, log));
            tracker.Finished++;
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load scenario", ex);
            return new(LoadProjectState.FAILED);
        }

        try
        {
            tracker.Focus("Group Selections", scenarioFile.Scenario.Selects.Count);
            GroupSelectionItem[] selections = scenarioFile.Scenario.Selects.Select((s, i) =>
            {
                tracker.Finished++;
                return new GroupSelectionItem(s, i, this);
            }).ToArray();
            items.AddRange(selections);
            var gsCol = db.GetCollection<GroupSelectionItemShim>(nameof(GroupSelectionItem));
            gsCol.InsertBulk(selections.Select(gs => new GroupSelectionItemShim(gs)));
            ((ScenarioItem)items.First(i => i.Type == ItemType.Scenario)).RefreshWithDb(db);
        }
        catch (Exception ex)
        {
            log.LogException($"Failed to load group selections", ex);
            return new(LoadProjectState.FAILED);
        }

        if (ItemNames is null)
        {
            try
            {
                ItemNames = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Extensions.GetLocalizedFilePath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Defaults", "DefaultNames"), "json")));
                foreach (ItemDescription item in items.Where(item => !ItemNames.ContainsKey(item.Name) && item.CanRename))
                {
                    ItemNames.Add(item.Name, item.DisplayName);
                }
            }
            catch (Exception ex)
            {
                log.LogException($"Failed to load item names", ex);
                return new(LoadProjectState.FAILED);
            }
        }

        foreach (ItemDescription item in items.Where(i => i.CanRename || i.Type == ItemType.Place))
        {
            if (ItemNames.TryGetValue(item.Name, out string value))
            {
                item.DisplayName = value;
            }
            else
            {
                ItemNames.Add(item.Name, item.DisplayName);
            }
        }

        items = [.. items.OrderBy(i => i.DisplayName)];

        var itemsCol = db.GetCollection<ItemDescription>(ItemsCollectionName);

        tracker.Focus("Building database", items.Count);
        foreach (ItemDescription item in items)
        {
            itemsCol.Insert(item);
            tracker.Finished++;
        }

        var shimsCol = db.GetCollection<ItemShim>(ShimsCollectionName);
        shimsCol.InsertBulk(items.Select(i => new ItemShim(i)));
        db.Dispose();

        return LoadShims(tracker, log, renameItems: false);
    }

    public bool VoiceMapIsV06OrHigher()
    {
        return Evt.Files.AsParallel().Any(f => f.Name == "VOICEMAPS") && Encoding.ASCII.GetString(Evt.GetFileByName("VOICEMAPS").Data.Skip(0x08).Take(4).ToArray()) == "SUBS";
    }

    public bool VoiceMapIsV08OrHigher()
    {
        return Evt.Files.AsParallel().Any(f => f.Name == "VOICEMAPS") && Encoding.ASCII.GetString(Evt.GetFileByName("VOICEMAPS").Data.Skip(0x08).Take(4).ToArray()) == "SUB2";
    }

    public void RecalculateEventTable()
    {
        short currentFlag = 0;
        int prevScriptIndex = 0;
        foreach (EventTableEntry entry in EventTableFile.EvtTbl.Entries)
        {
            if (currentFlag == 0 && entry.FirstReadFlag > 0)
            {
                currentFlag = entry.FirstReadFlag;
                prevScriptIndex = entry.EventFileIndex;
            }
            else if (entry.FirstReadFlag > 0)
            {
                currentFlag += (short)(Evt.GetFileByIndex(prevScriptIndex).ScriptSections.Count + 1);
                entry.FirstReadFlag = currentFlag;
                prevScriptIndex = entry.EventFileIndex;
            }
        }

        using LiteDatabase db = new(DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(ItemsCollectionName);
        List<ScriptItem> scripts = itemsCol.Find(i => i.Type == ItemType.Script).Cast<ScriptItem>().ToList();
        scripts.ForEach(s => s.UpdateEventTableInfo(EventTableFile.EvtTbl));
        itemsCol.Update(scripts);
    }

    public void MigrateProject(string newRom, Config config, ILogger log, IProgressTracker tracker)
    {
        log.Log($"Attempting to migrate base ROM to {newRom}");

        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        NdsProjectFile.Create("temp", newRom, tempDir);
        IO.CopyFiles(Path.Combine(tempDir, "data"), Path.Combine(BaseDirectory, "original", "archives"), log, "*.bin");
        IO.CopyFiles(Path.Combine(tempDir, "data", "bgm"), Path.Combine(BaseDirectory, "original", "bgm"), log, "*.bin");
        IO.CopyFiles(Path.Combine(tempDir, "data", "vce"), Path.Combine(BaseDirectory, "original", "vce"), log, "*.bin");
        IO.CopyFiles(Path.Combine(tempDir, "overlay"), Path.Combine(BaseDirectory, "original", "overlay"), log, "*.bin");
        IO.CopyFiles(Path.Combine(tempDir, "data", "movie"), Path.Combine(BaseDirectory, "rom", "data", "movie"), log, "*.mods");

        Build.BuildBase(this, config, log, tracker);

        Directory.Delete(tempDir, true);
    }

    public static void ClearOrCreateCaches(string cachesDirectory, ILogger log)
    {
        if (Directory.Exists(cachesDirectory))
        {
            Directory.Delete(cachesDirectory, true);
        }

        log.Log("Creating cache directory...");
        Directory.CreateDirectory(cachesDirectory);

        string bgmCache = Path.Combine(cachesDirectory, "bgm");
        log.Log("Creating BGM cache...");
        Directory.CreateDirectory(bgmCache);

        string vceCache = Path.Combine(cachesDirectory, "vce");
        log.Log("Creating voice file cache...");
        Directory.CreateDirectory(vceCache);
    }

    public ItemDescription FindItem(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        using LiteDatabase db = new(DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(ItemsCollectionName);

        ItemDescription item = itemsCol.FindById(ItemShims.FirstOrDefault(s => s.DisplayName == name)?.Name ?? string.Empty);
        item?.InitializeAfterDbLoad(this);
        return item;
    }

    public static (Project Project, LoadProjectResult Result) OpenProject(string projFile, Config config, Func<string, string> localize, ILogger log, IProgressTracker tracker)
    {
        log.Log($"Loading project from '{projFile}'...");
        if (!File.Exists(projFile))
        {
            log.LogError($"Project file {projFile} not found -- has it been deleted?");
            return (null, new(LoadProjectState.NOT_FOUND));
        }
        try
        {
            tracker.Focus($"{Path.GetFileNameWithoutExtension(projFile)} Project Data", 1);
            Project project = JsonSerializer.Deserialize<Project>(File.ReadAllText(projFile), SERIALIZER_OPTIONS);
            project.Localize = localize;
            tracker.Finished++;

            // If we detect an old NP format, auto-migrate it
            if (!File.Exists(Path.Combine(config.ProjectsDirectory, project.Name, "base", "rom", $"{project.Name}.json")))
            {
                NdsProjectFile.ConvertProjectFile(Path.Combine(config.ProjectsDirectory, project.Name, "base", "rom", $"{project.Name}.xml"));
                NdsProjectFile.ConvertProjectFile(Path.Combine(config.ProjectsDirectory, project.Name, "iterative", "rom", $"{project.Name}.xml"));
                NdsProjectFile.ConvertProjectFile(Path.Combine(config.ProjectsDirectory, project.Name, "base", "original", $"{project.Name}.xml"));
            }

            LoadProjectResult result = project.Load(config, log, tracker, loadItems: false);
            if (result.State == LoadProjectState.LOOSELEAF_FILES)
            {
                log.LogWarning("Found looseleaf files in iterative directory; prompting user for build before loading archives...");
            }
            else if (result.State == LoadProjectState.CORRUPTED_FILE)
            {
                log.LogWarning("Found corrupted file in archive; prompting user for action before continuing...");
            }
            return (project, result);
        }
        catch (Exception ex)
        {
            log.LogException(localize("Error while loading project"), ex);
            return (null, new(LoadProjectState.FAILED));
        }
    }

    public void Save(ILogger log)
    {
        try
        {
            File.WriteAllText(ProjectFile, JsonSerializer.Serialize(this, SERIALIZER_OPTIONS));
        }
        catch (Exception ex)
        {
            log.LogException("Failed to save project file! Check logs for more information.", ex);
        }
    }

    public void Export(string slzipFile, ILogger log)
    {
        try
        {
            log.Log($"Creating slzip at '{slzipFile}'...");
            using FileStream slzipFs = File.Create(slzipFile);
            using ZipArchive slzip = new(slzipFs, ZipArchiveMode.Create);
            log.Log($"Adding '{ProjectFile}' to slzip...");
            slzip.CreateEntryFromFile(ProjectFile, Path.GetFileName(ProjectFile)!);
            slzip.Comment = BaseRomHash;
            log.Log("Adding charset.json to slzip...");
            slzip.CreateEntryFromFile(Path.Combine(MainDirectory, "font", "charset.json"), Path.Combine("font", "charset.json"));
            foreach (string file in Directory.GetFiles(BaseDirectory, "*"))
            {
                log.Log($"Adding '{file}' to slzip...");
                slzip.CreateEntryFromFile(file, Path.GetRelativePath(MainDirectory, file));
            }
            foreach (string file in Directory.GetFiles(Path.Combine(BaseDirectory, "assets"), "*", SearchOption.AllDirectories))
            {
                log.Log($"Adding '{file}' to slzip...");
                slzip.CreateEntryFromFile(file, Path.GetRelativePath(MainDirectory, file));
            }
            foreach (string file in Directory.GetFiles(Path.Combine(BaseDirectory, "src"), "*", SearchOption.AllDirectories))
            {
                log.Log($"Adding '{file}' to slzip...");
                slzip.CreateEntryFromFile(file, Path.GetRelativePath(MainDirectory, file));
            }
            slzip.CreateEntryFromFile(Path.Combine(BaseDirectory, "rom", $"{Name}.json"), Path.Combine("base", "rom", $"{Name}.json"));
            slzip.CreateEntryFromFile(Path.Combine(BaseDirectory, "rom", "banner.bin"), Path.Combine("base", "rom", "banner.bin"));
            foreach (string file in Directory.GetFiles(Path.Combine(BaseDirectory, "rom", "data", "bgm"), "*"))
            {
                log.Log($"Adding '{file}' to slzip...");
                slzip.CreateEntryFromFile(file, Path.GetRelativePath(MainDirectory, file));
            }
            foreach (string file in Directory.GetFiles(Path.Combine(BaseDirectory, "rom", "data", "movie"), "*"))
            {
                log.Log($"Adding '{file}' to slzip...");
                slzip.CreateEntryFromFile(file, Path.GetRelativePath(MainDirectory, file));
            }
            foreach (string file in Directory.GetFiles(Path.Combine(BaseDirectory, "rom", "data", "vce"), "*"))
            {
                log.Log($"Adding '{file}' to slzip...");
                slzip.CreateEntryFromFile(file, Path.GetRelativePath(MainDirectory, file));
            }
        }
        catch (Exception ex)
        {
            log.LogException(Localize("Failed to export project"), ex);
        }
    }

    public static (Project Project, LoadProjectResult LoadResult) Import(string slzipFile, string romPath, Config config, Func<string, string> localize, ILogger log, IProgressTracker tracker)
    {
        try
        {
            using FileStream slzipFs = File.OpenRead(slzipFile);
            using ZipArchive slzip = new(slzipFs, ZipArchiveMode.Read);
            string slprojTemp = Path.GetTempFileName();
            slzip.Entries.FirstOrDefault(f => f.Name.EndsWith(".slproj"))?.ExtractToFile(slprojTemp, overwrite: true);
            Project project = JsonSerializer.Deserialize<Project>(File.ReadAllText(slprojTemp), SERIALIZER_OPTIONS);
            project.Config = config;
            File.Delete(slprojTemp);
            string oldProjectName = project.Name;
            while (Directory.Exists(project.MainDirectory))
            {
                Match numEnding = ProjectNameAppendedNumber().Match(project.Name);
                if (numEnding.Success)
                {
                    project.Name = project.Name.Replace(numEnding.Value, $"({int.Parse(numEnding.Groups["num"].Value) + 1})");
                }
                else
                {
                    project.Name = $"{project.Name} (1)";
                }
            }

            IO.OpenRom(project, romPath, log, tracker);
            slzip.ExtractToDirectory(project.MainDirectory, overwriteFiles: true);
            string newNdsProjFile = Path.Combine("rom", $"{project.Name}.json");
            if (!project.Name.Equals(oldProjectName))
            {
                string oldNdsProjFile = Path.Combine("rom", $"{oldProjectName}.json");
                File.Move(Path.Combine(project.BaseDirectory, oldNdsProjFile), Path.Combine(project.BaseDirectory, newNdsProjFile), overwrite: true);
            }
            project.Settings = new(NdsProjectFile.Deserialize(Path.Combine(project.BaseDirectory, newNdsProjFile)), log);
            Directory.CreateDirectory(project.IterativeDirectory);
            IO.CopyFiles(project.BaseDirectory, project.IterativeDirectory, log, recursive: true);
            Build.BuildBase(project, config, log, tracker);

            return (project, project.Load(config, log, tracker, loadItems: true));
        }
        catch (Exception ex)
        {
            log.LogException(localize("Failed to import project"), ex);
            return (null, new() { State = LoadProjectState.FAILED });
        }
    }

    public void SetBaseRomHash(string romPath)
    {
        BaseRomHash = string.Join("", SHA1.HashData(File.ReadAllBytes(romPath)).Select(b => $"{b:X2}"));
    }

    public List<ReactiveItemShim> GetSearchResults(string query, ILogger logger)
    {
        return GetSearchResults(SearchQuery.Create(query), logger);
    }

    public List<ReactiveItemShim> GetSearchResults(SearchQuery query, ILogger log, IProgressTracker tracker = null)
    {
        string term = query.Term.Trim();
        tracker?.Focus("Searching", query.Scopes.Count);

        try
        {
            using LiteDatabase db = new(DbFile);
            return query.Scopes.SelectMany(s =>
            {
                if (tracker is not null)
                {
                    tracker.Finished++;
                }
                return GetQueryResults(term, s, db, log);
            }).ToList();
        }
        catch (Exception ex)
        {
            log.LogException("Failed to get search results!", ex);
            return [];
        }
    }

    public CharacterItem GetCharacterBySpeaker(Speaker speaker)
    {
        using LiteDatabase db = new(DbFile);
        var itemsCol = db.GetCollection<ItemDescription>(ItemsCollectionName);
        var charactersCol = db.GetCollection<CharacterItemShim>(nameof(CharacterItem));
        string displayName = $"CHR_{Characters[(int)speaker].Name}";
        return (CharacterItem)charactersCol.FindOne(c => c.DisplayName == displayName)?.GetItem(itemsCol);
    }

    private IEnumerable<ReactiveItemShim> GetQueryResults(string term, SearchQuery.DataHolder scope, LiteDatabase db, ILogger logger)
    {
        var bgCol = db.GetCollection<BackgroundItemShim>(nameof(BackgroundItem));
        var bgmCol =  db.GetCollection<BackgroundMusicItemShim>(nameof(BackgroundMusicItem));
        var charCol =  db.GetCollection<CharacterItemShim>(nameof(CharacterItem));
        var chrSprCol =  db.GetCollection<CharacterSpriteItemShim>(nameof(CharacterSpriteItem));
        var chessCol =  db.GetCollection<ChessPuzzleItemShim>(nameof(ChessPuzzleItem));
        var chibiCol =  db.GetCollection<ChibiItemShim>(nameof(ChibiItem));
        var gsCol =  db.GetCollection<GroupSelectionItemShim>(nameof(GroupSelectionItem));
        var itemItemCol =  db.GetCollection<ItemItemShim>(nameof(ItemItem));
        var layoutCol =  db.GetCollection<LayoutItemShim>(nameof(LayoutItem));
        var mapCol = db.GetCollection<MapItemShim>(nameof(MapItem));
        var placeCol = db.GetCollection<PlaceItemShim>(nameof(PlaceItem));
        var puzzleCol = db.GetCollection<PuzzleItemShim>(nameof(PuzzleItem));
        var scriptCol = db.GetCollection<ScriptItemShim>(nameof(ScriptItem));
        var sysTexCol = db.GetCollection<SystemTextureItemShim>(nameof(SystemTextureItem));
        var topicCol = db.GetCollection<TopicItemShim>(nameof(TopicItem));
        var vceCol = db.GetCollection<VoicedLineItemShim>(nameof(VoicedLineItem));

        List<ReactiveItemShim> results = [];

        switch (scope)
        {
            case SearchQuery.DataHolder.Title:
                return ItemShims.Where(s => s.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                       s.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase));

            case SearchQuery.DataHolder.Background_ID:
                if (int.TryParse(term, out int backgroundId))
                {
                    return bgCol.Find(b => b.Id == backgroundId).Select(b => new ReactiveItemShim(b, this));
                }
                return [];

            case SearchQuery.DataHolder.Archive_Index:
                if (int.TryParse(term, out int archiveIdx) || ((term?.StartsWith("0x") ?? false) && int.TryParse(term[2..], NumberStyles.HexNumber, CultureInfo.CurrentCulture.NumberFormat, out archiveIdx)))
                {
                    results.AddRange(bgCol.Find(b => b.ArchiveIndices.Contains(archiveIdx)).Select(s => new ReactiveItemShim(s, this)));
                    if (MessInfo.Index == archiveIdx)
                    {
                        results.AddRange(charCol.FindAll().Select(s => new ReactiveItemShim(s, this)));
                    }
                    results.AddRange(chrSprCol.Find(b => b.ArchiveIndices.Contains(archiveIdx)).Select(s => new ReactiveItemShim(s, this)));
                    results.AddRange(chessCol.Find(c => c.Index == archiveIdx).Select(s => new ReactiveItemShim(s, this)));
                    results.AddRange(chibiCol.Find(c => c.ArchiveIndices.Contains(archiveIdx)).Select(s => new ReactiveItemShim(s, this)));
                    if (archiveIdx == Evt.GetFileByName("SCENARIOS").Index)
                    {
                        results.AddRange(gsCol.FindAll().Select(g => new ReactiveItemShim(g, this)));
                        results.Add(ItemShims.First(s => s.Type == ItemType.Scenario));
                    }
                    results.AddRange(itemItemCol.Find(i => i.GraphicIndex == archiveIdx).Select(s => new ReactiveItemShim(s, this)));
                    results.AddRange(layoutCol.Find(l => l.ArchiveIndices.Contains(archiveIdx)).Select(s => new ReactiveItemShim(s, this)));
                    results.AddRange(mapCol.Find(m => m.ArchiveIndices.Contains(archiveIdx)).Select(s => new ReactiveItemShim(s, this)));
                    results.AddRange(placeCol.Find(p => p.GraphicIndex == archiveIdx).Select(s => new ReactiveItemShim(s, this)));
                    results.AddRange(puzzleCol.Find(p => p.PuzzleIndex == archiveIdx).Select(s => new ReactiveItemShim(s, this)));
                    results.AddRange(scriptCol.Find(s => s.EventIndex == archiveIdx).Select(s => new ReactiveItemShim(s, this)));
                    results.AddRange(sysTexCol.Find(s => s.GraphicIndex == archiveIdx).Select(s => new ReactiveItemShim(s, this)));
                    if (Evt.GetFileByName("TOPICS").Index == archiveIdx)
                    {
                        results.AddRange(topicCol.FindAll().Select(s => new ReactiveItemShim(s, this)));
                    }

                    return results;
                }
                return [];
            //
            // case SearchQuery.DataHolder.Archive_Filename:
            //     switch (item.Type)
            //     {
            //         case ItemType.Background:
            //             return ((BackgroundItem)item).Graphic1.Name.Contains(term, StringComparison.OrdinalIgnoreCase) || (((BackgroundItem)item).Graphic2?.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false);
            //         case ItemType.Character:
            //             return MessInfo.Name.Contains(term, StringComparison.OrdinalIgnoreCase);
            //         case ItemType.Character_Sprite:
            //             CharacterSpriteItem sprite = (CharacterSpriteItem)item;
            //             string[] sprNames = [.. new[]
            //             {
            //                 sprite.Sprite.TextureIndex1, sprite.Sprite.TextureIndex2, sprite.Sprite.TextureIndex3,
            //                 sprite.Sprite.LayoutIndex, sprite.Sprite.EyeAnimationIndex,
            //                 sprite.Sprite.MouthAnimationIndex, sprite.Sprite.EyeTextureIndex,
            //                 sprite.Sprite.MouthTextureIndex
            //             }.Select(i => Grp.GetFileByIndex(i).Name)];
            //             return sprNames.Any(n => n.Contains(term, StringComparison.OrdinalIgnoreCase));
            //         case ItemType.Chess_Puzzle:
            //             return ((ChessPuzzleItem)item).ChessPuzzle.Name.Contains(term, StringComparison.OrdinalIgnoreCase);
            //         case ItemType.Chibi:
            //             return ((ChibiItem)item).Chibi.ChibiEntries.Select(c => Grp.GetFileByIndex(c.Texture)?.Name)
            //                 .Any(n => n?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
            //                 || ((ChibiItem)item).Chibi.ChibiEntries.Select(c => Grp.GetFileByIndex(c.Animation)?.Name)
            //                 .Any(n => n?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false);
            //         case ItemType.Group_Selection:
            //         case ItemType.Scenario:
            //             return "SCENARIOS".Contains(term, StringComparison.OrdinalIgnoreCase);
            //         case ItemType.Item:
            //             return ((ItemItem)item).ItemGraphic.Name.Contains(term, StringComparison.OrdinalIgnoreCase);
            //         case ItemType.Layout:
            //             return ((LayoutItem)item).Layout.Name.Contains(term, StringComparison.OrdinalIgnoreCase);
            //         case ItemType.Map:
            //             return ((MapItem)item).Map.Name.Contains(term, StringComparison.OrdinalIgnoreCase);
            //         case ItemType.Place:
            //             return ((PlaceItem)item).PlaceGraphic.Name.Contains(term, StringComparison.OrdinalIgnoreCase);
            //         case ItemType.Puzzle:
            //             return ((PuzzleItem)item).Puzzle.Name.Contains(term, StringComparison.OrdinalIgnoreCase);
            //         case ItemType.Script:
            //             return ((ScriptItem)item).Event.Name.Contains(term, StringComparison.OrdinalIgnoreCase);
            //         case ItemType.System_Texture:
            //             return Grp.GetFileByIndex(((SystemTextureItem)item).SysTex.GrpIndex).Name.Contains(term, StringComparison.OrdinalIgnoreCase);
            //         case ItemType.Topic:
            //             return "TOPICS".Contains(term, StringComparison.OrdinalIgnoreCase);
            //     }
            //     return false;

            case SearchQuery.DataHolder.Dialogue_Text:
                return scriptCol.Find(s => s.DialogueLines.Contains(term)).Select(s => new ReactiveItemShim(s, this));

            // case SearchQuery.DataHolder.Command:
            //     if (item is ScriptItem commandScript)
            //     {
            //         EventFile.CommandVerb command;
            //         List<string> parameters = [];
            //         int firstParen = term.IndexOf('(');
            //         if (firstParen > 0)
            //         {
            //             command = Enum.Parse<EventFile.CommandVerb>(term[..firstParen]);
            //             parameters.AddRange(term[(firstParen+1)..term.IndexOf(')')].Split(','));
            //         }
            //         else
            //         {
            //             command = Enum.Parse<EventFile.CommandVerb>(term);
            //         }
            //
            //         return commandScript.GetScriptCommandTree(this, logger)
            //             .Any(s => s.Value.Any(c => c.Verb == command &&
            //                                        parameters.Count <= c.Parameters.Count &&
            //                                        c.Parameters.Zip(parameters)
            //                                            .All(z => string.IsNullOrWhiteSpace(z.Second) || z.Second == "*" ||
            //                                                      (z.Second.StartsWith("!") && !(z.First?.GetValueString(this)?.Contains(z.Second, StringComparison.OrdinalIgnoreCase) ?? false)) ||
            //                                                      (z.First?.GetValueString(this)?.Contains(z.Second, StringComparison.OrdinalIgnoreCase) ?? false))));
            //     }
            //     return false;

            case SearchQuery.DataHolder.Flag:
                short flag = -1;
                short.TryParse(term, out flag);
                results.AddRange(scriptCol.Find(s => s.FlagIds.Contains(flag)
                                                     || s.FlagNames.Contains(term, StringComparer.OrdinalIgnoreCase)
                                                     || s.FlagNicknames.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .Select(s => new ReactiveItemShim(s, this)));
                results.AddRange(bgCol.Find(b => b.Flag == flag
                                                 || b.FlagName.Equals(term, StringComparison.OrdinalIgnoreCase)
                                                 || b.FlagNickname.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .Select(s => new ReactiveItemShim(s, this)));
                results.AddRange(bgmCol.Find(b => b.Flag == flag
                                                 || b.FlagName.Equals(term, StringComparison.OrdinalIgnoreCase)
                                                 || b.FlagNickname.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .Select(s => new ReactiveItemShim(s, this)));
                results.AddRange(puzzleCol.Find(p => p.Flags.Contains(flag)
                                                  || p.FlagNames.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .Select(s => new ReactiveItemShim(s, this)));
                results.AddRange(gsCol.Find(g => g.RouteFlags.Contains(flag)
                                                     || g.RouteFlagNames.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .Select(s => new ReactiveItemShim(s, this)));
                return results;

            case SearchQuery.DataHolder.Conditional:
                return scriptCol.Find(s => s.Conditionals.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .Select(s => new ReactiveItemShim(s, this));

            case SearchQuery.DataHolder.Speaker_Name:
                return scriptCol.Find(s => s.SpeakerStrings.Any(sp => sp.Contains(term, StringComparison.OrdinalIgnoreCase)))
                    .Select(s => new ReactiveItemShim(s, this));

            case SearchQuery.DataHolder.Background_Type:
                return bgCol.Find(b => b.BackgroundType.ToString().Contains(term, StringComparison.OrdinalIgnoreCase))
                    .Select(s => new ReactiveItemShim(s, this));

            // case SearchQuery.DataHolder.Episode_Number:
            //     if (int.TryParse(term, out int episodeNumber))
            //     {
            //         return ItemShims.Where(i => ItemIsInEpisode(i.Shim, episodeNumber, unique: false, db));
            //     }
            //     return [];
            //
            // case SearchQuery.DataHolder.Episode_Unique:
            //     if (int.TryParse(term, out int episodeNumUnique))
            //     {
            //         return ItemShims.Where(i => ItemIsInEpisode(i.Shim, episodeNumUnique, unique: true, db));
            //     }
            //     return [];
            //
            // case SearchQuery.DataHolder.Orphaned_Items:
            //     // assume SFX that are in groups are referenced in code. william doesn't quite know what jonko means here but he trusts the process
            //     return ItemShims.Where(i => !IGNORED_ORPHAN_TYPES.Contains(i.Type) && i.Type != ItemType.SFX)
            //         .Where(i => i.Shim.GetReferencesTo(this).Count == 0);

            default:
                logger.LogError($"Unimplemented search scope: {scope}");
                return [];
        }
    }

    private bool ItemIsInEpisode(ItemShim item, int episodeNum, bool unique, LiteDatabase db)
    {
        int scenarioEpIndex = Scenario.Commands.FindIndex(c => c.Verb == ScenarioCommand.ScenarioVerb.NEW_GAME && c.Parameter == episodeNum);
        if (scenarioEpIndex >= 0)
        {
            int scenarioNextEpIndex = Scenario.Commands.FindIndex(c => c.Verb == ScenarioCommand.ScenarioVerb.NEW_GAME && c.Parameter == episodeNum + 1);
            if (item is ScriptItemShim script)
            {
                return ScriptIsInEpisode(script, scenarioEpIndex, scenarioNextEpIndex, db);
            }
            else
            {
                List<ItemShim> references = item.GetReferencesTo(this, db).Select(r => r.Shim).ToList();
                if (unique)
                {
                    return references.Any(r => r.Type == ItemType.Script) &&
                           references.Where(r => r.Type == ItemType.Script)
                               .All(r => ScriptIsInEpisode((ScriptItemShim)r, scenarioEpIndex, scenarioNextEpIndex, db));
                }
                else
                {
                    return references.Any(r => r.Type == ItemType.Script && ScriptIsInEpisode((ScriptItemShim)r, scenarioEpIndex, scenarioNextEpIndex, db));
                }
            }
        }
        return false;
    }

    private bool ScriptIsInEpisode(ScriptItemShim script, int scenarioEpIndex, int scenarioNextEpIndex, LiteDatabase db)
    {
        int scriptFileScenarioIndex = Scenario.Commands.FindIndex(c => c.Verb == ScenarioCommand.ScenarioVerb.LOAD_SCENE && c.Parameter == script.EventIndex);
        if (scriptFileScenarioIndex < 0)
        {
            List<ItemShim> references = script.GetReferencesTo(this, db).Select(r => r.Shim).ToList();
            ItemShim groupSelection = references.Find(r => r.Type == ItemType.Group_Selection);
            if (groupSelection is not null)
            {
                scriptFileScenarioIndex = Scenario.Commands.FindIndex(c => c.Verb == ScenarioCommand.ScenarioVerb.ROUTE_SELECT && c.Parameter == ((GroupSelectionItemShim)groupSelection).Index);
            }
        }
        if (scenarioNextEpIndex < 0)
        {
            scenarioNextEpIndex = int.MaxValue;
        }

        return scriptFileScenarioIndex > scenarioEpIndex && scriptFileScenarioIndex < scenarioNextEpIndex;
    }

    [GeneratedRegex(@"\((?<num>\d+)\)")]
    private static partial Regex ProjectNameAppendedNumber();
}
