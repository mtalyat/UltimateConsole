using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public static bool ClearAfterPrint { get; set; } = true;

        public static Point CursorPosition { get; private set; } = new Point();

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

        public static bool Running { get; private set; } = false;

        private static ConsoleForm form;

        public static Color DefaultForeColor { get; set; } = Color.White;
        public static Color DefaultBackColor { get; set; } = Color.Black;

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

                SetWindowSize();
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

            form.KeyPreview = true;
            //events
            form.Paint += Form_Paint;
            form.KeyDown += Form_KeyDown;
            form.KeyUp += Form_KeyUp;

            Running = true;
        }

        #region Events

        private static void Form_KeyDown(object sender, KeyEventArgs e)
        {
            newKeyStates[e.KeyCode.ToString()] = KeyStates.Down;

            //Log("Key down for: " + e.KeyCode.ToString());
        }

        private static void Form_KeyUp(object sender, KeyEventArgs e)
        {
            newKeyStates[e.KeyCode.ToString()] = KeyStates.Up;

            //Log("Key up for: " + e.KeyCode.ToString());
        }

        private static void Form_Paint(object sender, PaintEventArgs e)
        {
            Print(e.Graphics);
        }

        #endregion

        /// <summary>
        /// Clears the data, not the screen. Use Clear(), followed by Update() to clear the screen.
        /// </summary>
        public static void Clear()
        {
            //clear the text
            for (int i = 0; i < bufferSize.Height; i++)
            {
                chars[i] = ConsoleChar.FromString(new string(' ', BufferSize.Width), DefaultForeColor);
            }

            //clear the rects
            rects.Clear();
        }

        private static void SetBufferSize(Size size)
        {
            //don't set if the values are the same
            if (bufferSize == size) return;

            bufferSize = size;

            //set the data to the correct size
            chars = new ConsoleChar[size.Height][];
            Clear();

            SetWindowSize();
        }

        private static void SetWindowSize()
        {
            form.Size = new Size(FontWidth * bufferSize.Width, FontHeight * bufferSize.Height);
        }

        //make it so the popup stays up
        public static void Run()
        {
            //form.ShowDialog();
            Application.Run(form);
        }

        public static void SetDefaultColors(Color fore, Color back)
        {
            DefaultForeColor = fore;
            DefaultBackColor = back;
        }

        #region Writing

        public static void Write(object o)
        {
            throw new NotImplementedException();
        }

        public static void WriteLine(object o)
        {
            Write(o.ToString() + '\n');
        }

        #endregion

        #region Draw

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
            Draw(c, DefaultForeColor, left, top);
        }

        public static void Draw(string str, int left, int top)
        {
            Draw(str, DefaultForeColor, left, top);
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
            Draw(strs, DefaultForeColor, left, top);
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
            Draw(chars, DefaultForeColor, left, top);
        }

        public static void Draw(char[][] chars, Color fgColor, int left, int top)
        {
            for(int i = 0; i < chars.Length; i++)
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
            g.Clear(DefaultBackColor);

            int height = FontHeight;
            int width = FontWidth;

            //draw the rectangles
            for (int i = 0; i < rects.Count; i++)
            {
                using (Brush brush = new SolidBrush(rects[i].Color))
                {
                    g.FillRectangle(brush, rects[i] * new Size(width, height));
                }
            }

            //draw the text over the rectangles
            for (int i = 0; i < bufferSize.Height; i++)
            {
                for (int j = 0; j < bufferSize.Width; j++)
                {
                    using (Brush brush = new SolidBrush(chars[i][j].Color))
                    {
                        g.DrawString(chars[i][j].ToString(), form.Font, brush, j * width, i * height);
                    }
                }
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
                foreach(string key in newKeyStates.Keys)
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
            for(int i = keyStates.Count - 1; i >= 0; i--)
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

        public static bool IsKeyDown(string keyName)
        {
            return keyStates.ContainsKey(keyName) && keyStates[keyName] == KeyStates.Down;
        }

        public static bool IsKeyPressed(string keyName)
        {
            return keyStates.ContainsKey(keyName) && keyStates[keyName] == KeyStates.Pressed;
        }

        public static bool IsKeyUp(string keyName)
        {
            return keyStates.ContainsKey(keyName) && keyStates[keyName] == KeyStates.Up;
        }

        #endregion

        //writes to the System.Console
        public static void Log(object o)
        {
            System.Console.WriteLine(o);
        }

    }//end Console
}
