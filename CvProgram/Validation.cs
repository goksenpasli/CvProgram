using System.ComponentModel;

namespace CvProgram
{
    public partial class Veriler : IDataErrorInfo
    {
        public string Error => string.Empty;

        public string this[string columnName] =>
            columnName switch
            {
                "Ad" when string.IsNullOrWhiteSpace(Ad) => "Ad Boş Olamaz.",
                "Soyad" when string.IsNullOrWhiteSpace(Soyad) => "Soyad Boş Olamaz.",

                _ => null
            };
    }
}