
using Microsoft.AspNetCore.Mvc;
using Dimage.Models;
using Newtonsoft.Json.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Dimage.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ImageController : ControllerBase
    {
        #region 初始化过程
        /// <summary>
        /// 程序变量
        /// </summary>
        private IWebHostEnvironment _env { get; set; }
        /// <summary>
        /// 数据库变量
        /// </summary>
        private readonly SqlContext _context;
        /// <summary>
        /// 随机码类型
        /// </summary>
        public enum PasswordMode
        {
            Number, Hex, LString, AllString
        }
        /// <summary>
        /// 初始化控制器
        /// </summary>
        /// <param name="env"></param>
        /// <param name="context"></param>
        public ImageController(IWebHostEnvironment env, SqlContext context)
        {
            _env = env;
            _context = context;
            CreateDircetoryUser();
        }
        #endregion

        #region 公用内部过程
        /// <summary>
        /// 检查并创建目录用户信息
        /// </summary>
        private void CreateDircetoryUser()
        {
            string path = Path.Combine(_env.ContentRootPath, "res", "image");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            DirectoryInfo di = new DirectoryInfo(path);
            DirectoryInfo[] dis = di.GetDirectories();
            if (_context.Users == null) return;
            foreach (DirectoryInfo d in dis)
            {
                try
                {
                    var Ui = _context.Users.Where(C => C.Username.ToLower() == d.Name.ToLower()).FirstOrDefault();
                    if (Ui == null)
                    {
                        string imgpath = Path.Combine(d.FullName, "thumbnail");
                        if (!Directory.Exists(imgpath)) { Directory.CreateDirectory(imgpath); }
                        int nuid = _context.Users.Count() + 1;
                        _context.Add(new UserInfo()
                        {
                            Uid = nuid,
                            Username = d.Name.ToLower(),
                            Password = "88888999",
                            Timeout = DateTime.Now,
                            Token = new Guid()
                        });
                        _context.SaveChanges();
                    }
                }
                catch (Exception) { }
            }
        }
        /// <summary>
        /// 检查并创建登录用户目录
        /// </summary>
        /// <param name="Username"></param>
        private void CreateUserDirectory(string Username)
        {
            //检查目录
            string path = Path.Combine(_env.ContentRootPath, "res", "image", Username);
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            path = Path.Combine(path, "thumbnail");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        }

        #region 生成随机码过程
        /// <summary>
        /// 创建随机码过程
        /// </summary>
        /// <param name="Count">可选：默认8位密码</param>
        /// <param name="Mode">可选：默认纯数字</param>
        /// <returns></returns>
        public string CreateRandomPassword(int Count = 8, PasswordMode Mode = PasswordMode.Number)
        {
            string PasswordString = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            switch (Mode)
            {
                case PasswordMode.Hex:
                    {
                        PasswordString = "0123456789ABCDEF";
                        break;
                    }
                case PasswordMode.LString:
                    {
                        PasswordString = "0123456789abcdefghijklmnopqrstuvwxyz";
                        break;
                    }
                case PasswordMode.Number:
                    {
                        PasswordString = "0123456789";
                        break;
                    }
            }
            Random Rnd = new Random();
            string Pwd = String.Empty;
            do
            {
                int x = Rnd.Next(PasswordString.Length - 1);
                Pwd += PasswordString.Substring(x, 1);
            }
            while (Pwd.Length < Count);
            return Pwd;
        }
        #endregion
        /// <summary>
        /// 测试Guid合法性
        /// </summary>
        /// <param name="GuidString"></param>
        /// <returns></returns>
        private static bool IsGuid(string GuidString)
        {
            Guid gv = new Guid();
            try { gv = new Guid(GuidString); } catch (Exception) { }
            return gv == Guid.Empty ? false : true;
        }
        /// <summary>
        /// 获取网络图片
        /// </summary>
        /// <param name="Path">图片磁盘路径</param>
        /// <returns></returns>
        private FileContentResult? GetImageFile(string Path)
        {
            FileInfo fi = new FileInfo(Path);
            if (fi.Exists)
            {
                FileStream fs = fi.OpenRead();
                byte[] buffer = new byte[fs.Length];
                fs.Read(buffer, 0, Convert.ToInt32(fi.Length));
                var resource = File(buffer, "image/jpeg");
                fs.Close();
                return resource;
            }
            else { return null; }
        }
        /// <summary>
        /// 生成缩略图
        /// </summary>
        /// <param name="Src"></param>
        /// <param name="nWidth"></param>
        /// <returns></returns>
        public string ThumbnailImage(string Src, int nWidth = 150)
        {
            string? path = Path.GetDirectoryName(Src);
            string fn = Path.GetFileName(Src);
            string SavePath = Path.Combine(path, "thumbnail", fn);
            if (!System.IO.File.Exists(SavePath))
            {
                try
                {
                    using (Image image = Image.Load(Src))
                    {
                        if (image.Size().Width >= nWidth)
                        {
                            image.Mutate(x => x.Resize(nWidth, 0));
                            image.Save(SavePath);
                        }
                        else
                        {
                            SavePath = Src;
                        }
                    }
                }
                catch (Exception) { return Src; }
            }
            return SavePath;
        }
        #endregion

        #region 控制器
        [HttpGet]
        public ActionResult Get()
        {
            try
            {
                string path = Path.Combine(_env.ContentRootPath, "res", "db", "dimage.db");
                _context.Database.EnsureCreated();
                if (System.IO.File.Exists(path)) { return Ok("fileExists"); }
                else { return BadRequest(path); }
            }
            catch (Exception e) { return NotFound(e); }
        }

        #region 图片相关
        /// <summary>
        /// 获取外部连接图片
        /// </summary>
        /// <param name="pid">图片id</param>
        /// <param name="ex">图片类型，img=原图，thumb=缩略图</param>
        /// <returns></returns>
        [HttpGet("{pid}.{ex}")]
        public ActionResult GetImg(Guid pid, string ex)
        {
            if (_context.Images == null || _context.Users == null) return NotFound();
            string? uri = Request.Headers.Referer.FirstOrDefault();
            var Imgs = _context.Images.Where(x => x.Pid == pid).FirstOrDefault();
            if (Imgs != null && Imgs.Path.Length > 0)
            {
                var Ui = _context.Users.Where(x => x.Uid == Imgs.Uid).FirstOrDefault();
                switch (Imgs.Share)
                {
                    case 1: //公开分享
                        {
                            FileContentResult? file = ex == "img" ? GetImageFile(Imgs.Path) : GetImageFile(Imgs.Thumbnail);
                            return file == null ? NotFound() : file;
                        }
                    case 2: //限时分享
                        {
                            if (Imgs.Timeout > DateTime.Now)
                            {
                                FileContentResult? file = ex == "img" ? GetImageFile(Imgs.Path) : GetImageFile(Imgs.Thumbnail);
                                return file == null ? NotFound() : file;
                            }
                            break;
                        }
                    case 3: //白名单
                        {
                            if (uri == null) return NotFound();
                            string[] hosts = Imgs.Filter.Split(',');
                            foreach (string host in hosts)
                            {
                                if (uri.ToLower().IndexOf(host.ToLower()) > -1)
                                {
                                    FileContentResult? file = ex == "img" ? GetImageFile(Imgs.Path) : GetImageFile(Imgs.Thumbnail);
                                    return file == null ? NotFound() : file;
                                }
                            }
                            return NotFound();
                        }
                    case 4: //黑名单
                        {
                            if (uri == null) return NotFound();
                            string[] hosts = Imgs.Filter.Split(',');
                            foreach (string host in hosts)
                            {
                                if (uri.ToLower().IndexOf(host.ToLower()) > -1)
                                {
                                    return NotFound();
                                }
                            }
                            FileContentResult? file = ex == "img" ? GetImageFile(Imgs.Path) : GetImageFile(Imgs.Thumbnail);
                            return file == null ? NotFound() : file;
                        }
                }
            }
            return NotFound();
        }
        /// <summary>
        /// 管理页获取图片
        /// </summary>
        /// <param name="token"></param>
        /// <param name="pid"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        [HttpGet("{token}/{pid}.{ex}")]
        public ActionResult GetImg(Guid token, Guid pid, string ex)
        {
            if (_context.Images == null || _context.Users == null) return NotFound();
            try
            {
                var Ui = _context.Users.Where(x => x.Token == token).FirstOrDefault();
                if (Ui == null) return NotFound();
                int Uid = Ui.Uid;
                var Imgs = _context.Images.Where(C => C.Pid == pid && C.Uid == Uid).FirstOrDefault();
                if (Imgs != null)
                {
                    if (ex == "img")
                    {
                        FileContentResult? file = GetImageFile(Imgs.Path);
                        return file == null ? NotFound() : file;
                    }
                    else
                    {
                        FileContentResult? file = GetImageFile(Imgs.Thumbnail);
                        return file == null ? NotFound() : file;
                    }
                }
            }
            catch (Exception) { }
            return NotFound();

        }
        /// <summary>
        /// 管理页获取用户图片列表
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpGet("{token}/list")]
        public ActionResult GetList(Guid token)
        {
            if (_context.Images == null || _context.Users == null) return NotFound();
            var Ui = _context.Users.Where(C => C.Token == token && C.Timeout > DateTime.Now).FirstOrDefault();
            if (Ui == null) return NotFound();
            var Imgs = _context.Images.Where(C => C.Uid == Ui.Uid).ToList();
            JArray list = new JArray();
            if (Imgs.Count > 0)
            {
                foreach (var img in Imgs)
                {
                    string host = Request.Headers.Host;
                    string filter = img.Filter;
                    string[] fa = filter.Split(',');
                    list.Add(new JObject()
                    {
                        {"pid",img.Pid },
                        {"img",host + @"/image/" + img.Pid +".img"},
                        {"thumb",host + @"/image/" + img.Pid +".thumb"},
                        {"src", @"/image/" + token + @"/" + img.Pid +".thumb"},
                        {"prv", @"/image/" + token + @"/" + img.Pid +".img"},
                        {"timeout",img.Timeout },
                        {"share",img.Share},
                        {"filter",JArray.FromObject(fa) }
                    });
                }
            }
            return Ok(list);
        }
        /// <summary>
        /// 接收上传的图片，并生成缩略图
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        [HttpPost("{token}/upload")]
        public async Task<ActionResult> UploadFile(Guid token)
        {
            if (_context.Images == null || _context.Users == null) return NotFound();
            DateTime dateTime = DateTime.Now;
            var Ui = _context.Users.Where(x => x.Token == token && x.Timeout > dateTime).FirstOrDefault();
            if (Ui == null) { return NotFound(); }
            var files = Request.Form.Files;
            string SavePath = Path.Combine(_env.ContentRootPath, "res", "image", Ui.Username);
            foreach (var file in files)
            {
                Guid pid = Guid.NewGuid();
                string exname = Path.GetExtension(file.FileName);
                string filename = pid + exname;
                string filepath = Path.Combine(SavePath, filename);
                using (var stream = System.IO.File.Create(filepath))
                {
                    await file.CopyToAsync(stream);
                }
                string thumb = ThumbnailImage(filepath);
                _context.Images.Add(new ImageInfo()
                {
                    Pid = pid,
                    Uid = Ui.Uid,
                    Path = filepath,
                    Thumbnail = thumb,
                    CreateDate = DateTime.Now,
                    Timeout = DateTime.Now,
                    Share = 0,
                    Filter = string.Empty
                });
                _context.SaveChanges();
            }
            return Ok(new RETURN_DEFAULT() { Error = 0, Msg = "上传完成" });
        }
        /// <summary>
        /// 修改图片信息
        /// </summary>
        /// <param name="PostData"></param>
        /// <returns></returns>
        [HttpPost("set")]
        public ActionResult SetImage([FromBody] POST_TOKEN_AND_IMAGE P)
        {
            if (_context.Images == null || _context.Users == null) return NotFound();
            try
            {
                var Ui = _context.Users.Where(C => C.Token == P.Token && C.Timeout > DateTime.Now).FirstOrDefault();
                if (Ui == null) return NotFound();
                int uid = Ui.Uid;
                var Imgs = _context.Images.Where(C => C.Uid == uid && C.Pid == P.Pid).FirstOrDefault();
                if (Imgs == null) return NotFound();
                Imgs.Share = P.Share;
                Imgs.Timeout = P.Timeout;
                Imgs.Filter = P.Filter;
                _context.SaveChanges();
                return Ok(new RETURN_DEFAULT() { Error = 0, Msg = "修改成功" });
            }
            catch (Exception e) { return Ok(new RETURN_DEFAULT() { Error = 1, Msg = e.Message }); }
        }
        /// <summary>
        /// 删除图片
        /// </summary>
        /// <param name="PostData"></param>
        /// <returns></returns>
        [HttpPost("del")]
        public ActionResult DelImage([FromBody] POST_TOKEN_AND_PID P)
        {
            if (_context.Images == null || _context.Users == null) return NotFound();
            var Ui = _context.Users.Where(C => C.Token == P.Token && C.Timeout > DateTime.Now).FirstOrDefault();
            if (Ui == null) return NotFound();
            int uid = Ui.Uid;
            var Imgs = _context.Images.Where(C => C.Uid == uid && C.Pid == P.Pid).FirstOrDefault();
            if (Imgs != null)
            {
                try { System.IO.File.Delete(Imgs.Path); } catch (Exception) { }
                try { System.IO.File.Delete(Imgs.Thumbnail); } catch (Exception) { }
                _context.Images.Remove(Imgs);
                _context.SaveChanges();
                return Ok(new RETURN_DEFAULT() { Error = 0, Msg = "删除成功" });
            }
            else { return Ok(new RETURN_DEFAULT() { Error = 1, Msg = "删除失败" }); }
        }
        #endregion

        #region 帐号相关
        /// <summary>
        /// 登录管理页
        /// </summary>
        /// <param name="PostData"></param>
        /// <returns></returns>
        [HttpPost("login")]
        public ActionResult Login([FromBody] POST_LOGIN P)
        {
            if (_context.Images == null || _context.Users == null ||
                P.Username == null || P.Password == null) return NotFound();
            var Ui = _context.Users.Where(C => C.Username == P.Username).FirstOrDefault();
            if (Ui == null) return Ok(new RETURN_DEFAULT() { Error = 1, Msg = "用户名或密码错误" });
            if (Ui.Passfail > 2 && Ui.PassTime > DateTime.Now)
                return Ok(new RETURN_DEFAULT() { Error = 1, Msg = "请在 " + Ui.PassTime.ToString() + " 后再试。" });
            if (Ui.Password == P.Password)
            {
                Guid Token = Guid.NewGuid();
                Ui.Token = Token;
                Ui.Timeout = DateTime.Now.AddHours(6);
                Ui.Passfail = 0;
                Ui.PassTime = DateTime.Now;
                _context.SaveChanges();
                CreateUserDirectory(P.Username);
                return Ok(new RETURN_LOGIN() { Error = 0, Msg = "登录成功", Token = Token });
            }
            else
            {
                Ui.Passfail++;
                switch (Ui.Passfail)
                {
                    case 1:
                        {
                            Ui.PassTime = DateTime.Now;
                            break;
                        }
                    case 2:
                        {
                            Ui.PassTime = DateTime.Now;
                            break;
                        }
                    case 3:
                        {
                            Ui.PassTime = DateTime.Now.AddMinutes(1);
                            break;
                        }
                    case 4:
                        {
                            Ui.PassTime = DateTime.Now.AddMinutes(5);
                            break;
                        }
                    case 5:
                        {
                            Ui.PassTime = DateTime.Now.AddMinutes(10);
                            break;
                        }
                    case 6:
                        {
                            Ui.PassTime = DateTime.Now.AddMinutes(30);
                            break;
                        }
                    case 7:
                        {
                            Ui.PassTime = DateTime.Now.AddHours(1);
                            break;
                        }
                    case >= 8:
                        {
                            Ui.PassTime = DateTime.Now.AddHours(6);
                            break;
                        }
                }
                _context.SaveChanges();
                return Ok(new RETURN_DEFAULT() { Error = 1, Msg = "用户名或密码错误" });
            }
        }
        /// <summary>
        /// 检查并延长管理页登录状态，成功延长6小时
        /// </summary>
        /// <param name="PostData"></param>
        /// <returns></returns>
        [HttpPost("check")]
        public ActionResult CheckToken([FromBody] POST_TOKEN P)
        {
            try
            {
                if (_context.Images == null || _context.Users == null) return NotFound();
                Guid token = P.Token;
                var Ui = _context.Users.Where(C => C.Token == token && C.Timeout > DateTime.Now).FirstOrDefault();
                if (Ui != null)
                {
                    Ui.Timeout = DateTime.Now.AddHours(6);
                    _context.SaveChanges();
                    CreateUserDirectory(Ui.Username);
                    return Ok(new RETURN_DEFAULT() { Error = 0, Msg = "已登录" });
                }
                else { return Ok(new RETURN_DEFAULT() { Error = 1, Msg = "未登录" }); }
            }
            catch (Exception e)
            {
                return NotFound(new JObject() { { "error", 1010 }, { "try", JObject.FromObject(e) } });
            }
        }
        /// <summary>
        /// 修改用户密码
        /// </summary>
        /// <param name="PostData"></param>
        /// <returns></returns>
        [HttpPost("setpassword")]
        public ActionResult ResetUserPassword([FromBody] POST_SETPASSWORD P)
        {
            if (_context.Images == null || _context.Users == null) return NotFound();
            Guid Token = P.Token;
            string pwd = P.Password;
            string npwd = P.Newpassword;
            var Ui = _context.Users.Where(C => C.Token == Token && C.Timeout > DateTime.Now).FirstOrDefault();
            if (Ui == null) return NotFound();
            if (Ui.Password == pwd)
            {
                Ui.Password = npwd;
                _context.SaveChanges();
                return Ok(new RETURN_DEFAULT() { Error = 0, Msg = "修改密码成功" });
            }
            else { return Ok(new RETURN_DEFAULT() { Error = 1, Msg = "修改密码失败" }); }
        }
        /// <summary>
        /// 创建新用户
        /// </summary>
        /// <param name="PostData"></param>
        /// <returns></returns>
        [HttpPost("createuser")]
        public ActionResult CreateUser([FromBody] POST_REGINFO P)
        {
            if (_context.Users == null || _context.Regs == null) return NotFound();
            var Regs = _context.Regs.Where(C => C.Code == P.Code && C.Uid == 0).FirstOrDefault();
            if (Regs != null)
            {
                var Ui = _context.Users.ToList();
                var ui = new UserInfo()
                {
                    Uid = Ui.Count + 1,
                    Username = P.Username,
                    Password = P.Password
                };
                _context.Users.Add(ui);
                Regs.Uid = Ui.Count + 1;
                _context.SaveChanges();
                return Ok(new RETURN_DEFAULT() { Error = 0, Msg = "注册成功" });
            }
            else { return Ok(new RETURN_DEFAULT() { Error = 1, Msg = "注册失败" }); }
        }
        /// <summary>
        /// 创建注册码
        /// </summary>
        /// <param name="P"></param>
        /// <returns></returns>
        [HttpPost("createreg")]
        public ActionResult CreateRegcode([FromBody] POST_TOKEN P)
        {
            if (_context.Users == null || _context.Regs == null) return NotFound();
            var Ui = _context.Users.Where(C => C.Token == P.Token && C.Timeout > DateTime.Now).FirstOrDefault();
            if (Ui == null) return NotFound();
            var regs = _context.Regs.ToList();
            string code = CreateRandomPassword();
            Register reg = new Register()
            {
                Id = regs.Count + 1,
                Uid = 0,
                Code = code
            };
            regs.Add(reg);
            _context.SaveChanges();
            return Ok(new RETURN_REGCODE() { Error = 0, Msg = "创建注册码成功", Code = code });
        }
        /// <summary>
        /// 获取未使用注册码
        /// </summary>
        /// <param name="P"></param>
        /// <returns></returns>
        [HttpPost("regcode")]
        public ActionResult RegcodeList([FromBody] POST_TOKEN P)
        {
            if (_context.Users == null || _context.Regs == null) return NotFound();
            var Ui = _context.Users.Where(C => C.Token == P.Token && C.Timeout > DateTime.Now).FirstOrDefault();
            if (Ui == null) return NotFound();
            var regs = _context.Regs.Where(C => C.Uid == 0).ToList();
            List<string> codes = new List<string>();
            if (regs.Count > 0)
            {
                regs.ForEach(c => codes.Add(c.Code));
            }
            return Ok(new RETURN_LIST_STRING() { Error = 0, Msg = "获取未使用注册码", List = codes });
        }
        #endregion

        #endregion

    }
}
