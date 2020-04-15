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
        public bool Running { get; set; } = false;

        public ConsoleForm()
        {
            InitializeComponent();
            InitializeConsole();
        }

        private void InitializeConsole()
        {
            //replace the font with a new one
            Font.Dispose();
            Font = new Font("Lucida Console", 12);

            //Cursor.Hide();
        }

        private void ConsoleForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Running = false;
        }

        private void ConsoleForm_Load(object sender, EventArgs e)
        {
            Running = true;
        }
    }
}
