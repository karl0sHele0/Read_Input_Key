using System;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using System.ComponentModel;
using System.Windows.Forms;//Es requerida para los keys


public class KeyHooker
{
    #region Windows structure definitions

    [StructLayout(LayoutKind.Sequential)]
    private class POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private class MouseHookStruct
    {
        public POINT pt;
        public int hwnd;
        public int wHitTestCode;
        public int dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private class MouseLLHookStruct
    {
        public POINT pt;
        public int mouseData;
        public int flags;
        public int time;
        public int dwExtraInfo;
    }


    [StructLayout(LayoutKind.Sequential)]
    private class KeyboardHookStruct
    {

        public int vkCode;

        public int scanCode;

        public int flags;

        public int time;
        public int dwExtraInfo;
    }
    #endregion

    #region Windows function imports

    [DllImport("user32.dll", CharSet = CharSet.Auto,
       CallingConvention = CallingConvention.StdCall, SetLastError = true)]
    private static extern int SetWindowsHookEx(
        int idHook,
        HookProc lpfn,
        IntPtr hMod,
        int dwThreadId);


    [DllImport("user32.dll", CharSet = CharSet.Auto,
        CallingConvention = CallingConvention.StdCall, SetLastError = true)]
    private static extern int UnhookWindowsHookEx(int idHook);


    [DllImport("user32.dll", CharSet = CharSet.Auto,
         CallingConvention = CallingConvention.StdCall)]
    private static extern int CallNextHookEx(
        int idHook,
        int nCode,
        int wParam,
        IntPtr lParam);


    private delegate int HookProc(int nCode, int wParam, IntPtr lParam);


    [DllImport("user32")]
    private static extern int ToAscii(
        int uVirtKey,
        int uScanCode,
        byte[] lpbKeyState,
        byte[] lpwTransKey,
        int fuState);


    /// http://msdn.microsoft.com/library/default.asp?url=/library/en-us/winui/winui/windowsuserinterface/userinput/keyboardinput/keyboardinputreference/keyboardinputfunctions/toascii.asp
    /// </remarks>
    [DllImport("user32")]
    private static extern int GetKeyboardState(byte[] pbKeyState);

    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    private static extern short GetKeyState(int vKey);

    #endregion

    #region Windows constants


    private const int WH_MOUSE_LL = 14;

    private const int WH_KEYBOARD_LL = 13;


    private const int WH_MOUSE = 7;

    private const int WH_KEYBOARD = 2;


    private const int WM_MOUSEMOVE = 0x200;

    private const int WM_LBUTTONDOWN = 0x201;

    private const int WM_RBUTTONDOWN = 0x204;

    private const int WM_MBUTTONDOWN = 0x207;

    private const int WM_LBUTTONUP = 0x202;

    private const int WM_RBUTTONUP = 0x205;

    private const int WM_MBUTTONUP = 0x208;

    private const int WM_LBUTTONDBLCLK = 0x203;

    private const int WM_RBUTTONDBLCLK = 0x206;

    private const int WM_MBUTTONDBLCLK = 0x209;

    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_KEYDOWN = 0x100;

    private const int WM_KEYUP = 0x101;

    private const int WM_SYSKEYDOWN = 0x104;

    private const int WM_SYSKEYUP = 0x105;

    private const byte VK_MENU = 0x12;
    private const byte VK_CONTROL = 0x11;
    private const byte VK_SHIFT = 0x10;
    private const byte VK_CAPITAL = 0x14;
    private const byte VK_NUMLOCK = 0x90;

    #endregion
    public KeyHooker()
    {
        Start();
    }


    public KeyHooker(bool InstallMouseHook, bool InstallKeyboardHook)
    {
        Start(InstallMouseHook, InstallKeyboardHook);
    }


    ~KeyHooker()
    {
        //uninstall hooks and do not throw exceptions
        Stop(true, true, false);
    }

    public delegate void KeyInfoEventHandler(object sender, Keys KeyCode, bool ctrl);

