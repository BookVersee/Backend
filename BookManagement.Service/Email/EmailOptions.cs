namespace BookManagement.Service.Email
{
    public class EmailOptions
    {
        public string SmtpServer { get; set; } = null!;
        public int SmtpPort { get; set; }
        public string SenderName { get; set; } = null!;
        public string SenderEmail { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string AppPassword { get; set; } = null!;
        public bool EnableSsl { get; set; } = true;
    }
}
