namespace Ca_Bank_Api.Dtos
{
    public partial class SignupDto
    {
        public string Email { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Password { get; set; } = "";
        public string PasswordConfirmation { get; set; } = "";
    }
}
