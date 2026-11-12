using System;
using System.Collections.Generic;
using System.Text;
using Orleans;

namespace MineCase.Engine.Serialization
{
    /// <summary>
    /// 加载状态后
    /// </summary>
    [GenerateSerializer]
    public sealed class AfterReadState : IEntityMessage
    {
        public static readonly AfterReadState Default = new AfterReadState();
    }

    /// <summary>
    /// 写入状态前
    /// </summary>
    [GenerateSerializer]
    public sealed class BeforeWriteState : IEntityMessage
    {
        public static readonly BeforeWriteState Default = new BeforeWriteState();
    }
}
