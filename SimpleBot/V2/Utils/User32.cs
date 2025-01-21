using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace SimpleBot.v2
{
    static class User32
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern int GetWindowText(nint hwnd, StringBuilder lpString, int nMaxCount);

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT_mouse
        {
            public uint type = 0;
            public MOUSEINPUT U;
            public static int Size
            {
                get { return Marshal.SizeOf(typeof(INPUT_mouse)); }
            }

            public INPUT_mouse(int dx, int dy, int mouseData, MOUSEEVENTF dwFlags)
            {
                U = new()
                {
                    dx = dx,
                    dy = dy,
                    mouseData = mouseData,
                    dwFlags = dwFlags,
                    time = 0,
                    dwExtraInfo = 0
                };
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct MOUSEINPUT
        {
            internal int dx;
            internal int dy;
            internal int mouseData;
            internal MOUSEEVENTF dwFlags;
            internal uint time;
            internal UIntPtr dwExtraInfo;
        }
        [Flags]
        internal enum MOUSEEVENTF : uint
        {
            ABSOLUTE = 0x8000,
            HWHEEL = 0x01000,
            MOVE = 0x0001,
            MOVE_NOCOALESCE = 0x2000,
            LEFTDOWN = 0x0002,
            LEFTUP = 0x0004,
            RIGHTDOWN = 0x0008,
            RIGHTUP = 0x0010,
            MIDDLEDOWN = 0x0020,
            MIDDLEUP = 0x0040,
            VIRTUALDESK = 0x4000,
            WHEEL = 0x0800,
            XDOWN = 0x0080,
            XUP = 0x0100
        }
        [DllImport("user32.dll")] public static extern uint SendInput(uint nInputs, INPUT_mouse[] pInputs, int cbSize);
        [DllImport("user32.dll")] public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, UInt32 uFlags);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
        [DllImport("User32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("User32.dll")] public static extern uint RegisterWindowMessage(string Message);
        [DllImport("User32.dll")] public static extern bool RegisterShellHook(IntPtr hWnd, int flags);

        /// Registers a specified Shell window to receive certain messages for events or notifications that are useful to
        /// Shell applications. The event messages received are only those sent to the Shell window associated with the
        /// specified window's desktop. Many of the messages are the same as those that can be received after calling
        /// the SetWindowsHookEx function and specifying WH_SHELL for the hook type. The difference with
        /// RegisterShellHookWindow is that the messages are received through the specified window's WindowProc
        /// and not through a call back procedure. 
        /// </summary>
        /// <param name="hWnd">[in] Handle to the window to register for Shell hook messages.</param>
        /// <returns>TRUE if the function succeeds; FALSE if the function fails. </returns>
        /// <remarks>
        /// As with normal window messages, the second parameter of the window procedure identifies the
        /// message as a "WM_SHELLHOOKMESSAGE". However, for these Shell hook messages, the
        /// message value is not a pre-defined constant like other message identifiers (IDs) such as
        /// WM_COMMAND. The value must be obtained dynamically using a call to
        /// RegisterWindowMessage(TEXT("SHELLHOOK"));. This precludes handling these messages using
        /// a traditional switch statement which requires ID values that are known at compile time.
        /// For handling Shell hook messages, the normal practice is to code an If statement in the default
        /// section of your switch statement and then handle the message if the value of the message ID
        /// is the same as the value obtained from the RegisterWindowMessage call. 
        /// 
        /// for more see MSDN
        /// </remarks>
        [DllImport("User32.dll")]
        public static extern bool RegisterShellHookWindow(IntPtr hWnd);
    }
}