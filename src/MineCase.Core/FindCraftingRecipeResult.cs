using System;
using System.Collections.Generic;
using System.Text;
using Orleans;

namespace MineCase
{
    [GenerateSerializer]
    public class FindCraftingRecipeResult
    {
        [Id(0)]
        public Slot Result { get; set; }

        [Id(1)]
        public Slot[,] AfterTake { get; set; }
    }
}
