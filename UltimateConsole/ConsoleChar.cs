using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace UltimateConsole
{
    struct ConsoleChar
    {
        public char Char { get; set; }
        public Color Color { get; set; }

        public ConsoleChar(char c, Color color)
        {
            Char = c;
            Color = color;
        }

        public static implicit operator char(ConsoleChar c) => c.Char;

        public static ConsoleChar[] FromString(string str, Color color)
        {
            ConsoleChar[] output = new ConsoleChar[str.Length];

            for (int i = 0; i < str.Length; i++)
            {
                output[i] = new ConsoleChar(str[i], color);
            }

            return output;
        }

        public static string ToStringFromArray(ConsoleChar[] array)
        {
            string output = "";

            for (int i = 0; i < array.Length; i++)
            {
                output += array[i];
            }

            return output;
        }

        public override string ToString()
        {
            return Char.ToString();
        }
    }
}
