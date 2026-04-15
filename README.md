# MỌI NGƯỜI NHỚ ĐỌC CÁI NÀY TRƯỚC NHÉ
# 📰 NewsAggregator - Đồ án Tổng hợp tin tức
Dự án tổng hợp và quản lý tin tức sử dụng công nghệ ASP.NET Core. Dự án có tích hợp AI để hỗ trợ xử lý nội dung.
Để đảm bảo bảo mật cho API Key và mật khẩu Database, các file cấu hình quan trọng đã được chặn khỏi Git. Vui lòng làm theo các bước sau để thiết lập môi trường chạy:

### 1. Thiết lập cấu hình (Configuration)
Dự án sử dụng file `appsettings.Example.json` làm mẫu. Bạn cần:
1. Copy file `appsettings.Example.json` và tạo một file mới xong đổi tên là `appsettings.Development.json`.
2. Mở file `appsettings.Development.json` vừa đổi tên xong và điền các thông tin vào:
   - **ConnectionStrings**: Thay đổi `User Id` và `Password` theo tài khoản SQL Server trên máy bạn.
   - **AI_Settings**: Điền API Key (Gemini/OpenAI) mà nhóm đã cung cấp.

