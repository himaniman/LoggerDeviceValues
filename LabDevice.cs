﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LoggerDeviceValues
{
    public class LabDevice
    {
        public enum DeviceTypes
        {
            Serial, USB_HID
        }

        public enum SupportedDevices
        {
            HP53132A, UT71D
        }

        public enum DataTypes
        {
            Freq, Voltage, Resistance, Capacity, Temperature, Current, Abstract
        }

        public static String ConvertScientific(decimal value, DataTypes type)
        {
            if (type == DataTypes.Abstract)
                return string.Format("{0:E}", value);
            if (type == DataTypes.Freq)
                return string.Format("{0:E}", value) + " Hz";
            if (type == DataTypes.Voltage)
                return string.Format("{0:E}", value) + " V";
            if (type == DataTypes.Resistance)
                return string.Format("{0:E}", value) + " Om";
            if (type == DataTypes.Capacity)
                return string.Format("{0:E}", value) + " F";
            if (type == DataTypes.Current)
                return string.Format("{0:E}", value) + " A";
            if (type == DataTypes.Temperature)
                return string.Format("{0:E}", value) + " °C";
            return "";
        }

        public static String ConvertFixedPoint(decimal value, DataTypes type)
        {
            if (type == DataTypes.Abstract)
                return string.Format("{0:0.#############}", value);
            else
            {
                string output = "";
                if (Math.Abs(value) < (decimal)1e12 && Math.Abs(value) >= (decimal)1e9) output = string.Format("{0:0.######}", value / (decimal)1e9) + " G";
                if (Math.Abs(value) < (decimal)1e9 && Math.Abs(value) >= (decimal)1e6) output = string.Format("{0:0.######}", value / (decimal)1e6) + " M";
                if (Math.Abs(value) < (decimal)1e6 && Math.Abs(value) >= (decimal)1e3) output = string.Format("{0:0.######}", value / (decimal)1e3) + " k";
                if (Math.Abs(value) < (decimal)1e3 && Math.Abs(value) >= 1) output = string.Format("{0:0.######}", value) + " ";
                if (Math.Abs(value) < 1 && Math.Abs(value) >= (decimal)1e-3) output = string.Format("{0:0.######}", value * (decimal)1e3) + " m";
                if (Math.Abs(value) < (decimal)1e-3 && Math.Abs(value) >= (decimal)1e-6) output = string.Format("{0:0.######}", value * (decimal)1e6) + " u";
                if (Math.Abs(value) < (decimal)1e-6 && Math.Abs(value) >= (decimal)1e-9) output = string.Format("{0:0.######}", value * (decimal)1e9) + " n";
                if (Math.Abs(value) < (decimal)1e-9 && Math.Abs(value) >= (decimal)1e-12) output = string.Format("{0:0.######}", value * (decimal)1e12) + " p";
                if (value == 0) output = "0 ";
                if (type == DataTypes.Freq) return output += "Hz";
                if (type == DataTypes.Voltage) return output += "V";
                if (type == DataTypes.Resistance) return output += "Om";
                if (type == DataTypes.Capacity) return output += "F";
                if (type == DataTypes.Current) return output += "A";
                if (type == DataTypes.Temperature) return output += "°C";
            }
            return "";
        }

        //public static String ConvertNative(decimal value, DataTypes type)
        //{
        //    if (type == DataTypes.Freq)
        //    {
        //        if (value>=1000 && value<100000) return string.Format("{0:R}", value/1000) + " kHz";
        //    }
        //    return "";
        //}
    }
}