using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using ICSharpCode.SharpZipLib.Zip; //Dışarıdan indirip eklediğimiz zip kütüphanesi
using System.Net;
using System.Net.Mail;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SQLOtoKayit
{
    public partial class Ayarlar : Form
    {
        public Ayarlar()
        {
            InitializeComponent();
        }

        public static string baglantiCumlesi; //Form1 den gelen bağlantı cümlesini tutar.
        public void Cikis()
        {

            #region  Bu metod eğer açıksa Form1 e dönmemizi sağlar.
            var dialog = MessageBox.Show("Yaptığınız işlemler kaybolacak. Kapatmak istiyor musunuz?", "Uyarı", MessageBoxButtons.YesNo);
            if (dialog == DialogResult.Yes)
            {
                foreach (Form form in Application.OpenForms)
                {
                    if (form.Name == "Form1")
                    {
                        form.Show();
                    }
                }
                this.Dispose();
                this.Close();
            }
            
            #endregion
        }
        
        public void Ziple(string dosyaYolu)
        {
            #region Bu metod seçili klasörün içindeki dosyaları Zipler.

            FastZip fz = new FastZip();
            fz.CreateEmptyDirectories = true;
            if (Directory.Exists(Application.StartupPath+"\\Loglar"))
            {
                //Zip oluşturulacak klasör yoksa o klasörü oluşturur. 
                if (!Directory.Exists(Application.StartupPath + "\\LogZip"))
                {
                    Directory.CreateDirectory("LogZip");
                }
                fz.CreateZip(Application.StartupPath +"\\LogZip\\"+ dosyaYolu, Application.StartupPath + "\\Loglar", true, "");

            }
            else
            {
                MessageBox.Show("Zip alınacak klasör mevcut değil.");
            }
            #endregion
        }
        public void Backup()
        {
            #region Backup alma ve bu Backup ı XML e, ZİP e ve Windows Olay Görüntüleyiciye kaydetme 
            lblBackupMesaj.Visible = true; //Bu label etiketini gizli olma ihtimali olduğu için burda açıyoruz.

            try
            {
                if (cbSistemLog.Checked == true)
                {
                    //Aşağıdaki if sorgusunda eğer belirtilen dosya kaynağı yoksa oluşturuluyor.
                    if (!EventLog.Exists("BackupLog"))
                    {
                        EventLog.CreateEventSource("BackupLog", "Backup Loglari"); // Event adı Backup Loglari
                    }
                    EventLog myEvent = new EventLog();
                    myEvent.Source = "BackupLog"; // Oluşturduğumzu event kaynağının adı BackupLog
                    EventLogTraceListener listener = new EventLogTraceListener(myEvent);
                    Trace.Listeners.Add(listener); //trace listemizin içine listener ile alınan tüm event log bilgilerini aktarıp tutuyoruz.
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Sistem log almada oluştu");
            }
            //Eğer cbSistemLog seçili iase bunu oluşturuyoruz.
           

            var tarih = string.Format("{0:ddMMyyyyHHmm}", DateTime.Now);
            //Yeni bir xml dosyası oluşturuyoruz.

            if (listYedekDatabase.Items.Count > 0) //Eğer yedeklenecek listboxta database varsa
            {
                //Loglar klasörü yoksa bu klasörü oluşturuyoruz.
                if (!Directory.Exists(Application.StartupPath + "\\Loglar"))
                {
                    Directory.CreateDirectory("Loglar");
                }
                XmlTextWriter yaz = new XmlTextWriter(new FileStream(Application.StartupPath + "//Loglar//log" + tarih + ".xml", FileMode.OpenOrCreate), Encoding.Unicode);
                yaz.Formatting = Formatting.Indented; //Girinti olacağını bildiriyoruz.
                //Xml yazma işlemi başlıyor.
                yaz.WriteStartDocument();
                yaz.WriteStartElement("log");
                //Yedeklenecek listboxta ki database sayısı kadar işlem yapıyoruz.
                for (int i = 0; i < listYedekDatabase.Items.Count; i++) 
                {
                   
                    string data = listYedekDatabase.Items[i].ToString();
                    //listbox ın item elemanına göre aşağıda backup alıyoruz.

                    try
                    {
                       
                        var sqlkomut = "backup database " + data + " to disk='" + yolutut + "\\" + data + ".bak'";
                        SqlCommand sorgu = new SqlCommand(sqlkomut, conn);
                        sorgu.ExecuteNonQuery();
                        yaz.WriteStartElement("bilgiler");
                        yaz.WriteElementString("Tarih", DateTime.Now.ToString());
                        yaz.WriteElementString("Database", data.ToString());
                        yaz.WriteElementString("Durum", "Backup Başarılı Bir Şekilde Gerçekleşti.");
                        yaz.WriteEndElement();
                        lblBackupMesaj.Text = "Backup alındı."; 
                        
                        if(cbSistemLog.Checked==true)
                        {
                            //Aşağıdaki yazdırma işlemi Trace ile Olay Görüntüleyiciye bilgi yazdırdık.
                            Trace.WriteLine("Backup Başarıyla Alındı.");  
                        }
                                              
                    }
                    catch (Exception ex)
                    {
                        //Xml e hatalı bilgileri yazıyoruz.
                        yaz.WriteStartElement("bilgiler");
                        yaz.WriteElementString("Tarih", DateTime.Now.ToString());
                        yaz.WriteElementString("Database", data.ToString());
                        yaz.WriteElementString("Durum", "Backup Alınamadı.");
                        yaz.WriteEndElement();
                        lblBackupMesaj.Text = "Backup alınamayan dosya veya dosyalar var.";
                        if (cbSistemLog.Checked == true)
                        {
                            //Aşağıdaki yazdırma işlemi Trace ile Olay Görüntüleyiciye hata bilgisi yazdırdık.
                            Trace.TraceError("Backup Alınamadı.");
                        }
                        
                        MessageBox.Show("Hata oluştu: "+ ex.Message);
                        DialogResult dialog = MessageBox.Show("Eğer program otomatik bakup modunda ise devam etsin mi?", "Hata oluştu!", MessageBoxButtons.YesNo);
                       if (dialog == DialogResult.No)
                       {
                           timer1.Stop();
                           backupSayisi = 0;
                           lblBackupSayisi.Text = "";
                           lblBackupMesaj.Visible = false;
                           btnZamanlıYedek.Enabled = true;
                           btnIptal.Enabled = false;
                           dtTarih.Enabled = true;
                           dtSaat.Enabled = true;
                           txtKacZaman.Enabled = true;
                           rbDakika.Enabled = true;
                           rbGun.Enabled = true;
                           rbSaat.Enabled = true;
                           txtYol.Enabled = true;
                           btnHedef.Enabled = true;
                       }
                    }

                }
                yaz.WriteEndElement(); 
                yaz.Flush(); //Yazdığımız xml stream i tamamlıyoruz.
                yaz.Close();
                Ziple("log" + tarih + ".zip"); //Zip metodu çağırılarak Zipleme yapılıyor.
            }
            else
            {
                timer1.Stop(); //Eğer database yoksa timer ı durdurduk.
                lblBackupMesaj.Text = "Backup alınamadı.Lütfen tekrar deneyiniz. "; 
            }
            Trace.Flush();
            Trace.Listeners.Clear();  
            #endregion
        }
        public void DosyaEki()
        {
            #region LogZip klasöründeki son eklenmiş zip dosyasının Maile eklenmesi
            List<DateTime> dt = new List<DateTime>();
            string[] dosyalar = Directory.GetFiles(Application.StartupPath + "\\LogZip"); //tüm dosyaları dosyalar dizisine çekiyoruz. Dosyanın tüm yol bilgisi geliyor.
            
            //Bütün dosyaların son yazılma tarihini dt adlı listemize ekliyoruz.
            for (int j = 0; j < dosyalar.Length; j++)
            {
                dt.Add(Directory.GetLastWriteTime(dosyalar[j]));
            }
            dt.Sort(); //dt listemizi küçük olandan büyük olana sıralıyoruz.
            dt.Reverse(); //Sıraladığımız listeyi bu metod ile ters çeviriyoruz.

            //Ters çevirdiğimiz listenin ilk elemanının sahip olduğu tarih hangi dosyamızın tariji ile örtüşüyorsa o en son eklenen dosya olduğu için onu mailimiz için seçiyoruz.
            for (int j = 0; j < dosyalar.Length; j++)
            {
                if (dt[0] == Directory.GetLastWriteTime(dosyalar[j]))
                {
                    var dosyaAdi = dosyalar[j].Split('\\').LastOrDefault();
                    txtSecilenDosya.Text = dosyaAdi;

                }
            }
            #endregion
        }

        public async void MailGonder()
        {
            #region Mail gönderme işlemleri
            lblBilgiMesaji.Text = ""; //Butona basıldığında eğer bu label ın içi doluysa boşaltıyoruz.
            SmtpClient sc = new SmtpClient();
            MailMessage mail = new MailMessage();

            if (string.IsNullOrEmpty(txtYourAdres.Text) || string.IsNullOrEmpty(txtSifre.Text) ||
                string.IsNullOrEmpty(txtKonu.Text) || string.IsNullOrEmpty(richMesaj.Text) ||
            string.IsNullOrEmpty(txtGonAdres.Text) || string.IsNullOrEmpty(cmbMailServis.Text))
            {
                MessageBox.Show("Lütfen tüm alanları doldurunuz.");
            }
            else
            {
                try
                {
                   
                    if (cmbMailServis.Text == "@hotmail.com")
                    {
                        sc.Host = "smtp.live.com"; //Mail servisini belirler
                        sc.Port = 587;
                    }
                    if (cmbMailServis.Text == "@gmail.com")
                    {
                        sc.Host = "smtp.gmail.com"; //Mail servisini belirler
                        sc.Port = 587;
                    }
                    if (cmbMailServis.Text == "@yahoo.com")
                    {
                        sc.Host = "smtp.mail.yahoo.com"; //Mail servisini belirler
                        sc.Port = 465;
                    }
                    sc.EnableSsl = true; //Güvenli igirşe izin verdik.
                    sc.Credentials = new NetworkCredential(txtYourAdres.Text + cmbMailServis.Text, txtSifre.Text); //Mail servisi üzerinden var olan hesabımıza giriş yaptık.

                    //Mesajımızı ve içeriğini oluşturduk.
                    mail.From = new MailAddress(txtYourAdres.Text + cmbMailServis.Text, txtKonu.Text);

                    mail.To.Add(txtGonAdres.Text);
                    mail.Subject = txtKonu.Text;
                    mail.IsBodyHtml = true;
                    mail.Body = richMesaj.Text;
                    if (cbDosya.Checked == true)
                    {
                        if (string.IsNullOrEmpty(txtSecilenDosya.Text))
                        {
                            MessageBox.Show("Lütfen bir dosya seçiniz.");
                        }
                        else
                        {
                            //Eğer checkbox seçiliyse mailimize bir dosya eki ekliyoruz.
                            mail.Attachments.Add(new Attachment(Application.StartupPath + "\\LogZip\\" + txtSecilenDosya.Text));
                        }
                    }

                    await sc.SendMailAsync(mail);
                    //mailimizi gönderiyoruz
                    lblMailSend.Text = "Mailiniz iletildi.";

                }
                catch (Exception)
                {
                    MessageBox.Show("Mail hatası oluştu. Bilgileriniz eksik yada hatalı olabilir. Veya erişmeye çalıştığınız mail hesabınız güvenlik nedeniyle giriş izni vermemiş olabilir.");
                }
                finally 
                {
                    mail.Dispose();
                }
            }
            #endregion
        }
        private void btnEkle_Click(object sender, EventArgs e)
        {
            #region Eğer ekli değilse listYedekDatabase listbox ına seçili Database i ekler. 
            if (listYedekDatabase.Items.Contains(listDatabase.SelectedItem))
            {
                MessageBox.Show("Bu Veritabanı zaten ekli.");
            }
            else
            {
                listYedekDatabase.Items.Add(listDatabase.SelectedItem);
            }
            #endregion

        }

        private void Çıkar_Click(object sender, EventArgs e)
        {
            //ListYedekDatabase listbox ından ekli olan elemanları çıkarmaya yarar.
            listYedekDatabase.Items.Remove(listYedekDatabase.SelectedItem);
        }
        string yolutut = Application.StartupPath + "\\BackupFile"; //Yedek alınacak dosya yolunu tutan element.
        SqlConnection conn; // bağlantıyı tüm forma yayan connection elementi.
        private void btnHedef_Click(object sender, EventArgs e)
        {
            #region Yedek dosyalarımızı nereye alacağımızı seçiyoruz.
            var dialog = folderBrowserDialog1.ShowDialog();
            if (dialog == DialogResult.OK)
            {
                txtYol.Text = folderBrowserDialog1.SelectedPath;
                yolutut = txtYol.Text;
            }
            #endregion
        }

        private void btnNowBackup_Click(object sender, EventArgs e)
        {

            Backup(); //Şimdi Yedekle butonuna tıkladığımızda Backup metodunu çağırdık
               
        }
        private void Ayarlar_Load(object sender, EventArgs e)
        {
            #region Ayarlar formu yüklenirken gerekli talimatlar
            conn = new SqlConnection(baglantiCumlesi); //sql bağlantısını çağırdık.
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open(); //Eğer açık değilse bağlantıyı açtık.
            }

            //BackupFile klasörü yoksa oluşturuyoruz.
            if (!Directory.Exists(Application.StartupPath + "\\BackupFile"))
            {
                Directory.CreateDirectory(Application.StartupPath + "\\BackupFile");
            }
            //LogZip klasörü yoksa oluşturuyoruz.
            if (!Directory.Exists(Application.StartupPath + "\\LogZip"))
            {
                Directory.CreateDirectory("LogZip");
            }

            //Loglar klasörü yoksa bu klasörü oluşturuyoruz.
            if (!Directory.Exists(Application.StartupPath + "\\Loglar"))
            {
                Directory.CreateDirectory("Loglar");
            }

            lblBilgiMesaji.Text = "Birden fazla mail adresi için adreslerin arasına virgül(,) koyunuz.";

            lblBilgi.Text = "Aşağıdaki seçeneği işaretlerseniz, program her backup aldığında, yukarıda" + Environment.NewLine + "belirtilen bilgiler doğrultusunda otomatik mail gönderilecektir." + Environment.NewLine + "NOT: Bu modda program son alınan backup zip dosyasını maile ekler.";

            rbDakika.Checked = true;
            txtYol.Text = yolutut;
            txtSecilenDosya.Enabled = false;
            btnGozat.Enabled = false;
            btnIptal.Enabled = false;
            txtKacZaman.Text = Convert.ToString(0);
            cmbMailServis.SelectedItem = cmbMailServis.Items[0];

            lblProgramBilgi.Text = "Seçtiğiniz veritabanlarının SQL Backup dosyalarını alır. Dilerseniz Ayarlar" + Environment.NewLine + "sekmesi altında, seçtiğiniz ayarlar doğrultusunda otomatik backup oluşturmanızı" + Environment.NewLine + "sağlar. Aynı zamanda dilediğiniz zaman mail gönderebilir, hatta mail gönderme" + Environment.NewLine + "işlemini de Backup alma işlemi ile birlikte otomatik ayarlayabilirsiz.";
           
            #endregion
        }

        private  void btnGonder_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Bu işlem birkaç dakika sürebilir. Lütfen iletildi mesajını alana kadar bekleyiniz.");
            MailGonder();
        }

        private void btnGozat_Click(object sender, EventArgs e)
        {
            #region Mail için dosya seçme işlemleri
            string dosyaAdi;
            openFileDialog1.InitialDirectory = Application.StartupPath+"\\LogZip"; //Programın debug klasörü içerisindeki LogZip adlı klasörden başlıyor.
            openFileDialog1.FileName = null; //Dosya Adı kutucuğunu adını boş getirir.
            openFileDialog1.Filter = "zip dosyası (*.zip)|*.zip"; //Sadece zip dosyalarını seçebiliriz.           
            var dialog = openFileDialog1.ShowDialog();
            if (dialog == DialogResult.OK)
            {
               var dosyaYolu = openFileDialog1.FileName;
               dosyaAdi = dosyaYolu.Split('\\').LastOrDefault(); //Dosyayolunu belirtilen işaretten kesip son değeri yani dosya adı değerini aldık.
               txtSecilenDosya.Text = dosyaAdi;
            }
            #endregion
        }

        private void cbDosya_CheckedChanged(object sender, EventArgs e)
        {
            #region Mail formundaki dosya seç adlı checkbox ın etkin ve pasif olma durumu
            if (cbDosya.Checked == true)
            {
                DosyaEki(); //Eğer seçiliyse DosyaEki adlı metoddaki en son oluşturulan zip dosyasını otomatik seçiyor.
                btnGozat.Enabled = true;
            }
            else if (cbDosya.Checked == false)
            {
                btnGozat.Enabled = false;
                txtSecilenDosya.Clear();
            }
            #endregion
        }

        int backupSayisi = 0;
        private void btnZamanlıYedek_Click(object sender, EventArgs e)
        {
            backupSayisi = 0;
            lblBackupMesaj.Text = "";
            timer1.Start(); //Timer başlatıldı.
            timer1_Tick(null, null); //Butona tıklandığı anda koşullar sağlanıyorsa timer bir kez çalıştı ve sonra interval değerine göre çalışmaya devam etti.
        }
        
        private void timer1_Tick(object sender, EventArgs e)
        {
            #region  Otomatik Yedek alma ve isteğe bağlı mail gönderme işlemleri
            
            string tarih = string.Format("{0:dd.MM.yyyy}", DateTime.Now); //şimdiki tarihi alıyoruz.
            //Eğer aldığımız tarif formatıyla bilgisayarın tarih formatı farklıysa aşağıdaki işlemi yapıyoruz.
            if (tarih.Length != dtTarih.Text.Length)
            {
                tarih = string.Format("{0:d.MM.yyyy}", DateTime.Now); //Gün bilgisinin tek haneli yazımı.
            }
           
            string saat = string.Format("{0:HH:mm:ss}", DateTime.Now); //Şimdiki saat bilgisini alıyoruz.

            //Eğer tarih veya saat şu anki tarih veya saatten gerideyse otomatik backup ı durdurup uyarı veriyoruz.
            if (Convert.ToDateTime(dtTarih.Text) < Convert.ToDateTime(tarih) || Convert.ToDateTime(dtSaat.Text) < Convert.ToDateTime(saat))
            {
                
                timer1.Stop();
                MessageBox.Show("Geçmiş bir tarih veya saat seçtiniz.");   
            }
            else
            {
                if (tarih == dtTarih.Text && saat == dtSaat.Text) //seçilen ve tarih ve saat o ana eşit olduğunda
                {
                    //Hangi radiobutton seçiliyse ona göre ekleme işlemi yapıyoruz.
                    if (rbDakika.Checked == true)
                    {
                        double dakika = Convert.ToDouble(txtKacZaman.Text);
                        dtSaat.Text = DateTime.Now.AddMinutes(dakika).ToString();
                        dtTarih.Text = DateTime.Now.AddMinutes(dakika).ToString();

                    }
                    if (rbSaat.Checked == true)
                    {
                        double kacSaat = Convert.ToDouble(txtKacZaman.Text);
                        dtSaat.Text = DateTime.Now.AddHours(kacSaat).ToString();
                        dtTarih.Text = DateTime.Now.AddHours(kacSaat).ToString();
                    }
                    if (rbGun.Checked == true)
                    {
                        double gun = Convert.ToDouble(txtKacZaman.Text);
                        dtTarih.Text = DateTime.Now.AddDays(gun).ToString(); //Mevcut tarihin üzerine kullanıcının girdiği gün sayısı eklendi ve tarihin değeri o gün oldu.
                    }
                    btnZamanlıYedek.Enabled = false;
                    dtTarih.Enabled = false;
                    dtSaat.Enabled = false;
                    txtKacZaman.Enabled = false;
                    rbDakika.Enabled = false;
                    rbGun.Enabled = false;
                    rbSaat.Enabled = false;
                    txtYol.Enabled = false;
                    btnHedef.Enabled = false;
                    cbSistemLog.Enabled = false;
                    btnIptal.Enabled = true;
                    lblBackupMesaj.Visible = true;
                    backupSayisi++; //Backup sayısı arttı.
                    lblBackupSayisi.Text = Convert.ToString(backupSayisi) + " kez Backup Alındı."; //Backup sayısı ekrana yazdırıldı.
                    Backup(); // Backup metodu çağırılıp Backup alındı.
                    lblBackupMesaj.Text = "Program Otomatik Backup Modunda.";
                    //Eğer Otomatik mail gönderme işaretlendiyse ayarlı bilgiler doğrultusunda otomatik backup ile birlikte mail gönderiyoruz
                    if (cbOtoMail.Checked == true)
                    {
                        lblMailSend.Visible = false;
                        DosyaEki(); //Dosya ekini ekliyoruz.
                       MailGonder(); //Maili gönderiyoruz
                    }  
                }
                
            }
            #endregion
        }

        private void btnMailCikis_Click(object sender, EventArgs e)
        {
            Cikis(); // Çıkış metodu çağırılarak mevcut form kapatılıp Form1 e dönüldü.
        }
        private void btnIptal_Click(object sender, EventArgs e)
        {   
            // İptal metoduna basıldığı anda timer durduruldu ve false olan Zamanlı Yedek butonu aktifleştirildi.
            timer1.Stop();
            btnZamanlıYedek.Enabled = true;
            btnIptal.Enabled = false;
            dtTarih.Enabled = true;
            dtSaat.Enabled = true;
            txtKacZaman.Enabled = true;
            rbDakika.Enabled = true;
            rbGun.Enabled = true;
            rbSaat.Enabled = true;
            txtYol.Enabled = true;
            btnHedef.Enabled = true;
            cbSistemLog.Enabled = true;
            lblBackupMesaj.Text = "Program otomotik backup modundan çıkarıldı.";
            lblBackupSayisi.Text = "";
            
        }

        private void btnCikis_Click(object sender, EventArgs e)
        {
            Cikis(); // Çıkış metodu çağırılarak mevcut form kapatılıp Form1 e dönüldü.
        }

        private void rbDakika_CheckedChanged(object sender, EventArgs e)
        {
            lblKacZaman.Text = "Kaç Dakikada Bir:";
        }

        private void rbSaat_CheckedChanged(object sender, EventArgs e)
        {
            lblKacZaman.Text = "Kaç Saatte Bir:";
        }

        private void rbGun_CheckedChanged(object sender, EventArgs e)
        {
            lblKacZaman.Text = "Kaç Günde Bir:";
        }

        private void txtYourAdres_TextChanged(object sender, EventArgs e)
        {
            if (txtYourAdres.Text.Contains('@'))
            {
                MessageBox.Show("Lütfen mail servisinizi yan taraftan seçiniz.");
            }
        }

        private void cbOtoMail_CheckedChanged(object sender, EventArgs e)
        {
            #region cbOtoMail seçeneğinin aktif ve pasif hallerinde gerçekleşmesi gereken olaylar.
            if (cbOtoMail.Checked == true)
            {
                if (string.IsNullOrEmpty(txtYourAdres.Text) || string.IsNullOrEmpty(txtSifre.Text) ||
                  string.IsNullOrEmpty(txtKonu.Text) || string.IsNullOrEmpty(richMesaj.Text) ||
              string.IsNullOrEmpty(txtGonAdres.Text) || string.IsNullOrEmpty(cmbMailServis.Text))
                {
                    MessageBox.Show("Lütfen tüm alanları doldurunuz.");
                    cbOtoMail.Checked = false;
                }
                else
                {
                    if (cbDosya.Checked == false)
                    {
                        MessageBox.Show("Lütfen 'Dosya ekle' seçeceğini seçili bırakınız.");
                        cbOtoMail.Checked = false;
                    }
                    else
                    {
                        lblMailSend.Text = "";
                        txtGonAdres.Enabled = false;
                        txtKonu.Enabled = false;
                        txtSifre.Enabled = false;
                        txtYourAdres.Enabled = false;
                        richMesaj.Enabled = false;
                        cbDosya.Enabled = false;
                        btnGozat.Enabled = false;
                        cmbMailServis.Enabled = false;
                        btnGonder.Enabled = false;
                        
                    }
                }
            }
            if (cbOtoMail.Checked == false)
            {
                txtGonAdres.Enabled = true;
                txtKonu.Enabled = true;
                txtSifre.Enabled = true;
                txtYourAdres.Enabled = true;
                richMesaj.Enabled = true;
                cbDosya.Enabled = true;
                btnGozat.Enabled = true;
                cmbMailServis.Enabled = true;
                btnGonder.Enabled = true;
            }
            #endregion
        }

        private void Ayarlar_FormClosing(object sender, FormClosingEventArgs e)
        {
            #region Form kapatılmak istendiğinde bir uyarı mesajı veriyor ve seçime göre devam ediyoruz.
            var dialog = MessageBox.Show("Yaptığınız işlemler kaybolacak yinede kapatmak istiyor musunuz?", "Uyarı", MessageBoxButtons.YesNo);
            if (dialog == DialogResult.Yes)
            {
                foreach (Form form in Application.OpenForms)
                {
                    if (form.Name == "Form1")
                    {
                        form.Show();
                    }
                }
                this.Dispose();
                this.Close();
            }
            if (dialog == DialogResult.No)
            {
                e.Cancel = true; //Formun kapatılmadığını true döndürür.
            }
            #endregion

        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            #region Hakkında sekmesi için formun boyutlandırmasını yeniden yapma.
            if (tabControl1.SelectedIndex == 2)
            { 
                tabControl1.Height = 300;
                Ayarlar.ActiveForm.Height = 300;

            }
            else if (tabControl1.SelectedIndex == 1)
            {
                tabControl1.Height = 473;
                Ayarlar.ActiveForm.Height = 513;
            }
            else if (tabControl1.SelectedIndex == 0)
            {
                tabControl1.Height = 473;
                Ayarlar.ActiveForm.Height = 513;
            }
            #endregion
        }   
    }
}
