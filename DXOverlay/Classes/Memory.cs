using System;
using System.Linq;
using System.Text;
using System.Security;
using System.Numerics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Memory
{
    public class Memorys
    {
        public static System.Diagnostics.Process TargetProcess = default;
        public static string Name = string.Empty;
        public static int Id = default;
        public static IntPtr Handle = default;
        public static int BaseAddress = default;

        public static Enums.InitalizeResult Initalize(string ProcessName)
        {
            System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcessesByName(ProcessName);
            if (processes.Length != 0)
            {
                try
                {
                    TargetProcess = processes.FirstOrDefault();
                    Name = TargetProcess.ProcessName;
                    Id = TargetProcess.Id;
                    BaseAddress = (Int32)TargetProcess.MainModule.BaseAddress;
                    Handle = WinAPI.OpenProcess(Enums.ProcessAccessFlags.VirtualMemoryRead | Enums.ProcessAccessFlags.VirtualMemoryOperation, false, Id);

                    return Enums.InitalizeResult.OK;
                }
                catch
                {
                    return Enums.InitalizeResult.ACCES_VIOLATION_ERROR;
                }
            }
            else
            {
                return Enums.InitalizeResult.PROCESS_NOT_FOUND;
            }
        }

        public static Enums.InitalizeResult Initalize(string ProcessName, Enums.ProcessAccessFlags ExtraFlags)
        {
            System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcessesByName(ProcessName);
            if (processes.Length != 0)
            {
                try
                {
                    TargetProcess = processes.FirstOrDefault();
                    Name = TargetProcess.ProcessName;
                    Id = TargetProcess.Id;
                    BaseAddress = (Int32)TargetProcess.MainModule.BaseAddress;
                    Handle = WinAPI.OpenProcess(Enums.ProcessAccessFlags.VirtualMemoryRead | Enums.ProcessAccessFlags.VirtualMemoryOperation | ExtraFlags, false, Id);

                    return Enums.InitalizeResult.OK;
                }
                catch
                {
                    return Enums.InitalizeResult.ACCES_VIOLATION_ERROR;
                }
            }
            else
            {
                return Enums.InitalizeResult.PROCESS_NOT_FOUND;
            }
        }

        public static void Restore()
        {
            try
            {
                TargetProcess = default;
                Name = string.Empty;
                Id = default;
                if (Handle != default || Handle != IntPtr.Zero || Handle != null)
                    WinAPI.CloseHandle(Handle);
            }
            catch { }
        }

        #region Read

        private static IntPtr BytesRead = IntPtr.Zero;
        private static IntPtr BytesWritten = IntPtr.Zero;

        public static byte[] ReadMemory(int address, int size)
        {
            byte[] buffer = new byte[size];
            WinAPI.ReadProcessMemory(Handle, address, buffer, buffer.Length, out BytesRead);
            return buffer;
        }

        public static bool ReadBytes(int address, ref byte[] buffer)
        {
            return WinAPI.ReadProcessMemory(Handle, address, buffer, buffer.Length, out BytesRead) ? true : false;
        }

        public static byte[] ReadBytes(int address, int size)
        {
            byte[] buffer = new byte[size];
            WinAPI.ReadProcessMemory(Handle, address, buffer, buffer.Length, out BytesRead);
            return buffer;
        }

        public static T Read<T>(int address) where T : struct
        {
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
            WinAPI.ReadProcessMemory(Handle, address, buffer, buffer.Length, out BytesRead);
            return Transformation.ByteArrayToStructure<T>(buffer);
        }

        public static T Read<T>(int address, int[] offsets) where T : struct
        {
            return Read<T>((int)FindDMAAddy(new IntPtr(address), offsets));
        }

        public static IntPtr FindDMAAddy(IntPtr ptr, int[] offsets)
        {
            var buffer = new byte[IntPtr.Size];
            foreach (int i in offsets)
            {
                WinAPI.ReadProcessMemory(Handle, (int)ptr, buffer, buffer.Length, out BytesRead);

                ptr = (IntPtr.Size == 4)
                ? IntPtr.Add(new IntPtr(BitConverter.ToInt32(buffer, 0)), i)
                : ptr = IntPtr.Add(new IntPtr(BitConverter.ToInt64(buffer, 0)), i);
            }
            return ptr;
        }

        public static float[] ReadMatrix<T>(int address, int matrixSize) where T : struct
        {
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T)) * matrixSize];
            WinAPI.ReadProcessMemory(Handle, address, buffer, buffer.Length, out BytesRead);
            return Transformation.ConvertToFloatArray(buffer);
        }

        public static string ReadString(int address, int size, Encoding encoding = default)
        {
            return Transformation.CutString(encoding.GetString(ReadMemory(address, size)));
        }

        #endregion

        #region Features

        public static int GetModuleAddress(string module_name)
        {
            foreach (ProcessModule module in TargetProcess.Modules)
            {
                if (module.ModuleName == module_name)
                {
                    return (Int32)module.BaseAddress;
                }
            }
            return -1;
        }

        public static bool WorldToScreen(Vector3 target, out Vector2 pos, float[] viewmatrix, int width, int height, Enums.GraphicAPI graphicApi = Enums.GraphicAPI.DirectX)
        {
            //Matrix-vector Product, multiplying world(eye) coordinates by projection matrix = clipCoords
            pos = new Vector2(0, 0);
            Vector4 clipCoords = new Vector4()
            {
                X = target.X * viewmatrix[0] + target.Y * viewmatrix[(graphicApi == Enums.GraphicAPI.DirectX) ? 1 : 4] + target.Z * viewmatrix[(graphicApi == Enums.GraphicAPI.DirectX) ? 2 : 8] + viewmatrix[(graphicApi == Enums.GraphicAPI.DirectX) ? 3 : 12],
                Y = target.X * viewmatrix[(graphicApi == Enums.GraphicAPI.DirectX) ? 4 : 1] + target.Y * viewmatrix[(graphicApi == Enums.GraphicAPI.DirectX) ? 5 : 5] + target.Z * viewmatrix[(graphicApi == Enums.GraphicAPI.DirectX) ? 6 : 9] + viewmatrix[(graphicApi == Enums.GraphicAPI.DirectX) ? 7 : 13],
                Z = target.X * viewmatrix[(graphicApi == Enums.GraphicAPI.DirectX) ? 8 : 2] + target.Y * viewmatrix[(graphicApi == Enums.GraphicAPI.DirectX) ? 9 : 6] + target.Z * viewmatrix[(graphicApi == Enums.GraphicAPI.DirectX) ? 10 : 10] + viewmatrix[(graphicApi == Enums.GraphicAPI.DirectX) ? 11 : 14],
                W = target.X * viewmatrix[(graphicApi == Enums.GraphicAPI.DirectX) ? 12 : 3] + target.Y * viewmatrix[(graphicApi == Enums.GraphicAPI.DirectX) ? 13 : 7] + target.Z * viewmatrix[(graphicApi == Enums.GraphicAPI.DirectX) ? 14 : 11] + viewmatrix[15]
            };

            if (clipCoords.W < 0.1f)
                return false;

            //perspective division, dividing by clip.W = Normalized Device Coordinates
            Vector3 NDC;
            NDC.X = clipCoords.X / clipCoords.W;
            NDC.Y = clipCoords.Y / clipCoords.W;
            NDC.Z = clipCoords.Z / clipCoords.W;

            pos.X = (width / 2 * NDC.X) + (NDC.X + width / 2);
            pos.Y = -(height / 2 * NDC.Y) + (NDC.Y + height / 2);
            return true;
        }

        public static int ScanPattern(string module_name, string signature)
        {
            byte[] ModuleBuffer = null;
            int ModuleAddress = -1;
            foreach (ProcessModule process_module in TargetProcess.Modules)
            {
                if (process_module.ModuleName == module_name)
                {
                    ModuleBuffer = new byte[process_module.ModuleMemorySize];
                    ModuleAddress = (Int32)process_module.BaseAddress;
                }
            }

            if (ModuleAddress == -1 || ModuleBuffer == null)
                return -1; // MODULE NOT FOUND

            byte[] pattern = Transformation.SignatureToPattern(signature);
            string mask = Transformation.GetSignatureMask(signature);

            if (ReadBytes(ModuleAddress, ref ModuleBuffer))
            {
                for (int i = 0; i < ModuleBuffer.Length; i++)
                {
                    bool IsFounded = false;

                    for (int a = 0; a < pattern.Length; a++)
                    {
                        if (mask[a] == '?')
                            continue;

                        if (pattern[a] == ModuleBuffer[i + a])
                            IsFounded = true;
                    }

                    if (!IsFounded) continue;
                    return i;
                }
            }

            return -1;
        }

        public static int ScanPattern(string module_name, string signature, int offset, int extra, bool relative)
        {
            byte[] ModuleBuffer = null;
            int ModuleAddress = -1;
            foreach (ProcessModule process_module in TargetProcess.Modules)
            {
                if (process_module.ModuleName == module_name)
                {
                    ModuleBuffer = new byte[process_module.ModuleMemorySize];
                    ModuleAddress = (Int32)process_module.BaseAddress;
                }
            }

            if (ModuleAddress == -1 || ModuleBuffer == null)
                return -1; // MODULE NOT FOUND

            byte[] pattern = Transformation.SignatureToPattern(signature);
            string mask = Transformation.GetSignatureMask(signature);

            if (ReadBytes(ModuleAddress, ref ModuleBuffer))
            {
                int Address = -1;

                for (int i = 0; i < ModuleBuffer.Length; i++)
                {
                    bool IsFounded = false;

                    for (int a = 0; a < pattern.Length; a++)
                    {
                        if (mask[a] == '?')
                            continue;

                        if (pattern[a] == ModuleBuffer[i + a])
                            IsFounded = true;
                    }

                    if (!IsFounded) continue;

                    if (relative)
                        Address = BitConverter.ToInt32(ReadBytes(i + offset, 4), 0) + extra;
                    else
                        Address = BitConverter.ToInt32(ReadBytes(ModuleAddress + i + offset, 4), 0) + extra;

                    return Address;
                }
            }

            return -1;
        }

        #endregion

        class Transformation
        {
            public static byte[] SignatureToPattern(string sig)
            {
                string[] parts = sig.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                byte[] patternArray = new byte[parts.Length];

                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i] == "?")
                    {
                        patternArray[i] = 0;
                        continue;
                    }

                    if (!byte.TryParse(parts[i], System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.DefaultThreadCurrentCulture, out patternArray[i]))
                    {
                        throw new Exception();
                    }
                }

                return patternArray;
            }

            public static string GetSignatureMask(string sig)
            {
                string[] parts = sig.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                string mask = "";

                for (int i = 0; i < parts.Length; i++)
                {
                    if (parts[i] == "?")
                    {
                        mask += "?";
                    }
                    else
                    {
                        mask += "x";
                    }
                }

                return mask;
            }

            public static string CutString(string mystring)
            {
                char[] chArray = mystring.ToCharArray();
                string str = "";
                for (int i = 0; i < mystring.Length; i++)
                {
                    if ((chArray[i] == ' ') && (chArray[i + 1] == ' '))
                    {
                        return str;
                    }
                    if (chArray[i] == '\0')
                    {
                        return str;
                    }
                    str = str + chArray[i].ToString();
                }
                return mystring.TrimEnd(new char[] { '0' });
            }

            public static float[] ConvertToFloatArray(byte[] bytes)
            {
                if (bytes.Length % 4 != 0) throw new ArgumentException();

                float[] floats = new float[bytes.Length / 4];

                for (int i = 0; i < floats.Length; i++) floats[i] = BitConverter.ToSingle(bytes, i * 4);

                return floats;
            }

            public static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
            {
                GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);

                try
                {
                    return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
                }
                finally
                {
                    handle.Free();
                }
            }

            public static byte[] StructureToByteArray(object obj)
            {
                int length = Marshal.SizeOf(obj);

                byte[] array = new byte[length];

                IntPtr pointer = Marshal.AllocHGlobal(length);

                Marshal.StructureToPtr(obj, pointer, true);
                Marshal.Copy(pointer, array, 0, length);
                Marshal.FreeHGlobal(pointer);

                return array;
            }
        }

        public class Enums
        {
            public enum InitalizeResult
            {
                OK,
                PROCESS_NOT_FOUND,
                ACCES_VIOLATION_ERROR
            }

            public enum GraphicAPI
            {
                DirectX,
                OpenGL
            }

            // ABOUT PROCESS MEMORY READING
            [Flags]
            public enum ProcessAccessFlags : uint
            {
                All = 0x001F0FFF,
                Terminate = 0x00000001,
                CreateThread = 0x00000002,
                VirtualMemoryOperation = 0x00000008,
                VirtualMemoryRead = 0x00000010,
                DuplicateHandle = 0x00000040,
                CreateProcess = 0x000000080,
                SetQuota = 0x00000100,
                SetInformation = 0x00000200,
                QueryInformation = 0x00000400,
                QueryLimitedInformation = 0x00001000,
                Synchronize = 0x00100000
            }

        }

        class WinAPI
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr OpenProcess(
                 uint processAccess,
                 bool bInheritHandle,
                 int processId
            );

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr OpenProcess(
                 Enums.ProcessAccessFlags processAccess,
                 bool bInheritHandle,
                 int processId
            );

            [DllImport("kernel32.dll", SetLastError = true)]
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [SuppressUnmanagedCodeSecurity]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool ReadProcessMemory(
                IntPtr hProcess,
                int lpBaseAddress,
                [Out] byte[] lpBuffer,
                int dwSize,
                out IntPtr lpNumberOfBytesRead
            );

        }
    }
}