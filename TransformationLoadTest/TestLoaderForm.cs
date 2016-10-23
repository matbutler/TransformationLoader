using Logging;
using PipeTest;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Xml.Linq;
using Transformation.Loader;

namespace TransformationLoadTest
{
    public partial class LoadTestForm : Form
    {
        private string _filename = string.Empty;
        private readonly ILogger _logger;
        private CancellationTokenSource _cancellationTokenSource;

        public LoadTestForm()
        {
            InitializeComponent();

            _logger = new ListboxLogger(statusListBox);
        }

        private void statusListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;

            Dictionary<string, object> props = (((ListBox) sender).Items[e.Index] as Dictionary<string, object>);
            SolidBrush foregroundBrush = new SolidBrush(props.ContainsKey("ForeColor") ? (Color)props["ForeColor"] : e.ForeColor);
            string text = props.ContainsKey("Text") ? (string)props["Text"] : string.Empty;
            RectangleF rectangle = new RectangleF(new PointF(e.Bounds.X, e.Bounds.Y), new SizeF(e.Bounds.Width, g.MeasureString(text, e.Font).Height));

            g.DrawString(text, e.Font, foregroundBrush, rectangle);

            foregroundBrush.Dispose();
            g.Dispose();
        }

        private void openBtn_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _filename = openFileDialog.FileName;

                filenameLabel.Text = string.Format("Filename: {0}", _filename);
            }
        }

        private async void runBtn_Click(object sender, EventArgs e)
        {
            XElement config;
            if (string.IsNullOrWhiteSpace(_filename))
            {
                _logger.Fatal("Please Select a file.");
                return;
            }

            try
            {
                config = XElement.Parse(configTextbox.Text);
            }
            catch(Exception ex)
            {
                _logger.Fatal(string.Format("Error parsing config : {0}", ex.Message));
                return;
            }

            statusListBox.Items.Clear();

            _cancellationTokenSource = new System.Threading.CancellationTokenSource();
            var _runner = new LoadProcess(config, _cancellationTokenSource, _logger);

            var processInfo = new XElement("processinfo",new XAttribute("id", Guid.NewGuid().ToString()), new XElement("filename", _filename));

            await _runner.Run(processInfo);
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }
    }
}