    public event MouseEventHandler OnMouseActivity;
    public event KeyInfoEventHandler KeyDown;
    public event KeyInfoEventHandler KeyPress;
    public event KeyEventHandler KeyUp;



    private int hMouseHook = 0;

    private int hKeyboardHook = 0;



    private static HookProc MouseHookProcedure;

    private static HookProc KeyboardHookProcedure;


    public void Start()
    {
        this.Start(true, true);
    }


    public void Start(bool InstallMouseHook, bool InstallKeyboardHook)
    {
        // install Mouse hook only if it is not installed and must be installed
        if (hMouseHook == 0 && InstallMouseHook)
        {
            // Create an instance of HookProc.
            MouseHookProcedure = new HookProc(MouseHookProc);
            //install hook
            hMouseHook = SetWindowsHookEx(
                WH_MOUSE_LL,
                MouseHookProcedure,
                Marshal.GetHINSTANCE(
                    Assembly.GetExecutingAssembly().GetModules()[0]),
                0);
            //If SetWindowsHookEx fails.
            if (hMouseHook == 0)
            {
                //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
                int errorCode = Marshal.GetLastWin32Error();
                //do cleanup
                Stop(true, false, false);
                //Initializes and throws a new instance of the Win32Exception class with the specified error. 
                throw new Win32Exception(errorCode);
            }
        }

        // install Keyboard hook only if it is not installed and must be installed
        if (hKeyboardHook == 0 && InstallKeyboardHook)
        {
            // Create an instance of HookProc.
            KeyboardHookProcedure = new HookProc(KeyboardHookProc);
            //install hook
            hKeyboardHook = SetWindowsHookEx(
                WH_KEYBOARD_LL,
                KeyboardHookProcedure,
                Marshal.GetHINSTANCE(
                Assembly.GetExecutingAssembly().GetModules()[0]),
                0);
            //If SetWindowsHookEx fails.
            if (hKeyboardHook == 0)
            {
                //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
                int errorCode = Marshal.GetLastWin32Error();
                //do cleanup
                Stop(false, true, false);
                //Initializes and throws a new instance of the Win32Exception class with the specified error. 
                throw new Win32Exception(errorCode);
            }
        }
    }

    public void Stop()
    {
        this.Stop(true, true, true);
    }

    public void Stop(bool UninstallMouseHook, bool UninstallKeyboardHook, bool ThrowExceptions)
    {
        //if mouse hook set and must be uninstalled
        if (hMouseHook != 0 && UninstallMouseHook)
        {
            //uninstall hook
            int retMouse = UnhookWindowsHookEx(hMouseHook);
            //reset invalid handle
            hMouseHook = 0;
            //if failed and exception must be thrown
            if (retMouse == 0 && ThrowExceptions)
            {
                //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
                int errorCode = Marshal.GetLastWin32Error();
                //Initializes and throws a new instance of the Win32Exception class with the specified error. 
                throw new Win32Exception(errorCode);
            }
        }

        //if keyboard hook set and must be uninstalled
        if (hKeyboardHook != 0 && UninstallKeyboardHook)
        {
            //uninstall hook
            int retKeyboard = UnhookWindowsHookEx(hKeyboardHook);
            //reset invalid handle
            hKeyboardHook = 0;
            //if failed and exception must be thrown
            if (retKeyboard == 0 && ThrowExceptions)
            {
                //Returns the error code returned by the last unmanaged function called using platform invoke that has the DllImportAttribute.SetLastError flag set. 
                int errorCode = Marshal.GetLastWin32Error();
                //Initializes and throws a new instance of the Win32Exception class with the specified error. 
                throw new Win32Exception(errorCode);
            }
        }
    }


