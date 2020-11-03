﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mini
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.textBox2.KeyDown += new KeyEventHandler(Enter_KeyDown);
            textBox1.Text = "Admin";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBox2.Text.Equals("0000"))
            {
                Form2 f2 = new Form2();
                f2.ShowDialog();
                this.Close();
            }
            else
            {
                textBox2.Text = "";
                return;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox2.Text = "";
            return;
        }

        private void Enter_KeyDown(object sender,KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.button1_Click(sender, e);
            }
        }
    }
}
