using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Automation;
using WindowsRect = System.Windows.Rect;

namespace Desktop4AI
{
    public class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr OpenInputDesktop(uint dwFlags, bool fInherit, uint dwDesiredAccess);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetThreadDesktop(IntPtr hDesktop);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool CloseDesktop(IntPtr hDesktop);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        public static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        public static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;
        private const uint MOUSEEVENTF_WHEEL = 0x0800;

        [StructLayout(LayoutKind.Sequential)]
        public struct INPUT
        {
            public uint type;
            public KEYBDINPUT ki;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
            public uint padding1;
            public uint padding2;
        }

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        private const uint INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint KEYEVENTF_UNICODE = 0x0004;

        public static void MoveCursorHardware(int x, int y)
        {
            int sw = GetSystemMetrics(0);
            int sh = GetSystemMetrics(1);
            SetCursorPos(x, y);
            uint absX = (uint)((x * 65535) / Math.Max(1, sw));
            uint absY = (uint)((y * 65535) / Math.Max(1, sh));
            mouse_event(0x0001 | 0x8000, absX, absY, 0, UIntPtr.Zero);
        }

        public static void SmoothMoveTo(int targetX, int targetY, int durationMs = 250)
        {
            POINT start;
            GetCursorPos(out start);
            int steps = Math.Max(10, durationMs / 12);
            for (int i = 1; i <= steps; i++)
            {
                double t = (double)i / steps;
                // Ease-out cubic curve for natural human motion
                double factor = 1.0 - Math.Pow(1.0 - t, 3);
                int curX = (int)(start.X + (targetX - start.X) * factor);
                int curY = (int)(start.Y + (targetY - start.Y) * factor);
                MoveCursorHardware(curX, curY);
                Thread.Sleep(12);
            }
            MoveCursorHardware(targetX, targetY);
        }

        public class ElementNode
        {
            public int id;
            public string type;
            public string name;
            public int x;
            public int y;
            public int width;
            public int height;
            public int centerX;
            public int centerY;
            public bool isActionable;
        }

        public class WindowItem
        {
            public IntPtr handle;
            public uint pid;
            public string title;
            public int x;
            public int y;
            public int width;
            public int height;
        }

