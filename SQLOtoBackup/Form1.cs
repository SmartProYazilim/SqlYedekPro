using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Data.OleDb;
using System.IO;

namespace SQLOtoKayit
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
    
        private void btnBaglan_Click(object sender, EventArgs e)
        {
            #region Girilen değerler ile bir server database bağlantısı gerçekleştirdik. Sonra bağlantıdaki sorguda bulunan database sonuçlarını Ayarlar.cs formumuzun listDatabase kontrolüne atadık.
            if (string.IsNullOrEmpty(txtServer.Text) || string.IsNullOrEmpty(txtLogin.Text) || string.IsNullOrEmpty(txtSifre.Text))
            {
                MessageBox.Show("Lütfen boş alanları doldurunuz.");
            }
            else
            {
                SqlConnection conn = new SqlConnection("Server=" + txtServer.Text + ";Database=master;User Id=" + txtLogin.Text + ";Password=" + txtSifre.Text + ";");


                Ayarlar ayar = new Ayarlar();

                try
                {
                    if (conn.State == ConnectionState.Closed)
                    {
                        conn.Open();
                    }
                    //var sqlkomut = "backup database CRM to disk='C:\\DB\\CRM.bak'";
                    var sqlkomut = "SELECT name from sys.databases";
                    SqlCommand sorgu = new SqlCommand(sqlkomut, conn);
                    SqlDataReader oku;
                    oku = sorgu.ExecuteReader();
                    var bulunanlist = (ListBox)ayar.Controls.Find("listDatabase", true).FirstOrDefault();
                    if (oku.HasRows)
                    {
                        Ayarlar.baglantiCumlesi = "Server=" + txtServer.Text + ";Database=master;User Id=" + txtLogin.Text + ";Password=" + txtSifre.Text + ";";
                        this.Hide();
                        ayar.Show();
                        while (oku.Read())
                        {
                            bulunanlist.Items.Add(oku[0]);

                        }
                    }
                    else
                    {
                        MessageBox.Show("Sorguyla eşleşen bir sonuç bulunamadı.");
                    }
                    
                }
                catch (Exception)
                {

                    MessageBox.Show("Bağlantı hatası oluştu.");
                }   
            }
            txtServer.Clear();
            txtLogin.Clear();
            txtSifre.Clear();
            #endregion
        }

    }
}
