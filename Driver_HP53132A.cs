using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoggerDeviceValues
{
    public class Driver_HP53132A
    {
        //public double CurrentValue;
        //public int DataType;

        //public Driver_HP53132A()
        //{
        //    DeviceType = (int)DeviceTypes.Serial;
        //    DeviceName = (int)SupportedDevices.HP53132A;
        //    DataType = (int)DataTypes.Freq;
        //}
        

        public static bool TryParceData(string data, out decimal Resoult)
        {
            Resoult = 0;
            try
            {
                if (data.Contains("kHz"))
                {
                    if (decimal.TryParse(data.Replace(",", "").Replace(".", ",").Replace(" kHz\r", ""), out Resoult)) return false;
                    else { Resoult *= 1000L; return true; }
                }
                else if (data.Contains("MHz"))
                {
                    if (decimal.TryParse(data.Replace(",", "").Replace(".", ",").Replace(" MHz\r", ""), out Resoult)) return false;
                    else { Resoult *= 1000000L; return true; }
                }
                else if (data.Contains("GHz"))
                {
                    if (decimal.TryParse(data.Replace(",", "").Replace(".", ",").Replace(" GHz\r", ""), out Resoult)) return false;
                    else { Resoult *= 1000000000L; return true; }
                }
                else if (data.Contains("Hz"))
                {
                    if (decimal.TryParse(data.Replace(",", "").Replace(".", ",").Replace(" Hz\r", ""), out Resoult)) return false;
                    else return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

    }
}
