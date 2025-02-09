﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace UltimateConsole
{
    public static class Console
    {
        public static string Title { get; set; } = "Ultimate Console";

        //consts
        private const char NEW_LINE = '\n';
        private const char TAB = '\t';

        //settings
        public static bool ClearAfterPrint { get; set; } = false;
        public static bool HideKeyInput { get; set; } = false;
        public static bool StopOnError { get; set; } = true;
        public static bool InstantWrite { get; set; } = true;
        public static int TabLength { get; set; } = 2;

        private static bool drawMode = false;
        public static bool DrawMode
        {
            get => drawMode;
            set
            {
                drawMode = value;
                IsControlBox = !value;
                ClearAfterPrint = value;
                HideKeyInput = value;
                if (value)
                    HideCursor();
                else
                    ShowCursor();
                AllowHighlight = !value;
            }
        }

        public static bool ShowTitle { get; set; } = true;
        public static bool ShowFPS { get; set; } = false;
        public static bool AllowMinimize
        {
            get => buttons[B_MIN].Enabled;
            set => buttons[B_MIN].Enabled = value;
        }
        public static bool AllowMaximize
        {
            get => buttons[B_MAX].Enabled;
            set => buttons[B_MAX].Enabled = value;
        }
        public static bool AllowRestart
        {
            get => buttons[B_RES].Enabled;
            set => buttons[B_RES].Enabled = value;
        }
        public static bool AllowSave
        {
            get => buttons[B_SAV].Enabled;
            set => buttons[B_SAV].Enabled = value;
        }
        public static bool AllowHighlight { get; set; } = true;

        //control box stuff
        public static bool IsControlBox { get; set; } = true;
        private static ConsoleButton[] buttons;
        private static int ButtonWidth
        {
            get => FontWidth * 3;
        }
        private const int BUTTON_AMOUNT = 5;
        private const int B_MIN = 0;
        private const int B_MAX = 1;
        private const int B_SAV = 2;
        private const int B_RES = 3;
        private const int B_CLO = 4;

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
        private static int bufferLength
        {
            get => bufferSize.Width * bufferSize.Height;
        }

        private static ConsoleChar[] chars;
        private static List<ConsoleRect> rects;

        public static bool Running
        {
            get => form.Running;
        }

        private static ConsoleForm form;

        public static bool IsMaximized { get; private set; } = false;

        public static Color ForeColor { get; set; } = Color.White;
        public static Color BackColor { get; set; } = Color.Black;

        public static Font Font
        {
            get => form.Font;
            set
            {
                form.Font.Dispose();
                form.Font = value;
                FixWindowSize();
            }
        }
        public static int FontHeight
        {
            get => Font.Height;
        }
        private static double fontWidthScale = 0.75;
        public static double FontWidthScale
        {
            get => fontWidthScale;
            set
            {
                fontWidthScale = value;
                FixWindowSize();
            }
        }
        public static int FontWidth
        {
            get => (int)(FontHeight * FontWidthScale);
        }
        public static float FontSize
        {
            get => Font.Size;
            set
            {
                Font = new Font(Font.Name, value);
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

        private static Point PointerPosition = OutOfView;

        public static ConsoleCursor TextCursor { get; private set; } = new ConsoleCursor(ConsoleCursor.Shape.Line);
        private static Timer cursorTimer;

        private static bool shift = false;
        private static bool ctrl = false;
        private static bool caps = false;

        private static readonly Point OutOfView = new Point(-100, -100);
        private const int OutOfRange = -100;

        //highlighting
        private static int highlightStart = OutOfRange;
        private static int highlightEnd = OutOfRange;
        public static string HighlightedText { get; private set; } = "";
        public static bool IsHighlighting { get => HighlightedText.Any(); }
        public static Color HighlightColor { get; set; } = Color.RoyalBlue;
        //keeping these seperate from rects so they can always be drawn on top without a hassle of sorting them all
        private static List<Point> highlightZones = new List<Point>();

        //input
        private static string input = "";
        private static bool reading = false;

        public static int BorderWidth { get; set; } = 6;
        public static Color BorderColor { get; set; } = Color.DimGray;

        private static int _topMargin = 0;
        public static int TopMargin
        {
            get => _topMargin + (IsControlBox ? FontHeight + BorderWidth : 0);
            set => _topMargin = value;
        }
        public static int BottomMargin { get; set; } = 0;
        public static int LeftMargin { get; set; } = 0;
        public static int RightMargin { get; set; } = 0;

        private static int XMargin
        {
            get => LeftMargin + RightMargin;
        }
        private static int YMargin
        {
            get => TopMargin + BottomMargin;
        }

        public static string SavePath = "";

        //FPS
        public static int FPS { get; private set; } = 0;
        private static int frames = 0;
        private static Timer fpsTimer;

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
            SetWindowSize(size);

            cursorTimer = new Timer();
            cursorTimer.Interval = 1000;
            cursorTimer.Tick += CursorTimer_Tick;

            fpsTimer = new Timer();
            fpsTimer.Interval = 1000;
            fpsTimer.Tick += FpsTimer_Tick;

            TextCursor.Position = new Point(0, 0);

            //control buttons
            buttons = new ConsoleButton[BUTTON_AMOUNT];
            for (int i = 0; i < BUTTON_AMOUNT; i++)
            {
                buttons[i] = new ConsoleButton(new Rectangle(
                    form.Size.Width - (BorderWidth + RightMargin) - BUTTON_AMOUNT * ButtonWidth + i * ButtonWidth, 
                    BorderWidth, ButtonWidth, FontHeight), BorderColor, form);
                buttons[i].TextColor = ForeColor;
            }
            buttons[B_MIN].Text = "-";//minimize
            buttons[B_MIN].OnClick = ToggleMinimize;
            buttons[B_MAX].Text = "[]";//maximize
            buttons[B_MAX].OnClick = ToggleMaximize;
            buttons[B_RES].Text = "R";//restart
            buttons[B_RES].OnClick = Restart;
            buttons[B_CLO].Text = "X";//close
            buttons[B_CLO].OnClick = Close;
            buttons[B_SAV].Text = "S";//save
            buttons[B_SAV].OnClick = Save;

            //events
            form.Paint += Form_Paint;
            form.KeyDown += Form_KeyDown;
            form.KeyUp += Form_KeyUp;
            form.MouseMove += Form_MouseMove;
            form.MouseLeave += Form_MouseLeave;
            form.MouseDown += Form_MouseDown;
            form.MouseUp += Form_MouseUp;

            fpsTimer.Start();
            cursorTimer.Start();
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

        private static bool IsValid(int index)
        {
            return index >= 0 && index < chars.Length;
        }

        private static bool IsValid(Point p) => IsValid(p.X, p.Y);
        private static bool IsValid(int x, int y)
        {
            return x >= 0 && y >= 0 && x < bufferSize.Width && y < bufferSize.Height;
        }

        private static Point ScreenPositionToBufferPosition(Point screenPos, int xOffset = 0, int yOffset = 0)
        {
            return new Point((screenPos.X - BorderWidth - LeftMargin + xOffset) / FontWidth, (screenPos.Y - BorderWidth - TopMargin + yOffset) / FontHeight);
        }

        #region Data Management

        private static void Data_Shift(int startIndex, int direction, int amount = -1)
        {
            if (direction == 0) return;

            int length = amount <= 0 ? bufferLength - (startIndex + Math.Max(0, direction)) : amount;

            //shift the data over
            Array.Copy(chars, startIndex, chars, startIndex + direction, length);
            //clear the end
            if (direction > 0)//shifted to the right
                Array.Clear(chars, startIndex, direction);
            else//shifted to the left
                Array.Clear(chars, startIndex + direction + length - 1, Math.Abs(direction));
        }

        private static void Data_Insert(int index, ConsoleChar c) => Data_Insert(index, new ConsoleChar[] { c });
        private static void Data_Insert(int index, ConsoleChar[] newData)
        {
            Data_Shift(index, newData.Length);
            Array.Copy(chars, index, newData, 0, newData.Length);
        }

        private static void Data_Remove(int index, int amount = 1)
        {
            Data_Shift(index + amount, -amount);
        }

        private static void Data_Replace(int index, ConsoleChar[] newData)
        {
            Array.Copy(chars, index, newData, 0, newData.Length);
        }

        private static ConsoleChar GetChar(int x, int y)
        {
            int i = GetIndex(x, y);

            if (!IsValid(i)) return new ConsoleChar();

            return chars[i];
        }

        private static void SetChar(int x, int y, ConsoleChar c)
        {
            int i = GetIndex(x, y);

            if (!IsValid(i)) return;

            chars[i] = c;
        }

        private static int GetIndex(Point p) => GetIndex(p.X, p.Y);
        private static int GetIndex(int x, int y)
        {
            return y * bufferSize.Width + x;
        }

        private static Point GetPoint(int index)
        {
            if (index < 0) return OutOfView;

            int x = index % bufferSize.Width;
            return new Point(x, (index - x) / bufferSize.Width);
        }

        #endregion

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
            }
            else if (!ctrl && (Control.ModifierKeys & Keys.Control) != 0)
            {
                ctrl = true;
            }
            else if (e.KeyCode == Keys.CapsLock)
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

            //shortcuts
            if (ctrl)
            {
                switch (e.KeyCode)
                {
                    case Keys.A://highlight all
                        SelectAll();
                        break;
                    case Keys.C://copy
                        Copy();
                        break;
                    case Keys.V://paste
                        Paste();
                        break;
                    case Keys.X://cut
                        Cut();
                        break;
                    case Keys.S://save
                        Save();
                        break;
                    default: return;//just return if no shortcut clicked
                }

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
                    default:
                        char c = KeyToChar(e.KeyCode);
                        Write(c);
                        input += c;
                        break;
                }

                RefreshTextCursorTimer();
                Update();
            }
        }

        private static void Form_KeyUp(object sender, KeyEventArgs e)
        {
            newKeyStates[e.KeyCode.ToString()] = KeyStates.Up;

            if (shift && (Control.ModifierKeys & Keys.Shift) == 0)
            {
                shift = false;
            } else if (ctrl && (Control.ModifierKeys & Keys.Control) == 0)
            {
                ctrl = false;
            }
        }

        private static void Form_Paint(object sender, PaintEventArgs e)
        {
            Print(e.Graphics);
        }

        private static void Form_MouseUp(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case System.Windows.Forms.MouseButtons.Left:
                    if (!newKeyStates.ContainsKey("LButton"))
                        newKeyStates["LButton"] = KeyStates.Up;
                    break;
                case System.Windows.Forms.MouseButtons.Middle:
                    if (!newKeyStates.ContainsKey("MButton"))
                        newKeyStates["MButton"] = KeyStates.Up;
                    break;
                case System.Windows.Forms.MouseButtons.Right:
                    if (!newKeyStates.ContainsKey("RButton"))
                        newKeyStates["RButton"] = KeyStates.Up;
                    break;
            }

            if (AllowHighlight && e.Button == System.Windows.Forms.MouseButtons.Left)//left click: highlight time
            {
                if (IsHighlighting) Highlight(highlightStart, highlightEnd, false);

                SetCursorPositionToPointerPosition();

                highlightStart = GetIndex(TextCursor.Position);

                Update();
            }
        }

        private static void Form_MouseDown(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {
                case System.Windows.Forms.MouseButtons.Left:
                    if(!newKeyStates.ContainsKey("LButton"))
                        newKeyStates["LButton"] = KeyStates.Down; 
                    break;
                case System.Windows.Forms.MouseButtons.Middle:
                    if (!newKeyStates.ContainsKey("MButton"))
                        newKeyStates["MButton"] = KeyStates.Down; 
                    break;
                case System.Windows.Forms.MouseButtons.Right:
                    if (!newKeyStates.ContainsKey("RButton"))
                        newKeyStates["RButton"] = KeyStates.Down; 
                    break;
            }

            if(AllowHighlight && e.Button == System.Windows.Forms.MouseButtons.Left)//left click: highlight time
            {
                if(IsHighlighting) Highlight(highlightStart, highlightEnd, false);

                SetCursorPositionToPointerPosition();

                highlightStart = GetIndex(TextCursor.Position);

                Update();
            }
        }

        private static void CursorTimer_Tick(object sender, EventArgs e)
        {
            TextCursor.Visible = !TextCursor.Visible;

            Update();
        }

        private static void FpsTimer_Tick(object sender, EventArgs e)
        {
            FPS = frames;
            frames = 0;
        }

        private static void Form_MouseLeave(object sender, EventArgs e)
        {
            MovePointerOutOfView();
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
                PointerPosition = ScreenPositionToBufferPosition(e.Location, FontWidth / 2);

                //if highlighting
                if(AllowHighlight && e.Button == System.Windows.Forms.MouseButtons.Left && PointerPosition != OutOfView && highlightStart != OutOfRange)
                {
                    SetCursorPositionToPointerPosition();

                    if (GetIndex(PointerPosition) != highlightStart)
                    {
                        Highlight(highlightStart, GetIndex(TextCursor.Position));
                    } else
                    {
                        Highlight(highlightStart, GetIndex(TextCursor.Position), false);
                    }

                    Update();
                }
            }
        }

        #endregion

        #region Mouse and Pointer

        private static void MovePointerOutOfView()
        {
            PointerPosition = OutOfView;
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

        private static void SetCursorPositionToPointerPosition()
        {
            //move text cursor when left clicked
            TextCursor.Position = new Point(FindFirstFilledFromRight(PointerPosition.Y, PointerPosition.X), PointerPosition.Y);
            RefreshTextCursorTimer();
            TextCursor.Visible = true;
        }

        private static void Highlight(int hStart, int hEnd, bool highlight = true)
        {
            HighlightedText = "";
            highlightZones.Clear();

            if (!highlight) return;

            int start = hStart;
            int end = hEnd;

            //swap the start and end if they need to be switched so the algorithm here works
            if (start > end)
            {
                start = hEnd;
                end = hStart;
            }

            if (start < 0 || end < 0) return;

            bool wasFilled = false;
            bool h = false;
            int firstFilledIndex = 0;

            //go through each row/spot and check if it is filled, then highlight it if it is
            for (int i = start; i < end; i++)
            {
                ConsoleChar c = chars[i];

                if (c.IsFilled)
                {
                    if (!wasFilled)//this one is filled, last one was not
                    {
                        firstFilledIndex = i;
                    }

                    wasFilled = true;

                    HighlightedText += c.Char;
                } else
                {
                    if (wasFilled)//this one isn't filled, last one was
                    {
                        h = true;
                    }

                    wasFilled = false;
                }

                if(h || i == end - 1)
                {
                    highlightZones.Add(new Point(firstFilledIndex, i + 1));

                    h = false;
                }
            }
        }

        #endregion

        #region Display

        public static void Close()
        {
            Environment.Exit(0);
        }

        public static void Restart()
        {
            Application.Restart();
            Environment.Exit(0);
        }

        /// <summary>
        /// Clears the data, not the screen.
        /// </summary>
        public static void Clear()
        {
            //clear the text
            chars = new ConsoleChar[bufferSize.Width * bufferSize.Height];

            //clear the rects
            rects.Clear();
            highlightZones.Clear();
        }

        private static ConsoleChar[] NewConsoleCharLine
        {
            get => new ConsoleChar[bufferSize.Width];
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
            chars = new ConsoleChar[bufferLength];

            Clear();
        }

        //resizes the buffer if the window size was changed outside of this code
        private static void FixBufferSize()
        {
            int bWidth = (form.Width - BorderWidth * 2 - XMargin) / FontWidth;
            int bHeight = (form.Height - BorderWidth * 2 - YMargin) / FontHeight;

            ResizeBuffer(bWidth, bHeight);
        }

        public static void SetWindowSize(Size buffSize)
        {
            form.Size = new Size(buffSize.Width * FontWidth + BorderWidth * 2 + XMargin, buffSize.Height * FontHeight + BorderWidth * 2 + YMargin);
        }

        private static void FixWindowSize()
        {
            SetWindowSize(bufferSize);
            UpdateButtonPositions();
        }

        public static void SetDefaultColors(Color fore, Color back)
        {
            ForeColor = fore;
            BackColor = back;
        }

        public static void ResizeBuffer(int width, int height) => ResizeBuffer(new Size(width, height));
        public static void ResizeBuffer(Size size)
        {
            //this function was a BITCH to make.. twice.. but it works

            if (size == bufferSize) return;//not changing size

            if (DrawMode)
            {
                //don't adjust positions using new lines if drawing
                DM_ResizeBuffer(size);
                return;
            }

            CheckAndAddNewLines();

            bufferSize = size;
            Array.Resize(ref chars, bufferLength);

            //find the end of lines (\n) and then fill with spaces until the end of the actual line
            for(int i = 1; i < bufferLength; i++)
            {
                i = FindFirstNewLine(i - 1);
                Point p = GetPoint(i);
                int nextLineIndex = FindFirstFilledFromLeft(GetIndex(p.X + 1, p.Y));

                Point nextLinePoint = GetPoint(nextLineIndex);

                //check if at the end
                if(nextLineIndex == -1 || i == -1)
                {
                    break;
                }

                int countAdjustment = 0;

                //find if you need to remove or add spaces to the end
                if(nextLinePoint.Y != p.Y)//need to remove
                {
                    Log("Removing");

                    countAdjustment = nextLineIndex - i - (bufferSize.Width - (i % bufferSize.Width));

                    Data_Remove(nextLineIndex - countAdjustment, countAdjustment);

                    countAdjustment *= -1;
                } else//need to add
                {
                    Log("Adding");

                    countAdjustment = bufferSize.Width - (nextLineIndex % bufferSize.Width);

                    Data_Insert(i + 1, new ConsoleChar[countAdjustment]);
                }

                i = nextLineIndex + countAdjustment;
            }
        }

        private static void DM_ResizeBuffer(Size size)
        {
            //only resize at first if the new size's area is bigger
            if(size.Width * size.Height > bufferLength)
                Array.Resize(ref chars, size.Width * size.Height);

            //fix the width
            if (size.Width > bufferSize.Width)//expanding
            {
                for (int y = bufferSize.Height - 1; y >= 0; y--)
                {
                    //add to the end of each line
                    Data_Insert(GetIndex(bufferSize.Width - 1, y), new ConsoleChar[size.Width - bufferSize.Width]);
                }
            } else if (size.Width < bufferSize.Width)//shrinking
            {
                for(int y = 0; y < size.Height; y++)
                {
                    //remove off of the end of each line
                    Data_Remove(size.Width, bufferSize.Width - size.Width);
                }
            }

            //width fixed

            //fix the height
            bufferSize = size;
            Array.Resize(ref chars, bufferLength);
        }

        //adds new lines at the beginning of lines if there isn't any there to begin with
        private static void CheckAndAddNewLines()
        {
            for (int i = 0; i < bufferLength; i += bufferSize.Width)
            {
                if (!chars[i].IsFilled)
                {
                    chars[i] = new ConsoleChar(NEW_LINE, ForeColor);
                }
            }
        }

        public static void ToggleMinimize()
        {
            if(form.WindowState == FormWindowState.Minimized)
            {
                form.WindowState = FormWindowState.Normal;
            } else
            {
                form.WindowState = FormWindowState.Minimized;
            }
        }

        //toggles fullscreen
        public static void ToggleMaximize()
        {
            if (form.WindowState == FormWindowState.Maximized)
            {
                form.WindowState = FormWindowState.Normal;
                IsMaximized = false;
            }
            else
            {
                form.WindowState = FormWindowState.Maximized;
                IsMaximized = true;
            }

            FixBufferSize();
            UpdateButtonPositions();
        }

        private static void UpdateButtonPositions()
        {
            for (int i = 0; i < BUTTON_AMOUNT; i++)
            {
                buttons[i].Bounds = new Rectangle(
                    form.Size.Width - (BorderWidth + RightMargin) - BUTTON_AMOUNT * ButtonWidth + i * ButtonWidth,
                    BorderWidth, ButtonWidth, FontHeight);
            }
        }

        public static void SetMargins(int m) => SetMargins(m, m, m, m);
        public static void SetMargins(int left, int top, int right, int bottom)
        {
            LeftMargin = left;
            TopMargin = top;
            RightMargin = right;
            BottomMargin = bottom;
        }

        #endregion

        #region Writing, Reading and Waiting

        public static void Write(object o)
        {
            string str = o.ToString();

            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];

                if (c == TAB)
                {
                    Tab();
                } else if (c >= 32 || c == NEW_LINE)//32 is space, 10 is NEW_LINE
                {
                    TypeAndMove(new ConsoleChar(c, ForeColor));
                }
            }

            if (InstantWrite) Update();
            //don't update when you write, only when you read
            //then if you have many Write() statements it will be instant
            //if there is no read after the write the Console will just close anyways
        }

        public static void WriteLine() => WriteLine("");
        public static void WriteLine(object o)
        {
            Write(o.ToString() + '\n');
        }

        //write overlaps, insert does not
        public static void Insert(object o, int left, int top)
        {
            string s = o.ToString();
            int i = GetIndex(left, top);

            Data_Insert(i, ConsoleChar.FromString(s, ForeColor));
        }

        public static string ReadLine()
        {
            Update();

            input = "";
            reading = true;

            while (!input.EndsWith("\n"))
                Application.DoEvents();

            reading = false;

            return input.Remove(input.Length - 1);
        }

        public static string GetRange(Point start, Point stop)
        {
            Point topLeft = new Point(Math.Max(Math.Min(start.X, stop.X), 0), Math.Max(Math.Min(start.Y, stop.Y), 0));
            Point bottomRight = new Point(Math.Min(Math.Max(start.X, stop.X), bufferSize.Width - 1), Math.Min(Math.Max(start.Y, stop.Y), bufferSize.Height - 1));

            string output = "";

            for (int y = topLeft.Y; y < bottomRight.Y; y++)
            {
                for(int x = topLeft.X; x < bottomRight.X; x++)
                {
                    ConsoleChar c = chars[GetIndex(x, y)];
                    if (c.IsFilled)
                    {
                        if (c.Char == NEW_LINE)
                        {
                            output += Environment.NewLine;
                        }
                        else
                        {
                            output += c.Char;
                        }
                    }
                }
            }

            return output;
        }

        public static string GetLine(Point start, int length)
        {
            return GetRange(start, new Point(start.X + length, start.Y));
        }

        public static int ReadInt()
        {
            string str;
            int num;

            do
            {
                str = ReadLine();
            } while (!int.TryParse(str, out num));

            return num;
        }

        public static double ReadDouble()
        {
            string str;
            double num;

            do
            {
                str = ReadLine();
            } while (!double.TryParse(str, out num));

            return num;
        }

        public static char Read()
        {
            Update();

            input = "";
            reading = true;

            while (string.IsNullOrEmpty(input))
                Application.DoEvents();

            reading = false;

            return input[0];
        }

        //waits until user presses enter to continue
        public static void Wait()
        {
            GoToEmptyLine();
            WriteLine("Press Enter to continue.");
            ReadLine();
        }

        #endregion

        #region Shortcuts

        private static void SelectAll()
        {
            Highlight(0, chars.Length);

            Update();
        }

        private static void Cut()
        {
            if (IsHighlighting)
            {
                SetClipboardText(HighlightedText);
                DeleteHighlightedZone();
                Highlight(highlightStart, highlightEnd, false);
            }
            else
            {
                SetClipboardText(GetTextFromRow(TextCursor.Position.Y));
                DeleteLine(TextCursor.Position.Y);
            }

            Update();
        }

        private static void Copy()
        {
            if (IsHighlighting)
            {
                SetClipboardText(HighlightedText);
                Highlight(highlightStart, highlightEnd, false);
            }
            else
            {
                SetClipboardText(GetTextFromRow(TextCursor.Position.Y));
            }

            Update();
        }

        private static void SetClipboardText(string text)
        {
            System.Threading.Thread STAThread = new System.Threading.Thread(delegate ()
           {
               Clipboard.SetText(text.Replace(NEW_LINE.ToString(), Environment.NewLine));
           });
            STAThread.SetApartmentState(System.Threading.ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();
        }

        private static string GetClipboardText()
        {
            string output = "";

            System.Threading.Thread STAThread = new System.Threading.Thread(delegate ()
            {
                output = Clipboard.GetText();
            });
            STAThread.SetApartmentState(System.Threading.ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();

            return output.Replace(Environment.NewLine, NEW_LINE.ToString());
        }

        private static void Paste()
        {
            string text = GetClipboardText();

            Insert(text, TextCursor.Position.X, TextCursor.Position.Y);

            TextCursor.Index += text.Length;

            if(IsHighlighting) Highlight(highlightStart, highlightEnd, false);

            Update();
        }

        //only saves what is currently on the screen, as a text file
        public static void Save()
        {
            if (string.IsNullOrEmpty(SavePath))
            {
                SaveAs();
            } else
            {
                SaveText(SavePath, ToString());
            }
        }

        public static void SaveAs()
        {
            SaveTextAs(ToString());
        }

        private static void SaveTextAs(string contents)
        {
            System.Threading.Thread STAThread = new System.Threading.Thread(delegate ()
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Text Files (.txt)|*.txt|All Files|*";
                sfd.RestoreDirectory = true;
                sfd.Title = "Save Console Text";

                //they did not cancel
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    SaveText(sfd.FileName, contents);
                }
            });
            STAThread.SetApartmentState(System.Threading.ApartmentState.STA);
            STAThread.Start();
            STAThread.Join();
        }

        private static void SaveText(string path, string contents)
        {
            System.IO.File.WriteAllText(path, contents);
            SavePath = path;
        }

        #endregion

        #region Typing

        public static void GoToEmptyLine()
        {
            TextCursor.Position = new Point(0, FindFirstEmptyLine(TextCursor.Position.Y));
        }

        //moves to the next line
        private static void NextLine()
        {
            ShiftLines(TextCursor.Position.Y, -1);

            TextCursor.Position = new Point(0, TextCursor.Position.Y + 1);

            RefreshTextCursorTimer();
        }

        private static void ShiftLines(int top, int amount)
        {
            if (amount == 0) return;

            if(amount < 0)//shifting down
            {
                for (int i = 0; i < amount; i++)
                {
                    //bottom row gets 'deleted'
                    Data_Remove(chars.Length - bufferSize.Width, bufferSize.Width);
                    Data_Insert(GetIndex(0, top), NewConsoleCharLine);
                }
            } else//shifting up
            {
                for (int i = 0; i < amount; i++)
                {
                    //top row gets 'deleted'
                    Data_Remove(0, bufferSize.Width);
                    Data_Insert(GetIndex(0, top), NewConsoleCharLine);
                }

                //hasn't been tested
                throw new NotImplementedException();
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

            if (x >= bufferSize.Width - 1 || c.Char == NEW_LINE)
            {
                //at the end, gonna have to make a new line
                NextLine();
            } else
            {
                TextCursor.Position = new Point(x + 1, y);
            }

            RefreshTextCursorTimer();
        }

        private static void BackSpace()
        {
            if (IsHighlighting)
            {
                DeleteHighlightedZone();
                return;
            }

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
        }

        private static void Delete()
        {
            //TODO: delete any char that is highlighted, or the one char at the pointer
            if (IsHighlighting)
            {
                DeleteHighlightedZone();
                return;
            }

            //delete the one that the cursor is on (appears to the right, or on it depending on shape)
            Data_Remove(TextCursor.Index);

            //find the next new line, and then the first filled after that
            //use the character right before to add
            int i = FindFirstFilledFromLeft(FindFirstNewLine(TextCursor.Index) + 1) - 1;

            Data_Insert(i, new ConsoleChar());
        }

        private static void DeleteHighlightedZone()
        {
            int start = highlightStart;
            int end = highlightEnd;

            if(highlightStart > highlightEnd)
            {
                start = highlightEnd;
                end = highlightStart;
            }

            Data_Remove(start, end - start);
        }

        //used for cursor
        private static int FindFirstFilledFromRight(int top, int left = -1)
        {
            if (!IsValid(0, top)) return 0;

            for (int i = bufferSize.Width - 1; i >= 0; i--)
            {
                if (GetChar(i, top).IsFilled)
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

        private static int FindFirstFilledFromLeft(int index)
        {
            if (index < 0) return -1;

            for (int i = index; i < chars.Length; i++)
            {
                if (chars[i].IsFilled) return i;
            }

            return -1;//no new lines
        }

        private static int FindFirstEmptyLine(int startLine = 0)
        {
            if (!IsValid(0, startLine)) return -1;

            for (int y = startLine; y < bufferSize.Height; y++)
            {
                if (!GetChar(0, y).IsFilled) return y;
            }

            return -1;//no empty lines
        }

        private static int FindFirstNewLine(int startIndex)
        {
            if (!IsValid(startIndex)) return -1;

            for (int i = startIndex; i < chars.Length; i++)
            {
                if (chars[i].Char == NEW_LINE) return i;
            }

            return -1;//no new lines
        }

        private static void ClearAt(Point p) => ClearAt(p.X, p.Y);
        private static void ClearAt(int left, int top)
        {
            SetChar(left, top, new ConsoleChar());
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

        private static string GetTextFromRow(int top)
        {
            string output = "";

            for (int i = bufferSize.Width * top; i < bufferSize.Width * bufferSize.Height - 1 && chars[i] != NEW_LINE; i++)
            {
                if (chars[i].IsFilled) output += chars[i].Char;
            }

            return output;
        }

        private static void DeleteLine(int top)
        {
            int i = top * bufferSize.Width;
            Data_Remove(i, Math.Max(FindFirstNewLine(i), bufferSize.Width) - i);
        }

        #endregion

        #region Drawing

        private static void DM_Warning()
        {
            LogWarning("Draw mode is not enabled. Please set DrawMode to True to use the Draw method.");
        }

        private static void Copy(ConsoleChar[] sourceArray, int sourceIndex, int destinationIndex, int length)
        {
            Array.Copy(sourceArray, sourceIndex, chars, destinationIndex, length);
        }

        //ACTUAL DRAWING FUNCTION
        private static void Draw(char c, Color color, int left, int top)
        {
            SetChar(left, top, new ConsoleChar(c, color));
        }

        public static void Draw(string str, int left, int top, bool transparent = false)
        {
            Draw(str, ForeColor, left, top);
        }

        //draws a string to the screen, in its color
        //ACTUAL DRAWING FUNCTION
        public static void Draw(string str, Color fgColor, int left, int top, bool transparent = false)
        {
            if (!DrawMode)
            {
                DM_Warning();
                return;
            }

            int length = str.Length;

            //don't even bother: completely outside of the console
            if (left + length < 0 || left >= bufferSize.Width ||
                top < 0 || top >= bufferSize.Height)
            {
                return;
            }

            ConsoleChar[] arr = ConsoleChar.FromString(str, fgColor);

            if (transparent)
            {
                Draw_Transparent(arr, left, top);
                return;
            }

            int startIndex = Math.Max(0, -left);
            int destinationStart = Math.Max(0, left - startIndex);

            Copy(arr, startIndex, GetIndex(destinationStart, top), Math.Min(length - startIndex, bufferSize.Width - destinationStart));
        }

        //slower, but allows for text "transparency"
        //ACTUAL DRAWING FUNCTION
        private static void Draw_Transparent(ConsoleChar[] arr, int left, int top)
        {
            int startX = Math.Max(0, -left);
            int destinationStart = Math.Max(0, left) - startX;
            int length = Math.Min(arr.Length, bufferSize.Width - destinationStart);

            for (int x = startX; x < length; x++)
            {
                if (arr[x].IsVisible)
                {
                    chars[GetIndex(destinationStart + x, top)] = arr[x];
                }
            }
        }

        public static void Draw(string str, Color fgColor, Color bgColor, int left, int top, bool transparent = false)
        {
            Draw(str, fgColor, left, top, transparent);

            if (DrawMode)
                rects.Add(new ConsoleRect(left, top, str.Length, 1, bgColor));
        }

        //uses default colors
        public static void Draw(string[] strs, int left, int top, bool transparent = false)
        {
            Draw(strs, ForeColor, left, top, transparent);
        }

        //uses default bg color
        public static void Draw(string[] strs, Color fgColor, int left, int top, bool transparent = false)
        {
            for (int i = 0; i < strs.Length; i++)
            {
                Draw(strs[i], fgColor, left, top + i, transparent);
            }
        }

        public static void Draw(string[] strs, int width, Color fgColor, Color bgColor, int left, int top, bool transparent = false)
        {
            Draw(strs, fgColor, left, top, transparent);

            if (DrawMode)
                rects.Add(new ConsoleRect(left, top, width, strs.Length, bgColor));
        }

        public static void Draw(char[][] chars, int left, int top, bool transparent = false)
        {
            Draw(chars, ForeColor, left, top, transparent);
        }

        public static void Draw(char[][] chars, Color fgColor, int left, int top, bool transparent = false)
        {
            for (int i = 0; i < chars.Length; i++)
            {
                Draw(new string(chars[i]), fgColor, left, top + i, transparent);
            }
        }

        public static void Draw(char[][] chars, int width, Color fgColor, Color bgColor, int left, int top, bool transparent = false)
        {
            Draw(chars, fgColor, left, top, transparent);

            if(DrawMode)
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

            frames++;
        }

        private static void Print(Graphics g)
        {
            //FPS
            if (ShowFPS)
            {
                string fps = FPS.ToString();
                Draw(fps, bufferSize.Width - fps.Length, bufferSize.Height - 1);
            }

            int height = FontHeight;
            int width = FontWidth;

            //draw the rectangles
            for (int i = 0; i < rects.Count; i++)
            {
                using (Brush brush = new SolidBrush(rects[i].Color))
                {
                    g.FillRectangle(brush, rects[i] * new Size(width, height) + new Point(BorderWidth + LeftMargin, BorderWidth + TopMargin));
                }
            }

            //draw the highlighted rectangles
            if (IsHighlighting)
            {
                using (Brush brush = new SolidBrush(HighlightColor))
                {
                    foreach (Point p in highlightZones)
                    {
                        Point p1 = GetPoint(p.X);
                        Point p2 = GetPoint(p.Y);

                        g.FillRectangle(brush,
                            p1.X * width + BorderWidth + LeftMargin,
                            p1.Y * height + BorderWidth + TopMargin,
                            (p2.X - p1.X) * width, height);
                    }
                }
            }

            //draw the text over the rectangles
            for (int y = 0; y < bufferSize.Height; y++)
            {
                for (int x = 0; x < bufferSize.Width; x++)
                {
                    ConsoleChar c = GetChar(x, y);

                    if (!c.IsVisible) continue;//don't even bother if you can't see it

                    using (Brush brush = new SolidBrush(c.Color))
                    {
                        g.DrawString(c.ToString(), Font, brush, x * width + BorderWidth + LeftMargin, y * height + BorderWidth + TopMargin);
                    }
                }
            }

            //draw the cursor
            if (TextCursor.Visible)
            {
                TextCursor.DrawShape(g, FontWidth, BorderWidth + LeftMargin, FontHeight, BorderWidth + TopMargin);
            }

            //draw the border, if it exists
            if (BorderWidth > 0)
            {
                using (Pen p = new Pen(BorderColor, BorderWidth))
                {
                    g.DrawRectangle(p, BorderWidth / 2, BorderWidth / 2, form.Size.Width - BorderWidth, form.Size.Height - BorderWidth);

                    if (IsControlBox)
                    {
                        //extra border when control box, to outline the top
                        int y = TopMargin + BorderWidth / 2;
                        g.DrawLine(p, 0, y, form.Size.Width, y);
                    }
                }
            }

            //draw the control stuff
            if (IsControlBox)
            {
                if (ShowTitle)
                {
                    using (Brush brush = new SolidBrush(ForeColor))
                    {
                        g.DrawString(Title, form.Font, brush, new Point(BorderWidth + LeftMargin, BorderWidth));
                    }
                }

                for (int i = 0; i < buttons.Length; i++)
                {
                    buttons[i].Draw(g);
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

        //public static bool IsKeyDown(Keys key) => IsKeyDown(key.ToString());
        public static bool IsKeyDown(string keyName)
        {
            return keyStates.ContainsKey(keyName) && keyStates[keyName] == KeyStates.Down;
        }

        //public static bool IsKeyPressed(Keys key) => IsKeyPressed(key.ToString());
        public static bool IsKeyPressed(string keyName)
        {
            return keyStates.ContainsKey(keyName) && (
                keyStates[keyName] == KeyStates.Pressed ||
                keyStates[keyName] == KeyStates.Down);
        }

        //public static bool IsKeyUp(Keys key) => IsKeyUp(key.ToString());
        public static bool IsKeyUp(string keyName)
        {
            return keyStates.ContainsKey(keyName) && keyStates[keyName] == KeyStates.Up;
        }

        public enum MouseButtons
        {
            LButton,
            MButton,
            RButton
        }

        public static bool IsMouseDown(MouseButtons button)
        {
            return keyStates.ContainsKey(button.ToString()) && keyStates[button.ToString()] == KeyStates.Down;
        }

        public static bool IsMousePressed(MouseButtons button)
        {
            return keyStates.ContainsKey(button.ToString()) && keyStates[button.ToString()] == KeyStates.Pressed;
        }

        public static bool IsMouseUp(MouseButtons button)
        {
            return keyStates.ContainsKey(button.ToString()) && keyStates[button.ToString()] == KeyStates.Up;
        }

        //borrowed from Stack Overflow
        private static char KeyToChar(Keys key)
        {

            if (IsKeyPressed(Keys.Alt.ToString()) ||
                IsKeyPressed(Keys.Control.ToString()))
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

        #region Strings

        new public static string ToString()
        {
            return GetRange(new Point(0, 0), new Point(bufferSize));
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