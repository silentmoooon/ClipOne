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
        image=0,
        html=1<<0,
        file = 1<<1,
        QQ_Unicode_RichEdit_Format=1<<2,
        text=1<<3
    }
}
