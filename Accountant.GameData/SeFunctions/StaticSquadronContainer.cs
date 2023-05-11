﻿using System;
using Accountant.Internal;
using Dalamud.Game;

namespace Accountant.SeFunctions;

public sealed class StaticSquadronContainer : SeAddressBase
{
    public StaticSquadronContainer(SigScanner sigScanner)
        : base(sigScanner, Signatures.SquadronContainer)
    { }

    public unsafe DateTime MissionEnd
        => Address == IntPtr.Zero ? DateTime.MaxValue : Helpers.DateFromTimeStamp(*(uint*)(Address + Offsets.Squadrons.MissionEnd));

    public unsafe DateTime TrainingEnd
        => Address == IntPtr.Zero ? DateTime.MaxValue : Helpers.DateFromTimeStamp(*(uint*) (Address + Offsets.Squadrons.TrainingEnd));

    public unsafe ushort MissionId
        => Address == IntPtr.Zero ? ushort.MaxValue : *(ushort*) (Address + Offsets.Squadrons.MissionId);

    public unsafe ushort TrainingId
        => Address == IntPtr.Zero ? ushort.MaxValue : *(ushort*)(Address + Offsets.Squadrons.TrainingId);

    public unsafe bool NewRecruits
        => Address != IntPtr.Zero && *(byte*)(Address + Offsets.Squadrons.NewRecruits) != 0;
}