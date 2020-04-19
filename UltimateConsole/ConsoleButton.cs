using System.Drawing;
using System.Windows.Forms;

namespace UltimateConsole
{
    class ConsoleButton
    {
        private readonly ConsoleForm form;

        public string Text { get; set; }
        public Rectangle Bounds { get; set; }

        private bool enabled = true;
        public bool Enabled
        {
            get => enabled;
            set
            {
                enabled = value;
                over = false;
                down = false;
            }
        }

        private Color disabledTextColor;
        private Color textColor;
        public Color TextColor
        {
            get => textColor;
            set
            {
                textColor = value;
                disabledTextColor = Color.FromArgb(textColor.A, textColor.R / 2, textColor.G / 2, textColor.B / 2);
            }
        }
        public Color Color { get; set; }
        public Color BackColor { get; set; }
        private const double MOUSE_OVER_PERCENT = 0.6;
        private Color mouseOverColor
        {
            get => Color.FromArgb(Color.A, (int)(Color.R * MOUSE_OVER_PERCENT), (int)(Color.G * MOUSE_OVER_PERCENT), (int)(Color.B * MOUSE_OVER_PERCENT));
        }

        private const double MOUSE_DOWN_PERCENT = 0.4;
        private Color mouseDownColor
        {
            get => Color.FromArgb(Color.A, (int)(Color.R * MOUSE_DOWN_PERCENT), (int)(Color.G * MOUSE_DOWN_PERCENT), (int)(Color.B * MOUSE_DOWN_PERCENT));
        }

        public System.Action OnClick { get; set; } = () => { };

        private bool over = false;
        private bool down = false;

        public ConsoleButton(Point pos, Size size, Color color, ConsoleForm f) : this(new Rectangle(pos, size), color, f) { }
        public ConsoleButton(Rectangle bounds, Color color, ConsoleForm f)
        {
            Text = "";
            Bounds = bounds;
            TextColor = color;
            Color = color;
            const double backPercent = 0.8;
            BackColor = Color.FromArgb(color.A, (int)(color.R * backPercent), (int)(color.G * backPercent), (int)(color.G * backPercent));
            form = f;

            //events
            form.MouseDown += Form_MouseDown;
            form.MouseUp += Form_MouseUp;
            form.MouseMove += Form_MouseMove;
        }

        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (!Enabled) return;

            bool contains = Bounds.Contains(e.Location);
            if (!over && contains)
            {
                over = true;
                Console.Update();
            }
            else if(over && !contains)
            {
                over = false;
                Console.Update();
            }
        }

        private void Form_MouseUp(object sender, MouseEventArgs e)
        {
            if (!Enabled) return;

            if (down)
            {
                down = false;
                Console.Update();

                if (Enabled && Bounds.Contains(e.X, e.Y))
                {
                    //click happened, now run the function
                    OnClick();
                }
            }
        }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (!Enabled) return;

            if (!down && Bounds.Contains(e.X, e.Y))
            {
                down = true;
                Console.Update();
            }
        }

        public void Draw(Graphics g)
        {
            //background
            //the highlight, when clicked or mouse overed
            if ((down || over) && Enabled)
            {
                using (Brush b = new SolidBrush(down ? mouseDownColor : mouseOverColor))
                {
                    g.FillRectangle(b, Bounds);
                }
            } else
            {
                //do normal, no highlight or mouse down
                using (Brush b = new SolidBrush(BackColor))
                {
                    g.FillRectangle(b, Bounds);
                }
            }

            //the outline
            using (Pen p = new Pen(Color))
            {
                g.DrawRectangle(p, Bounds);
            }

            using (Brush b = new SolidBrush(Enabled ? textColor : disabledTextColor))
            {
                int w = Text.Length * Console.FontWidth;
                //center the string on the image
                g.DrawString(Text, Console.Font, b, new Point(Bounds.Left + (Bounds.Width - w) / 2, Bounds.Top + (Bounds.Height - Console.FontHeight) / 2));
            }
        }
    }
}
