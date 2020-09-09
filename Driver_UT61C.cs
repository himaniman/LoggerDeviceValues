using HidSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LoggerDeviceValues
{
    public class Driver_UT61C
    {
        byte[] LocalBuffer;
        byte[] HIDBuffer;
        public Thread ThreadRead_Discriptor;
        public HidDevice HIDDevice_Discriptor;
        public HidStream HIDStream_Discriptor;
        public LabDevice TargetLabDevice;

        ConcurrentQueue<DeviceManager.MeasureStruct> QueueNewValues;
        public int DriverID;

        public ConventerNTC ConventerNTC_obj = new ConventerNTC();

        public Driver_UT61C(ConcurrentQueue<DeviceManager.MeasureStruct> _QueueNewValuesGlobal, int _id)
        {
            QueueNewValues = _QueueNewValuesGlobal;
            DriverID = _id;
            LocalBuffer = new byte[1];

            ConventerNTC_obj.mode = ConventerNTC.ConversionModes.B25;
        }

        public bool Connect(HidDevice _HidDevice)
        {
            HIDDevice_Discriptor = _HidDevice;
            if (HIDDevice_Discriptor == null) return false; //Ошибка доступа к HID устройству
            if (!HIDDevice_Discriptor.TryOpen(out HIDStream_Discriptor)) return false; //Ошибка получения потока чтения для HID устройства
            byte[] InitialConfigStructure = new byte[] { 0x00, 0x4B, 0x00, 0x00, 0x00, 0x03 };
            HIDStream_Discriptor.SetFeature(InitialConfigStructure);
            HIDBuffer = new byte[11];
            ThreadRead_Discriptor = new Thread(HIDStreamDataReceived);
            ThreadRead_Discriptor.Start();
            return true;
        }

        public void Disconnect()
        {
            ThreadRead_Discriptor.Abort();
        }

        void HIDStreamDataReceived()
        {
            try
            {
                while (true)
                {
                    HIDBuffer = HIDStream_Discriptor.Read();
                    decimal value;
                    string valueRAW;
                    LabDevice.DataTypes type;

                    if (TryParceData(HIDBuffer, out value, out type, out valueRAW))
                    {
                        if (type == LabDevice.DataTypes.Resistance && ConventerNTC_obj.mode != ConventerNTC.ConversionModes.None)
                        {
                            value = ConventerNTC_obj.Convert((double)value);
                            type = LabDevice.DataTypes.Temperature;
                        }
                        QueueNewValues.Enqueue(new DeviceManager.MeasureStruct { Val = value, Typ = type, TS = DateTime.Now, DrvID = DriverID, RAW = valueRAW });
                        //Debug.WriteLine(valueRAW, type.ToString());
                        //System_NewValue(value, type, valueRAW);
                        //TargetLabDevice.NewValue(value);
                        //DelegateForNewValue(value);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return;
            }
        }

        public bool TryParceData(byte[] data, out decimal Resoult, out LabDevice.DataTypes CurrentType, out string ResoultInRAW)
        {
            Resoult = 0;
            ResoultInRAW = "";
            CurrentType = LabDevice.DataTypes.Abstract;
            if (data[1] != 240 && data[1] != 0)
            {
                LocalBuffer = LocalBuffer.Concat(data.Skip(2).Take(data[1] - 240).ToArray()).ToArray();
            }
            //Debug.WriteLine(LocalBuffer.Length);
            int EndPos = IndexOf(LocalBuffer, new byte[] { 0x0D }, 0);
            if (EndPos != -1)
            {
                if (EndPos >= 13)
                {
                    char[] asciiChars = Encoding.ASCII.GetChars(LocalBuffer);
                    string asciiString = new string(asciiChars);
                    //Debug.WriteLine(System.Text.Encoding.ASCII.GetString(LocalBuffer));
                    int CurrentStart = EndPos - 13;

                    Resoult += (LocalBuffer[CurrentStart + 2] & 0x0F) * 1000;
                    Resoult += (LocalBuffer[CurrentStart + 3] & 0x0F) * 100;
                    Resoult += (LocalBuffer[CurrentStart + 4] & 0x0F) * 10;
                    Resoult += (LocalBuffer[CurrentStart + 5] & 0x0F);

                    if (LocalBuffer[CurrentStart + 7] == 49) Resoult /= 1000;
                    if (LocalBuffer[CurrentStart + 7] == 50) Resoult /= 100;
                    if (LocalBuffer[CurrentStart + 7] == 52) Resoult /= 10;

                    if (LocalBuffer[CurrentStart + 10] == 32) Resoult *= 1000;
                    if (LocalBuffer[CurrentStart + 10] == 16) Resoult *= 1000000;
                    CurrentType = LabDevice.DataTypes.Resistance;

                    ////int multiplex = 1000;
                    ////int devider = 1;
                    ////for (int i = CurrentStart + 5; i< CurrentStart + 10; i++)
                    ////{
                    ////    if (LocalBuffer[i] == 0x2E)
                    ////    {
                    ////        devider = multiplex;
                    ////    }
                    ////    else
                    ////    {
                    ////        Resoult += (LocalBuffer[i] & 0x0F) * multiplex;
                    ////        multiplex /= 10;
                    ////    }
                    ////}
                    ////Resoult /= devider;

                    //Resoult = digits[4] + digits[3] * 10 + digits[2] * 100 + digits[1] * 1000 + digits[0] * 10000;
                    //int rangeindex = LocalBuffer[CurrentStart + 5] & 0x0F;
                    //int function = (LocalBuffer[CurrentStart + 6] & 0x0F);
                    //decimal multi = (decimal)multlut[function, rangeindex];
                    //Resoult *= multi;
                    ////if (LocalBuffer[CurrentStart + 4] == 0x2D) Resoult *= -1;
                    ////switch (CurrentStart + 1)
                    ////{
                    ////    case (byte)'D': CurrentType = LabDevice.DataTypes.Voltage; break;
                    ////    case (byte)'O': CurrentType = LabDevice.DataTypes.Resistance; break;
                    ////    case (byte)'C': CurrentType = LabDevice.DataTypes.Capacity; break;
                    ////    case (byte)'T': CurrentType = LabDevice.DataTypes.Temperature; break;
                    ////    case (byte)'A': CurrentType = LabDevice.DataTypes.Current; break;
                    ////    case 12: CurrentType = LabDevice.DataTypes.Freq; break;
                    ////}
                    ResoultInRAW = BitConverter.ToString(LocalBuffer.Skip(CurrentStart).ToArray());
                    LocalBuffer = new byte[1];
                    //if (digits[0] == 0x0a && digits[1] == 0x0a && digits[2] == 0x00 && digits[3] == 0x0c && digits[4] == 0x0a)
                    //{
                    //    Resoult = 0;
                    //    return false;
                    //}
                    return true;
                }
                else
                {
                    LocalBuffer = new byte[1];
                    return false;
                }
            }

            return false;
            //try
            //{
            //    LocalBuffer = LocalBuffer.Concat(data).ToArray();
            //    Resoult = LocalBuffer.Length;
            //    //CurrentValue = Resoult;
            //    return true;
            //}
            //catch
            //{
            //    return false;
            //}
        }

        public static int IndexOf(byte[] array, byte[] pattern, int offset)
        {
            int success = 0;
            for (int i = offset; i < array.Length; i++)
            {
                if (array[i] == pattern[success])
                {
                    success++;
                }
                else
                {
                    success = 0;
                }

                if (pattern.Length == success)
                {
                    return i - pattern.Length + 1;
                }
            }
            return -1;
        }
    
}
}
