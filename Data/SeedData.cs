using Microsoft.EntityFrameworkCore;
using NewsAggregator.Models;
using NewsAggregator.Services;

namespace NewsAggregator.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordService = scope.ServiceProvider.GetRequiredService<PasswordService>();

            await context.Database.EnsureCreatedAsync();
            await EnsureUserSchemaAsync(context);

            if (!await context.Menus.AnyAsync())
            {
                var menus = new List<Menu>
                {
                    new() { MenuName = "Trang chu", ControllerName = "Home", ActionName = "Index", Link = "/", MenuOrder = 1, Position = 1, IsActive = true },
                    new() { MenuName = "Cong nghe", ControllerName = "Home", ActionName = "Category", Link = "/Home/Category/2", MenuOrder = 2, Position = 1, IsActive = true },
                    new() { MenuName = "Kinh doanh", ControllerName = "Home", ActionName = "Category", Link = "/Home/Category/3", MenuOrder = 3, Position = 1, IsActive = true },
                    new() { MenuName = "The thao", ControllerName = "Home", ActionName = "Category", Link = "/Home/Category/4", MenuOrder = 4, Position = 1, IsActive = true },
                    new() { MenuName = "Giai tri", ControllerName = "Home", ActionName = "Category", Link = "/Home/Category/5", MenuOrder = 5, Position = 1, IsActive = true }
                };

                context.Menus.AddRange(menus);
                await context.SaveChangesAsync();
            }

            var existingUsers = await context.AppUsers.ToListAsync();
            var hasLegacyPassword = false;

            foreach (var user in existingUsers.Where(u => !u.Password.StartsWith("PBKDF2$", StringComparison.Ordinal)))
            {
                user.Password = passwordService.HashPassword(user.Password);
                hasLegacyPassword = true;
            }

            if (hasLegacyPassword)
            {
                await context.SaveChangesAsync();
            }

            await EnsureDemoUserAsync(
                context,
                passwordService,
                fullName: "Tran Dinh Anh Bao",
                email: "bao@news.local",
                userName: "anhbao",
                role: UserRoles.Admin,
                phoneNumber: "0900000001",
                address: "Ho Chi Minh City",
                bio: "Quan tri he thong va quan ly tai khoan nguoi dung.");

            await EnsureDemoUserAsync(
                context,
                passwordService,
                fullName: "Nguyen Van Duc",
                email: "duc@news.local",
                userName: "vanduc",
                role: UserRoles.Editor,
                phoneNumber: "0900000002",
                address: "Da Nang",
                bio: "Bien tap vien chuyen quan ly bai viet.");

            await EnsureDemoUserAsync(
                context,
                passwordService,
                fullName: "Ha Huy Hoang",
                email: "hoang@news.local",
                userName: "huyhoang",
                role: UserRoles.Moderator,
                phoneNumber: "0900000003",
                address: "Ha Noi",
                bio: "Kiem duyet binh luan va noi dung cong dong.");

            await EnsureDemoUserAsync(
                context,
                passwordService,
                fullName: "Thanh Vien Demo",
                email: "member@news.local",
                userName: "memberdemo",
                role: UserRoles.Member,
                phoneNumber: "0900000004",
                address: "Can Tho",
                bio: "Thanh vien thu nghiem cho he thong.");

            if (!await context.Posts.AnyAsync())
            {
                var menus = await context.Menus.OrderBy(m => m.MenuOrder).ToListAsync();
                var menuMap = menus.ToDictionary(m => m.MenuName, m => m.MenuID);

                context.Posts.AddRange(
                    new Post
                    {
                        Title = "Ung dung AI trong tong hop tin tuc hoat dong ra sao",
                        Abstract = "Tong hop cach he thong crawling, tim kiem va goi y tin tuc phuc vu trang tin tong hop.",
                        Contents = "He thong tong hop tin tuc se lay du lieu tu nhieu nguon, chuan hoa noi dung, phan loai theo menu va cho phep nguoi dung tim kiem nhanh. Day la bai viet mo ta tong quan de demo cho do an.",
                        Images = "/images/news/news-01.jpg",
                        Author = "Tran Dinh Anh Bao",
                        CreatedDate = DateTime.Now.AddHours(-3),
                        CrawledAt = DateTime.Now.AddHours(-2),
                        ViewCount = 128,
                        MenuID = menuMap["Trang chu"],
                        IsActive = true
                    },
                    new Post
                    {
                        Title = "Thi truong cong nghe Viet Nam tiep tuc tang truong manh",
                        Abstract = "Nhieu doanh nghiep day manh san pham so, AI va du lieu lon trong nam nay.",
                        Contents = "Linh vuc cong nghe dang tro thanh diem sang khi doanh nghiep dau tu vao ha tang du lieu, dich vu so va he thong AI. Bai viet nay duoc su dung lam du lieu mau cho chuc nang tim kiem va hien thi theo chuyen muc.",
                        Images = "/images/news/news-02.jpg",
                        Author = "Nguyen Van Duc",
                        CreatedDate = DateTime.Now.AddHours(-5),
                        CrawledAt = DateTime.Now.AddHours(-4),
                        ViewCount = 96,
                        MenuID = menuMap["Cong nghe"],
                        IsActive = true
                    },
                    new Post
                    {
                        Title = "Doanh nghiep chu dong doi moi de thich nghi xu huong moi",
                        Abstract = "Ap luc chuyen doi so buoc doanh nghiep toi uu van hanh va mo rong kenh phan phoi.",
                        Contents = "O nhom kinh doanh, cac don vi dang chuyen sang khai thac du lieu, tu dong hoa va bao cao thong minh. Noi dung nay phu hop cho chuc nang loc menu va thong ke luot xem tren trang chu.",
                        Images = "/images/news/news-03.jpg",
                        Author = "Ha Huy Hoang",
                        CreatedDate = DateTime.Now.AddHours(-8),
                        CrawledAt = DateTime.Now.AddHours(-7),
                        ViewCount = 72,
                        MenuID = menuMap["Kinh doanh"],
                        IsActive = true
                    },
                    new Post
                    {
                        Title = "Lich thi dau hom nay duoc cap nhat tren he thong tong hop",
                        Abstract = "Nguoi dung co the theo doi nhanh cac tin the thao noi bat tren mot giao dien thong nhat.",
                        Contents = "Tin the thao duoc phan bo theo chuyen muc rieng, ho tro tim kiem va binh luan duoi bai viet. Du lieu mau nay giup minh hoa kha nang dieu huong menu va hien thi chi tiet bai viet.",
                        Images = "/images/news/news-04.jpg",
                        Author = "Nguyen Van Duc",
                        CreatedDate = DateTime.Now.AddHours(-11),
                        CrawledAt = DateTime.Now.AddHours(-10),
                        ViewCount = 54,
                        MenuID = menuMap["The thao"],
                        IsActive = true
                    },
                    new Post
                    {
                        Title = "Su kien giai tri noi bat trong tuan duoc doc gia quan tam",
                        Abstract = "Noi dung giai tri thu hut luong truy cap cao va luot binh luan lon.",
                        Contents = "Chuyen muc giai tri la noi phu hop de test phan binh luan. Bai viet duoc seed san de demo danh sach bai viet, tim kiem theo tu khoa va thong tin tac gia.",
                        Images = "/images/news/news-05.jpg",
                        Author = "Tran Dinh Anh Bao",
                        CreatedDate = DateTime.Now.AddHours(-14),
                        CrawledAt = DateTime.Now.AddHours(-13),
                        ViewCount = 211,
                        MenuID = menuMap["Giai tri"],
                        IsActive = true
                    });

                await context.SaveChangesAsync();
            }

            if (!await context.Comments.AnyAsync())
            {
                var firstPost = await context.Posts.OrderBy(p => p.PostID).FirstAsync();
                var users = await context.AppUsers.Where(u => !u.IsDeleted).ToListAsync();

                context.Comments.AddRange(
                    new Comment
                    {
                        PostID = firstPost.PostID,
                        UserID = users.First().AppUserID,
                        AuthorName = users.First().FullName,
                        AuthorEmail = users.First().Email,
                        Content = "Giao dien template da duoc noi voi du lieu that, co the mo rong tiep phan dang nhap sau.",
                        IsApproved = true,
                        CreatedAt = DateTime.Now.AddHours(-1)
                    },
                    new Comment
                    {
                        PostID = firstPost.PostID,
                        AuthorName = "Doc gia demo",
                        AuthorEmail = "reader@example.com",
                        Content = "Phan tim kiem va binh luan duoi bai viet hoat dong tot cho bai demo.",
                        IsApproved = true,
                        CreatedAt = DateTime.Now.AddMinutes(-30)
                    });

                await context.SaveChangesAsync();
            }
        }

        private static async Task EnsureUserSchemaAsync(AppDbContext context)
        {
            var commands = new[]
            {
                "IF COL_LENGTH('tblUsers', 'PhoneNumber') IS NULL ALTER TABLE tblUsers ADD PhoneNumber NVARCHAR(20) NULL;",
                "IF COL_LENGTH('tblUsers', 'DateOfBirth') IS NULL ALTER TABLE tblUsers ADD DateOfBirth DATETIME2 NULL;",
                "IF COL_LENGTH('tblUsers', 'Address') IS NULL ALTER TABLE tblUsers ADD Address NVARCHAR(250) NULL;",
                "IF COL_LENGTH('tblUsers', 'Bio') IS NULL ALTER TABLE tblUsers ADD Bio NVARCHAR(1000) NULL;",
                "IF COL_LENGTH('tblUsers', 'AvatarUrl') IS NULL ALTER TABLE tblUsers ADD AvatarUrl NVARCHAR(250) NULL;",
                "IF COL_LENGTH('tblUsers', 'LastLoginAt') IS NULL ALTER TABLE tblUsers ADD LastLoginAt DATETIME2 NULL;",
                "IF COL_LENGTH('tblUsers', 'IsDeleted') IS NULL ALTER TABLE tblUsers ADD IsDeleted BIT NOT NULL CONSTRAINT DF_tblUsers_IsDeleted DEFAULT(0);",
                "IF COL_LENGTH('tblUsers', 'DeletedAt') IS NULL ALTER TABLE tblUsers ADD DeletedAt DATETIME2 NULL;"
            };

            foreach (var command in commands)
            {
                await context.Database.ExecuteSqlRawAsync(command);
            }
        }

        private static async Task EnsureDemoUserAsync(
            AppDbContext context,
            PasswordService passwordService,
            string fullName,
            string email,
            string userName,
            string role,
            string phoneNumber,
            string address,
            string bio)
        {
            var existingUser = await context.AppUsers
                .FirstOrDefaultAsync(u => u.UserName == userName || u.Email == email);

            if (existingUser is not null)
            {
                if (existingUser.IsDeleted)
                {
                    existingUser.IsDeleted = false;
                    existingUser.DeletedAt = null;
                }

                existingUser.IsActive = true;
                existingUser.FullName = fullName;
                existingUser.Email = email;
                existingUser.UserName = userName;
                existingUser.Role = role;
                existingUser.PhoneNumber = phoneNumber;
                existingUser.Address = address;
                existingUser.Bio = bio;

                await context.SaveChangesAsync();
                return;
            }

            context.AppUsers.Add(new AppUser
            {
                FullName = fullName,
                Email = email,
                UserName = userName,
                Password = passwordService.HashPassword("123456"),
                Role = role,
                PhoneNumber = phoneNumber,
                Address = address,
                Bio = bio,
                IsActive = true,
                CreatedAt = DateTime.Now
            });

            await context.SaveChangesAsync();
        }
    }
}
