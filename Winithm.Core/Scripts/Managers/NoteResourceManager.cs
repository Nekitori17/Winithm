using Godot;
using System;
using System.Collections.Generic;
using Winithm.Core.Common;
using Winithm.Core.Data;

namespace Winithm.Core.Managers
{
  public enum NotePart
  {
    Left,
    Center,
    Right,
    Overlay
  }

  public struct ResourcePackConfig
  {
    public bool Particle;
    public Color HighlightColor;
  }

  public struct ResourcePack
  {
    public Dictionary<NoteType, Dictionary<NotePart, Texture>> TEX;
    public Dictionary<NoteType, AudioStream> SFX;
    public SpriteFrames VFX;
    public ResourcePackConfig Config;
  }

  public class NoteResourceManager : Node
  {
    public static NoteResourceManager Instance { get; private set; }

    private Dictionary<string, ResourcePack> _resourcePacks;
    
    // Cache active pack for high performance O(1) access
    private ResourcePack _activeResourcePack;
    private string _activeResourcePackName = "default";

    public string ActiveResourcePackName
    {
      get => _activeResourcePackName;
      set => SetActiveResourcePack(value);
    }

    public override void _Ready()
    {
      Instance = this;

      string resourcePacksPath = "res://Winithm.Core/Resources/ResourcePacks";

      Directory resourcePacksDir = new Directory();
      if (resourcePacksDir.Open(resourcePacksPath) != Error.Ok)
      {
        GD.PushError($"[NoteResourceManager] Failed to open resource packs directory: {resourcePacksPath}");
        return;
      }

      // Pre-allocate to avoid resize overhead during initialization
      _resourcePacks = new Dictionary<string, ResourcePack>(StringComparer.OrdinalIgnoreCase);

      resourcePacksDir.ListDirBegin(skipNavigational: true);

      string resourcePackName;
      while ((resourcePackName = resourcePacksDir.GetNext()) != "")
      {
        if (resourcePacksDir.CurrentIsDir() == false) continue; // Only process directories

        string resourcePackPath = resourcePacksPath.PlusFile(resourcePackName);

        // Initialize base structures to prevent KeyNotFound/NullReference exceptions
        var resourcePack = new ResourcePack
        {
          TEX = new Dictionary<NoteType, Dictionary<NotePart, Texture>>(),
          SFX = new Dictionary<NoteType, AudioStream>(),
          VFX = new SpriteFrames(),
          Config = new ResourcePackConfig
          {
            Particle = true,
            HighlightColor = Colors.Yellow,
          }
        };

        // Load modules
        LoadConfig(resourcePackPath.PlusFile("config.ini"), ref resourcePack);
        LoadTexture(resourcePackPath.PlusFile("tex"), ref resourcePack);
        LoadSoundEffect(resourcePackPath.PlusFile("sfx"), ref resourcePack);

        // // Load VFX if it exists
        // string vfxFramesPath = resourcePackPath.PlusFile("hit_frames.tres");
        // if (new File().FileExists(vfxFramesPath))
        // {
        //   resourcePack.VFX = GD.Load<SpriteFrames>(vfxFramesPath);
        // }

        _resourcePacks[resourcePackName] = resourcePack;
      }

      resourcePacksDir.ListDirEnd();

      // Ensure active resource pack is cached properly
      SetActiveResourcePack(_activeResourcePackName);
    }

    private static void LoadConfig(string path, ref ResourcePack resourcePack)
    {
      File file = new File();

      if (file.Open(path, File.ModeFlags.Read) != Error.Ok)
        return; // Config is optional

      try
      {
        while (!file.EofReached())
        {
          string line = file.GetLine().Trim();
          if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

          // Faster parsing with IndexOf instead of allocating array from Split
          int delimiterIdx = line.IndexOf('=');
          if (delimiterIdx == -1) continue;

          string key = line.Substring(0, delimiterIdx).Trim();
          string val = line.Substring(delimiterIdx + 1).Trim();

          switch (key)
          {
            case "particle":
              resourcePack.Config.Particle = bool.Parse(val);
              break;
            case "highlightColor":
              resourcePack.Config.HighlightColor = StringToColor(val);
              break;
          }
        }
      }
      finally
      {
        file.Close();
      }
    }

    private static Color StringToColor(string str)
    {
      string[] parts = str.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

      float r = 0, g = 0, b = 0, a = 1;

      if (parts.Length >= 1) float.TryParse(parts[0], out r);
      if (parts.Length >= 2) float.TryParse(parts[1], out g);
      if (parts.Length >= 3) float.TryParse(parts[2], out b);
      if (parts.Length >= 4) float.TryParse(parts[3], out a);

      return new Color(r, g, b, a);
    }

    private static void LoadTexture(string path, ref ResourcePack resourcePack)
    {
      Directory dir = new Directory();
      if (dir.Open(path) != Error.Ok) return;

      dir.ListDirBegin(skipNavigational: true);

      string fileName;
      while ((fileName = dir.GetNext()) != "")
      {
        if (fileName.EndsWith(".import")) continue; // Skip Godot import metadata files

        string filePath = path.PlusFile(fileName);
        string fileNameWOExt = System.IO.Path.GetFileNameWithoutExtension(fileName);

        // Name format: NoteType_NotePart
        int underscoreIdx = fileNameWOExt.IndexOf('_');
        if (underscoreIdx == -1) continue;

        string ntStr = fileNameWOExt.Substring(0, underscoreIdx);
        string tpStr = fileNameWOExt.Substring(underscoreIdx + 1);

        NoteType noteType = Enum.TryParse<NoteType>(ntStr, true, out var nt) ? nt : NoteType.Tap;
        NotePart texturePart = Enum.TryParse<NotePart>(tpStr, true, out var tp) ? tp : NotePart.Center;

        // Initialize dictionary lazy to prevent KeyNotFoundException
        if (!resourcePack.TEX.ContainsKey(noteType))
        {
          resourcePack.TEX[noteType] = new Dictionary<NotePart, Texture>();
        }

        resourcePack.TEX[noteType][texturePart] = GD.Load<Texture>(filePath);
      }
      
      dir.ListDirEnd(); // Free resources
    }

    private static void LoadSoundEffect(string path, ref ResourcePack resourcePack)
    {
      Directory dir = new Directory();
      if (dir.Open(path) != Error.Ok) return;

      dir.ListDirBegin(skipNavigational: true);

      string fileName;
      while ((fileName = dir.GetNext()) != "")
      {
        if (fileName.EndsWith(".import")) continue;

        string filePath = path.PlusFile(fileName);
        string fileNameWOExt = System.IO.Path.GetFileNameWithoutExtension(fileName);

        NoteType noteType = Enum.TryParse<NoteType>(fileNameWOExt, true, out var nt) ? nt : NoteType.Tap;
        resourcePack.SFX[noteType] = GD.Load<AudioStream>(filePath);
      }
      
      dir.ListDirEnd();
    }

    public void SetActiveResourcePack(string resourcePackName)
    {
      // TryGetValue acts directly without double lookup (ContainsKey + Indexer)
      if (_resourcePacks.TryGetValue(resourcePackName, out ResourcePack pack))
      {
        _activeResourcePackName = resourcePackName;
        _activeResourcePack = pack; // Cache current pack object
      }
      else
      {
        GD.PushError($"[NoteResourceManager] Skin pack not found: {resourcePackName}");
      }
    }

    // Direct memory access without dictionary lookup guarantees O(1) high performance calls
    public ResourcePack GetActiveResourcePack() => _activeResourcePack;
  }
}
