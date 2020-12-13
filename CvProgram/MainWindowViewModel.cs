using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace CvProgram
{
    public class MainWindowViewModel : InpcBase
    {
        private ObservableCollection<Veriler> veri;

        public ObservableCollection<Veriler> Veri
        {
            get { return veri; }
            set
            {
                if (veri != value)
                {
                    veri = value;
                    OnPropertyChanged(nameof(Veri));
                }
            }
        }

        private static string Yükle(OpenFileDialog openFileDialog)
        {
            Guid name = Guid.NewGuid();
            var yüklenendosyaadı = name + Path.GetExtension(openFileDialog.FileName);
            File.Copy(openFileDialog.FileName, $"{GlobalSettings.BasePath}{yüklenendosyaadı}");
            return yüklenendosyaadı.ToLower();
        }

        public MainWindowViewModel()
        {
            using var Ctx = new CvModel();
            Veri = new ObservableCollection<Veriler>(Ctx.Veriler.AsNoTracking());
            Sil = new RelayCommand(parameter =>
            {
                using var Ctx = new CvModel();

                var dc = parameter as Veriler;
                Ctx.Veriler.Local.Remove(Ctx.Veriler.Find(dc.Id));
                Ctx.SaveChanges();
                Veri = new ObservableCollection<Veriler>(Ctx.Veriler.AsNoTracking());
            }, parameter => true);

            Ekle = new RelayCommand(parameter =>
            {
                using var Ctx = new CvModel();

                var dc = parameter as Veriler;
                dc.KayıtTarihi = DateTime.Now;
                Ctx.Veriler.Local.Add(dc);
                Ctx.SaveChanges();
                Veri = new ObservableCollection<Veriler>(Ctx.Veriler.AsNoTracking());
            }, parameter => true);

            Aç = new RelayCommand(parameter =>
            {
                var dc = parameter as Veriler;
                if (!string.IsNullOrEmpty(dc.Dosya))
                {
                    try
                    {
                        Process.Start($"{GlobalSettings.ExePath}{dc.Dosya}");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }, parameter => parameter is Veriler dc ? !string.IsNullOrEmpty(dc.Dosya) : false);

            Güncelle = new RelayCommand(parameter =>
            {
                using var Ctx = new CvModel();

                var dc = parameter as Veriler;
                var güncellenecek = Ctx.Veriler.Find(dc.Id);
                güncellenecek.Aciklama = dc.Aciklama;
                güncellenecek.Telefon = dc.Telefon;
                güncellenecek.Adres = dc.Adres;
                güncellenecek.Sehir = dc.Sehir;
                Ctx.SaveChanges();
            }, parameter => true);

            DosyaEkle = new RelayCommand(parameter =>
            {
                var dc = parameter as Veriler;
                OpenFileDialog openFileDialog = new OpenFileDialog { Multiselect = false, Filter = "Evrak Dosyaları (*.pdf;*.xps;*.jpg;*.jpeg;*.tif;*.tiff;*.docx)|*.pdf;*.xps;*.jpg;*.jpeg;*.tif;*.tiff;*.docx" };
                if (openFileDialog.ShowDialog() == true)
                {
                    dc.Dosya = $"{GlobalSettings.DataFolder}{Yükle(openFileDialog)}";
                }
            }, parameter => true);

            ResimEkle = new RelayCommand(parameter =>
            {
                var dc = parameter as Veriler;
                OpenFileDialog openFileDialog = new OpenFileDialog { Multiselect = false, Filter = "Resim Dosyaları (*.jpg;*.jpeg)|*.jpg;*.jpeg" };
                if (openFileDialog.ShowDialog() == true)
                {
                    dc.Resim = $"{GlobalSettings.DataFolder}{Yükle(openFileDialog)}";
                }
            }, parameter => true);
        }

        public RelayCommand Sil { get; }

        public RelayCommand Ekle { get; }

        public RelayCommand Güncelle { get; }

        public RelayCommand Aç { get; }

        public RelayCommand DosyaEkle { get; }

        public RelayCommand ResimEkle { get; }
    }
}