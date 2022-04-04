

namespace Dimage.Models
{
    public class RETURN_DEFAULT
    {
        public int Error { get; set; }
        public string Msg { get; set; } = string.Empty;
    }
    public class RETURN_LIST_STRING
    {
        public int Error { get; set; }
        public string Msg { get; set; } = string.Empty;
        public List<string> List { get; set; } = new List<string>();

    }
    public class RETURN_LOGIN
    {
        public int Error { get; set; }
        public string Msg { get; set; } = string.Empty;
        public Guid Token { get; set; } = Guid.Empty;

    }
    public class RETURN_REGCODE
    {
        public int Error { get; set; }
        public string Msg { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;

    }
    public class POST_TOKEN_AND_IMAGE
    {
        public Guid Token { get; set; }
        public Guid Pid { get; set; }
        public DateTime Timeout { set; get; } = DateTime.Now;
        public int Share { get; set; } = 0;
        public string Filter { get; set; } = string.Empty;
    }
    public class POST_TOKEN_AND_PID
    {
        public Guid Token { get; set; }
        public Guid Pid { get; set; }
    }
    public class POST_LOGIN
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
    public class POST_REGINFO
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Code { get; set; }

    }
    public class POST_SETPASSWORD
    {
        public Guid Token { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Newpassword { get; set; }
    }
    public class POST_TOKEN
    {
        public Guid Token { get; set; }
    }
}
