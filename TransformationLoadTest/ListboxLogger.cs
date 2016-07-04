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
        private readonly MessageLevel _logLevel;

        public ListboxLogger(ListBox listBox, MessageLevel logLevel)
        {
            _listBox = listBox;
            _logLevel = logLevel;
        }
        public void Log(string message, MessageLevel msgLevel)
        {
            if (msgLevel < _logLevel)
            {
                return;
            }

            if (_listBox.InvokeRequired)
            {
                _listBox.BeginInvoke(new Action<string, MessageLevel>(Log), new object[] { message, msgLevel });
            }
            else
            {
                _listBox.BeginUpdate();

                _listBox.Items.Add(new Dictionary<string, object> { { "Text", message }, { "ForeColor", GetMessageColor(msgLevel) } });
                _listBox.TopIndex = _listBox.Items.Count - 1;
                _listBox.EndUpdate();
            }
        }

        private static Color GetMessageColor(MessageLevel msgLevel)
        {
            switch (msgLevel)
            {
                case MessageLevel.Info:
                    return Color.Green;
                case MessageLevel.Action:
                    return Color.Green;
                case MessageLevel.Warn:
                    return Color.Orange;
                case MessageLevel.Critical:
                    return Color.Red;
                default :
                    return Color.Gray;
            }
        }
    }
}
