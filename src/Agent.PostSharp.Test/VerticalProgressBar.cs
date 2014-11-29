// /*
//    Copyright 2013 Gibraltar Software, Inc.
//    
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// */

using System.Drawing;
using System.Windows.Forms;

namespace GSharp.Samples
{
    public partial class VerticalProgressBar : Control
    {
        private Color _borderColor = Color.Black;
        private int _borderWidth = 50;
        private Color _fillColor = Color.Green;
        private volatile bool _invalidated;
        private int _progressInPercentage = 10;

        public VerticalProgressBar()
        {
            _progressInPercentage = 0;

            InitializeComponent();

            // Enable double buffering
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
        }

        public Color BorderColor
        {
            get { return _borderColor; }
            set
            {
                _borderColor = value;

                ThreadSafeInvalidate();
            }
        }

        public int BorderWidth
        {
            get { return _borderWidth; }
            set
            {
                _borderWidth = value;

                ThreadSafeInvalidate();
            }
        }

        public Color FillColor
        {
            get { return _fillColor; }
            set
            {
                _fillColor = value;

                ThreadSafeInvalidate();
            }
        }
        
        public int ProgressInPercentage
        {
            get { return _progressInPercentage; }
            set
            {
                _progressInPercentage = value;
                ThreadSafeInvalidate();
            }
        }

        private void ThreadSafeInvalidate()
        {
            if (!_invalidated)
            {
                _invalidated = true;
                if (InvokeRequired)
                    Invoke(new MethodInvoker(Invalidate));
                else
                    Invalidate();
            }
        }

        protected override void OnResize(System.EventArgs e)
        {
            _invalidated = true;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            Pen pen = null;
            SolidBrush brush = null;

            base.OnPaint(pe);

            try
            {
                pen = new Pen(_borderColor, _borderWidth);
                brush = new SolidBrush(_fillColor);

                Rectangle borderRec = ClientRectangle;

                borderRec.X = borderRec.X + _borderWidth / 2;
                borderRec.Y = borderRec.Y + _borderWidth / 2;
                borderRec.Width = borderRec.Width - _borderWidth;
                borderRec.Height = borderRec.Height - _borderWidth;

                int fillHeight = (borderRec.Height * (100 - _progressInPercentage)) / 100;

                Rectangle fillRec = new Rectangle(borderRec.X, borderRec.Y + fillHeight, borderRec.Width, borderRec.Height - fillHeight);

                pe.Graphics.FillRectangle(brush, fillRec);
                pe.Graphics.DrawRectangle(pen, borderRec);
            }
            finally
            {
                _invalidated = false;

                if (pen != null)
                    pen.Dispose();

                if (brush != null)
                    brush.Dispose();
            }
        }
    }
}