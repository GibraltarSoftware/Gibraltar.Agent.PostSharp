#region Released to Public Domain by eSymmetrix, Inc.

/********************************************************************************
 *   This file is sample code demonstrating Gibraltar integration with PostSharp
 *   
 *   This sample is free software: you have an unlimited rights to
 *   redistribute it and/or modify it.
 *   
 *   This sample is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
 *   
 *******************************************************************************/

using System.Drawing;
using System.Windows.Forms;

#endregion

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