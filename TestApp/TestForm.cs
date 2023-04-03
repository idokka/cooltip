using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace TestApp
{
    public partial class TestForm : Form
    {
        public TestForm()
        {
            InitializeComponent();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (coolTip.Validate(textBox1, textBox1.Text.Length > 0, "Text for help tip cannot be empty.\rPlease type some text."))
                coolTip.SetHelpText(textBox1, textBox1.Text);
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            var button = sender as RadioButton;
            if (button.Checked)
            {
                if (button == radioButton1)
                    listView1.View = View.LargeIcon;
                else if (button == radioButton2)
                    listView1.View = View.Details;
                else if (button == radioButton3)
                    listView1.View = View.List;
            }
        }
    }
}
