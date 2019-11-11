using HidSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LoggerDeviceValues
{
    public class Driver_UT71D
    {
        public static double[,] multlut  = new double[17,8] {
            { 1e-5, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0},
            { 1.0, 1e-4, 1e-3, 1e-2, 1e-1, 1.0, 1.0, 1.0},
            { 1.0, 1e-4, 1e-3, 1e-2, 1e-1, 1.0, 1.0, 1.0},
            { 1e-5, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0},
            { 1.0, 1e-2, 1e-1, 1.0, 1e1, 1e2, 1e3, 1.0},
            { 1.0, 1e-12, 1e-11, 10e-10, 1e-9, 1e-8, 1e-7, 1e-6},
            { 1e-1, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0},
            { 1e-8, 1e-7, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0},
            { 1e-6, 1e-5, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0},
            { 1.0, 1e-3, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0},
            { 1e-2, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0},
            { 1e-4, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0},
            { 1e-3, 1e-2, 1e-1, 1.0, 1e1, 1e2, 1e3, 1e4},
            { 1e-1, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0},
            { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0},
            { 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0},
            { 1e-2, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0}};

        byte[] LocalBuffer;
        byte[] HIDBuffer;
        public Thread ThreadRead_Discriptor;
        public HidDevice HIDDevice_Discriptor;
        public HidStream HIDStream_Discriptor;
        //LabDevice.NewValueDelegate DelegateForNewValue;

        //public Driver_UT71D(LabDevice.NewValueDelegate _valueDelegate)
        //{
        //    LocalBuffer = new byte[1];
        //    //DelegateForNewValue = _valueDelegate;
        //}

        public bool Connect(HidDevice _HidDevice)
        {
            HIDDevice_Discriptor = _HidDevice;
            //DeviceList.Local.TryGetHidDevice(out HIDDevice_Discriptor, vendorID: 6790, productID: 57352);
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
                        //Debug.WriteLine(valueRAW, type.ToString());
                        //System_NewValue(value, type, valueRAW);
                        //TargetLabDevice.NewValue(value);
                        //DelegateForNewValue(value);
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
                return;
            }
        }

        public bool TryParceData(byte [] data, out decimal Resoult, out LabDevice.DataTypes CurrentType, out string ResoultInRAW)
        {
            Resoult = 0;
            ResoultInRAW = "";
            CurrentType = LabDevice.DataTypes.Abstract;
            if (data[1] != 240 && data[1] != 0)
            {
                LocalBuffer = LocalBuffer.Concat(data.Skip(2).Take(data[1]-240).ToArray()).ToArray();
            }
            //Debug.WriteLine(LocalBuffer.Length);
            int EndPos = IndexOf(LocalBuffer, new byte[] { 0x0D, 0x8A }, 0);
            if (EndPos != -1)
            {
                if (EndPos >= 9)
                {
                    //Debug.WriteLine(EndPos);
                    int CurrentStart = EndPos - 9;
                    int [] digits = new int[5];
                    digits[0] = (LocalBuffer[CurrentStart+0] & 0x0F);
                    digits[1] = (LocalBuffer[CurrentStart+1] & 0x0F);
                    digits[2] = (LocalBuffer[CurrentStart+2] & 0x0F);
                    digits[3] = (LocalBuffer[CurrentStart+3] & 0x0F);
                    digits[4] = (LocalBuffer[CurrentStart+4] & 0x0F);

                    Resoult = digits[4] + digits[3] * 10 + digits[2] * 100 + digits[1] * 1000 + digits[0] * 10000;
                    int rangeindex = LocalBuffer[CurrentStart + 5] & 0x0F;
                    int function = (LocalBuffer[CurrentStart + 6] & 0x0F);
                    decimal multi = (decimal)multlut[function, rangeindex];
                    Resoult *= multi;
                    if ((LocalBuffer[CurrentStart + 8] & 0x04)>0) Resoult *= -1;
                    switch (function)
                    {
                        case 0: case 1: case 2: case 3: CurrentType = LabDevice.DataTypes.Voltage; break;
                        case 4: CurrentType = LabDevice.DataTypes.Resistance; break;
                        case 5: CurrentType = LabDevice.DataTypes.Capacity; break;
                        case 6: CurrentType = LabDevice.DataTypes.Temperature; break;
                        case 7: case 8: case 9: CurrentType = LabDevice.DataTypes.Current; break;
                        case 12: CurrentType = LabDevice.DataTypes.Freq; break;
                    }
                    ResoultInRAW = BitConverter.ToString(LocalBuffer.Skip(CurrentStart).ToArray());
                    LocalBuffer = new byte[1];
                    if (digits[0] == 0x0a && digits[1] == 0x0a && digits[2] == 0x00 && digits[3] == 0x0c && digits[4] == 0x0a)
                    {
                        Resoult = 0;
                        return false;
                    }
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
