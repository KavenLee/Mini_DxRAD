using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Login
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            textBox1.Text = "Admin";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox2.Text.Equals("0000"))
            {
                
            }
            else
            {
                return;
            }
        }
    }
}
