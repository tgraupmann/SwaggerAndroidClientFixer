using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace SwaggerAndroidClientFixer
{
    public partial class Form1 : Form
    {
        private const string KEY_APP = "SWAGGER_IO_FIXER";
        private const string KEY_APP_DEFAULT_FOLDER = "SWAGGER_IO_FIXER_DEFAULT_FOLDER";

        private bool _mShouldExit = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            _mShouldExit = true;
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

        private void CacheFolder()
        {
            Microsoft.Win32.RegistryKey key;
            key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(KEY_APP);
            key.SetValue(KEY_APP_DEFAULT_FOLDER, txtFolder.Text);
            key.Close();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = txtFolder.Text;
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                txtFolder.Text = folderBrowserDialog1.SelectedPath;
                CacheFolder();
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

        private void FindAllChildren(DirectoryInfo folder, string extension, Action<FileInfo> action)
        {
            if (folder.Exists)
            {
                foreach (FileInfo fileInfo in folder.GetFiles("*.java"))
                {
                    action(fileInfo);
                }
                foreach (DirectoryInfo child in folder.GetDirectories())
                {
                    if (child.Name == ".")
                    {
                        continue;
                    }
                    FindAllChildren(child, extension, action);
                }
            }
        }

        private void SetStatus(string text)
        {
            Action action = () =>
                {
                    lblStatus.Text = text;
                };
            this.Invoke(action);
        }

        string GetNamedValue(int val)
        {
            string result = HumanFriendlyInteger.IntegerToWritten(val);
            return result.Replace(" ", "_");
        }

        private void ProcessFile(FileInfo fileInfo)
        {
            if (_mShouldExit)
            {
                return;
            }

            if (!fileInfo.Exists)
            {
                SetStatus(string.Format("Status: File does not exist: {0}", fileInfo.Name));
                return;
            }

            SetStatus(string.Format("Status: Reading: {0}", fileInfo.Name));
            using (StreamReader reader = new StreamReader(fileInfo.FullName))
            {
                String previousLine = null;
                String line = null;
                do
                {
                    previousLine = line;
                    line = reader.ReadLine();
                    string replaceLine = null;

                    // ref: https://github.com/swagger-api/swagger-codegen/issues/4278
                    const string TOKEN_PUBLIC_ENUM = "public enum ";
                    const string TOKEN_CURLY_BRACE = " {";
                    if (null != previousLine &&
                        previousLine.Contains(TOKEN_PUBLIC_ENUM) &&
                        null != line &&
                        line.Contains(","))
                    {
                        int startOfClass = previousLine.IndexOf(TOKEN_PUBLIC_ENUM) + TOKEN_PUBLIC_ENUM.Length;
                        string subString1 = previousLine.Substring(startOfClass);
                        if (null != subString1 &&
                            subString1.Contains(TOKEN_CURLY_BRACE))
                        {
                            int curlyIndex = subString1.IndexOf(TOKEN_CURLY_BRACE);
                            string className = subString1.Substring(0, curlyIndex);
                            if (!string.IsNullOrEmpty(className))
                            {
                                try
                                {
                                    replaceLine = string.Empty;
                                    string[] numbers = line.Split(new[] { ',' });
                                    int i = 0;
                                    foreach (string number in numbers)
                                    {
                                        if (!string.IsNullOrEmpty(number) &&
                                            !string.IsNullOrEmpty(number.Trim()))
                                        {
                                            int val;
                                            if (!int.TryParse(number, out val))
                                            {
                                                throw new FormatException();
                                            }
                                            string namedVal = GetNamedValue(val);
                                            if (i == 0)
                                            {
                                                replaceLine = "\t\t";
                                            }
                                            else
                                            {
                                                replaceLine += ", ";
                                            }
                                            ++i;
                                            replaceLine += string.Format("{0}({1})", namedVal, val);
                                        }
                                    }
                                    if (!string.IsNullOrEmpty(replaceLine))
                                    {
                                        replaceLine += ";\r\n";
                                        replaceLine += "\t\tprivate int value;\r\n";
                                        replaceLine +=
                                            string.Format("\t\tprivate {0}(int value) {{\r\n",
                                            className);
                                        replaceLine += "\t\t\tthis.value = value;\r\n";
                                        replaceLine += "\t\t}\r\n";
                                        replaceLine += "\t\tpublic int getValue() {\r\n";
                                        replaceLine += "\t\t\treturn value;\r\n";
                                        replaceLine += "\t\t}\r\n";
                                        string debug = string.Format("Status: Processing: {0}\r\n" ,fileInfo.Name);
                                        debug += replaceLine.Replace("\t", "    ");
                                        SetStatus(debug);
                                        Thread.Sleep(1000);
                                    }
                                }
                                catch (FormatException e)
                                {
                                    //skip
                                    replaceLine = null;
                                }
                            }
                        }
                    }
                }
                while (line != null);
            }

            SetStatus(string.Format("Status: Processed: {0}", fileInfo.Name));
        }

        private void btnProcess_Click(object sender, EventArgs e)
        {
            if (!CheckIsReady())
            {
                return;
            }

            CacheFolder();

            ThreadStart threadStart = new ThreadStart(() =>
            {
                DirectoryInfo root = new DirectoryInfo(txtFolder.Text);
                if (root.Exists)
                {
                    foreach (DirectoryInfo child in root.GetDirectories())
                    {
                        if (child.Name == "src")
                        {
                            FindAllChildren(child, "java", ProcessFile);
                        }
                    }
                }
                SetStatus("Status: DONE");
            });
            Thread thread = new Thread(threadStart);
            thread.Start();
        }

        private void txtFolder_TextChanged(object sender, EventArgs e)
        {
            CheckIsReady();
        }
    }
}
