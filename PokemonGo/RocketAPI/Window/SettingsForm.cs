using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using GMap.NET.MapProviders;
using GMap.NET;
using System.Configuration;
using System.Globalization;

namespace PokemonGo.RocketAPI.Window
{
    partial class SettingsForm : Form
    {
        private ISettings _setting;

        public SettingsForm(ISettings setting)
        {
            InitializeComponent();
            _setting = setting;
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            authTypeCb.Text = _setting.AuthType.ToString();
            if (authTypeCb.Text == "google")
            {
                ptcUserText.Text = _setting.Email;
                ptcPassText.Text = _setting.Password;
            }
            else
            {
                ptcUserText.Text = _setting.PtcUsername;
                ptcPassText.Text = _setting.PtcPassword;
            }
            latitudeText.Text = _setting.DefaultLatitude.ToString();
            longitudeText.Text = _setting.DefaultLongitude.ToString();
            razzmodeCb.Text = _setting.RazzBerryMode;
            razzSettingText.Text = _setting.RazzBerrySetting.ToString();
            transferTypeCb.Text = _setting.TransferType;
            transferCpThresText.Text = _setting.TransferCPThreshold.ToString();
            transferIVThresText.Text = _setting.TransferIVThreshold.ToString();
            evolveAllChk.Checked = _setting.EvolveAllGivenPokemons;
            CatchPokemonBox.Checked = _setting.CatchPokemon;
            TravelSpeedBox.Text = _setting.TravelSpeed.ToString();
            // Initialize map:
            //use google provider
            gMapControl1.MapProvider = GoogleMapProvider.Instance;
            //get tiles from server only
            gMapControl1.Manager.Mode = AccessMode.ServerOnly;
            //not use proxy
            GMapProvider.WebProxy = null;
            //center map on moscow
            string lat = ConfigurationManager.AppSettings["DefaultLatitude"];
            string longit = ConfigurationManager.AppSettings["DefaultLongitude"];
            lat.Replace(',', '.');
            longit.Replace(',', '.');
            gMapControl1.Position = new PointLatLng(double.Parse(lat.Replace(",", "."), CultureInfo.InvariantCulture), double.Parse(longit.Replace(",", "."), CultureInfo.InvariantCulture));



            //zoom min/max; default both = 2
            gMapControl1.DragButton = MouseButtons.Left;

            gMapControl1.CenterPen = new Pen(Color.Red, 2);
            gMapControl1.MinZoom = trackBar.Maximum = 1;
            gMapControl1.MaxZoom = trackBar.Maximum = 20;
            trackBar.Value = 10;

            //set zoom
            gMapControl1.Zoom = trackBar.Value;          
        }

        private void saveBtn_Click(object sender, EventArgs e)
        {
            MessageBox.Show("dont do that!");
        }

        private void authTypeCb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (authTypeCb.Text == "google")
            {
                ptcUserLabel.Text = "Email:";
            }
            else
            {
                ptcUserLabel.Text = "Username:";

            }
        }

        private void gMapControl1_MouseClick(object sender, MouseEventArgs e)
        {
            Point localCoordinates = e.Location;
            gMapControl1.Position = gMapControl1.FromLocalToLatLng(localCoordinates.X, localCoordinates.Y);

            if (e.Clicks >= 2)
            {
                gMapControl1.Zoom += 5;
            }
            
            double X = Math.Round(gMapControl1.Position.Lng, 6);
            double Y = Math.Round(gMapControl1.Position.Lat, 6);
            string longitude = X.ToString();
            string latitude = Y.ToString();
            latitudeText.Text = latitude;
            longitudeText.Text = longitude;            
        }

        private void trackBar_Scroll(object sender, EventArgs e)
        {
            gMapControl1.Zoom = trackBar.Value;
        }

        private void FindAdressButton_Click(object sender, EventArgs e)
        {
            gMapControl1.SetPositionByKeywords(AdressBox.Text);
            gMapControl1.Zoom = 15;
        }

        private void authTypeLabel_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void transferCpThresText_TextChanged(object sender, EventArgs e)
        {
        }

        private void transferTypeCb_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (transferTypeCb.Text == "CP")
            {
                label4.Visible = true;
                transferCpThresText.Visible = true;
            }
            else
            {
                label4.Visible = false;
                transferCpThresText.Visible = false;

            }

            if (transferTypeCb.Text == "IV")
            {
                label6.Visible = true;
                transferIVThresText.Visible = true;
            }
            else
            {
                label6.Visible = false;
                transferIVThresText.Visible = false;

            }

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void gMapControl1_Load(object sender, EventArgs e)
        {

        }

        private void FindAdressButton_Click_1(object sender, EventArgs e)
        {
            gMapControl1.SetPositionByKeywords(AdressBox.Text);
            gMapControl1.Zoom = 15;
            double X = Math.Round(gMapControl1.Position.Lng, 6);
            double Y = Math.Round(gMapControl1.Position.Lat, 6);
            string longitude = X.ToString();
            string latitude = Y.ToString();
            latitudeText.Text = latitude;
            longitudeText.Text = longitude;
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void evolveAllChk_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}
