﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using HaruhiChokuretsuLib.Util;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace SerialLoops.Lib.Hacks;

public class AsmHack : ReactiveObject
{
    public string Name { get; set; }
    public string Description { get; set; }
    public List<InjectionSite> InjectionSites { get; set; }
    public List<HackFile> Files { get; set; }
    [JsonIgnore]
    public bool ValueChanged { get; set; }
    [JsonIgnore]
    [Reactive]
    public bool IsApplied { get; set; }

    public bool Applied(Project project)
    {
        foreach (InjectionSite site in InjectionSites)
        {
            if (site.Code.Equals("ARM9"))
            {
                using FileStream arm9 = File.OpenRead(Path.Combine(project.IterativeDirectory, "rom", "arm9.bin"));
                arm9.Seek(site.Offset + 3, SeekOrigin.Begin);
                // All BL functions start with 0xEB
                if (arm9.ReadByte() == 0xEB)
                {
                    return true;
                }
            }
            else
            {
                using FileStream overlay = File.OpenRead(Path.Combine(project.IterativeDirectory, "rom", "overlay", $"main_{int.Parse(site.Code):X4}.bin"));
                overlay.Seek(site.Offset + 3, SeekOrigin.Begin);
                if (overlay.ReadByte() == 0xEB)
                {
                    return true;
                }
            }
        }
        return false;
    }

    public void Apply(Project project, Config config, Dictionary<HackFile, SelectedHackParameter[]> selectedParameters, ILogger log, bool forceApplication = false)
    {
        if (Applied(project) && !forceApplication)
        {
            log.LogWarning($"Hack '{Name}' already applied, skipping.");
            return;
        }

        foreach (HackFile file in Files)
        {
            string destination = Path.Combine(project.BaseDirectory, "src", file.Destination);
            if (!Directory.Exists(Path.GetDirectoryName(destination)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
            }
            string fileText = File.ReadAllText(Path.Combine(config.HacksDirectory, file.File));
            foreach (SelectedHackParameter parameter in selectedParameters[file])
            {
                fileText = fileText.Replace($"{{{{{parameter.Parameter.Name}}}}}", parameter.Value);
            }
            File.WriteAllText(destination, fileText);
        }
    }

    public void Revert(Project project, ILogger log)
    {
        bool oneSuccess = false;
        foreach (HackFile file in Files)
        {
            string fileToDelete = Path.Combine(project.BaseDirectory, "src", file.Destination);
            if (File.Exists(fileToDelete))
            {
                File.Delete(fileToDelete);
                oneSuccess = true;
            }
        }
        // If there's at least one success, we assume that an older version of the hack was applied and we've now rolled it back
        if (!oneSuccess)
        {
            log.LogError(string.Format(project.Localize("Failed to delete files for hack '{0}' -- this hack is likely applied in the ROM base and can't be disabled."), Name));
            IsApplied = true;
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is string name)
        {
            return name.Equals(Name);
        }
        return ((AsmHack)obj).Name.Equals(Name);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public bool DeepEquals(AsmHack other)
    {
        return other.Name.Equals(Name) && other.Description.Equals(Description) && other.InjectionSites.SequenceEqual(InjectionSites) && other.Files.SequenceEqual(Files);
    }
}

public class InjectionSite
{
    [JsonIgnore]
    public uint Offset { get; set; }
    public string Code { get; set; }
    public string Location
    {
        get
        {
            int startAddress;
            if (!Code.Equals("ARM9"))
            {
                startAddress = 0x020C7660;
            }
            else
            {
                startAddress = 0x02000000;
            }
            return $"{(uint)(Offset + startAddress):X8}";
        }
        set
        {
            int startAddress;
            if (!Code.Equals("ARM9"))
            {
                startAddress = 0x020C7660;
            }
            else
            {
                startAddress = 0x02000000;
            }
            Offset = (uint)(uint.Parse(value, System.Globalization.NumberStyles.HexNumber) - startAddress);
        }
    }

    public override bool Equals(object obj)
    {
        return ((InjectionSite)obj).Offset == Offset && ((InjectionSite)obj).Code.Equals(Code);
    }

    public override int GetHashCode()
    {
        return Offset.GetHashCode() * Code.GetHashCode() - (Offset.GetHashCode() + Code.GetHashCode());
    }
}

public class HackFile
{
    public string File { get; set; }
    public string Destination { get; set; }
    public string[] Symbols { get; set; }
    public HackParameter[] Parameters { get; set; }

    public override bool Equals(object obj)
    {
        return (((HackFile)obj).File?.Equals(File) ?? false) && (((HackFile)obj).Destination?.Equals(Destination) ?? false) && (((HackFile)obj)?.Symbols.SequenceEqual(Symbols) ?? false) && (((HackFile)obj).Parameters?.SequenceEqual(Parameters) ?? false);
    }

    public override int GetHashCode()
    {
        return File.GetHashCode();
    }
}

public class SelectedHackParameter
{
    public HackParameter Parameter { get; set; }
    public int Selection { get; set; } = -1;
    public string Value => Parameter.Values[Selection].Value;
}

public class HackParameter
{
    public string Name { get; set; }
    public string DescriptiveName { get; set; }
    public HackParameterValue[] Values { get; set; }

    public override bool Equals(object obj)
    {
        return (((HackParameter)obj).Name?.Equals(Name) ?? false) && (((HackParameter)obj).DescriptiveName?.Equals(DescriptiveName) ?? false) && (((HackParameter)obj).Values?.Equals(Values) ?? false);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}

public class HackParameterValue
{
    public string Name { get; set; }
    public string Value { get; set; }

    public override bool Equals(object obj)
    {
        return (((HackParameterValue)obj).Name?.Equals(Name) ?? false) && (((HackParameterValue)obj).Value?.Equals(Value) ?? false);
    }
    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}

public enum HackLocation
{
    ARM9 = -1,
    Overlay_00,
    Overlay_01,
    Overlay_02,
    Overlay_03,
    Overlay_04,
    Overlay_05,
    Overlay_06,
    Overlay_07,
    Overlay_08,
    Overlay_09,
    Overlay_10,
    Overlay_11,
    Overlay_12,
    Overlay_13,
    Overlay_14,
    Overlay_15,
    Overlay_16,
    Overlay_17,
    Overlay_18,
    Overlay_19,
    Overlay_20,
    Overlay_21,
    Overlay_22,
    Overlay_23,
    Overlay_24,
    Overlay_25,
}
