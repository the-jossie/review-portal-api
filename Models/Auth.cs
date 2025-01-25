namespace Restaurant_Review_Api.Models
{
    public partial class Auth
    {
        public string Email {get; set;} = "";
        public byte[] PasswordHash { get; set; } = [0];
        public byte[] PasswordSalt { get; set; } = [0];
    }
}
