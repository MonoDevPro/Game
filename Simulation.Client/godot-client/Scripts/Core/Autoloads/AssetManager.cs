using System;
using System.Collections.Generic;
using Game.Domain.Enums;
using Godot;

namespace GodotClient.Core.Autoloads;

/// <summary>
/// Gerenciador centralizado de assets com cache e pré-carregamento.
/// Autor: MonoDevPro
/// Data: 2025-01-11 14:29:27
/// </summary>
public partial class AssetManager : Node
{
    private static AssetManager? _instance;
    public static AssetManager Instance => _instance ?? throw new InvalidOperationException("AssetManager not initialized");

    // Cache de recursos carregados
    private readonly Dictionary<string, Texture2D> _textureCache = new();
    private readonly Dictionary<string, SpriteFrames> _spriteFramesCache = new();
    private readonly Dictionary<string, AudioStream> _audioCache = new();

    public override void _Ready()
    {
        base._Ready();
        _instance = this;
        
        // Pré-carrega recursos críticos
        PreloadCriticalAssets();
    }

    /// <summary>
    /// Carrega sprite com cache automático.
    /// </summary>
    public Texture2D GetTexture(string path)
    {
        if (_textureCache.TryGetValue(path, out var cached))
            return cached;

        var texture = GD.Load<Texture2D>(path);
        if (texture is null)
        {
            GD.PushError($"Failed to load texture: {path}");
            return GetFallbackTexture();
        }

        _textureCache[path] = texture;
        return texture;
    }

    /// <summary>
    /// Carrega SpriteFrames baseado em vocação e gênero.
    /// </summary>
    public SpriteFrames GetSpriteFrames(VocationType vocation, Gender gender)
    {
        var vocationName = vocation.ToString();
        var genderSuffix = gender == Gender.Female ? "_female" : "_male";
        var path = $"res://Resources/SpriteSheets/{vocationName.ToLower()}{genderSuffix}_frames.tres";

        return GetSpriteFrames(path);
    }

    /// <summary>
    /// Carrega SpriteFrames (animações) com cache.
    /// </summary>
    public SpriteFrames GetSpriteFrames(string path)
    {
        if (_spriteFramesCache.TryGetValue(path, out var cached))
            return cached;

        var frames = GD.Load<SpriteFrames>(path);
        if (frames is null)
        {
            GD.PushError($"Failed to load SpriteFrames: {path}");
            return GetFallbackSpriteFrames();
        }

        _spriteFramesCache[path] = frames;
        return frames;
    }

    /// <summary>
    /// Carrega áudio com cache.
    /// </summary>
    public AudioStream GetAudio(string path)
    {
        if (_audioCache.TryGetValue(path, out var cached))
            return cached;

        var audio = GD.Load<AudioStream>(path);
        if (audio is null)
        {
            GD.PushError($"Failed to load audio: {path}");
            return null!;
        }

        _audioCache[path] = audio;
        return audio;
    }

    /// <summary>
    /// Pré-carrega recursos críticos na inicialização.
    /// </summary>
    private void PreloadCriticalAssets()
    {
        GD.Print("[AssetManager] Preloading critical assets...");

        // Sprites de personagens
        PreloadCharacterSprites();
        
        // UI essencial
        PreloadUIAssets();
        
        // Sons comuns
        PreloadCommonSounds();

        GD.Print($"[AssetManager] Preloaded {_textureCache.Count} textures, {_spriteFramesCache.Count} animations");
    }

    private void PreloadCharacterSprites()
    {
        // Carrega animações de cada vocação
        foreach (var vocation in Enum.GetValues<VocationType>())
            // Carrega ambos os gêneros
            foreach (var gender in Enum.GetValues<Gender>())
                GetSpriteFrames(vocation, gender);
    }

    private void PreloadUIAssets()
    {
        //GetTexture("res://Assets/UI/health_bar.png");
        //GetTexture("res://Assets/UI/mana_bar.png");
        //GetTexture("res://Assets/UI/cursor.png");
    }

    private void PreloadCommonSounds()
    {
        //GetAudio("res://Assets/Audio/footstep.wav");
        //GetAudio("res://Assets/Audio/attack_hit.wav");
    }

    /// <summary>
    /// Libera recursos não utilizados (chamado em mudança de cena).
    /// </summary>
    public void ClearUnusedCache()
    {
        _textureCache.Clear();
        _spriteFramesCache.Clear();
        _audioCache.Clear();
        GD.Print("[AssetManager] Cache cleared");
    }

    private Texture2D GetFallbackTexture()
    {
        // Retorna textura de fallback (quadrado rosa para debug)
        var image = Image.Create(32, 32, false, Image.Format.Rgba8);
        image.Fill(Colors.Magenta);
        return ImageTexture.CreateFromImage(image);
    }

    private SpriteFrames GetFallbackSpriteFrames()
    {
        var frames = new SpriteFrames();
        frames.AddAnimation("default");
        frames.AddFrame("default", GetFallbackTexture());
        return frames;
    }
}