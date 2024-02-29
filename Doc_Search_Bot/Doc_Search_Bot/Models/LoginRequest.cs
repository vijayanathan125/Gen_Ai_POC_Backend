using System.ComponentModel.DataAnnotations;

namespace Query_Quasar_Bot_API.Models
{
        public class LoginRequest
        {
            [Key]
            public string UserMail { get; set; }
            public string Password { get; set; }
            public bool IsAdmin { get; }
    }

}
