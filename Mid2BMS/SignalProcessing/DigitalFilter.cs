using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    interface DigitalFilter
    {
        // 久しぶりのユーザー定義インターフェースですね
        // あれ、初めてかな？？

        // doubleにしたのは内部での精度確保のため。
        // 内部でdoubleにすればいいだけであってインターフェースはfloatでも良かったかもしれない。

        double CharacteristicCurve(double _2pi_normalized_frequency);
        double Process(double val);
        void Reset();
        DigitalFilter Clone();
    }
}
