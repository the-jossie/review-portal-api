namespace Restaurant_Review_Api.Dtos
{
    public partial class LoginConfirmationDto
    {
        public byte[] PasswordHash { get; set; } = [0];
        public byte[] PasswordSalt { get; set; } = [0];
    }
}
