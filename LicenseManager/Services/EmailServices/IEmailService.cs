using LicenseManager.Models;

namespace LicenseManager.Services.EmailServices
{
   
    public interface IEmailService
    {
        void SendEmail(EmailDto request);
    }
}
