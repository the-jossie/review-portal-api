namespace Restaurant_Review_Api
{
    public partial class User{
        public int UserId {get; set;}
        public string Email {get; set;} = "";
        public string UserName {get; set;} = "";
        public string UserRole {get; set;} = "";
    }
}
