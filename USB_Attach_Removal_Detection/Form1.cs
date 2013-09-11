using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace USB_Attach_Removal_Detection
{
    public partial class Form1 : Form
    {
        internal const Int32 DBT_DEVTYP_DEVICEINTERFACE = 5;
        internal const Int32 DBT_DEVICEARRIVAL = 0X8000;
        internal const Int32 DBT_DEVICEREMOVECOMPLETE = 0X8004;
        internal const Int32 DEVICE_NOTIFY_WINDOW_HANDLE = 0;
        internal const Int32 DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 4;
        internal const Int32 DIGCF_PRESENT = 2;
        internal const Int32 DIGCF_DEVICEINTERFACE = 0X10;
        internal const Int32 WM_DEVICECHANGE = 0X219;

        [StructLayout(LayoutKind.Sequential)]
        internal class DEV_BROADCAST_DEVICEINTERFACE
        {
            internal Int32 dbcc_size;
            internal Int32 dbcc_devicetype;
            internal Int32 dbcc_reserved;
            internal Guid dbcc_classguid;
            internal Int16 dbcc_name;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal class DEV_BROADCAST_HDR
        {
            internal Int32 dbch_size;
            internal Int32 dbch_devicetype;
            internal Int32 dbch_reserved;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal class DEV_BROADCAST_DEVICEINTERFACE_1
        {
            internal Int32 dbcc_size;
            internal Int32 dbcc_devicetype;
            internal Int32 dbcc_reserved;
            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 16)]
            internal Byte[] dbcc_classguid;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
            internal Char[] dbcc_name;
        }
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, Int32 Flags);
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern Boolean UnregisterDeviceNotification(IntPtr Handle);
        [DllImport("hid.dll", SetLastError = true)]
        public static extern void HidD_GetHidGuid(ref System.Guid HidGuid);

        // variables
        private Boolean USB_Printing = false;
        internal Form1 frmMy;
        private System.Guid myGuid;
        private IntPtr deviceNotificationHandle;
        // End of variables

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            while (USB_Printing==true);
            textBox1.Clear();
        }

        internal void OnDeviceChange(Message m)
        {
            DEV_BROADCAST_DEVICEINTERFACE_1 devBroadcastDeviceInterface = new DEV_BROADCAST_DEVICEINTERFACE_1();
            DEV_BROADCAST_HDR devBroadcastHeader = new DEV_BROADCAST_HDR();

            if ((int)m.LParam != 0)
            {
                Marshal.PtrToStructure(m.LParam, devBroadcastHeader);
            }
            else
            {
                return;
            }

            if ((devBroadcastHeader.dbch_devicetype == DBT_DEVTYP_DEVICEINTERFACE))
            {
                Int32 stringSize = Convert.ToInt32((devBroadcastHeader.dbch_size - 32) / 2);
                Array.Resize(ref devBroadcastDeviceInterface.dbcc_name, stringSize);
                Marshal.PtrToStructure(m.LParam, devBroadcastDeviceInterface);
                String deviceNameString = new String(devBroadcastDeviceInterface.dbcc_name, 0, stringSize);

                USB_Printing = true;

                if ((m.WParam.ToInt32() == DBT_DEVICEARRIVAL))
                {
                    textBox1.AppendText(deviceNameString);
                    textBox1.AppendText(" attached\r\n");
                }
                else if ((m.WParam.ToInt32() == DBT_DEVICEREMOVECOMPLETE))
                {
                    textBox1.AppendText(deviceNameString);
                    textBox1.AppendText(" removed\r\n");
                }

                USB_Printing = false;

            }
        }

        protected override void WndProc(ref Message m)
        {
            // Processing event notification
            if (m.Msg == WM_DEVICECHANGE)
            {
                OnDeviceChange(m);
            }
            base.WndProc(ref m);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Register device notification in Form_Load
            HidD_GetHidGuid(ref myGuid);

            frmMy = this;
            DEV_BROADCAST_DEVICEINTERFACE devBroadcastDeviceInterface = new DEV_BROADCAST_DEVICEINTERFACE();
            IntPtr devBroadcastDeviceInterfaceBuffer;
            Int32 size = 0;

            size = Marshal.SizeOf(devBroadcastDeviceInterface);
            devBroadcastDeviceInterface.dbcc_size = size;
            devBroadcastDeviceInterface.dbcc_devicetype = DBT_DEVTYP_DEVICEINTERFACE;
            devBroadcastDeviceInterface.dbcc_reserved = 0;
            devBroadcastDeviceInterface.dbcc_classguid = myGuid;
            devBroadcastDeviceInterfaceBuffer = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(devBroadcastDeviceInterface, devBroadcastDeviceInterfaceBuffer, true);
            deviceNotificationHandle = RegisterDeviceNotification(frmMy.Handle, devBroadcastDeviceInterfaceBuffer,
                                        (DEVICE_NOTIFY_WINDOW_HANDLE | DEVICE_NOTIFY_ALL_INTERFACE_CLASSES));
            Marshal.FreeHGlobal(devBroadcastDeviceInterfaceBuffer);
        }

        private void Form1_FormClosing(Object sender, FormClosingEventArgs e)
        {
            // un-register device notification in Form_Closing
            UnregisterDeviceNotification(deviceNotificationHandle);
        }

        
        /*
        private void Form1_Load(object sender, EventArgs e)
        {
        }

*/
    }
}