        public static int Main(string[] args)
        {
            try
            {
                SetProcessDPIAware();
            }
            catch { }

            if (args.Length == 0)
            {
                PrintHelp();
                return 0;
            }

            string cmd = args[0].ToLowerInvariant();
            try
            {
                switch (cmd)
                {
                    case "cursor":
                        return HandleCursor();
                    case "windows":
                        return HandleWindows();
                    case "focus":
                        return HandleFocus(args);
                    case "tree":
                        return HandleTree(args);
                    case "click":
                        return HandleClick(args);
                    case "move":
                        return HandleMove(args);
                    case "drag":
                        return HandleDrag(args);
                    case "scroll":
                        return HandleScroll(args);
                    case "type":
                        return HandleType(args);
                    case "hotkey":
                        return HandleHotkey(args);
                    case "capture":
                        return HandleCapture(args);
                    default:
                        Console.Error.WriteLine("Unknown command: " + cmd);
                        PrintHelp();
                        return 1;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message + "\n" + ex.StackTrace);
                return 2;
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Desktop4AI Native Engine v1.0.0");
            Console.WriteLine("Commands:");
            Console.WriteLine("  cursor                           - Get current mouse coordinates");
            Console.WriteLine("  windows                          - List visible desktop windows");
            Console.WriteLine("  focus <title|pid>                - Bring window to foreground");
            Console.WriteLine("  tree [--limit n] [--window title]- Extract visible interactive UI tree");
            Console.WriteLine("  click <x> <y> [left|right|double]- Click at coordinates");
            Console.WriteLine("  move <x> <y> [--smooth]          - Move cursor");
            Console.WriteLine("  drag <x1> <y1> <x2> <y2>         - Drag and drop");
            Console.WriteLine("  scroll <amount> [x] [y]          - Scroll mouse wheel (positive=up, negative=down)");
            Console.WriteLine("  type <text>                      - Type text or clipboard paste");
            Console.WriteLine("  hotkey <ctrl+c|enter|alt+tab>    - Press key combination");
            Console.WriteLine("  capture <out.png> [--som]        - Screenshot (optional Set-of-Marks overlay)");
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateThread(IntPtr lpThreadAttributes, uint dwStackSize, ThreadProc lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        public delegate uint ThreadProc(IntPtr lpParameter);

        private static Action s_pendingAction;
        private static Exception s_pendingException;

        private static uint NativeThreadWorker(IntPtr param)
        {
            IntPtr hDesktop = OpenInputDesktop(0, false, 0x01FF);
            if (hDesktop != IntPtr.Zero)
            {
                SetThreadDesktop(hDesktop);
            }
            try
            {
                if (s_pendingAction != null)
                {
                    s_pendingAction();
                }
            }
            catch (Exception ex)
            {
                s_pendingException = ex;
            }
            finally
            {
                if (hDesktop != IntPtr.Zero)
                {
                    CloseDesktop(hDesktop);
                }
            }
            return 0;
        }

        private static void RunOnInputDesktop(Action action)
        {
            s_pendingAction = action;
            s_pendingException = null;
            uint tid;
            IntPtr hThread = CreateThread(IntPtr.Zero, 0, NativeThreadWorker, IntPtr.Zero, 0, out tid);
            if (hThread == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to create native thread: " + Marshal.GetLastWin32Error());
            }
            WaitForSingleObject(hThread, 30000);
            CloseHandle(hThread);
            s_pendingAction = null;
            if (s_pendingException != null)
            {
                throw s_pendingException;
            }
        }

        private static int HandleCursor()
        {
            RunOnInputDesktop(delegate()
            {
                POINT p;
                GetCursorPos(out p);
                Console.WriteLine("{{\"x\": {0}, \"y\": {1}}}", p.X, p.Y);
            });
            return 0;
        }

        private static int HandleWindows()
        {
            List<WindowItem> list = new List<WindowItem>();
            RunOnInputDesktop(delegate()
            {
                EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
                {
                    if (!IsWindowVisible(hWnd)) return true;
                    StringBuilder sb = new StringBuilder(256);
                    GetWindowText(hWnd, sb, 256);
                    string title = sb.ToString().Trim();
                    if (string.IsNullOrEmpty(title) || title == "Program Manager") return true;

                    RECT r;
                    GetWindowRect(hWnd, out r);
                    int w = r.Right - r.Left;
                    int h = r.Bottom - r.Top;
                    if (w <= 30 || h <= 30) return true;

                    uint pid;
                    GetWindowThreadProcessId(hWnd, out pid);

                    list.Add(new WindowItem
                    {
                        handle = hWnd,
                        pid = pid,
                        title = title,
                        x = r.Left,
                        y = r.Top,
                        width = w,
                        height = h
                    });
                    return true;
                }, IntPtr.Zero);
            });

            StringBuilder json = new StringBuilder();
            json.Append("[");
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                if (i > 0) json.Append(",");
                json.Append(string.Format(
                    "{{\"pid\":{0},\"title\":{1},\"x\":{2},\"y\":{3},\"width\":{4},\"height\":{5}}}",
                    item.pid,
                    EscapeJson(item.title),
                    item.x,
                    item.y,
                    item.width,
                    item.height
                ));
            }
            json.Append("]");
            Console.WriteLine(json.ToString());
            return 0;
        }

        private static int HandleFocus(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Missing target window title or PID");
                return 1;
            }
            string target = args[1].ToLowerInvariant();
            uint targetPid = 0;
            uint.TryParse(target, out targetPid);

            bool found = false;
            RunOnInputDesktop(delegate()
            {
                EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
                {
                    if (!IsWindowVisible(hWnd)) return true;
                    StringBuilder sb = new StringBuilder(256);
                    GetWindowText(hWnd, sb, 256);
                    string title = sb.ToString();

                    uint pid;
                    GetWindowThreadProcessId(hWnd, out pid);

                    if ((targetPid > 0 && pid == targetPid) ||
                        (!string.IsNullOrEmpty(title) && title.ToLowerInvariant().Contains(target)))
                    {
                        ShowWindow(hWnd, 9); // SW_RESTORE
                        BringWindowToTop(hWnd);
                        SetForegroundWindow(hWnd);
                        found = true;
                        return false;
                    }
                    return true;
                }, IntPtr.Zero);
            });

