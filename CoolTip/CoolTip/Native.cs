using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace CoolTip
{
    internal static class Native
    {
        [DllImport("User32")]
        public static extern bool SetWindowPos(HandleRef hWnd, HandleRef hWndInsertAfter,
            int x, int y, int cx, int cy, int flags);

        [DllImport("User32", EntryPoint = "GetWindowLong")]
        public static extern IntPtr GetWindowLong32(HandleRef hWnd, int nIndex);

        [DllImport("User32", EntryPoint = "GetWindowLongPtr")]
        public static extern IntPtr GetWindowLongPtr64(HandleRef hWnd, int nIndex);

        [DllImport("User32")]
        public static extern IntPtr BeginPaint(HandleRef hWnd, [In, Out] ref PAINTSTRUCT lpPaint);

        [DllImport("User32")]
        public static extern bool EndPaint(HandleRef hWnd, ref PAINTSTRUCT lpPaint);

        [DllImport("User32")]
        public static extern bool GetWindowRect(HandleRef hWnd, [In, Out] ref RECT rect);

        [DllImport("User32")]
        public static extern bool ShowWindow(HandleRef hWnd, int nCmdShow);

        [DllImport("User32")]
        public static extern bool UpdateWindow(HandleRef hWnd);

        [DllImport("User32")]
        public static extern bool RedrawWindow(HandleRef hWnd,
            IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        [DllImport("User32")]
        public static extern IntPtr GetForegroundWindow();

        public static HandleRef HWND_TOPMOST = new HandleRef(null, new IntPtr(-1));

        public const int WS_POPUP = unchecked((int)0x80000000);
        public const int WS_EX_NOACTIVATE = 0x08000000;
        public const int WS_EX_TOPMOST = 0x00000008;

        public const int WM_USER = 0x0400;
        public const int WM_WINDOWPOSCHANGING = 0x0046;
        public const int WM_WINDOWPOSCHANGED = 0x0047;
        public const int WM_MOUSEACTIVATE = 0x0021;
        public const int WM_MOVE = 0x0003;
        public const int WM_PRINTCLIENT = 0x0318;
        public const int WM_PAINT = 0x000F;
        public const int WM_MOUSEMOVE = 0x0200;
        public const int WM_MOUSELEAVE = 0x02A3;
        public const int WM_KEYDOWN = 0x0100;

        public const int SWP_NOACTIVATE = 0x0010;
        public const int SWP_FRAMECHANGED = 0x0020;
        public const int SWP_NOOWNERZORDER = 0x0200;

        public const int SW_HIDE = 0;
        public const int SW_SHOWNOACTIVATE = 4;

        public const int RDW_INVALIDATE = 0x0001;

        [StructLayout(LayoutKind.Sequential)]
        public struct PAINTSTRUCT
        {
            public IntPtr hdc;
            public bool fErase;
            // rcPaint was a by-value RECT structure
            public int rcPaint_left;
            public int rcPaint_top;
            public int rcPaint_right;
            public int rcPaint_bottom;
            public bool fRestore;
            public bool fIncUpdate;
            public int reserved1;
            public int reserved2;
            public int reserved3;
            public int reserved4;
            public int reserved5;
            public int reserved6;
            public int reserved7;
            public int reserved8;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }

            public RECT(Rectangle r)
            {
                this.left = r.Left;
                this.top = r.Top;
                this.right = r.Right;
                this.bottom = r.Bottom;
            }

            public static RECT FromXYWH(int x, int y, int width, int height)
            {
                return new RECT(x, y, x + width, y + height);
            }

            public Size Size
            {
                get
                {
                    return new Size(this.right - this.left, this.bottom - this.top);
                }
            }

            public int Width { get { return this.right - this.left; } }
            public int Height { get { return this.bottom - this.top; } }
        }

        public static IntPtr GetWindowLong(HandleRef hWnd, int nIndex)
        {
            if (IntPtr.Size == 4)
            {
                return GetWindowLong32(hWnd, nIndex);
            }
            return GetWindowLongPtr64(hWnd, nIndex);
        }

        public static int SignedHIWORD(IntPtr n)
        {
            return SignedHIWORD(unchecked((int)(long)n));
        }

        public static int SignedLOWORD(IntPtr n)
        {
            return SignedLOWORD(unchecked((int)(long)n));
        }

        public static int SignedHIWORD(int n)
        {
            int i = (int)(short)((n >> 16) & 0xffff);

            return i;
        }

        public static int SignedLOWORD(int n)
        {
            int i = (int)(short)(n & 0xFFFF);

            return i;
        }
    }

}
