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
        private Color m_BorderColor = Color.Black;
        private int m_BorderWidth = 50;
        private Color m_FillColor = Color.Green;
        private volatile bool m_Invalidated;
        private int m_ProgressInPercentage = 10;

        public VerticalProgressBar()
        {
            m_ProgressInPercentage = 0;

            InitializeComponent();

            // Enable double buffering
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
        }

        public Color BorderColor
        {
            get { return m_BorderColor; }
            set
            {
                m_BorderColor = value;

                ThreadSafeInvalidate();
            }
        }

        public int BorderWidth
        {
            get { return m_BorderWidth; }
            set
            {
                m_BorderWidth = value;

                ThreadSafeInvalidate();
            }
        }

        public Color FillColor
        {
            get { return m_FillColor; }
            set
            {
                m_FillColor = value;

                ThreadSafeInvalidate();
            }
        }
        
        public int ProgressInPercentage
        {
            get { return m_ProgressInPercentage; }
            set
            {
                m_ProgressInPercentage = value;
                ThreadSafeInvalidate();
            }
        }

        private void ThreadSafeInvalidate()
        {
            if (!m_Invalidated)
            {
                m_Invalidated = true;
                if (InvokeRequired)
                    Invoke(new MethodInvoker(Invalidate));
                else
                    Invalidate();
            }
        }

        protected override void OnResize(System.EventArgs e)
        {
            m_Invalidated = true;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            Pen pen = null;
            SolidBrush brush = null;

            base.OnPaint(pe);

            try
            {
                pen = new Pen(m_BorderColor, m_BorderWidth);
                brush = new SolidBrush(m_FillColor);

                Rectangle borderRec = ClientRectangle;

                borderRec.X = borderRec.X + m_BorderWidth / 2;
                borderRec.Y = borderRec.Y + m_BorderWidth / 2;
                borderRec.Width = borderRec.Width - m_BorderWidth;
                borderRec.Height = borderRec.Height - m_BorderWidth;

                int fillHeight = (borderRec.Height * (100 - m_ProgressInPercentage)) / 100;

                Rectangle fillRec = new Rectangle(borderRec.X, borderRec.Y + fillHeight, borderRec.Width, borderRec.Height - fillHeight);

                pe.Graphics.FillRectangle(brush, fillRec);
                pe.Graphics.DrawRectangle(pen, borderRec);
            }
            finally
            {
                m_Invalidated = false;

                if (pen != null)
                    pen.Dispose();

                if (brush != null)
                    brush.Dispose();
            }
        }
    }
}