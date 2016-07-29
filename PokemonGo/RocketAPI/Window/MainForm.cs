using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PokemonGo.RocketAPI.Window
{
    public partial class MainForm : Form
    {
        Botting botting;

        public MainForm()
        {
            InitializeComponent();
        }

        public bool AddOrRemoveItemToComboBox(Client client, bool isAdd)
        {
            if (InvokeRequired)
                return (bool)Invoke(new Func<Client, bool, bool>(AddOrRemoveItemToComboBox), client, isAdd);
            if (isAdd)
                toolStripComboBox2.Items.Add(client);
            else if (!isAdd && toolStripComboBox2.Items.Contains(client))
                toolStripComboBox2.Items.Remove(client);
            else
                return false;
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

        private void MainForm_Load(object sender, EventArgs e)
        {
            toolStripComboBox2.Items.Add("All");
            toolStripComboBox2.SelectedItem = "All";

            botting = new Botting(this);
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

                botting.ConsoleText[username].Append($"{color.ToString()}:{textToAppend}");
            }

            botting.ConsoleText["All"].Append($"{color.ToString()}:[{username}]{textToAppend}");
            if ((username == "All" && botting.ConsoleText[username].Length > 50000) ||
                (username != "All" && botting.ConsoleText[username].Length > 5000))
                await WriteIntoLog(username);
        }

        private async Task WriteIntoLog(string username)
        {
            object syncRoot = new object();
            lock (syncRoot) // Added locking to prevent text file trying to be accessed by two things at the same time
            {
                string text = botting.ConsoleText[username].ToString();
                if (username != "All")
                    File.AppendAllText($"{AppDomain.CurrentDomain.BaseDirectory}\\Logs\\{username}.txt", text);
                else
                    File.AppendAllText($"{AppDomain.CurrentDomain.BaseDirectory}\\Logs\\All.txt", text);
            }
        }

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
            MessageBox.Show("dont do that!");
            await botting.UseLuckyEgg(toolStripComboBox2.SelectedItem as Client);
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
            MessageBox.Show("dont do that!");
            //var pForm = new PokeUi();
            //pForm.Show();
        }

        private void toolStripComboBox2_TextChanged(object sender, EventArgs e)
        {
            string username = toolStripComboBox2.SelectedItem.ToString();
            logTextBox.Clear();
            logTextBox.Text = botting.ConsoleText[username].ToString();
        }
    }
}
