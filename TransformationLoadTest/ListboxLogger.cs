using Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using TransformationCore.Enums;
using TransformationCore.Interfaces;

namespace PipeTest
{
    public class ListboxLogger : ILogger
    {
        private readonly ListBox _listBox;

        public ListboxLogger(ListBox listBox)
        {
            _listBox = listBox;
        }
        private void Log(string message, Color color)
        {
            if (_listBox.InvokeRequired)
            {
                _listBox.BeginInvoke(new Action<string, Color>(Log), new object[] { message, color });
            }
            else
            {
                _listBox.BeginUpdate();

                _listBox.Items.Add(new Dictionary<string, object> { { "Text", message }, { "ForeColor", color } });
                _listBox.TopIndex = _listBox.Items.Count - 1;
                _listBox.EndUpdate();
            }
        }

        public void Debug(string message)
        {
            Log(message, Color.Gray);
        }

        public void Debug(string message, Exception ex)
        {
            Log(message, Color.Gray);
        }

        public void DebugFormat(string message, params object[] args)
        {
            Log(message, Color.Gray);
        }

        public void Error(string message)
        {
            Log(message, Color.Red);
        }

        public void Error(string message, Exception ex)
        {
            Log(message, Color.Red);
        }

        public void ErrorFormat(string message, params object[] args)
        {
            Log(message, Color.Red);
        }

        public void Fatal(string message)
        {
            Log(message, Color.Red);
        }

        public void Fatal(string message, Exception ex)
        {
            Log(message, Color.Red);
        }

        public void FatalFormat(string message, params object[] args)
        {
            Log(message, Color.Red);
        }

        public void Info(string message)
        {
            Log(message, Color.Green);
        }

        public void Info(string message, Exception ex)
        {
            Log(message, Color.Green);
        }

        public void InfoFormat(string message, params object[] args)
        {
            Log(message, Color.Green);
        }

        public void Warn(string message)
        {
            Log(message, Color.Orange);
        }

        public void Warn(string message, Exception ex)
        {
            Log(message, Color.Orange);
        }

        public void WarnFormat(string message, params object[] args)
        {
            Log(message, Color.Orange);
        }
    }
}
