using System;
using System.IO;
using System.Windows;

namespace CvProgram
{
    public static class GlobalSettings
    {
        static GlobalSettings()
        {
            ExePath = Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory);
            DataFolder = "\\DOSYALAR\\";
            BasePath = ExePath + DataFolder;
        }

        public static string BasePath { get; }

        public static string DataFolder { get; }

        public static string ExePath { get; }

        public static void CheckAndCreateBaseFolder()
        {
            try
            {
                if (!Directory.Exists(BasePath))
                {
                    Directory.CreateDirectory(BasePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "EBYS", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            }
        }
    }
}