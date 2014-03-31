using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mid2BMS
{
    class SimpleFilter : DigitalFilter
    {
        double y0 = 0;
        double rate = 0.5;

        public SimpleFilter(double rate = 0.5)
        {
            this.rate = rate;
        }

        public double Process(double x0)
        {
            double y1 = y0;  // delay
            y0 = x0 * rate + y1 * (1 - rate);

            return y0;
        }

        public double CharacteristicCurve(double _2pi_normalized_frequency)
        {
            double omega = _2pi_normalized_frequency;
            //               r
            // H(z) = -----------------
            //         1 - (1 - r)z^-1

            Complex zinv = Complex.Exph(-omega); // z^-1 = e^(-iω)

            Complex H_z = rate / (1 - (1 - rate) * zinv);

            return H_z.Abs();
        }

        public void Reset()
        {
            throw new InvalidOperationException();
        }
        public DigitalFilter Clone()
        {
            throw new InvalidOperationException();
        }
    }
}
