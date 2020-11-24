using System;
using System.Windows.Forms;

namespace mini2_sono
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.textBox2.KeyDown += new KeyEventHandler(Enter_KeyDown);
            textBox1.Text = "Admin";
            textBox2.Focus();
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

        private void Enter_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.button1_Click(sender, e);
            }
        }

    }
}
