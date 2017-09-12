using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipOne.model
{
    [Flags]
    enum ClipType
    {
        qq = 1 << 0,
        html = 1 << 1,
        image =1<<2,
        file = 1<<3,
        text=1<<4
    }
}
