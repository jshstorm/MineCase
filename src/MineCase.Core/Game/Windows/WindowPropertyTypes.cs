using System;
using System.Collections.Generic;
using System.Text;
using Orleans;

namespace MineCase.Game.Windows
{
    [GenerateSerializer]
    public enum FurnaceWindowPropertyType : short
    {
        FireIcon = 0,
        MaximumFuelBurnTime = 1,
        ProgressArrow = 2,
        MaximumProgress = 3
    }
}
