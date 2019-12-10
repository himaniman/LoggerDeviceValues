using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoggerDeviceValues
{
    public class ConventerNTC
    {
        public enum ConversionModes
        {
            None, StHr, B25, RT8016, B57861S0103F040
        }

        static double [,]CoefRT8016 = new double[,]{ 
            { -55, 96.3, 7.4}, 
            { -50, 67.01, 7.2},
            { 20, 1.249, 4.5},
            { 25, 1, 4.4},
            { 30, 0.8057, 4.3}
        };

        public ConversionModes mode = ConversionModes.None;

        public double CoefA = 1;
        public double CoefB = 1;
        public double CoefC = 1;

        public double CoefB25 = 3988;
        public double CoefR25 = 10000;

        public double R25 = 10000;

        public decimal Convert(double Resistance)
        {
            decimal Temperature = 0;
            double betterCoefAlpha = -1;
            double betterCoefRT = -1;
            double betterCoefTemper = -1;
            double deltaResistanceForTarger = Resistance;
            switch (mode)
            {
                case ConversionModes.None:
                    Temperature = 0;
                    break;
                case ConversionModes.StHr:
                    Temperature = ((decimal)(1 / (CoefA + CoefB * Math.Log(Resistance) + CoefC * Math.Pow(Math.Log(Resistance), 3))) - 32) * ((decimal)5 / (decimal)9);
                    break;
                case ConversionModes.RT8016:
                    
                    for (int i=0;i< CoefRT8016.GetLength(0); i++)
                    {
                        double delta = Math.Abs(Resistance - CoefRT8016[i, 1] * R25);
                        if (delta < deltaResistanceForTarger)
                        {
                            deltaResistanceForTarger = delta;
                            betterCoefAlpha = CoefRT8016[i, 2];
                            betterCoefRT = CoefRT8016[i, 1];
                            betterCoefTemper = CoefRT8016[i, 0];
                        }
                    }
                    if (betterCoefAlpha > -1)
                    {
                        Temperature = (decimal)(((betterCoefAlpha / 100) * Math.Pow((betterCoefTemper + 273.15), 2)) / ((betterCoefAlpha / 100) * (betterCoefTemper + 273.15) + Math.Log(Resistance / (betterCoefRT * R25)))) - (decimal)273.15;
                    }
                    break;
                case ConversionModes.B57861S0103F040:
                    for (int i = 0; i < CoefRT8016.GetLength(0); i++)
                    {
                        double delta = Math.Abs(Resistance - CoefRT8016[i, 1] * 10000);
                        if (delta < deltaResistanceForTarger)
                        {
                            deltaResistanceForTarger = delta;
                            betterCoefAlpha = CoefRT8016[i, 2];
                            betterCoefRT = CoefRT8016[i, 1];
                            betterCoefTemper = CoefRT8016[i, 0];
                        }
                    }
                    if (betterCoefAlpha > -1)
                    {
                        Temperature = (decimal)(((betterCoefAlpha / 100) * Math.Pow((betterCoefTemper + 273.15), 2)) / ((betterCoefAlpha / 100) * (betterCoefTemper + 273.15) + Math.Log(Resistance / (betterCoefRT * 10000)))) - (decimal)273.15;
                    }
                    break;
            }
            return Temperature;
        }
    }
}
