using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace UltimateConsole
{
    struct ConsoleRect
    {
        public Point Position { get; set; }
        public int Left => Position.X;
        public int Top => Position.Y;

        public Size Size { get; set; }
        public int Width => Size.Width;
        public int Height => Size.Height;

        public Color Color { get; set; }

        public ConsoleRect(int left, int top, int w, int h, Color c)
        {
            Position = new Point(left, top);
            Size = new Size(w, h);
            Color = c;
        }

        public static implicit operator Rectangle(ConsoleRect cr) => new Rectangle(cr.Position, cr.Size);

        public static ConsoleRect operator *(ConsoleRect cr, int i)
        {
            return new ConsoleRect(cr.Left * i, cr.Top * i, cr.Width * i, cr.Height * i, cr.Color);
        }

        public static ConsoleRect operator *(ConsoleRect cr, Size s)
        {
            return new ConsoleRect((int)(cr.Left * s.Width), (int)(cr.Top * s.Height), (int)(cr.Width * s.Width), (int)(cr.Height * s.Height), cr.Color);
        }
    }
}
