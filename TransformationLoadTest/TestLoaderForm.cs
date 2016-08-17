using PipeTest;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using Transformation.Loader;
using TransformationCore.Interfaces;

namespace TransformationLoadTest
{
    public partial class LoadTestForm : Form
    {
        private string _filename = string.Empty;
        private readonly ILogger _logger;
        private LoadProcess _runner;


        public LoadTestForm()
        {
            InitializeComponent();

            _logger = new ListboxLogger(statusListBox, TransformationCore.Enums.MessageLevel.Info);
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

        private void runBtn_Click(object sender, EventArgs e)
        {
            XElement config;
            if (string.IsNullOrWhiteSpace(_filename))
            {
                _logger.Log("Please Select a file.", TransformationCore.Enums.MessageLevel.Critical);
                return;
            }

            try
            {
                config = XElement.Parse(configTextbox.Text);
            }
            catch(Exception ex)
            {
                _logger.Log(string.Format("Error parsing config : {0}", ex.Message), TransformationCore.Enums.MessageLevel.Critical);
                return;
            }

            statusListBox.Items.Clear();

            _runner = new LoadProcess(config, new System.Threading.CancellationTokenSource(), _logger);

            _runner.Start(_filename);
        }

    }
}
