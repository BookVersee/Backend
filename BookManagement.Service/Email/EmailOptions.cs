namespace BookManagement.Service.Email
{
    public class EmailOptions
    {
        public string SenderName { get; set; } = "BookManagement System";
        public string SenderEmail { get; set; } = null!;

        // Google Cloud Console (Gmail API / OAuth2)
        public string ClientId { get; set; } = null!;
        public string ClientSecret { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }
}
