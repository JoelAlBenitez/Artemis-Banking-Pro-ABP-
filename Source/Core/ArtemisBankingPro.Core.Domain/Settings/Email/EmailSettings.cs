namespace ArtemisBankingPro.Core.Domain.Settings.Email
{
    public sealed class EmailSettings
    {
        public required string SmtpServer { get; set; }
        public required int Port { get; set; }
        public required string SenderName { get; set; }
        public required string SenderEmail { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public bool EnableSsl { get; set; }

    }
}
