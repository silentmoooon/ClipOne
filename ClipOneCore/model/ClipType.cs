using System;

namespace ClipOne.model
{
    [Flags]
   public enum ClipType
    {
        qq = 1 << 0,
        html = 1 << 1,
        image =1<<2,
        file = 1<<3,
        text=1<<4
    }
}
