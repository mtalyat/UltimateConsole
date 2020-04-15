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
    partial class ConsoleForm : Form
    {
        public bool Running { get; set; } = true;

        public ConsoleForm()
        {
            InitializeComponent();
            InitializeConsole();
        }

        private void InitializeConsole()
        {
            //replace the font with a new one
            Font.Dispose();
            Font = new Font("Lucida Console", 16);
        }

        private void ConsoleForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Running = false;
        }
    }
}
