using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace UltimateConsole
{
    public static class Console
    {
        public static string Name
        {
            get => form.Text;
            set => form.Text = value;
        }

        //settings
        public static bool ClearAfterPrint { get; set; } = false;
        public static bool HideKeyInput { get; set; } = false;
        public static bool StopOnError { get; set; } = true;
        public static int TabLength { get; set; } = 2;

        private static bool isControl = true;
        public static bool IsControl
        {
            get
            {
                return isControl;
            }
            set
            {
                isControl = value;
            }
        }

        private static Size bufferSize;
        public static Size BufferSize
        {
            get
            {
                return bufferSize;
            }
            set
            {
                SetBufferSize(value);
            }
        }

        private static ConsoleChar[][] chars;
        private static List<ConsoleRect> rects;

        public static bool Running
        {
            get => form.Running;
        }

        private static ConsoleForm form;

        public static Color ForeColor { get; set; } = Color.White;
        public static Color BackColor { get; set; } = Color.Black;

        private static int FontHeight
        {
            get => form.Font.Height;
        }
        private static int FontWidth
        {
            get => (int)(FontHeight * 0.75);
        }
        public static float FontSize
        {
            get => form.Font.Size;
            set
            {
                Font f = new Font(form.Font.Name, value);
                form.Font.Dispose();
                form.Font = f;

                FixWindowSize();
            }
        }

        enum KeyStates
        {
            None,
            Down,
            Pressed,
            Up
        }

        private static Dictionary<string, KeyStates> keyStates = new Dictionary<string, KeyStates>();
        private static Dictionary<string, KeyStates> newKeyStates = new Dictionary<string, KeyStates>();

        private static string waitForKey = "";
        private static Action onWaitKeyPressed;

        public static ConsoleCursor MousePointer { get; private set; } = new ConsoleCursor(ConsoleCursor.Shape.Rectangle);
        public static int mouseState = -1;

        public static ConsoleCursor TextCursor { get; private set; } = new ConsoleCursor(ConsoleCursor.Shape.Underscore);
        private static Timer cursorTimer;

        private static bool shift = false;
        private static bool caps = false;

        //input
        private static string input = "";
        private static bool reading = false;

        public static int BorderWidth { get; set; } = 10;
        public static Color BorderColor { get; set; } = Color.Cyan;

        const int DEFAULT_MARGIN = 0;
        public static int TopMargin { get; set; } = DEFAULT_MARGIN;
        public static int BottomMargin { get; set; } = DEFAULT_MARGIN;
        public static int LeftMargin { get; set; } = DEFAULT_MARGIN;
        public static int RightMargin { get; set; } = DEFAULT_MARGIN;

        private static int XMargin
        {
            get => LeftMargin + RightMargin;
        }
        private static int YMargin
        {
            get => TopMargin + BottomMargin;
        }

        //TODO: highlighting
        //TODO: copy, cut, paste
        //TODO: top info: (text, exit button, minimize, maximize, etc) (isControl)
        //TODO: transparent draw

        static Console()
        {
            Initialize(new Size(70, 30));
        }

        /// <summary>
        /// Initializes the Console with the given buffer Size.
        /// </summary>
        private static void Initialize(Size size)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            form = new ConsoleForm();

            rects = new List<ConsoleRect>();
            SetBufferSize(size);

            cursorTimer = new Timer();
            cursorTimer.Interval = 1000;
            cursorTimer.Tick += CursorTimer_Tick;
            cursorTimer.Start();

            MousePointer.Position = new Point(-1, -1);
            TextCursor.Position = new Point(0, 0);

            form.KeyPreview = true;

            //events
            form.Paint += Form_Paint;
            form.KeyDown += Form_KeyDown;
            form.KeyUp += Form_KeyUp;
            form.MouseMove += Form_MouseMove;
            form.MouseLeave += Form_MouseLeave;
            form.MouseDown += Form_MouseDown;
            form.MouseUp += Form_MouseUp;
        }

        //make it so it pops up and stays up
        public static void Run()
        {
            Application.Run(form);
        }

        //pops up and closes when it is done
        public static void Show()
        {
            form.Show();
        }

        private static bool IsValid(Point p) => IsValid(p.X, p.Y);
        private static bool IsValid(int x, int y)
        {
            return x >= 0 && y >= 0 && x < bufferSize.Width && y < bufferSize.Height;
        }

        #region Events

        private static void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (newKeyStates.ContainsKey(e.KeyCode.ToString()) &&
                newKeyStates[e.KeyCode.ToString()] == KeyStates.Down)
            {
                //already been marked as down, don't do it again
                return;
            }

            newKeyStates[e.KeyCode.ToString()] = KeyStates.Down;

            if (!shift && (Control.ModifierKeys & Keys.Shift) != 0)
            {
                shift = true;
            } else if (e.KeyCode == Keys.CapsLock)
            {
                caps = !caps;
            }

            //moving
            switch (e.KeyCode)
            {
                case Keys.Up:
                    Move(0, -1);
                    return;
                case Keys.Down:
                    Move(0, 1);
                    return;
                case Keys.Left:
                    Move(-1, 0);
                    return;
                case Keys.Right:
                    Move(1, 0);
                    return;
            }

            //if show text
            if (!HideKeyInput)
            {
                switch (e.KeyCode)
                {
                    case Keys.Back:
                        BackSpace();
                        break;
                    case Keys.Delete:
                        Delete();
                        break;
                    case Keys.Tab:
                        input += new string(' ', Tab());
                        break;
                    case Keys.Enter:
                        NewLine();
                        input += '\n';
                        break;
                    default:
                        Write(KeyToChar(e.KeyCode));
                        input += KeyToChar(e.KeyCode);
                        break;
                }
            }
        }

        private static void Form_KeyUp(object sender, KeyEventArgs e)
        {
            newKeyStates[e.KeyCode.ToString()] = KeyStates.Up;

            if (shift && (Control.ModifierKeys & Keys.Shift) == 0)
            {
                shift = false;
            }
        }

        private static void Form_Paint(object sender, PaintEventArgs e)
        {
            Print(e.Graphics);
        }

        private static void Form_MouseUp(object sender, MouseEventArgs e)
        {
            if (MousePointer.DisplayShape == ConsoleCursor.Shape.SolidRectangle)
            {
                MousePointer.DisplayShape = ConsoleCursor.Shape.Rectangle;
            }

            mouseState = -1;

            Update();
        }

        private static void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (MousePointer.DisplayShape == ConsoleCursor.Shape.Rectangle)
            {
                MousePointer.DisplayShape = ConsoleCursor.Shape.SolidRectangle;
            }

            switch (e.Button)
            {
                case MouseButtons.Left: mouseState = 0; break;
                case MouseButtons.Right: mouseState = 1; break;
                case MouseButtons.Middle: mouseState = 2; break;
                default:
                    mouseState = -1; break;
            }

            TextCursor.Position = new Point(e.X / FontWidth, e.Y / FontHeight);
            TextCursor.Visible = true;
            cursorTimer.Stop();
            cursorTimer.Start();

            Update();
        }

        private static void CursorTimer_Tick(object sender, EventArgs e)
        {
            TextCursor.Visible = !TextCursor.Visible;

            Update();
        }

        private static void Form_MouseLeave(object sender, EventArgs e)
        {
            MovePointerOutOfView();

            Update();
        }

        private static void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.X <= BorderWidth + LeftMargin ||
                e.X > form.Size.Width - BorderWidth - RightMargin ||
                e.Y <= BorderWidth + TopMargin ||
                e.Y > form.Size.Height - BorderWidth - BottomMargin)
            {
                MovePointerOutOfView();
            } else
            {
                //in view, not on border
                MousePointer.Position = new Point((e.X - BorderWidth - LeftMargin) / FontWidth, (e.Y - BorderWidth - TopMargin) / FontHeight);
            }

            Update();
        }

        #endregion

        #region Mouse and Pointer

        private static void MovePointerOutOfView()
        {
            MousePointer.Position = new Point(-100, -100);
        }

        public static void ShowCursor()
        {
            TextCursor.Visible = true;
            cursorTimer.Start();
        }

        public static void HideCursor()
        {
            cursorTimer.Stop();
            TextCursor.Visible = false;
        }

        public static void ShowPointer()
        {
            MousePointer.Visible = true;
        }

        public static void HidePointer()
        {
            MousePointer.Visible = false;
        }

        #endregion

        #region Display

        /// <summary>
        /// Clears the data, not the screen.
        /// </summary>
        public static void Clear()
        {
            //clear the text
            for (int i = 0; i < bufferSize.Height; i++)
            {
                chars[i] = ConsoleChar.FromString(new string(' ', BufferSize.Width), ForeColor);
            }

            //clear the rects
            rects.Clear();
        }

        public static void ClearScreen()
        {
            Clear();
            Update();
        }

        public static void SetBufferSize(Size size)
        {
            //don't set if the values are the same
            if (bufferSize == size) return;

            bufferSize = size;


            //TODO: adjust data, don't delete it
            //set the data to the correct size
            chars = new ConsoleChar[size.Height][];
            Clear();
        }

        public static void SetWindowSize(Size size)
        {
            form.Size = new Size(size.Width / FontWidth * FontWidth + BorderWidth * 2 + XMargin, size.Height / FontHeight * FontHeight + BorderWidth * 2 + YMargin);
        }

        private static void FixWindowSize()
        {
            SetWindowSize(bufferSize);
        }

        public static void SetDefaultColors(Color fore, Color back)
        {
            ForeColor = fore;
            BackColor = back;
        }

        #endregion

        #region Writing and Reading

        public static void Write(object o)
        {
            string str = o.ToString();

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (c == '\t')
                {
                    Tab();
                } else if (c == '\n')
                {
                    NewLine();
                } else if (c >= 32)//32 is space, or something like that
                {
                    TypeAndMove(new ConsoleChar(c, ForeColor));
                }
            }

            Update();
        }

        public static void WriteLine(object o)
        {
            Write(o.ToString() + '\n');
        }

        public static string ReadLine()
        {
            input = "";
            reading = true;

            while (!input.EndsWith("\n"))
                Application.DoEvents();

            reading = false;

            return input.Remove(input.Length - 1);
        }

        public static char Read()
        {
            input = "";
            reading = true;

            while (string.IsNullOrEmpty(input))
                Application.DoEvents();

            reading = false;

            return input[0];
        }

        #endregion

        #region Typing

        private static void NewLine()
        {
            Shift(TextCursor.Position.Y, -1);

            TextCursor.Position = new Point(0, TextCursor.Position.Y + 1);

            RefreshTextCursorTimer();
        }

        private static void Shift(int top, int amount)
        {
            if (amount == 0) return;

            if(amount < 0)//shifting down
            {
                //bottom row gets 'deleted'
                for (int i = bufferSize.Height - 1; i > top + 1 && i + amount >= 0; i--)
                {
                    Array.Copy(chars[i + amount], 0, chars[i], 0, bufferSize.Width);
                }
            } else//shifting up
            {
                for(int i = 0; i < top - 1 && i + amount < bufferSize.Height; i++)
                {
                    Array.Copy(chars[i + amount], 0, chars[i], 0, bufferSize.Width);
                }
            }

            //TODO: split rectangles if there are in the middle
            //and shift rectangles
        }

        private static int Tab()
        {
            int iterations = 0;

            for (int i = TextCursor.Position.X; i % TabLength != 0 || iterations == 0; i++)
            {
                TypeAndMove(new ConsoleChar(' ', ForeColor));
                iterations++;
            }

            return iterations;
        }

        private static void TypeAndMove(ConsoleChar c)
        {
            int x = TextCursor.Position.X;
            int y = TextCursor.Position.Y;

            Draw(c.Char, c.Color, x, y);
            chars[y][x].Filled = true;

            if (x >= bufferSize.Width - 1)
            {
                //at the end, gonna have to make a new line
                NewLine();
            } else
            {
                TextCursor.Position = new Point(x + 1, y);
            }

            RefreshTextCursorTimer();
        }

        private static void BackSpace()
        {
            int x = TextCursor.Position.X;
            int y = TextCursor.Position.Y;

            if (x <= 0)
            {
                //go back to the previous line
                TextCursor.Position = new Point(FindFirstFilledFromRight(y - 1), y - 1);
            } else
            {
                TextCursor.Position = new Point(TextCursor.Position.X - 1, TextCursor.Position.Y);
            }

            if (reading && input.Length >= 1)
            {
                input = input.Remove(input.Length - 1);
            }

            ClearAt(TextCursor.Position);

            RefreshTextCursorTimer();
            Update();
        }

        private static void Delete()
        {
            throw new NotImplementedException();
        }

        private static int FindFirstFilledFromRight(int top, int left = -1)
        {
            if (!IsValid(0, top)) return 0;

            for (int i = bufferSize.Width - 1; i >= 0; i--)
            {
                if (chars[top][i].Filled)
                {
                    //wouldn't make sense for it to jump to the right
                    if (i > left && left >= 0)
                    {
                        return left;
                    } else
                    {
                        return i;
                    }
                }
            }

            return 0;
        }

        private static void ClearAt(Point p) => ClearAt(p.X, p.Y);

        private static void ClearAt(int left, int top)
        {
            chars[top][left] = new ConsoleChar();
        }

        private static void Move(int x, int y)
        {
            int left = TextCursor.Position.X;
            int top = TextCursor.Position.Y;

            Point newPos = new Point(FindFirstFilledFromRight(top + y, left + x), top + y);

            if (!IsValid(newPos)) return;

            TextCursor.Position = newPos;
            RefreshTextCursorTimer();
            Update();
        }

        private static void RefreshTextCursorTimer()
        {
            cursorTimer.Stop();
            cursorTimer.Start();
            TextCursor.Visible = true;
        }

        #endregion

        #region Drawing

        public static void Draw(char c, Color fgColor, Color bgColor, int left, int top)
        {
            Draw(c, fgColor, left, top);

            rects.Add(new ConsoleRect(left, top, 1, 1, bgColor));
        }

        public static void Draw(char c, Color color, int left, int top)
        {
            chars[top][left] = new ConsoleChar(c, color);
        }

        public static void Draw(char c, int left, int top)
        {
            Draw(c, ForeColor, left, top);
        }

        public static void Draw(string str, int left, int top)
        {
            Draw(str, ForeColor, left, top);
        }

        //draws a string to the screen, in its color
        public static void Draw(string str, Color fgColor, int left, int top)
        {
            int stringLength = str.Length;

            //don't even bother: completely outside of the console
            if (stringLength + left < 0 || left >= bufferSize.Width || top < 0 || top >= bufferSize.Height)
            {
                return;
            }

            ConsoleChar[] arr = ConsoleChar.FromString(str, fgColor);

            int fromIndex = Math.Max(0, left * -1);
            int toIndex = Math.Max(0, left);
            int length = Math.Min(arr.Length - fromIndex, BufferSize.Width - 1 - toIndex);

            Array.Copy(arr, fromIndex, chars[top], toIndex, length);
        }

        public static void Draw(string str, Color fgColor, Color bgColor, int left, int top)
        {
            Draw(str, fgColor, left, top);

            rects.Add(new ConsoleRect(left, top, str.Length, 1, bgColor));
        }

        //uses default colors
        public static void Draw(string[] strs, int left, int top)
        {
            Draw(strs, ForeColor, left, top);
        }

        //uses default bg color
        public static void Draw(string[] strs, Color fgColor, int left, int top)
        {
            for (int i = 0; i < strs.Length; i++)
            {
                Draw(strs[i], fgColor, left, top + i);
            }
        }

        public static void Draw(string[] strs, int width, Color fgColor, Color bgColor, int left, int top)
        {
            Draw(strs, fgColor, left, top);

            rects.Add(new ConsoleRect(left, top, width, strs.Length, bgColor));
        }

        public static void Draw(char[][] chars, int left, int top)
        {
            Draw(chars, ForeColor, left, top);
        }

        public static void Draw(char[][] chars, Color fgColor, int left, int top)
        {
            for (int i = 0; i < chars.Length; i++)
            {
                Draw(new string(chars[i]), fgColor, left, top + i);
            }
        }

        public static void Draw(char[][] chars, int width, Color fgColor, Color bgColor, int left, int top)
        {
            Draw(chars, fgColor, left, top);

            rects.Add(new ConsoleRect(left, top, width, chars.Length, bgColor));
        }

        #endregion

        #region Printing

        /// <summary>
        /// Draws the scene and updates key inputs.
        /// </summary>
        public static void Update()
        {
            if (!form.Running) return;

            if (form.InvokeRequired)
            {
                form.Invoke(new Action(() => form.Refresh()));
            } else
            {
                form.Refresh();
            }

            UpdateKeys();
        }

        private static void Print(Graphics g)
        {
            g.Clear(BackColor);

            int height = FontHeight;
            int width = FontWidth;

            //draw the border
            using (Pen p = new Pen(BorderColor, BorderWidth))
            {
                g.DrawRectangle(p, BorderWidth / 2, BorderWidth / 2, form.Size.Width - BorderWidth - LeftMargin, form.Size.Height - BorderWidth - TopMargin);
            }

            //draw the rectangles
            for (int i = 0; i < rects.Count; i++)
            {
                using (Brush brush = new SolidBrush(rects[i].Color))
                {
                    g.FillRectangle(brush, rects[i] * new Size(width, height) + new Point(BorderWidth + LeftMargin, BorderWidth + TopMargin));
                }
            }

            //draw the text over the rectangles
            for (int i = 0; i < bufferSize.Height; i++)
            {
                for (int j = 0; j < bufferSize.Width; j++)
                {
                    using (Brush brush = new SolidBrush(chars[i][j].Color))
                    {
                        g.DrawString(chars[i][j].ToString(), form.Font, brush, j * width + BorderWidth + LeftMargin, i * height + BorderWidth + TopMargin);
                    }
                }
            }

            //draw the cursor and pointer
            if (MousePointer.Visible)
            {
                MousePointer.DrawShape(g, FontWidth, BorderWidth + LeftMargin, FontHeight, BorderWidth + TopMargin);
            }
            if (TextCursor.Visible)
            {
                TextCursor.DrawShape(g, FontWidth, BorderWidth + LeftMargin, FontHeight, BorderWidth + TopMargin);
            }

            if (ClearAfterPrint)
            {
                Clear();
            }
        }

        #endregion

        #region Input

        public static void WaitForKey(string keyName, Action onInput)
        {
            keyStates.Clear();

            waitForKey = keyName;
            onWaitKeyPressed = onInput;
        }

        private static void UpdateKeys()
        {
            //search for that key you are waiting on and disreguard everything else
            if (!string.IsNullOrEmpty(waitForKey))
            {
                foreach (string key in newKeyStates.Keys)
                {
                    if (key == waitForKey && newKeyStates[key] == KeyStates.Down)
                    {
                        waitForKey = "";
                        onWaitKeyPressed();
                        return;
                    }
                }
                return;
            }

            //not looking for a specific key, go on as normal

            //put new key states into the ones that check whats up
            for (int i = keyStates.Count - 1; i >= 0; i--)
            {
                string key = keyStates.ElementAt(i).Key;

                if (keyStates[key] == KeyStates.Down)
                {
                    keyStates[key] = KeyStates.Pressed;
                }
                else if (keyStates[key] == KeyStates.Up)
                {
                    keyStates.Remove(key);
                }
            }

            foreach (KeyValuePair<string, KeyStates> pair in newKeyStates)
            {
                if (keyStates.ContainsKey(pair.Key))
                {
                    //don't change the state back to down if already on pressed
                    if (!(keyStates[pair.Key] == KeyStates.Pressed && pair.Value == KeyStates.Down))
                    {
                        keyStates[pair.Key] = pair.Value;
                    }
                } else
                {
                    keyStates[pair.Key] = pair.Value;
                }
            }

            newKeyStates.Clear();
        }

        public static bool IsKeyDown(Keys key) => IsKeyDown(key.ToString());
        public static bool IsKeyDown(string keyName)
        {
            return keyStates.ContainsKey(keyName) && keyStates[keyName] == KeyStates.Down;
        }

        public static bool IsKeyPressed(Keys key) => IsKeyPressed(key.ToString());
        public static bool IsKeyPressed(string keyName)
        {
            return keyStates.ContainsKey(keyName) && (
                keyStates[keyName] == KeyStates.Pressed ||
                keyStates[keyName] == KeyStates.Down);
        }

        public static bool IsKeyUp(Keys key) => IsKeyUp(key.ToString());
        public static bool IsKeyUp(string keyName)
        {
            return keyStates.ContainsKey(keyName) && keyStates[keyName] == KeyStates.Up;
        }

        public static bool IsMouseDown() {
            return mouseState != -1;
        }

        public static bool IsMouseDown(int state)
        {
            return mouseState == state;
        }

        //borrowed from Stack Overflow
        //
        private static char KeyToChar(Keys key)
        {

            if (IsKeyPressed(Keys.Alt) ||
                IsKeyPressed(Keys.Control))
            {
                return '\x00';
            }

            bool upper = shift != caps;

            switch (key)
            {
                case Keys.Enter: return '\n';
                case Keys.A: return (upper ? 'A' : 'a');
                case Keys.B: return (upper ? 'B' : 'b');
                case Keys.C: return (upper ? 'C' : 'c');
                case Keys.D: return (upper ? 'D' : 'd');
                case Keys.E: return (upper ? 'E' : 'e');
                case Keys.F: return (upper ? 'F' : 'f');
                case Keys.G: return (upper ? 'G' : 'g');
                case Keys.H: return (upper ? 'H' : 'h');
                case Keys.I: return (upper ? 'I' : 'i');
                case Keys.J: return (upper ? 'J' : 'j');
                case Keys.K: return (upper ? 'K' : 'k');
                case Keys.L: return (upper ? 'L' : 'l');
                case Keys.M: return (upper ? 'M' : 'm');
                case Keys.N: return (upper ? 'N' : 'n');
                case Keys.O: return (upper ? 'O' : 'o');
                case Keys.P: return (upper ? 'P' : 'p');
                case Keys.Q: return (upper ? 'Q' : 'q');
                case Keys.R: return (upper ? 'R' : 'r');
                case Keys.S: return (upper ? 'S' : 's');
                case Keys.T: return (upper ? 'T' : 't');
                case Keys.U: return (upper ? 'U' : 'u');
                case Keys.V: return (upper ? 'V' : 'v');
                case Keys.W: return (upper ? 'W' : 'w');
                case Keys.X: return (upper ? 'X' : 'x');
                case Keys.Y: return (upper ? 'Y' : 'y');
                case Keys.Z: return (upper ? 'Z' : 'z');
                case Keys.D0: return (shift ? ')' : '0');
                case Keys.D1: return (shift ? '!' : '1');
                case Keys.D2: return (shift ? '@' : '2');
                case Keys.D3: return (shift ? '#' : '3');
                case Keys.D4: return (shift ? '$' : '4');
                case Keys.D5: return (shift ? '%' : '5');
                case Keys.D6: return (shift ? '^' : '6');
                case Keys.D7: return (shift ? '&' : '7');
                case Keys.D8: return (shift ? '*' : '8');
                case Keys.D9: return (shift ? '(' : '9');
                case Keys.Oemplus: return (shift ? '+' : '=');
                case Keys.OemMinus: return (shift ? '_' : '-');
                case Keys.OemQuestion: return (shift ? '?' : '/');
                case Keys.Oemcomma: return (shift ? '<' : ',');
                case Keys.OemPeriod: return (shift ? '>' : '.');
                case Keys.OemOpenBrackets: return (shift ? '{' : '[');
                case Keys.OemQuotes: return (shift ? '"' : '\'');
                case Keys.Oem1: return (shift ? ':' : ';');
                case Keys.Oem3: return (shift ? '~' : '`');
                case Keys.Oem5: return (shift ? '|' : '\\');
                case Keys.Oem6: return (shift ? '}' : ']');
                case Keys.Tab: return '\t';
                case Keys.Space: return ' ';

                // Number Pad
                case Keys.NumPad0: return '0';
                case Keys.NumPad1: return '1';
                case Keys.NumPad2: return '2';
                case Keys.NumPad3: return '3';
                case Keys.NumPad4: return '4';
                case Keys.NumPad5: return '5';
                case Keys.NumPad6: return '6';
                case Keys.NumPad7: return '7';
                case Keys.NumPad8: return '8';
                case Keys.NumPad9: return '9';
                case Keys.Subtract: return '-';
                case Keys.Add: return '+';
                case Keys.Decimal: return '.';
                case Keys.Divide: return '/';
                case Keys.Multiply: return '*';

                default: return (char)key;
            }
        }

        #endregion

        #region Debugging

        //writes to the System.Console
        public static void Log(object o)
        {
            System.Console.WriteLine(o);
        }

        public static void LogWarning(object o)
        {
            Log("[WRN] " + o);
        }

        public static void LogError(object o)
        {
            Log("[ERR] " + o);

            if (StopOnError)
                throw new LogErrorException(o.ToString());
        }

        #endregion

    }//end Console
}

class LogErrorException : Exception
{
    public LogErrorException(string message) : base($"Log Error: \"{message}\"")
    {

    }
}