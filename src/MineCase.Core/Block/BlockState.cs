using System;
using System.Collections.Generic;
using System.Text;
using Orleans;

namespace MineCase.Block
{
    [GenerateSerializer]
    public struct BlockState : IEquatable<BlockState>
    {
        [Id(0)]
        public uint Id { get; set; }

        [Id(1)]
        public uint MetaValue { get; set; }

        public bool IsId(BlockId id)
        {
            return Id == (uint)id;
        }

        public bool IsSameId(BlockState other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            return obj is BlockState && Equals((BlockState)obj);
        }

        public bool Equals(BlockState other)
        {
            return Id == other.Id &&
                   MetaValue == other.MetaValue;
        }

        public override int GetHashCode()
        {
            var hashCode = -81208087;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + Id.GetHashCode();
            hashCode = hashCode * -1521134295 + MetaValue.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(BlockState state1, BlockState state2)
        {
            return state1.Equals(state2);
        }

        public static bool operator !=(BlockState state1, BlockState state2)
        {
            return !(state1 == state2);
        }
    }
}
