using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Codeology;
using Codeology.WinAPI;

namespace Demo
{

    public partial class MainForm : Form
    {

        private MessageWindowInstance msgwin;
        private IntPtr next_window;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Create message window
            msgwin = MessageWindow.Create(new MessageWindowProc(WindowProc));

            // Hook clipboard
            next_window = User.SetClipboardViewer(msgwin.Handle);
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Unhook clipboard
            User.ChangeClipboardChain(msgwin.Handle,next_window);

            next_window = IntPtr.Zero;

            // Release message window
            msgwin.Dispose();
        }

        private void WindowProc(ref Message msg)
        {
            switch (msg.Msg) {
                case User.WM_CHANGECBCHAIN: {
                    if (msg.WParam == next_window) {
                        next_window = msg.LParam;
                    } else if (next_window != IntPtr.Zero) {
                        // Pass on message
                        User.SendMessage(next_window,(uint)msg.Msg,msg.WParam,msg.LParam);
                    }

                    break;
                }
                case User.WM_DRAWCLIPBOARD: {
                    // Notify clipboard changed
                    ClipboardChanged();

                    // Pass on message
                    User.SendMessage(next_window,(uint)msg.Msg,msg.WParam,msg.LParam);

                    break;
                }
            }
        }

        private void ClipboardChanged()
        {
            string line_fmt = "[{0}] {1}\r\n";
            string line = String.Format(line_fmt,DateTime.Now.ToString(),"Clipboard Changed");

            txtConsole.Text += line;
        }

    }

}
