using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

#pragma warning disable CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。
namespace Dimage.Models
{
    public class SqlContext : DbContext
    {
        public SqlContext(DbContextOptions<SqlContext> options) : base(options) { }
        public DbSet<UserInfo>? Users { get; set; }
        public DbSet<ImageInfo>? Images { get; set; }
        public DbSet<Register>? Regs { get; set; }
    }
    /// <summary>
    /// 注册码信息
    /// </summary>
    public class Register
    {
        [Key]
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public int Uid { get; set; } = 0;
    }
    /// <summary>
    /// 用户信息-数据表
    /// </summary>
    public class UserInfo
    {
        [Key]
        public int Uid { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = "88888999";
        public int Passfail { get; set; } = 0;
        public DateTime PassTime { get; set; } = DateTime.Now;
        public Guid Token { get; set; } = new Guid();
        public DateTime Timeout { get; set; } = DateTime.Now;
    }
    /// <summary>
    /// 图片信息-数据表
    /// </summary>
    public class ImageInfo
    {
        [Key]
        public Guid Pid { get; set; }
        public int Uid { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Thumbnail { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; } = DateTime.Now;
        public DateTime Timeout { set; get; } = DateTime.Now;
        public int Share { get; set; } = 0;
        public string Filter { get; set; } = string.Empty;
    }
}
#pragma warning restore CS8618 // 在退出构造函数时，不可为 null 的字段必须包含非 null 值。请考虑声明为可以为 null。