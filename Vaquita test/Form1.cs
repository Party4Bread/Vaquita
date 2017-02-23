using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Orca;
using Orca.vm;

namespace Vaquita_test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var parser = new Parser();
            var program = parser.compile(textBox1.Text);
            parser.d
            var machine = new Machine();
            machine.load(program);
            machine.run();
            textBox3.Text = machine.io.console;
            GC.Collect();
        }
    }
}