            Console.WriteLine("{{\"success\": {0}}}", found ? "true" : "false");
            return found ? 0 : 1;
        }

        private static int HandleTree(string[] args)
        {
            int limit = 60;
            string filterWindow = null;
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--limit" && i + 1 < args.Length)
                {
                    int.TryParse(args[++i], out limit);
                }
                else if (args[i] == "--window" && i + 1 < args.Length)
                {
                    filterWindow = args[++i].ToLowerInvariant();
                }
            }

            List<ElementNode> elements = new List<ElementNode>();
            string winTitle = "";
            int winW = 0, winH = 0, winX = 0, winY = 0;

            RunOnInputDesktop(delegate()
            {
                AutomationElement win = null;
                if (!string.IsNullOrEmpty(filterWindow))
                {
                    AutomationElementCollection children = AutomationElement.RootElement.FindAll(
                        TreeScope.Children,
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window)
                    );
                    foreach (AutomationElement c in children)
                    {
                        try
                        {
                            if (c.Current.Name.ToLowerInvariant().Contains(filterWindow))
                            {
                                win = c;
                                break;
                            }
                        }
                        catch { }
                    }
                }

                if (win == null)
                {
                    // Find active foreground window
                    AutomationElement curr = AutomationElement.FocusedElement;
                    while (curr != null && curr != AutomationElement.RootElement)
                    {
                        try
                        {
                            if (curr.Current.ControlType == ControlType.Window)
                            {
                                win = curr;
                                break;
                            }
                            curr = TreeWalker.ControlViewWalker.GetParent(curr);
                        }
                        catch { break; }
                    }
                }

                if (win == null)
                {
                    // Fallback to first non-empty window
                    AutomationElementCollection children = AutomationElement.RootElement.FindAll(
                        TreeScope.Children,
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window)
                    );
                    foreach (AutomationElement c in children)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(c.Current.Name))
                            {
                                win = c;
                                break;
                            }
                        }
                        catch { }
                    }
                }

                if (win == null) return;

                try
                {
                    winTitle = win.Current.Name;
                    WindowsRect wr = win.Current.BoundingRectangle;
                    winX = (int)wr.X;
                    winY = (int)wr.Y;
                    winW = (int)wr.Width;
                    winH = (int)wr.Height;

                    int count = 0;
                    TraverseElements(win, wr, ref count, limit, elements);
                }
                catch { }
            });

            // Sort elements: top to bottom, then left to right
            elements.Sort(delegate(ElementNode a, ElementNode b)
            {
                int diffY = Math.Abs(a.y - b.y);
                if (diffY > 15) return a.y.CompareTo(b.y);
                return a.x.CompareTo(b.x);
            });

            // Re-index sorted elements
            for (int i = 0; i < elements.Count; i++)
            {
                elements[i].id = i + 1;
            }

            StringBuilder json = new StringBuilder();
            json.Append("{");
            json.Append(string.Format("\"window\":{{\"title\":{0},\"x\":{1},\"y\":{2},\"width\":{3},\"height\":{4}}},",
                EscapeJson(winTitle), winX, winY, winW, winH));
            json.Append(string.Format("\"count\":{0},\"elements\":[", elements.Count));

            for (int i = 0; i < elements.Count; i++)
            {
                var el = elements[i];
                if (i > 0) json.Append(",");
                json.Append(string.Format(
                    "{{\"id\":{0},\"type\":{1},\"label\":{2},\"x\":{3},\"y\":{4},\"width\":{5},\"height\":{6},\"centerX\":{7},\"centerY\":{8}}}",
                    el.id,
                    EscapeJson(el.type),
                    EscapeJson(el.name),
                    el.x,
                    el.y,
                    el.width,
                    el.height,
                    el.centerX,
                    el.centerY
                ));
            }
            json.Append("]}");

            Console.WriteLine(json.ToString());
            return 0;
        }

        private static void TraverseElements(AutomationElement root, WindowsRect winRect, ref int count, int max, List<ElementNode> list)
        {
            if (count >= max) return;
            AutomationElement child = null;
            try
            {
                child = TreeWalker.ControlViewWalker.GetFirstChild(root);
            }
            catch { return; }

            while (child != null && count < max)
            {
                try
                {
                    var c = child.Current;
                    WindowsRect r = c.BoundingRectangle;

                    // Filter out elements that don't intersect with the active window or are offscreen
                    bool isVisibleInWin = !r.IsEmpty && r.Width > 6 && r.Height > 6 &&
                        r.X + r.Width > winRect.X && r.X < winRect.X + winRect.Width &&
                        r.Y + r.Height > winRect.Y && r.Y < winRect.Y + winRect.Height;

                    bool isInteractive = c.ControlType == ControlType.Button ||
                                        c.ControlType == ControlType.Edit ||
                                        c.ControlType == ControlType.CheckBox ||
                                        c.ControlType == ControlType.RadioButton ||
                                        c.ControlType == ControlType.ComboBox ||
                                        c.ControlType == ControlType.TabItem ||
                                        c.ControlType == ControlType.MenuItem ||
                                        c.ControlType == ControlType.Hyperlink ||
                                        c.ControlType == ControlType.ListItem ||
                                        c.ControlType == ControlType.TreeItem ||
                                        c.ControlType == ControlType.SplitButton;

                    if (isVisibleInWin && isInteractive)
                    {
                        count++;
                        string typeName = c.ControlType.ProgrammaticName.Replace("ControlType.", "");
                        string name = string.IsNullOrEmpty(c.Name) ? "" : c.Name.Replace("\r", " ").Replace("\n", " ").Trim();
                        if (name.Length > 60) name = name.Substring(0, 57) + "...";

                        list.Add(new ElementNode
                        {
                            id = count,
                            type = typeName,
                            name = name,
                            x = (int)r.X,
                            y = (int)r.Y,
                            width = (int)r.Width,
                            height = (int)r.Height,
                            centerX = (int)(r.X + r.Width / 2.0),
                            centerY = (int)(r.Y + r.Height / 2.0),
                            isActionable = true
                        });
                    }
                }
                catch { }

                TraverseElements(child, winRect, ref count, max, list);

                try
                {
                    child = TreeWalker.ControlViewWalker.GetNextSibling(child);
                }
                catch { break; }
            }
        }

        private static int HandleClick(string[] args)
        {
            if (args.Length < 3)
            {
                Console.Error.WriteLine("Usage: click <x> <y> [left|right|middle|double]");
                return 1;
            }

            int x = int.Parse(args[1]);
            int y = int.Parse(args[2]);
            string btn = args.Length > 3 ? args[3].ToLowerInvariant() : "left";

            RunOnInputDesktop(delegate()
            {
                SmoothMoveTo(x, y, 200);
                Thread.Sleep(40);

                if (btn == "right")
                {
                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(25);
                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                }
                else if (btn == "middle")
                {
                    mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(25);
                    mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
                }
                else if (btn == "double")
                {
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(25);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(80);
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(25);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                }
                else
                {
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                    Thread.Sleep(25);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                }
            });

            Console.WriteLine("{{\"clicked\": true, \"x\": {0}, \"y\": {1}, \"button\": \"{2}\"}}", x, y, btn);
            return 0;
        }

        private static int HandleMove(string[] args)
        {
            if (args.Length < 3) return 1;
            int targetX = int.Parse(args[1]);
            int targetY = int.Parse(args[2]);
            bool smooth = args.Length > 3 && args[3] == "--smooth";

            RunOnInputDesktop(delegate()
            {
                if (!smooth)
                {
                    MoveCursorHardware(targetX, targetY);
                }
                else
                {
                    SmoothMoveTo(targetX, targetY, 300);
                }
            });

            Console.WriteLine("{{\"moved\": true, \"x\": {0}, \"y\": {1}}}", targetX, targetY);
            return 0;
        }

        private static int HandleDrag(string[] args)
        {
            if (args.Length < 5) return 1;
            int x1 = int.Parse(args[1]);
            int y1 = int.Parse(args[2]);
            int x2 = int.Parse(args[3]);
            int y2 = int.Parse(args[4]);

            RunOnInputDesktop(delegate()
            {
                SmoothMoveTo(x1, y1, 150);
                Thread.Sleep(30);
                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
                Thread.Sleep(50);

                int steps = 25;
                for (int i = 1; i <= steps; i++)
                {
                    double t = (double)i / steps;
                    int curX = (int)(x1 + (x2 - x1) * t);
                    int curY = (int)(y1 + (y2 - y1) * t);
                    MoveCursorHardware(curX, curY);
                    Thread.Sleep(12);
                }
                MoveCursorHardware(x2, y2);
                Thread.Sleep(50);
                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            });

            Console.WriteLine("{{\"dragged\": true, \"from\": [{0},{1}], \"to\": [{2},{3}]}}", x1, y1, x2, y2);
            return 0;
        }

        private static int HandleScroll(string[] args)
        {
            if (args.Length < 2) return 1;
            int amount = int.Parse(args[1]);

            RunOnInputDesktop(delegate()
            {
                if (args.Length >= 4)
                {
                    int x = int.Parse(args[2]);
                    int y = int.Parse(args[3]);
                    MoveCursorHardware(x, y);
                    Thread.Sleep(15);
                }
                mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)amount, UIntPtr.Zero);
            });

            Console.WriteLine("{{\"scrolled\": true, \"amount\": {0}}}", amount);
            return 0;
        }

        private static int HandleType(string[] args)
        {
            if (args.Length < 2) return 1;
            string text = args[1];

            RunOnInputDesktop(delegate()
            {
                // Send Unicode characters directly via SendInput
                int inputSize = Marshal.SizeOf(typeof(INPUT));
                foreach (char c in text)
                {
                    INPUT[] inputs = new INPUT[2];
                    inputs[0].type = INPUT_KEYBOARD;
                    inputs[0].ki.wVk = 0;
                    inputs[0].ki.wScan = (ushort)c;
                    inputs[0].ki.dwFlags = KEYEVENTF_UNICODE;

                    inputs[1].type = INPUT_KEYBOARD;
                    inputs[1].ki.wVk = 0;
                    inputs[1].ki.wScan = (ushort)c;
                    inputs[1].ki.dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP;

                    SendInput(2, inputs, inputSize);
                    Thread.Sleep(25);
                }
            });

            Console.WriteLine("{{\"typed\": true, \"length\": {0}}}", text.Length);
            return 0;
        }

        private static int HandleHotkey(string[] args)
        {
            if (args.Length < 2) return 1;
            string combo = args[1].ToLowerInvariant();

            RunOnInputDesktop(delegate()
            {
                string[] parts = combo.Split('+');
                List<byte> pressed = new List<byte>();

                for (int i = 0; i < parts.Length; i++)
                {
                    byte vk = KeyToVk(parts[i].Trim());
                    if (vk != 0)
                    {
                        keybd_event(vk, 0, 0, UIntPtr.Zero);
                        pressed.Add(vk);
                        Thread.Sleep(15);
                    }
                }

                Thread.Sleep(30);

                // Release in reverse order
                for (int i = pressed.Count - 1; i >= 0; i--)
                {
                    keybd_event(pressed[i], 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    Thread.Sleep(10);
                }
            });

            Console.WriteLine("{{\"hotkey\": true, \"combo\": \"{0}\"}}", combo);
            return 0;
        }

        private static byte KeyToVk(string k)
        {
            switch (k)
            {
                case "ctrl": case "control": return 0x11;
                case "shift": return 0x10;
                case "alt": return 0x12;
                case "win": case "cmd": case "super": return 0x5B;
                case "enter": case "return": return 0x0D;
                case "esc": case "escape": return 0x1B;
                case "tab": return 0x09;
                case "backspace": return 0x08;
                case "space": return 0x20;
                case "up": return 0x26;
                case "down": return 0x28;
                case "left": return 0x25;
                case "right": return 0x27;
                case "delete": case "del": return 0x2E;
                case "home": return 0x24;
                case "end": return 0x23;
                case "pageup": return 0x21;
                case "pagedown": return 0x22;
                case "f1": return 0x70;
                case "f2": return 0x71;
                case "f3": return 0x72;
                case "f4": return 0x73;
                case "f5": return 0x74;
                case "f6": return 0x75;
                case "f7": return 0x76;
                case "f8": return 0x77;
                case "f9": return 0x78;
                case "f10": return 0x79;
                case "f11": return 0x7A;
                case "f12": return 0x7B;
                default:
                    if (k.Length == 1)
                    {
                        char c = char.ToUpperInvariant(k[0]);
                        if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
                        {
                            return (byte)c;
                        }
                    }
                    return 0;
            }
        }

        private static int HandleCapture(string[] args)
        {
            if (args.Length < 2) return 1;
            string outPath = args[1];
            bool doSom = false;
            string filterWindow = null;

            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] == "--som") doSom = true;
                else if (args[i] == "--window" && i + 1 < args.Length) filterWindow = args[++i].ToLowerInvariant();
            }

            int w = 0, h = 0;
            bool success = false;
            List<ElementNode> elements = new List<ElementNode>();

            RunOnInputDesktop(delegate()
            {
                w = GetSystemMetrics(0); // SM_CXSCREEN
                h = GetSystemMetrics(1); // SM_CYSCREEN
                IntPtr hDesk = GetDesktopWindow();
                IntPtr hdcSrc = GetDC(hDesk);
                if (hdcSrc == IntPtr.Zero) return;

                IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
                IntPtr hBmp = CreateCompatibleBitmap(hdcSrc, w, h);
                IntPtr hOld = SelectObject(hdcDest, hBmp);

                bool blt = BitBlt(hdcDest, 0, 0, w, h, hdcSrc, 0, 0, 0x00CC0020 | 0x40000000); // SRCCOPY | CAPTUREBLT
                SelectObject(hdcDest, hOld);
                DeleteDC(hdcDest);
                ReleaseDC(hDesk, hdcSrc);

                if (blt)
                {
                    using (Bitmap bmp = Image.FromHbitmap(hBmp))
                    {
                        if (doSom)
                        {
                            // Gather visible elements to draw Set-of-Marks
                            try
                            {
                                AutomationElement fg = AutomationElement.FocusedElement;
                                AutomationElement win = null;
                                AutomationElement curr = fg;
                                while (curr != null && curr != AutomationElement.RootElement)
                                {
                                    if (curr.Current.ControlType == ControlType.Window) { win = curr; break; }
                                    try { curr = TreeWalker.ControlViewWalker.GetParent(curr); } catch { break; }
                                }
                                if (win == null)
                                {
                                    var wins = AutomationElement.RootElement.FindAll(TreeScope.Children, new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window));
                                    foreach (AutomationElement c in wins) { if (!string.IsNullOrEmpty(c.Current.Name)) { win = c; break; } }
                                }

                                if (win != null)
                                {
                                    int count = 0;
                                    WindowsRect wr = win.Current.BoundingRectangle;
                                    TraverseElements(win, wr, ref count, 50, elements);

                                    using (Graphics g = Graphics.FromImage(bmp))
                                    {
                                        g.SmoothingMode = SmoothingMode.AntiAlias;
                                        Font badgeFont = new Font("Arial", 9, FontStyle.Bold);
                                        Brush badgeBg = new SolidBrush(Color.FromArgb(230, 255, 69, 0)); // High-contrast Red-Orange
                                        Brush textBrush = Brushes.White;
                                        Pen borderPen = new Pen(Color.FromArgb(200, 255, 69, 0), 2);

                                        for (int i = 0; i < elements.Count; i++)
                                        {
                                            var el = elements[i];
                                            el.id = i + 1;
                                            // Draw bounding box
                                            g.DrawRectangle(borderPen, el.x, el.y, el.width, el.height);

                                            // Draw numeric tag badge
                                            string tag = "[" + el.id + "]";
                                            SizeF sz = g.MeasureString(tag, badgeFont);
                                            int badgeX = Math.Max(0, el.x);
                                            int badgeY = Math.Max(0, el.y - (int)sz.Height - 2);

                                            g.FillRectangle(badgeBg, badgeX, badgeY, sz.Width + 4, sz.Height + 2);
                                            g.DrawString(tag, badgeFont, textBrush, badgeX + 2, badgeY + 1);
                                        }
                                    }
                                }
                            }
                            catch { }
                        }

                        // Save image
                        string dir = Path.GetDirectoryName(outPath);
                        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        bmp.Save(outPath, ImageFormat.Png);
                        success = true;
                    }
                }
                DeleteObject(hBmp);
            });

            if (success)
            {
                Console.WriteLine("{{\"captured\": true, \"path\": {0}, \"width\": {1}, \"height\": {2}, \"somElements\": {3}}}",
                    EscapeJson(outPath), w, h, elements.Count);
                return 0;
            }
            else
            {
                Console.Error.WriteLine("Capture failed");
                return 1;
            }
        }

        private static string EscapeJson(string s)
        {
            if (s == null) return "\"\"";
            StringBuilder sb = new StringBuilder();
            sb.Append("\"");
            foreach (char c in s)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '\"': sb.Append("\\\""); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ')
                        {
                            sb.Append(string.Format("\\u{0:x4}", (int)c));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append("\"");
            return sb.ToString();
        }
    }
}
