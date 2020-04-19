using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace UltimateConsole
{
    public class ConsoleCursor
    {
        public enum Shape
        {
            SolidRectangle,
            Rectangle,
            Underscore,
            Line
        }

        public bool Visible { get; set; } = true;

        private Point pos = new Point();
        public Point Position
        {
            get => pos;
            set
            {
                pos = value;
                index = pos.Y * Console.BufferSize.Width + pos.X;
            }
        }
        private int index;
        public int Index
        {
            get => index;
            set
            {
                index = value;
                int x = index % Console.BufferSize.Width;
                pos = new Point(x, (index - x) / Console.BufferSize.Width);
            }
        }
        public Color Color { get; set; } = Color.White;
        public Shape DisplayShape { get; set; }

        public ConsoleCursor(Shape display)
        {
            DisplayShape = display;
        }

        public void DrawShape(Graphics g, int fontWidth, int xOffset, int fontHeight, int yOffset)
        {
            switch (DisplayShape)
            {
                case Shape.Rectangle:
                    using (Pen p = new Pen(Color))
                        g.DrawRectangle(p, Position.X * fontWidth + xOffset, Position.Y * fontHeight + yOffset, fontWidth, fontHeight);
                    break;
                case Shape.SolidRectangle:
                    using (Brush b = new SolidBrush(Color))
                        g.FillRectangle(b, Position.X * fontWidth + xOffset, Position.Y * fontHeight + yOffset, fontWidth, fontHeight);
                    break;
                case Shape.Underscore:
                    int uHeight = (int)(fontHeight * 0.2);
                    using (Brush b = new SolidBrush(Color))
                        g.FillRectangle(b, Position.X * fontWidth + xOffset, Position.Y * fontHeight + fontHeight - uHeight + yOffset, fontWidth, uHeight);
                    break;
                case Shape.Line:
                    Point pos = new Point(Position.X * fontWidth + xOffset, Position.Y * fontHeight + yOffset);
                    using (Pen p = new Pen(Color, 1))
                        g.DrawLine(p, pos.X, pos.Y, pos.X, pos.Y + fontHeight);
                    break;
            }
        }
    }
}
