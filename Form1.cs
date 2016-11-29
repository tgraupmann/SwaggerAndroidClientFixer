using System;
using System.IO;
using System.Windows.Forms;

namespace SwaggerAndroidClientFixer
{
    public partial class Form1 : Form
    {
        private const string KEY_APP = "SWAGGER_IO_FIXER";
        private const string KEY_APP_DEFAULT_FOLDER = "SWAGGER_IO_FIXER_DEFAULT_FOLDER";

        public Form1()
        {
            InitializeComponent();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private bool CheckIsReady()
        {
            DirectoryInfo dirInfo = new DirectoryInfo(txtFolder.Text);
            if (dirInfo.Exists)
            {
                lblStatus.Text = "Status: READY TO PROCESS";
                return true;
            }
            else
            {
                lblStatus.Text = "Status: FOLDER DOES NOT EXIST!";
                return false;
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = txtFolder.Text;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                txtFolder.Text = folderBrowserDialog1.SelectedPath;
                Microsoft.Win32.RegistryKey key;
                key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(KEY_APP);
                key.SetValue(KEY_APP_DEFAULT_FOLDER, folderBrowserDialog1.SelectedPath);
                key.Close();
            }

            CheckIsReady();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (string name in Microsoft.Win32.Registry.CurrentUser.GetSubKeyNames())
            {
                if (name == KEY_APP)
                {
                    Microsoft.Win32.RegistryKey key =
                        Microsoft.Win32.Registry.CurrentUser.OpenSubKey(KEY_APP);
                    if (null != key)
                    {
                        txtFolder.Text = (string)key.GetValue(KEY_APP_DEFAULT_FOLDER);
                        CheckIsReady();
                        return;
                    }
                }
            }
            txtFolder.Text = Directory.GetCurrentDirectory();
            CheckIsReady();
        }

        private void btnProcess_Click(object sender, EventArgs e)
        {
            if (!CheckIsReady())
            {
                return;
            }
            lblStatus.Text = "Status: DONE";
        }

        private void txtFolder_TextChanged(object sender, EventArgs e)
        {
            CheckIsReady();
        }
    }
}