    private int MouseHookProc(int nCode, int wParam, IntPtr lParam)
    {
        // if ok and someone listens to our events
        if ((nCode >= 0) && (OnMouseActivity != null))
        {
            //Marshall the data from callback.
            MouseLLHookStruct mouseHookStruct = (MouseLLHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseLLHookStruct));

            //detect button clicked
            MouseButtons button = MouseButtons.None;
            short mouseDelta = 0;
            switch (wParam)
            {
                case WM_LBUTTONDOWN:
                    button = MouseButtons.Left;
                    break;
                case WM_RBUTTONDOWN:
                    button = MouseButtons.Right;
                    break;
                case WM_MOUSEWHEEL:
                    mouseDelta = (short)((mouseHookStruct.mouseData >> 16) & 0xffff);
                    break;
            }

            //double clicks
            int clickCount = 0;
            if (button != MouseButtons.None)
                if (wParam == WM_LBUTTONDBLCLK || wParam == WM_RBUTTONDBLCLK) clickCount = 2;
                else clickCount = 1;

            //generate event 
            MouseEventArgs e = new MouseEventArgs(
                                               button,
                                               clickCount,
                                               mouseHookStruct.pt.x,
                                               mouseHookStruct.pt.y,
                                               mouseDelta);
            //raise it
            OnMouseActivity(this, e);
        }
        //call next hook
        return CallNextHookEx(hMouseHook, nCode, wParam, lParam);
    }
    private int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
    {
        //indicates if any of underlaing events set e.Handled flag
        bool handled = false;
        //it was ok and someone listens to events
        if ((nCode >= 0) && (KeyDown != null || KeyUp != null || KeyPress != null))
        {
            //read structure KeyboardHookStruct at lParam
            KeyboardHookStruct MyKeyboardHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));
            //raise KeyDown
            if (KeyDown != null && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
            {
                bool isDownCTRL = ((GetKeyState(VK_CONTROL) & 0x80) == 0x80 ? true : false);

                Keys keyData = (Keys)MyKeyboardHookStruct.vkCode;
                KeyEventArgs e = new KeyEventArgs(keyData);
                KeyDown(this, e.KeyCode, isDownCTRL);
                handled = handled || e.Handled;
            }

            // raise KeyPress
            if (KeyPress != null && wParam == WM_KEYDOWN)
            {
                Keys keyData = (Keys)MyKeyboardHookStruct.vkCode;
                bool isDownAlt = ((GetKeyState(VK_MENU) & 0x80) == 0x80 ? true : false);
                bool isDownCTRL = ((GetKeyState(VK_CONTROL) & 0x80) == 0x80 ? true : false);
                bool isDownShift = ((GetKeyState(VK_SHIFT) & 0x80) == 0x80 ? true : false);
                bool isDownCapslock = (GetKeyState(VK_CAPITAL) != 0 ? true : false);

                byte[] keyState = new byte[256];
                GetKeyboardState(keyState);
                byte[] inBuffer = new byte[2];
                if (ToAscii(MyKeyboardHookStruct.vkCode,
                          MyKeyboardHookStruct.scanCode,
                          keyState,
                          inBuffer,
                          MyKeyboardHookStruct.flags) == 1)
                {
                    char key = (char)inBuffer[0];
                    if ((isDownCapslock ^ isDownShift) && Char.IsLetter(key)) key = Char.ToUpper(key);
                    KeyEventArgs edata = new KeyEventArgs(keyData);
                    KeyPressEventArgs ekey = new KeyPressEventArgs(key);
                    KeyPress(this, edata.KeyCode, isDownCTRL);
                    handled = handled || ekey.Handled;
                }
            }

            // raise KeyUp
            if (KeyUp != null && (wParam == WM_KEYUP || wParam == WM_SYSKEYUP))
            {
                Keys keyData = (Keys)MyKeyboardHookStruct.vkCode;
                KeyEventArgs e = new KeyEventArgs(keyData);
                KeyUp(this, e);
                handled = handled || e.Handled;
            }

        }

        //if event handled in application do not handoff to other listeners
        if (handled)
            return 1;
        else
            return CallNextHookEx(hKeyboardHook, nCode, wParam, lParam);
    }
}

