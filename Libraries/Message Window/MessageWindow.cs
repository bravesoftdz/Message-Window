using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Codeology.WinAPI;

namespace Codeology
{

    public delegate void MessageWindowProc(ref Message msg);

    public static class MessageWindow
    {

        private static List<MessageWindowInstance> instances;

        static MessageWindow()
        {
            // Create somewhere to put instances
            instances = new List<MessageWindowInstance>();

            // Hook into domain unloading
            AppDomain.CurrentDomain.DomainUnload += new EventHandler(DomainUnload);

            // Register class
            if (!IsClassRegistered()) RegisterClass();
        }

        #region Methods

        public static MessageWindowInstance Create(MessageWindowProc windowProc)
        {
            MessageWindowInstance instance = new MessageWindowInstance(instances,windowProc);

            instances.Add(instance);

            return instance;
        }

        public static void Destroy(MessageWindowInstance windowInstance)
        {
            if (instances.Contains(windowInstance)) {
                // Dispose of instance
                windowInstance.Dispose();

                // Remove from instances
                instances.Remove(windowInstance);
            }
        }

        private static void DomainUnload(object sender, EventArgs args)
        {
            // Dispose of all instances
            while (instances.Count > 0) Destroy(instances[0]);
        }

        private static bool IsClassRegistered()
        {
            // Get instance handle
            IntPtr instance = Marshal.GetHINSTANCE(typeof(MessageWindow).Module);

            // Set class name
            string class_name = "WSUtilWindow";

            // Set up struct
            User.WNDCLASSEX window_class = new User.WNDCLASSEX { cbSize = (uint)Marshal.SizeOf(typeof(User.WNDCLASSEX)) };

            // Return if class exists
            return User.GetClassInfoEx(instance,class_name,ref window_class);
        }

        private static void RegisterClass()
        {
            // Set up struct
            User.WNDCLASSEX window_class = new User.WNDCLASSEX();

            window_class.cbSize = (uint)Marshal.SizeOf(typeof(User.WNDCLASSEX));
            window_class.style = User.ClassStyles.None;
            window_class.lpfnWndProc = new User.WindowProc(User.DefWindowProc);
            window_class.cbClsExtra = 0;
            window_class.cbWndExtra = 0;
            window_class.hInstance = Marshal.GetHINSTANCE(typeof(MessageWindow).Module);
            window_class.hIcon = IntPtr.Zero;
            window_class.hIconSm = IntPtr.Zero;
            window_class.lpszMenuName = String.Empty;
            window_class.lpszClassName = "WSUtilWindow";

            // Register class
            if (User.RegisterClassEx(ref window_class) == 0) throw new Exception("Could not register class.");
        }

        private static void UnregisterClass()
        {
            // Get instance handle
            IntPtr instance = Marshal.GetHINSTANCE(typeof(MessageWindow).Module);

            // Set class name
            string class_name = "WSUtilWindow";

            // Unregister class
            User.UnregisterClass(class_name,instance);
        }

        #endregion

    }

    public class MessageWindowInstance : IDisposable
    {

        private bool disposed;
        private List<MessageWindowInstance> instances;
        private MessageWindowProc window_proc;
        private IntPtr handle;
        private IntPtr old_window_proc;

        internal MessageWindowInstance(List<MessageWindowInstance> windowInstances, MessageWindowProc windowProc)
        {
            disposed = false;
            instances = windowInstances;
            window_proc = windowProc;
            handle = IntPtr.Zero;
            old_window_proc = IntPtr.Zero;

            // Create window
            CreateWnd();
        }

        #region Methods

        public void Dispose()
        {
            if (!disposed) {
                // Destroy window
                DestroyWnd();

                // Remove instance
                if (instances.Contains(this)) instances.Remove(this);

                // Supress finalization
                GC.SuppressFinalize(true);

                // Mark as disposed
                disposed = true;
            }
        }

        private void CreateWnd()
        {
            // Get instance handle
            IntPtr instance = Marshal.GetHINSTANCE(GetType().Module);

            // Create window
            handle = User.CreateWindowEx(User.WindowStylesEx.WS_EX_TOOLWINDOW,"WSUtilWindow","",User.WindowStyles.WS_POPUP,0,0,0,0,IntPtr.Zero,IntPtr.Zero,instance,IntPtr.Zero);

            // Register our window proc, and store the existing window proc
            old_window_proc = User.SetWindowLong(handle,User.GWL_WNDPROC,new User.WindowProc(WndProc));
        }

        private void DestroyWnd()
        {
            if (handle != IntPtr.Zero) {
                // Destroy window
                User.DestroyWindow(handle);

                // Zero handle
                handle = IntPtr.Zero;
            }
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            // Create message
            Message message = new Message();

            message.HWnd = hWnd;
            message.Msg = (int)msg;
            message.WParam = wParam;
            message.LParam = lParam;
            message.Result = IntPtr.Zero;

            // Call window proc
            if (window_proc != null) window_proc(ref message);

            // Just call old window proc
            return User.CallWindowProc(old_window_proc,hWnd,(uint)message.Msg,message.WParam,message.LParam);
        }

        #endregion

        #region Properties

        public IntPtr Handle
        {
            get {
                return handle;
            }
        }

        #endregion

    }

}
