using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ExternalUserCMD // https:github.com/Lufzys/ExternalUserCMD/blob/main/ExternalUserCMD/ExternalUserCMD/UserCMD.cs
{
    class UserCMD // The project written in C++ is adapted to C# by Lufzys1337 -> Original Source Code : https://www.unknowncheats.me/forum/counterstrike-global-offensive/468386-external-shellcode-execute-console-command.html
    {
        public static void Execute(string command)
        {
            IntPtr m_hEngine = WinAPI.FindWindowA("Valve001", null);
            if (m_hEngine == null) return;

            IntPtr commandPtr = (IntPtr)Marshal.StringToHGlobalAnsi(command);
            Structs.COPYDATASTRUCT copyData = new Structs.COPYDATASTRUCT();
            copyData.dwData = IntPtr.Zero;
            copyData.lpData = commandPtr;
            copyData.cbData = command.Length + 1;
            IntPtr copyDataBuff = Functions.IntPtrAlloc(copyData);
            WinAPI.SendMessageA(m_hEngine, Constants.WM_COPYDATA, IntPtr.Zero, copyDataBuff);

            // Free
            Functions.IntPtrFree(ref copyDataBuff);
            Functions.IntPtrFree(ref commandPtr);
        }
    }

    class WinAPI
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SendMessageA(IntPtr hWnd, int wMsg, IntPtr wParam, IntPtr lParam); // https://www.pinvoke.net/default.aspx/user32.SendMessageA

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindowA(string lpClassName, string lpWindowName); // https://www.pinvoke.net/default.aspx/user32/FindWindowA.html
    }

    class Structs // https://www.pinvoke.net/default.aspx/Structures/COPYDATASTRUCT.html
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct COPYDATASTRUCT
        {
            public IntPtr dwData;    // Any value the sender chooses.  Perhaps its main window handle?
            public int cbData;       // The count of bytes in the message.
            public IntPtr lpData;    // The address of the message.
        }
    }

    class Constants
    {
        public const int WM_COPYDATA = 0x004A;
    }

    class Functions // https://www.pinvoke.net/default.aspx/Structures/COPYDATASTRUCT.html
    {
        public static IntPtr IntPtrAlloc<T>(T param)
        {
            IntPtr retval = Marshal.AllocHGlobal(Marshal.SizeOf(param));
            Marshal.StructureToPtr(param, retval, false);
            return retval;
        }

        public static void IntPtrFree(ref IntPtr preAllocated)
        {
            if (IntPtr.Zero == preAllocated)
                throw (new NullReferenceException("Go Home"));
            Marshal.FreeHGlobal(preAllocated);
            preAllocated = IntPtr.Zero;
        }
    }
}