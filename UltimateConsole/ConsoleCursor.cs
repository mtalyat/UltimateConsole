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
            Underscore
        }

        public bool Visible { get; set; } = true;
        public Point Position { get; set; } = new Point(-1, -1);
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
            }
        }
    }
}
