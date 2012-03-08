using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics.Eventing;
using Microsoft.SharePoint.Administration;
using System.Security.AccessControl;
using System.Threading;
using System.Security.Principal;

namespace Phoenix.SharePointHelpers
{
    public class SPTraceLogger : EventProvider
    {
        private static string s_ExeName = System.Diagnostics.Process.GetCurrentProcess().ProcessName;

        private const uint TRACE_VERSION_CURRENT = 2;
        private const uint TRACE_FLUSH_TAG = 0xFFFFFFFFu;
        private const TraceSeverity TRACE_FLUSH_LEVEL = 0;
        private const string TRACE_FLUSH_EVENT_NAME = "Global\\Tracing_Service_Flush_Event";

        [Flags]
        enum TraceFlags
        {
            None = 0,
            TRACE_FLAG_START = 1,
            TRACE_FLAG_END = 2,
            TRACE_FLAG_MIDDLE = 3, // Notice middle==start+end, allows use as a bitmask.
            TRACE_FLAG_FLUSH = 8, // Ask trace manager to flush all file buffers.
        }

        public enum TraceSeverity : byte
        {
            CriticalEvent = 1,
            Exception = 4,
            Assert = 7,
            WarningEvent = 8,
            Unexpected = 10,
            Monitorable = 15,
            InformationEvent = 18,
            High = 20,
            Medium = 50,
            Verbose = 100,
            VerboseEx = 200
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 4)]
        private struct Payload
        {
            public ushort Size;
            public uint dwVersion;
            public uint Id;
            public TraceFlags dwFlags;
            public long TimeStamp;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string wzExeName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string wzProduct;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string wzCategory;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 801)]
            public string wzMessage;
        }

        // Wrapper class used to manually marshal data to unmanaged memory.
        // This lets us avoid using the "unsafe" keyword.
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct EVENT_DATA_DESCRIPTOR : IDisposable
        {
            IntPtr Ptr;      // Pointer to data.
            uint Size;       // Size of data in bytes.
            uint Reserved;

            public EVENT_DATA_DESCRIPTOR(Payload payload)
            {
                Ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Payload)));
                Marshal.StructureToPtr(payload, Ptr, false);
                Size = (uint)Marshal.SizeOf(typeof(Payload));
                Reserved = 0;
            }

            void IDisposable.Dispose()
            {
                Marshal.FreeHGlobal(Ptr);
            }
        }

        // Wrapper class used to manually marshal data to unmanaged memory.
        // This lets us avoid using the "unsafe" keyword.
        struct DataDescriptorWrapper : IDisposable
        {
            public IntPtr Ptr;     // Pointer to data

            public DataDescriptorWrapper(EVENT_DATA_DESCRIPTOR descriptor)
            {
                Ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(EVENT_DATA_DESCRIPTOR)));
                Marshal.StructureToPtr(descriptor, Ptr, false);
            }

            void IDisposable.Dispose()
            {
                Marshal.FreeHGlobal(Ptr);
            }
        }

        public SPTraceLogger()
            : base(SPFarm.Local.TraceSessionGuid)
        {
        }

        private void WriteImpl(TraceSeverity level, Payload payload)
        {
            EventDescriptor descriptor = new EventDescriptor(0, 0, 0, (byte)level, 0, 0, 0);
            using (EVENT_DATA_DESCRIPTOR data = new EVENT_DATA_DESCRIPTOR(payload))
            using (DataDescriptorWrapper wrapper = new DataDescriptorWrapper(data))
            {
                bool fResult = WriteEvent(ref descriptor, 1, wrapper.Ptr);
                if (!fResult)
                    Console.WriteLine("Failed to call WriteEvent for real payload {0}", Marshal.GetLastWin32Error());
            }
        }

        private void Write(TraceFlags flags, uint id, TraceSeverity level, string exeName, string area, string category, string message)
        {
            Payload payload = new Payload();
            payload.Size = (ushort)Marshal.SizeOf(typeof(Payload));
            payload.dwVersion = TRACE_VERSION_CURRENT;
            payload.Id = id;
            payload.TimeStamp = DateTime.Now.ToFileTime();
            payload.wzExeName = exeName;
            payload.wzProduct = area;
            payload.wzCategory = category;

            // If the message is smaller than 800 characters, no need to break it up.
            if (message == null || message.Length <= 800)
            {
                payload.wzMessage = message;
                payload.dwFlags = flags;
                WriteImpl(level, payload);
                return;
            }

            // For larger messages, break it into 800 character chunks.
            for (int i = 0; i < message.Length; i += 800)
            {
                int cchRemaining = Math.Min(800, message.Length - i);
                payload.wzMessage = message.Substring(i, cchRemaining);

                if (i == 0)
                    payload.dwFlags = TraceFlags.TRACE_FLAG_START | flags;
                else if (i + 800 < message.Length)
                    payload.dwFlags = TraceFlags.TRACE_FLAG_MIDDLE | flags;
                else
                    payload.dwFlags = TraceFlags.TRACE_FLAG_END | flags;

                WriteImpl(level, payload);
            }
        }

        public void Write(uint id, TraceSeverity level, string area, string category, string message)
        {
            Write(TraceFlags.None, id, level, s_ExeName, area, category, message);
        }

        public void Write(uint id, TraceSeverity level, string exeName, string area, string category, string message)
        {
            Write(TraceFlags.None, id, level, exeName, area, category, message);
        }

        private void FlushImpl()
        {
            Write(TraceFlags.TRACE_FLAG_FLUSH, TRACE_FLUSH_TAG, TRACE_FLUSH_LEVEL, "", "", "", " ");
        }

        public bool Flush(int timeout)
        {
            // Special case for timeout = 0; just send the request and immediately return.
            if (timeout == 0)
            {
                FlushImpl();
                return true;
            }

            // Create the wait handle with appropriate permissions.
            bool fCreatedNew;
            EventWaitHandleSecurity security = new EventWaitHandleSecurity();
            security.SetAccessRule(new EventWaitHandleAccessRule(new SecurityIdentifier(WellKnownSidType.ServiceSid, null), EventWaitHandleRights.Modify, AccessControlType.Allow));

            using (EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset, TRACE_FLUSH_EVENT_NAME, out fCreatedNew, security))
            {
                // Request the trace service to flush data, and wait for the service to signal completion.
                FlushImpl();
                return waitHandle.WaitOne(timeout);
            }
        }

        public bool Flush()
        {
            return Flush(5000);
        }

        public static uint TagFromString(string wzTag)
        {
            System.Diagnostics.Debug.Assert(wzTag.Length == 4);
            return (uint)(wzTag[0] << 24 | wzTag[1] << 16 | wzTag[2] << 8 | wzTag[3]);
        }
    }
}
