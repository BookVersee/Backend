namespace BookManagement.Service.Models;

public class EmailOptions
{
    public string SmtpServer { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = "BookManagement Support";
    public string AppPassword { get; set; } = string.Empty;
}
