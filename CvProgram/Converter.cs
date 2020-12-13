using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace CvProgram
{
    public class PathToResimConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return DependencyProperty.UnsetValue;
            }
            if (value is string resimyolu)
            {
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.None;
                bi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bi.UriSource = new Uri(GlobalSettings.ExePath + resimyolu);
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class NullableToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => parameter != null ? value == null : value != null;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class ResimUriSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return DependencyProperty.UnsetValue;
            }
            if (value is string resimyolu)
            {
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.CacheOption = BitmapCacheOption.None;
                bi.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                bi.DecodePixelHeight = 150;
                bi.UriSource = new Uri(GlobalSettings.ExePath + resimyolu);
                bi.EndInit();
                bi.Freeze();
                return bi;
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}