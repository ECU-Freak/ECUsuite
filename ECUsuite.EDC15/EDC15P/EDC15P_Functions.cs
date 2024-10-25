using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECUsuite.ECU.EDC15.EDC15P
{
    public class EDC15P_Functions
    {
        public int TorqueToPowerkW(int torque, int rpm)
        {
            double power = (torque * rpm) / 7121;
            // convert to kW in stead of horsepower
            power *= 0.73549875;
            return Convert.ToInt32(power);
        }

        public int PowerToTorque(int power, int rpm)
        {
            double torque = (power * 7121) / rpm;
            return Convert.ToInt32(torque);
        }

        public int TorqueToPower(int torque, int rpm)
        {
            double power = (torque * rpm) / 7121;
            return Convert.ToInt32(power);
        }

        public double GetCorrectionFactorForRpm(int rpm, int numberCylinders)
        {
            double correction = 1;
            if (numberCylinders == 6)
            {
                if (rpm >= 4000) correction = 0.80;
                else if (rpm >= 3500) correction = 0.90;
                else if (rpm >= 3250) correction = 0.90;
                else if (rpm >= 3000) correction = 0.93;
                else if (rpm >= 2500) correction = 0.90;
                else if (rpm >= 2250) correction = 0.90;
                else if (rpm >= 1700) correction = 0.90;
                else correction = 0.9;
            }
            else
            {
                if (rpm >= 4000) correction = 0.75;
                else if (rpm >= 3500) correction = 0.83;
                else if (rpm >= 3250) correction = 0.89;
                else if (rpm >= 3000) correction = 0.96;
                else if (rpm >= 2500) correction = 0.98;
                else if (rpm >= 2250) correction = 0.99;
                else correction = 1.00;
            }
            return correction;

        }

        public int IQToTorque(int IQ, int rpm, int numberCylinders)
        {
            double tq = Convert.ToDouble(IQ) * 6;

            // correct for number of cylinders
            tq *= numberCylinders;
            tq /= 4;

            double correction = GetCorrectionFactorForRpm(rpm, numberCylinders);
            tq *= correction;
            return Convert.ToInt32(tq);
        }

        public int TorqueToIQ(int torque, int rpm, int numberCylinders)
        {
            double iq = Convert.ToDouble(torque) / 6;

            // correct for number of cylinders
            iq *= 4;
            iq /= numberCylinders;


            double correction = GetCorrectionFactorForRpm(rpm, numberCylinders);
            iq /= correction;
            return Convert.ToInt32(iq);
        }
    }
}
