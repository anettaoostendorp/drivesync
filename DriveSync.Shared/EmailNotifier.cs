using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace DriveSync.Shared
{
    public class EmailNotifier
    {
        private readonly SyncSettings _settings;

        public EmailNotifier(SyncSettings settings)
        {
            _settings = settings;
        }

        public async Task SendAsync(string subject, string body)
        {
            if (string.IsNullOrWhiteSpace(_settings.EmailTo))
                return;

            if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
                return;

            try
            {
                using (var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort))
                {
                    client.EnableSsl = _settings.SmtpUseSsl;
                    client.Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword);

                    using (var message = new MailMessage())
                    {
                        message.From = new MailAddress(_settings.EmailFrom);
                        message.To.Add(_settings.EmailTo);
                        message.Subject = subject;
                        message.Body = body;
                        message.IsBodyHtml = false;

                        await client.SendMailAsync(message);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to send email: " + ex.Message);
            }
        }
    }
}
