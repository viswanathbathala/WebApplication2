namespace LicenseManager.Models.ViewModels
{
    public class LicenseViewModel
    {
        public int TotalLicenses { get; set; }
        public int ActiveLicenses { get; set; }
        public int ExpiredLicenses { get; set; }
        public IEnumerable<License> Licenses { get; set; }
    }
}
