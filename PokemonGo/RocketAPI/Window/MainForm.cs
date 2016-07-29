using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PokemonGo.RocketAPI.Window
{
    public partial class MainForm : Form
    {
        private Botting _botting;
        private CancellationTokenSource _comboBoxTokenSource;

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            _botting = new Botting(this);

            toolStripComboBox2.Items.Add("All");
            toolStripComboBox2.SelectedItem = "All";
        }

        public bool AddOrRemoveItemToComboBox(Client client, bool isAdd)
        {
            if (InvokeRequired)
                return (bool)Invoke(new Func<Client, bool, bool>(AddOrRemoveItemToComboBox), client, isAdd);
            if (isAdd && !toolStripComboBox2.Items.Contains(client))
                toolStripComboBox2.Items.Add(client);
            else if (!isAdd && toolStripComboBox2.Items.Contains(client))
                toolStripComboBox2.Items.Remove(client);
            else
                ColoredConsoleWrite(Color.Red, "AddOrRemoveItemToComboBox Failed!!", client.Name);
            return true;
        }

        public void SetStatusText(string text)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(SetStatusText), text);
                return;
            }

            statusLabel.Text = text;
        }

        public Object GetComboBoxItem()
        {
            if (InvokeRequired)
            {
                return Invoke(new Func<Object>(GetComboBoxItem));
            }

            return toolStripComboBox2.SelectedItem;
        }

        public async Task ColoredConsoleWrite(Color color, string text, string username = "All")
        {
            if (InvokeRequired)
            {
                Invoke(new Func<Color, string, string, Task>(ColoredConsoleWrite), color, text, username);
                return;
            }
            string textToAppend = "[" + DateTime.Now.ToString("HH:mm:ss tt") + "] " + text + "\r\n";
            if (username != "All")
            {
                if (toolStripComboBox2.SelectedItem.ToString() != "All" && username == toolStripComboBox2.SelectedItem.ToString())
                {
                    logTextBox.SelectionColor = color;
                    logTextBox.AppendText(textToAppend);
                }

                _botting.ConsoleText[username].Append($"{color.ToString()}:{textToAppend}");
            }

            _botting.ConsoleText["All"].Append($"{color.ToString()}:[{username}]{textToAppend}");
            if (_botting.ConsoleText["All"].Length > 50000)
                _botting.ConsoleText["All"].Remove(0, 25000);
                //await WriteIntoLog("All");
            if (username != "All" && _botting.ConsoleText[username].Length > 25000)
                _botting.ConsoleText[username].Remove(0, 12500);
                //await WriteIntoLog(username);
        }

        //private async Task WriteIntoLog(string username)
        //{
        //    object syncRoot = new object();
        //    lock (syncRoot) // Added locking to prevent text file trying to be accessed by two things at the same time
        //    {
        //        string text = _botting.ConsoleText[username].ToString();
        //        string path = $"{AppDomain.CurrentDomain.BaseDirectory}\\Logs\\{username}.txt";
        //        if (!File.Exists(path))
        //            File.Create(path);
        //        File.AppendAllText(path, text);
        //    }
        //}

        private void logTextBox_TextChanged(object sender, EventArgs e)
        {
            logTextBox.SelectionStart = logTextBox.Text.Length;
            logTextBox.ScrollToCaret();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("dont do that!");
            //SettingsForm settingsForm = new SettingsForm();
            //settingsForm.Show();
        }

        private async void useLuckyEggToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("dont do that!");
            var stripItem = sender as ToolStripMenuItem;
            stripItem.Enabled = false;
            bool isSuccess = await _botting.UseLuckyEgg(toolStripComboBox2.SelectedItem as Client);
            if (isSuccess)
                await Task.Delay(30000);
            stripItem.Enabled = true;
        }

        private void showAllToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            MessageBox.Show("dont do that!");
        }

        private void todoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("dont do that!");
            //SettingsForm settingsForm = new SettingsForm();
            //settingsForm.Show();
        }

        private void pokemonToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("dont do that!");
            Client client = toolStripComboBox2.SelectedItem as Client;
            if (client != null)
            {
                var pForm = new PokeUi(client);
                pForm.Show();
            }
            else
                MessageBox.Show("clinet = null!");
        }

        private void toolStripComboBox2_TextChanged(object sender, EventArgs e)
        {

            var username = toolStripComboBox2.SelectedItem.ToString();
            var client = toolStripComboBox2.SelectedItem as Client;
            
            if (!string.IsNullOrEmpty(username) && _botting.ConsoleText.ContainsKey(username))
            {
                logTextBox.Clear();
                logTextBox.Text = _botting.ConsoleText[username].ToString();

                if (_comboBoxTokenSource != null)
                    _comboBoxTokenSource.Cancel();
                if (username != "All")
                {
                    _comboBoxTokenSource = new CancellationTokenSource();
                    _botting.ConsoleLevelTitle(client, _comboBoxTokenSource.Token);
                }
                else
                {
                    //todo
                }
            }
        }
    }
}
