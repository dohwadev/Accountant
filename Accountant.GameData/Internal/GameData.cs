﻿using System;
using System.Collections.Generic;
using System.Linq;
using Accountant.Data;
using Accountant.Enums;
using Accountant.Structs;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui;
using Lumina.Excel.GeneratedSheets;

namespace Accountant.Internal;

internal class GameData : IGameData
{
    public const   int                       CurrentVersion = 3;
    private static Crops?                    _crops;
    private static Wheels?                   _wheels;
    private static Maps?                     _maps;
    private static Plots?                    _plots;
    private static Dictionary<uint, string>? _worldNames;
    private static Dictionary<uint, byte>?   _worldCactpotHours;
    private static Dictionary<string, uint>? _worldIds;
    private static FreeCompanyTracker?       _fcTracker;
    private static int                       _subscribers;

    public         bool                      Valid { get; private set; } = true;

    public int Version
        => CurrentVersion;

    private static InvalidOperationException NotReadyException()
        => new("Accessing Accountant.GameData before Initialize.");

    public (CropData Data, string Name) FindCrop(uint itemId)
        => _crops?.Find(itemId) ?? throw NotReadyException();

    public CropData FindCrop(string name)
        => _crops?.Find(name) ?? throw NotReadyException();

    public (Item Item, string Name, byte Grade) FindWheel(uint itemId)
        => _wheels?.Find(itemId) ?? throw NotReadyException();

    public (Item Item, string Name, byte Grade) FindWheel(string name)
        => _wheels?.Find(name) ?? throw NotReadyException();

    public Item? FindMap(uint itemId)
        => _maps == null ? throw NotReadyException() : _maps.Find(itemId);

    public int GetNumWards(InternalHousingZone zone = InternalHousingZone.Mist)
        => _plots?.GetNumWards(zone) ?? throw NotReadyException();

    public int GetNumPlots(InternalHousingZone zone = InternalHousingZone.Mist)
        => _plots?.GetNumPlots(zone) ?? throw NotReadyException();

    public PlotSize GetPlotSize(InternalHousingZone zone, ushort plot)
        => _plots?.GetSize(zone, plot) ?? throw NotReadyException();

    public IEnumerable<(string Name, uint Id)> Worlds()
        => _worldNames!.Select(w => (w.Value, w.Key));

    public bool IsValidWorldId(uint id)
        => _worldNames!.ContainsKey(id);

    public string GetWorldName(uint id)
        => _worldNames!.TryGetValue(id, out var ret)
            ? ret
            : throw new ArgumentOutOfRangeException($"{id} is not a valid world id. - World Name");
            //: $"Unknown World {id}"; // 6.3

    public string GetWorldName(PlayerCharacter player)
        => GetWorldName(player.HomeWorld.Id);

    public uint GetWorldId(string worldName)
        => _worldIds!.TryGetValue(worldName, out var ret)
            ? ret
            : throw new ArgumentOutOfRangeException($"{worldName} is not a valid world id. - World Id");

    public byte GetJumboCactpotResetHour(uint worldId)
        => _worldCactpotHours!.TryGetValue(worldId, out var ret)
            ? ret
            : throw new ArgumentOutOfRangeException($"{worldId} is not a valid world id. - Jumbo Cactpot");

    public GameData(GameGui gui, ClientState state, Framework framework, DataManager data)
    {
        _fcTracker ??= new FreeCompanyTracker(gui, state, framework);
        _crops     ??= new Crops(data);
        _wheels    ??= new Wheels(data);
        _maps      ??= new Maps(data);
        _plots     ??= new Plots(data);
        SetupWorlds(data);
        ++_subscribers;
        Localization.Initialize(data);
    }

    private static void SetupWorlds(DataManager data)
    {
        if (_worldNames != null)
            return;

        var sheet = data.GameData.GetExcelSheet<World>()!;
        //_worldNames = sheet.Where(w => w.IsPublic && !w.Name.RawData.IsEmpty)
        _worldNames = sheet.Where(w => w.Region == 3 && !w.Name.RawData.IsEmpty)
            .ToDictionary(w => w.RowId, w => w.Name.RawString);
        _worldIds = _worldNames.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
        _worldCactpotHours = _worldNames.ToDictionary(kvp => kvp.Key, kvp =>
        {
            var world = sheet.GetRow(kvp.Key)!;
            return (byte)(world.DataCenter.Row switch
            {
                1 => 12,
                2 => 12,
                3 => 12,
                4 => 26,
                5 => 26,
                6 => 19,
                7 => 20,
                8 => 26,
                9 => 10,
                _ => 12,
            });
        });
    }

    public (string Tag, string? Name, string? Leader) FreeCompanyInfo()
        => Valid ? _fcTracker!.FreeCompanyInfo : throw new InvalidOperationException("Trying to use disposed GameData.");

    public void Dispose()
    {
        if (!Valid)
            return;

        Valid = false;
        --_subscribers;
        if (_subscribers == 0)
            _fcTracker?.Dispose();
    }
}
